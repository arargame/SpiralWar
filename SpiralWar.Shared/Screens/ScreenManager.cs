using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Aktif ekranı stack ile yöneten yönetici.
    /// Paylaşılan kaynakları (Pixel, Font) taşır ve tüm screenlere dağıtır.
    /// </summary>
    public sealed class ScreenManager
    {
        private readonly Stack<IScreen>  _stack = new();
        private readonly Game            _game;
        private          SpriteBatch?    _spriteBatch;

        // Tüm screenlerin paylaştığı kaynaklar
        public Texture2D?   Pixel      { get; private set; }
        public SpriteFont?  GameFont   { get; private set; }
        public SpriteFont?  TitleFont  { get; private set; }

        public ScreenManager(Game game) => _game = game;

        public void LoadContent()
        {
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);

            // Programatik 1×1 beyaz pixel
            Pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            GameFont  = _game.Content.Load<SpriteFont>("Fonts/GameFont");
            TitleFont = _game.Content.Load<SpriteFont>("Fonts/TitleFont");
        }

        public void Push(IScreen screen)
        {
            screen.LoadContent(_game.Content);
            _stack.Push(screen);
        }

        public void Pop()
        {
            if (_stack.Count > 0) _stack.Pop();
        }

        public void Replace(IScreen screen)
        {
            if (_stack.Count > 0) _stack.Pop();
            Push(screen);
        }

        public void Update(GameTime gameTime)
        {
            if (_stack.Count > 0) _stack.Peek().Update(gameTime);
        }

        public void Draw(GameTime gameTime)
        {
            if (_stack.Count == 0 || _spriteBatch == null) return;

            _game.GraphicsDevice.Clear(new Color(4, 6, 18));
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _stack.Peek().Draw(_spriteBatch, gameTime);
            _spriteBatch.End();
        }
    }
}
