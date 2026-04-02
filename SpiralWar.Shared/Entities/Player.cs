using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Ekran merkezinde sabit duran, mouse ile dönen, ateş eden oyuncu.
    /// </summary>
    public sealed class Player : Entity
    {
        public float Rotation          { get; private set; }
        public float CurrentFireCooldown { get; private set; }
        public int   BulletDamage      { get; private set; }

        private float _cooldownTimer;
        private bool  _fireRateBoosted;
        private bool  _damageBoosted;
        private float _fireRateBoostTimer;
        private float _damageBoostTimer;
        private readonly Texture2D _pixel;

        private static readonly Color PlayerColor = new Color(0, 220, 255);

        // Oyuncunun ateş ettiği yönü ile birlikte tetiklenen event
        public event EventHandler<float>? BulletFired; // float = rotation açısı

        public bool IsFireRateBoosted  => _fireRateBoosted;
        public bool IsDamageBoosted    => _damageBoosted;
        public float FireRateTimeLeft  => _fireRateBoostTimer;
        public float DamageBoostTimeLeft => _damageBoostTimer;

        public Player(Texture2D pixel) : base(1) // snapshot HP (ölüm = yılan merkeze ulaştı)
        {
            _pixel = pixel;
            CurrentFireCooldown = GameConstants.DefaultFireCooldown;
            BulletDamage        = GameConstants.DefaultBulletDamage;
            SetPosition(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Mouse yönüne döndür
            var mouse = Mouse.GetState();
            var dir   = new Vector2(mouse.X, mouse.Y) - Position;
            if (dir.LengthSquared() > 1f)
                Rotation = MathF.Atan2(dir.Y, dir.X);

            // Ateş cooldown
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;

            // Sol tık → ateş
            if (mouse.LeftButton == ButtonState.Pressed && _cooldownTimer <= 0f)
            {
                float effectiveCooldown = _fireRateBoosted
                    ? CurrentFireCooldown * GameConstants.FireRateBoostMultiplier
                    : CurrentFireCooldown;
                _cooldownTimer = effectiveCooldown;
                BulletFired?.Invoke(this, Rotation);
            }

            // Boost süreleri
            UpdateBoosts(dt);
        }

        private void UpdateBoosts(float dt)
        {
            if (_fireRateBoosted)
            {
                _fireRateBoostTimer -= dt;
                if (_fireRateBoostTimer <= 0f) _fireRateBoosted = false;
            }
            if (_damageBoosted)
            {
                _damageBoostTimer -= dt;
                if (_damageBoostTimer <= 0f)
                {
                    _damageBoosted = false;
                    BulletDamage   = GameConstants.DefaultBulletDamage;
                }
            }
        }

        public void ApplyFireRateBoost()
        {
            _fireRateBoosted    = true;
            _fireRateBoostTimer = GameConstants.SupplyBoostDuration;
        }

        public void ApplyDamageBoost()
        {
            _damageBoosted    = true;
            _damageBoostTimer = GameConstants.SupplyBoostDuration;
            BulletDamage      = GameConstants.DefaultBulletDamage + GameConstants.DamageBoostAmount;
        }

        public Vector2 GetFireDirection() =>
            new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            NeonRenderer.DrawNeonPlayer(spriteBatch, _pixel, Position, Rotation, PlayerColor);
        }
    }
}
