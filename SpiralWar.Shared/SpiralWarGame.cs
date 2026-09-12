using System;
using Microsoft.Xna.Framework;
using SpiralWar.Audio;
using SpiralWar.Core.Config;
using SpiralWar.Levels;
using SpiralWar.Screens;

namespace SpiralWar
{
    /// <summary>
    /// Composition root. This is the only place where concrete implementations are chosen;
    /// everything below it receives its dependencies through interfaces, which is what lets
    /// the same shared project drive the desktop build and a future mobile one.
    /// </summary>
    public class SpiralWarGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        private IAudioService? _audio;
        private ILevelProvider? _levels;
        private ScreenManager? _screens;

        private bool _appliedFullScreen;
        private bool _resizing;

        public SpiralWarGame()
        {
            // Settings must exist before the graphics device is configured, because the window
            // size and full-screen flag come from them.
            GameConfig.Initialize();
            GameSettings settings = GameConfig.Settings;

            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = settings.WindowWidth,
                PreferredBackBufferHeight = settings.WindowHeight,
                IsFullScreen = settings.FullScreen,
                SynchronizeWithVerticalRetrace = settings.VSync
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            IsFixedTimeStep = true;

            Window.Title = "SpiralWar";
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;
        }

        protected override void Initialize()
        {
            _audio = new AudioManager();
            _levels = new LevelDesigner();
            _screens = new ScreenManager(this, _audio, _levels);
            _screens.StackEmptied += OnStackEmptied;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Synthesizing every sound takes a few milliseconds and happens exactly once.
            _audio!.Initialize();

            _screens!.LoadContent();
            _screens.SetRoot(new MenuScreen(_screens));
        }

        protected override void Update(GameTime gameTime)
        {
            ApplyFullScreenIfChanged();

            _screens?.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _screens?.Draw(gameTime);
            base.Draw(gameTime);
        }

        /// <summary>
        /// The settings screen only flips a flag; the actual device change is applied here so
        /// it never happens in the middle of a draw.
        /// </summary>
        private void ApplyFullScreenIfChanged()
        {
            bool wanted = GameConfig.Settings.FullScreen;
            if (wanted == _appliedFullScreen && _graphics.IsFullScreen == wanted) return;

            _appliedFullScreen = wanted;
            _graphics.IsFullScreen = wanted;

            try
            {
                _graphics.ApplyChanges();
                _screens?.OnResize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Video] Full screen toggle failed: {ex.Message}");
            }
        }

        private void OnClientSizeChanged(object? sender, EventArgs e)
        {
            // ApplyChanges can itself raise ClientSizeChanged; without this guard the two
            // bounce off each other and the window resizes forever.
            if (_resizing) return;

            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;
            if (width <= 0 || height <= 0) return;

            _resizing = true;
            try
            {
                _graphics.PreferredBackBufferWidth = width;
                _graphics.PreferredBackBufferHeight = height;
                _graphics.ApplyChanges();

                if (!GameConfig.Settings.FullScreen)
                {
                    GameConfig.Settings.WindowWidth = width;
                    GameConfig.Settings.WindowHeight = height;
                }

                _screens?.OnResize();
            }
            finally
            {
                _resizing = false;
            }
        }

        private void OnStackEmptied(object? sender, EventArgs e)
        {
            GameConfig.SaveAll();
            Exit();
        }

        protected override void UnloadContent()
        {
            GameConfig.SaveAll();
            base.UnloadContent();
        }
    }
}
