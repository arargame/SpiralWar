using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpiralWar.Rendering;
using SpiralWar.Core;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Ana menü ekranı — NEW GAME ve QUIT butonları.
    /// Neon cyberpunk stili, pulsing başlık.
    /// </summary>
    public sealed class MenuScreen : IScreen
    {
        private readonly ScreenManager _manager;
        private SpriteFont? _titleFont;
        private SpriteFont? _menuFont;
        private Texture2D?  _pixel;

        private int   _selectedIndex;    // 0: En üst buton
        private int   _maxIndex;
        private int   _savedLevel;

        private float _pulse;
        private float _titlePulse;

        private KeyboardState _prevKeys;
        private MouseState    _prevMouse;

        // Buton alanları (draw sırasında hesaplanır)
        private Rectangle _btnContinue;
        private Rectangle _btnNewGame;
        private Rectangle _btnQuit;

        private static readonly Color NeonCyan = new Color(0, 220, 255);
        private static readonly Color NeonPink = new Color(255, 0,  160);

        public MenuScreen(ScreenManager manager) => _manager = manager;

        public void LoadContent(ContentManager content)
        {
            _titleFont = _manager.TitleFont;
            _menuFont  = _manager.GameFont;
            _pixel     = _manager.Pixel;
            
            _savedLevel = SaveManager.LoadLevel();
            _maxIndex   = _savedLevel > 1 ? 2 : 1;
            _selectedIndex = 0;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _pulse      = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3.0) * 0.5f + 0.5f;
            _titlePulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 1.5) * 0.5f + 0.5f;

            var keyboard = Keyboard.GetState();
            var mouse    = Mouse.GetState();

            // Klavye navigasyon
            if (WasJustPressed(keyboard, _prevKeys, Keys.Up)   ||
                WasJustPressed(keyboard, _prevKeys, Keys.W))
                _selectedIndex = (_selectedIndex - 1 + (_maxIndex + 1)) % (_maxIndex + 1);

            if (WasJustPressed(keyboard, _prevKeys, Keys.Down)  ||
                WasJustPressed(keyboard, _prevKeys, Keys.S))
                _selectedIndex = (_selectedIndex + 1) % (_maxIndex + 1);

            // Mouse hover
            if (_savedLevel > 1)
            {
                if (_btnContinue.Contains(mouse.Position)) _selectedIndex = 0;
                if (_btnNewGame.Contains(mouse.Position))  _selectedIndex = 1;
                if (_btnQuit.Contains(mouse.Position))     _selectedIndex = 2;
            }
            else
            {
                if (_btnNewGame.Contains(mouse.Position)) _selectedIndex = 0;
                if (_btnQuit.Contains(mouse.Position))    _selectedIndex = 1;
            }

            // Onay
            bool confirm =
                WasJustPressed(keyboard, _prevKeys, Keys.Enter) ||
                WasJustPressed(keyboard, _prevKeys, Keys.Space) ||
                (mouse.LeftButton == ButtonState.Pressed &&
                 _prevMouse.LeftButton == ButtonState.Released &&
                 (_btnNewGame.Contains(mouse.Position) || _btnQuit.Contains(mouse.Position) || (_savedLevel > 1 && _btnContinue.Contains(mouse.Position))));

            if (confirm)
            {
                if (_savedLevel > 1)
                {
                    if (_selectedIndex == 0) // Continue
                        _manager.Replace(new GameScreen(_manager, _savedLevel));
                    else if (_selectedIndex == 1) // New Game
                    {
                        SaveManager.ClearSave();
                        _manager.Replace(new GameScreen(_manager, 1));
                    }
                    else // Quit
                        Environment.Exit(0);
                }
                else
                {
                    if (_selectedIndex == 0) // New Game
                    {
                        SaveManager.ClearSave();
                        _manager.Replace(new GameScreen(_manager, 1));
                    }
                    else // Quit
                        Environment.Exit(0);
                }
            }

            _prevKeys  = keyboard;
            _prevMouse = mouse;
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_pixel == null) return;

            int cx = GameConstants.ScreenWidth  / 2;
            int cy = GameConstants.ScreenHeight / 2;

            // Arka plan grid
            NeonRenderer.DrawGrid(spriteBatch, _pixel,
                GameConstants.ScreenWidth, GameConstants.ScreenHeight, 60);

            // Başlık
            DrawTitle(spriteBatch, cx, cy - 120);

            // Butonlar
            int startY = cy + 20;
            if (_savedLevel > 1)
            {
                DrawButton(spriteBatch, $"CONTINUE (LVL {_savedLevel})", cx, startY, _selectedIndex == 0, out _btnContinue);
                DrawButton(spriteBatch, "NEW GAME", cx, startY + 70, _selectedIndex == 1, out _btnNewGame);
                DrawButton(spriteBatch, "QUIT", cx, startY + 140, _selectedIndex == 2, out _btnQuit);
            }
            else
            {
                DrawButton(spriteBatch, "NEW GAME", cx, startY, _selectedIndex == 0, out _btnNewGame);
                DrawButton(spriteBatch, "QUIT", cx, startY + 70, _selectedIndex == 1, out _btnQuit);
            }

            // Alt bilgi
            DrawSubtitle(spriteBatch, cx, cy + 180);
        }

        private void DrawTitle(SpriteBatch sb, int cx, int y)
        {
            if (_titleFont == null || _pixel == null) return;

            string title  = "SPIRAL WAR";
            var    size   = _titleFont.MeasureString(title);
            var    pos    = new Vector2(cx - size.X / 2f, y);
            float  pScale = 1f + _titlePulse * 0.04f;

            // Glow layers
            for (int i = 4; i >= 1; i--)
            {
                float a = 0.10f / i;
                var   gPos = pos + new Vector2(-i, -i);
                sb.DrawString(_titleFont, title, gPos, NeonCyan * a,
                    0f, Vector2.Zero, pScale, SpriteEffects.None, 0f);
            }

            sb.DrawString(_titleFont, title, pos, NeonCyan,
                0f, Vector2.Zero, pScale, SpriteEffects.None, 0f);

            // İnce çizgi altında
            int lineY = (int)(y + size.Y * pScale + 6);
            PrimitiveDrawer.DrawLine(sb, _pixel,
                new Vector2(cx - 160, lineY), new Vector2(cx + 160, lineY),
                NeonCyan * 0.6f, 1);
        }

        private void DrawButton(SpriteBatch sb, string text, int cx, int y,
            bool selected, out Rectangle bounds)
        {
            if (_menuFont == null || _pixel == null) { bounds = Rectangle.Empty; return; }

            var   size    = _menuFont.MeasureString(text);
            float padX    = 40f;
            float padY    = 14f;
            int   bw      = (int)(size.X + padX * 2);
            int   bh      = (int)(size.Y + padY * 2);
            int   bx      = cx - bw / 2;
            int   by      = y  - bh / 2;
            bounds = new Rectangle(bx, by, bw, bh);

            Color accent = selected ? NeonPink : new Color(40, 55, 80);
            Color textC  = selected ? Color.White : new Color(120, 150, 180);

            // Arka plan
            PrimitiveDrawer.DrawFilledRect(sb, _pixel, bounds,
                selected ? new Color(30, 5, 25) : new Color(10, 15, 28));

            // Glow kenarlık (seçiliyse)
            if (selected)
            {
                for (int i = 3; i >= 1; i--)
                {
                    var expanded = new Rectangle(bx - i * 2, by - i * 2,
                                                 bw + i * 4, bh + i * 4);
                    PrimitiveDrawer.DrawHollowRect(sb, _pixel, expanded,
                        NeonPink * (0.12f / i), 1);
                }
            }

            PrimitiveDrawer.DrawHollowRect(sb, _pixel, bounds, accent, 1);

            // Yazı
            var textPos = new Vector2(cx - size.X / 2f, y - size.Y / 2f);
            if (selected)
            {
                sb.DrawString(_menuFont, text, textPos + Vector2.One, NeonPink * 0.6f);
            }
            sb.DrawString(_menuFont, text, textPos, textC);
        }

        private void DrawSubtitle(SpriteBatch sb, int cx, int y)
        {
            if (_menuFont == null) return;
            string hint = "[ MOUSE AIM + LEFT CLICK TO FIRE ]";
            var size    = _menuFont.MeasureString(hint);
            sb.DrawString(_menuFont, hint,
                new Vector2(cx - size.X / 2f, y),
                new Color(50, 80, 100));
        }

        private static bool WasJustPressed(KeyboardState curr, KeyboardState prev, Keys key)
            => curr.IsKeyDown(key) && prev.IsKeyUp(key);
    }
}
