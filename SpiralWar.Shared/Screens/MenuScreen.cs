using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SpiralWar.Core;
using SpiralWar.Core.Config;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Root screen. It is the only screen that can empty the stack, so "back" from anywhere
    /// else always has somewhere to land.
    /// </summary>
    public sealed class MenuScreen : BaseScreen
    {
        private int _savedLevel = 1;
        private bool _hasProgress;

        private const int IndexContinue = 0;

        public MenuScreen(ScreenManager manager) : base(manager) { }

        public override void LoadContent(ContentManager content) => RebuildButtons();

        public override void Enter()
        {
            base.Enter();
            RebuildButtons();
        }

        private void RebuildButtons()
        {
            _savedLevel = SaveManager.LoadLevel();
            _hasProgress = _savedLevel > 1;

            Buttons.Clear();

            // CONTINUE always occupies row 0 — it is just disabled on a fresh profile. Keeping
            // the row order stable means muscle memory survives between sessions.
            Buttons.Add(new MenuButton("CONTINUE",
                _hasProgress ? $"LEVEL {_savedLevel} / {GameConstants.MaxLevel}" : "NO RUN IN PROGRESS")
            {
                Enabled = _hasProgress
            });

            Buttons.Add(new MenuButton("NEW GAME", "START FROM LEVEL 1"));
            Buttons.Add(new MenuButton("SETTINGS", "AUDIO  -  VISUALS"));
            Buttons.Add(new MenuButton("CREDITS & SUPPORT", "ARAR GAMES"));
            Buttons.Add(new MenuButton("QUIT"));

            SelectedIndex = _hasProgress ? IndexContinue : 1;
        }

        protected override void OnButtonActivated(int index)
        {
            switch (index)
            {
                case 0: // CONTINUE
                    if (_hasProgress)
                        Manager.Replace(new GameScreen(Manager, _savedLevel));
                    break;

                case 1: // NEW GAME
                    SaveManager.ClearSave();
                    Manager.Replace(new GameScreen(Manager, 1));
                    break;

                case 2: // SETTINGS
                    Manager.Push(new SettingsScreen(Manager));
                    break;

                case 3: // CREDITS & SUPPORT
                    Manager.Push(new CreditsScreen(Manager));
                    break;

                default: // QUIT
                    GameConfig.SaveAll();
                    Environment.Exit(0);
                    break;
            }
        }

        public override void Draw(DrawContext ctx, GameTime gameTime)
        {
            Color accent = Manager.Levels.GetLevel(_savedLevel).ThemeColor;

            DrawBackdrop(ctx, accent);
            DrawCoreOrnament(ctx, gameTime, accent);

            float titleY = ctx.Height * 0.22f;
            DrawTitle(ctx, GameConstants.GameTitle, titleY, NeonCyan, 1f);

            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, GameConstants.StudioName,
                new Vector2(ctx.Width * 0.5f, titleY + 44f), DimText, 0.8f);

            float firstRow = ctx.Height * 0.45f;
            float spacing = MathF.Min(66f, (ctx.Height - firstRow - 70f) / Buttons.Count);
            DrawButtons(ctx, firstRow, spacing, NeonPink);

            DrawFooter(ctx, "AIM WITH THE POINTER   -   HOLD TO FIRE   -   [ESC] BACK");
        }

        /// <summary>
        /// A slow phantom spiral behind the menu — the same shape the player is about to
        /// fight, so the main menu already teaches what the game is.
        /// </summary>
        private void DrawCoreOrnament(DrawContext ctx, GameTime gameTime, Color accent)
        {
            ctx.UseWorld();

            float time = (float)gameTime.TotalGameTime.TotalSeconds;
            Vector2 center = new Vector2(ctx.Width * 0.5f, ctx.Height * 0.5f);

            const int points = 150;
            float turns = 3.2f;

            Vector2 previous = center;
            for (int i = 1; i <= points; i++)
            {
                float t = i / (float)points;
                float angle = t * MathF.Tau * turns + time * 0.18f;
                float radius = t * (ctx.Height * 0.62f);

                var p = new Vector2(
                    center.X + MathF.Cos(angle) * radius,
                    center.Y + MathF.Sin(angle) * radius);

                float alpha = 0.05f + t * 0.05f;
                PrimitiveDrawer.DrawLine(ctx.Batch, ctx.Pixel, previous, p, accent * alpha, 1.5f);
                previous = p;
            }

            NeonRenderer.DrawCoreRing(ctx.Batch, ctx.Pixel, center, 0.15f, time, accent);
        }
    }
}
