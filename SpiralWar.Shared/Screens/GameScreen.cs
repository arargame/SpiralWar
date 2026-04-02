using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpiralWar.Entities;
using SpiralWar.Rendering;
using SpiralWar.Systems;
using SpiralWar.Core;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Ana oyun ekranı. Tüm sistem ve entity'leri koordine eder.
    /// </summary>
    public sealed class GameScreen : IScreen
    {
        private readonly ScreenManager _manager;

        // ── Kaynaklar ───────────────────────────────────────────────────────────
        private Texture2D?  _pixel;
        private SpriteFont? _gameFont;

        // ── Oyun nesneleri ──────────────────────────────────────────────────────
        private Player?          _player;
        private Snake?           _snake;

        // ── Sistemler ───────────────────────────────────────────────────────────
        private BulletSystem?    _bulletSystem;
        private SupplySystem?    _supplySystem;
        private CollisionSystem? _collisionSystem;

        // ── Durum ────────────────────────────────────────────────────────────────
        private bool  _gameOver;
        private bool  _playerWon;
        private float _gameOverTimer;    // menüye dönüş gecikmesi
        private const float GameOverDelay = 3f;

        private KeyboardState _prevKeys;
        private readonly int _level;

        public GameScreen(ScreenManager manager, int level = 1)
        {
            _manager = manager;
            _level = level;
        }

        public void LoadContent(ContentManager content)
        {
            _pixel    = _manager.Pixel;
            _gameFont = _manager.GameFont;

            // Sistemler
            _bulletSystem    = new BulletSystem(_pixel!);
            _supplySystem    = new SupplySystem(_pixel!);
            _collisionSystem = new CollisionSystem(_bulletSystem, _supplySystem);

            // Oyuncu
            _player = new Player(_pixel!);
            _player.BulletFired += OnBulletFired;
            _player.Died        += OnPlayerDied;

            // Yılan
            _snake = new Snake(_level, _gameFont!, _pixel!);
            _snake.SegmentKilled += OnSegmentKilled;
        }

        // ── Event Handlers ────────────────────────────────────────────────────
        private void OnBulletFired(object? sender, float rotation)
        {
            if (_player == null || _bulletSystem == null) return;
            var dir = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation));
            _bulletSystem.SpawnBullet(_player.Position, dir, _player.BulletDamage);
        }

        private void OnPlayerDied(object? sender, EventArgs e)
        {
            _gameOver  = true;
            _playerWon = false;
        }

        private void OnSegmentKilled(object? sender, SnakeSegment seg)
        {
            _supplySystem?.TryDropSupply(seg.Position);

            // Tüm gövde segmentleri yok olduysa oyuncu kazandı
            if (_snake != null && !HasAnyAliveSegment())
            {
                _gameOver  = true;
                _playerWon = true;
            }
        }

        private bool HasAnyAliveSegment()
        {
            if (_snake == null) return false;
            foreach (var s in _snake.GetAliveBodySegments())
                return true;
            return false;
        }

        // ── Update ────────────────────────────────────────────────────────────
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keys = Keyboard.GetState();

            // ESC → menüye dön
            if (keys.IsKeyDown(Keys.Escape) && _prevKeys.IsKeyUp(Keys.Escape))
            {
                _manager.Replace(new MenuScreen(_manager));
                return;
            }

            if (_gameOver)
            {
                _gameOverTimer += dt;
                if (_gameOverTimer >= GameOverDelay)
                {
                    if (_playerWon)
                    {
                        SaveManager.SaveLevel(_level + 1);
                        _manager.Replace(new GameScreen(_manager, _level + 1));
                    }
                    else
                    {
                        _manager.Replace(new MenuScreen(_manager));
                    }
                }
                return;
            }

            _player?.Update(gameTime);
            _snake?.Update(gameTime);
            _bulletSystem?.Update(gameTime);
            _supplySystem?.Update(gameTime, _player);

            if (_player != null && _snake != null)
                _collisionSystem?.Update(_snake, _player);

            // Yılan kafası merkeze girdi → oyun bitti
            if (_snake != null && _snake.HeadReachedCenter && !_gameOver)
            {
                _gameOver  = true;
                _playerWon = false;
            }

            _prevKeys = keys;
        }

        // ── Draw ─────────────────────────────────────────────────────────────
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_pixel == null) return;

            // Arka plan
            NeonRenderer.DrawGrid(spriteBatch, _pixel,
                GameConstants.ScreenWidth, GameConstants.ScreenHeight, 55);

            // Oyun nesneleri
            _snake?.Draw(spriteBatch, gameTime);
            _supplySystem?.Draw(spriteBatch, gameTime);
            _bulletSystem?.Draw(spriteBatch, gameTime);
            _player?.Draw(spriteBatch, gameTime);

            // HUD
            DrawHud(spriteBatch, gameTime);

            // Game over overlay
            if (_gameOver)
                DrawGameOver(spriteBatch);
        }

        private void DrawHud(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_pixel == null || _gameFont == null || _player == null) return;

            // ESC ipucu
            spriteBatch.DrawString(_gameFont, "[ESC] Menu",
                new Vector2(8, 8), new Color(50, 70, 90));

            // Boost göstergeleri
            int y = 8;
            if (_player.IsFireRateBoosted)
            {
                string txt = $"FIRE BOOST  {_player.FireRateTimeLeft:F1}s";
                spriteBatch.DrawString(_gameFont, txt,
                    new Vector2(GameConstants.ScreenWidth - 220, y),
                    new Color(0, 255, 160));
                y += 22;
            }
            if (_player.IsDamageBoosted)
            {
                string txt = $"DMG  x{GameConstants.DefaultBulletDamage + GameConstants.DamageBoostAmount}  {_player.DamageBoostTimeLeft:F1}s";
                spriteBatch.DrawString(_gameFont, txt,
                    new Vector2(GameConstants.ScreenWidth - 220, y),
                    new Color(255, 130, 0));
            }

            // Level ve Kalan segment sayısı
            int alive = 0;
            if (_snake != null)
                foreach (var _ in _snake.GetAliveBodySegments()) alive++;

            string lvInfo = $"LEVEL: {_level}";
            spriteBatch.DrawString(_gameFont, lvInfo,
                new Vector2(GameConstants.ScreenWidth / 2f - _gameFont.MeasureString(lvInfo).X / 2f,
                            GameConstants.ScreenHeight - 56),
                new Color(255, 200, 0));

            string segInfo = $"SEGMENTS: {alive} / {(_snake != null ? _snake.TotalSegmentCount : 0)}";
            var segSize = _gameFont.MeasureString(segInfo);
            spriteBatch.DrawString(_gameFont, segInfo,
                new Vector2(GameConstants.ScreenWidth / 2f - segSize.X / 2f,
                            GameConstants.ScreenHeight - 28),
                new Color(80, 120, 160));
        }

        private void DrawGameOver(SpriteBatch spriteBatch)
        {
            if (_pixel == null || _gameFont == null) return;

            var overlay = new Rectangle(0, 0,
                GameConstants.ScreenWidth, GameConstants.ScreenHeight);
            PrimitiveDrawer.DrawFilledRect(spriteBatch, _pixel, overlay,
                new Color(0, 0, 0) * 0.65f);

            string msg   = _playerWon ? $"LEVEL {_level} CLEARED!" : "GAME OVER";
            Color  msgC  = _playerWon ? new Color(0, 255, 160) : new Color(255, 60, 60);
            var    size  = _gameFont.MeasureString(msg);
            var    pos   = new Vector2(
                GameConstants.ScreenWidth  / 2f - size.X / 2f,
                GameConstants.ScreenHeight / 2f - size.Y / 2f - 20);

            // Glow
            for (int i = 4; i >= 1; i--)
                spriteBatch.DrawString(_gameFont, msg,
                    pos + new Vector2(-i, -i), msgC * (0.12f / i));

            spriteBatch.DrawString(_gameFont, msg, pos, msgC);

            string sub  = _playerWon ? "Next level starting..." : "Returning to menu...";
            var    subSz = _gameFont.MeasureString(sub);
            spriteBatch.DrawString(_gameFont, sub,
                new Vector2(GameConstants.ScreenWidth  / 2f - subSz.X / 2f,
                            GameConstants.ScreenHeight / 2f + 20),
                new Color(80, 100, 120));
        }
    }
}
