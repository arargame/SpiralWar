using Microsoft.Xna.Framework;
using SpiralWar.Screens;

namespace SpiralWar
{
    /// <summary>
    /// Oyunun ana giriş sınıfı. Shared projeden kullanılır.
    /// Platform-spesifik kod buraya gelmez — Desktop/Mobile ayrı başlatır.
    /// </summary>
    public class SpiralWarGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private ScreenManager?                 _screenManager;

        public SpiralWarGame()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth  = GameConstants.ScreenWidth,
                PreferredBackBufferHeight = GameConstants.ScreenHeight,
                IsFullScreen              = false
            };

            Content.RootDirectory = "Content";
            IsMouseVisible         = true;
            Window.Title           = "SpiralWar";
        }

        protected override void Initialize()
        {
            _screenManager = new ScreenManager(this);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _screenManager!.LoadContent();
            _screenManager.Push(new MenuScreen(_screenManager));
        }

        protected override void Update(GameTime gameTime)
        {
            _screenManager?.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _screenManager?.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}
