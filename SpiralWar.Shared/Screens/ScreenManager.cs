using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Audio;
using SpiralWar.Core;
using SpiralWar.Core.Config;
using SpiralWar.Core.Input;
using SpiralWar.Levels;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Owns the screen stack, the shared resources and the transition between screens.
    ///
    /// NAVIGATION MODEL
    /// ----------------
    ///   Push     puts a screen on top and keeps the one underneath (used for pause menus,
    ///            settings, credits) — that history is what makes "back" work everywhere.
    ///   Pop      returns to the previous screen.
    ///   Replace  swaps the top without growing the stack (menu to gameplay, level to level).
    ///   PopToRoot unwinds to the main menu in one move.
    ///
    /// Every one of those is deferred through <see cref="_pendingChange"/> so the change lands
    /// at the midpoint of a fade, never in the middle of a screen's own Update. That single
    /// rule removes an entire class of bug — a screen mutating the stack while the manager is
    /// iterating it — and is also what makes transitions look deliberate instead of abrupt.
    /// </summary>
    public sealed class ScreenManager
    {
        private enum TransitionPhase { None, Out, In }

        private readonly List<IScreen> _stack = new List<IScreen>();
        private readonly Game _game;

        private SpriteBatch? _spriteBatch;
        private Action? _pendingChange;
        private TransitionPhase _phase = TransitionPhase.None;
        private float _phaseTimer;

        // ── Shared resources every screen can use ──────────────────────────
        public Texture2D? Pixel { get; private set; }
        public SpriteFont? GameFont { get; private set; }
        public SpriteFont? TitleFont { get; private set; }
        public ViewportAdapter? Viewport { get; private set; }
        public DrawContext? Context { get; private set; }
        public InputState? Input { get; private set; }

        public IAudioService Audio { get; }
        public ILevelProvider Levels { get; }
        public Starfield Background { get; private set; }

        public int ScreenCount => _stack.Count;
        public bool IsTransitioning => _phase != TransitionPhase.None;

        /// <summary>Raised when the last screen is popped, so the game can exit cleanly.</summary>
        public event EventHandler? StackEmptied;

        public ScreenManager(Game game, IAudioService audio, ILevelProvider levels)
        {
            _game = game;
            Audio = audio;
            Levels = levels;
            Background = new Starfield(200, GameConstants.ScreenWidth, GameConstants.ScreenHeight);
        }

        // ── Setup ──────────────────────────────────────────────────────────

        public void LoadContent()
        {
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);

            // A single white texel is all the renderer needs; no content pipeline art at all.
            Pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            GameFont = _game.Content.Load<SpriteFont>("Fonts/GameFont");
            TitleFont = _game.Content.Load<SpriteFont>("Fonts/TitleFont");

            Viewport = new ViewportAdapter(_game.GraphicsDevice);
            Input = new InputState(Viewport);

            Context = new DrawContext(_spriteBatch, Pixel, GameFont, TitleFont, Viewport)
            {
                GlowScale = GameConfig.Settings.GlowScale
            };
        }

        /// <summary>Recomputes the letterbox. Call when the window is resized.</summary>
        public void OnResize() => Viewport?.Refresh();

        // ── Stack operations ───────────────────────────────────────────────

        public void Push(IScreen screen) => RequestChange(() => PushImmediate(screen));

        public void Pop() => RequestChange(PopImmediate);

        public void Replace(IScreen screen) => RequestChange(() =>
        {
            PopImmediate(raiseEmptied: false);
            PushImmediate(screen);
        });

        public void PopToRoot() => RequestChange(() =>
        {
            while (_stack.Count > 1)
                PopImmediate(raiseEmptied: false);
        });

        /// <summary>Pushes the very first screen without a fade-out from nothing.</summary>
        public void SetRoot(IScreen screen)
        {
            _stack.Clear();
            PushImmediate(screen);
            _phase = TransitionPhase.In;
            _phaseTimer = 0f;
        }

        private void RequestChange(Action change)
        {
            // Ignore a second request while one is already in flight — double-clicking a
            // button must not queue two level loads.
            if (_pendingChange != null) return;

            _pendingChange = change;
            _phase = TransitionPhase.Out;
            _phaseTimer = 0f;
        }

        private void PushImmediate(IScreen screen)
        {
            if (_stack.Count > 0)
                _stack[_stack.Count - 1].Leave();

            screen.LoadContent(_game.Content);
            _stack.Add(screen);
            screen.Enter();

            Input?.ConsumeEdges();
        }

        private void PopImmediate() => PopImmediate(raiseEmptied: true);

        private void PopImmediate(bool raiseEmptied)
        {
            if (_stack.Count == 0) return;

            IScreen top = _stack[_stack.Count - 1];
            top.Leave();
            _stack.RemoveAt(_stack.Count - 1);

            if (_stack.Count > 0)
            {
                _stack[_stack.Count - 1].Enter();
                Input?.ConsumeEdges();
                return;
            }

            if (raiseEmptied)
                StackEmptied?.Invoke(this, EventArgs.Empty);
        }

        // ── Frame ──────────────────────────────────────────────────────────

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Input?.Update();
            Background.Update(dt);

            AdvanceTransition(dt);

            if (_stack.Count == 0 || Input == null) return;

            IScreen top = _stack[_stack.Count - 1];

            // The active screen keeps updating during a transition so nothing freezes, but it
            // does not receive the back gesture while the stack is mid-change.
            top.Update(gameTime, Input);

            if (!IsTransitioning && Input.BackPressed)
                HandleBack(top);
        }

        private void HandleBack(IScreen top)
        {
            if (top.OnBack()) return;

            Audio.Play(SoundId.MenuBack);

            if (_stack.Count > 1) Pop();
            else StackEmptied?.Invoke(this, EventArgs.Empty);
        }

        private void AdvanceTransition(float dt)
        {
            if (_phase == TransitionPhase.None) return;

            float half = GameConstants.TransitionDuration * 0.5f;
            _phaseTimer += dt;

            if (_phase == TransitionPhase.Out)
            {
                if (_phaseTimer < half) return;

                Action? change = _pendingChange;
                _pendingChange = null;
                change?.Invoke();

                _phase = TransitionPhase.In;
                _phaseTimer = 0f;
                return;
            }

            if (_phaseTimer >= half)
            {
                _phase = TransitionPhase.None;
                _phaseTimer = 0f;
            }
        }

        /// <summary>0 when settled, 1 at the darkest point of a transition.</summary>
        private float TransitionAmount
        {
            get
            {
                float half = GameConstants.TransitionDuration * 0.5f;
                if (half <= 0f || _phase == TransitionPhase.None) return 0f;

                float t = MathHelper.Clamp(_phaseTimer / half, 0f, 1f);
                return _phase == TransitionPhase.Out ? t : 1f - t;
            }
        }

        public void Draw(GameTime gameTime)
        {
            if (_spriteBatch == null || Context == null || Pixel == null) return;

            _game.GraphicsDevice.Clear(new Color(3, 5, 14));

            if (_stack.Count == 0) return;

            float fade = TransitionAmount;

            // Ease the curve so the darkest moment is brief and the settle is soft.
            float eased = fade * fade * (3f - 2f * fade);

            Context.GlowScale = GameConfig.Settings.GlowScale;
            Context.ContentScale = _phase == TransitionPhase.Out
                ? 1f - eased * 0.04f
                : 1f + eased * 0.04f;

            Context.BeginFrame();

            // Draw from the lowest screen that is not hidden by an opaque one above it.
            int firstVisible = _stack.Count - 1;
            while (firstVisible > 0 && _stack[firstVisible].IsPopup)
                firstVisible--;

            for (int i = firstVisible; i < _stack.Count; i++)
            {
                if (i > firstVisible && _stack[i].IsPopup)
                {
                    // Dim whatever is underneath a popup so the popup owns the eye.
                    Context.UseHud();
                    PrimitiveDrawer.DrawFilledRect(Context.Batch, Pixel,
                        new Rectangle(0, 0, Context.Width, Context.Height),
                        new Color(2, 4, 12) * 0.72f);
                }

                _stack[i].Draw(Context, gameTime);
            }

            if (eased > 0.001f)
            {
                Context.UseHud();
                PrimitiveDrawer.DrawFilledRect(Context.Batch, Pixel,
                    new Rectangle(0, 0, Context.Width, Context.Height),
                    Color.Black * eased);
            }

            Context.EndFrame();

            DrawLetterbox();
        }

        /// <summary>
        /// Paints the bars outside the letterboxed canvas. Without this, a resized window
        /// shows whatever the previous frame left in those pixels.
        /// </summary>
        private void DrawLetterbox()
        {
            if (_spriteBatch == null || Pixel == null || Viewport == null) return;

            Rectangle bounds = Viewport.Bounds;
            int bw = _game.GraphicsDevice.PresentationParameters.BackBufferWidth;
            int bh = _game.GraphicsDevice.PresentationParameters.BackBufferHeight;

            if (bounds.Width >= bw && bounds.Height >= bh) return;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque);

            var bar = new Color(0, 0, 0);
            if (bounds.Y > 0)
            {
                _spriteBatch.Draw(Pixel, new Rectangle(0, 0, bw, bounds.Y), bar);
                _spriteBatch.Draw(Pixel, new Rectangle(0, bounds.Bottom, bw, bh - bounds.Bottom), bar);
            }
            if (bounds.X > 0)
            {
                _spriteBatch.Draw(Pixel, new Rectangle(0, 0, bounds.X, bh), bar);
                _spriteBatch.Draw(Pixel, new Rectangle(bounds.Right, 0, bw - bounds.Right, bh), bar);
            }

            _spriteBatch.End();
        }
    }
}
