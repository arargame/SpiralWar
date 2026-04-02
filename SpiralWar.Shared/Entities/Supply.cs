using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    public enum SupplyType { FireRateBoost, DamageBoost }

    /// <summary>
    /// Ölü segmentten düşen supply. Ekranda belirli süre kalır, oyuncu toplarsa boost uygular.
    /// </summary>
    public sealed class Supply : Entity
    {
        public SupplyType Type      { get; private set; }

        private float   _lifetime;
        private float   _pulse;
        private readonly Texture2D _pixel;

        private static readonly Color FireRateColor = new Color(0, 255, 160);
        private static readonly Color DamageColor   = new Color(255, 80,  0);

        private static readonly Random _rng = new Random();

        public Color SupplyColor => Type == SupplyType.FireRateBoost ? FireRateColor : DamageColor;

        public Supply(Vector2 position, Texture2D pixel) : base(1)
        {
            _pixel   = pixel;
            _lifetime = GameConstants.SupplyLifetime;
            Type = _rng.Next(2) == 0 ? SupplyType.FireRateBoost : SupplyType.DamageBoost;
            SetPosition(position);
        }

        public float CollectRadius => GameConstants.SupplySize * 1.5f;

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _lifetime -= dt;
            if (_lifetime <= 0f) Deactivate();
            _pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 5f) * 0.5f + 0.5f;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Son saniyelerde yanıp söner
            if (_lifetime < 2f && (int)(_lifetime * 6) % 2 == 0) return;

            NeonRenderer.DrawNeonSupply(spriteBatch, _pixel, Position, SupplyColor, _pulse);
        }

        /// <summary>
        /// Oyuncuya uygun boost'u uygular ve supply'ı deaktive eder.
        /// </summary>
        public void Collect(Player player)
        {
            if (!IsActive) return;
            if (Type == SupplyType.FireRateBoost)
                player.ApplyFireRateBoost();
            else
                player.ApplyDamageBoost();
            Deactivate();
        }

        /// <summary>
        /// Supply'ı belirtilen hedefe doğru hareket ettirir.
        /// </summary>
        public void MoveTowards(Vector2 target, float speed, float dt)
        {
            if (!IsActive) return;
            Vector2 dir = target - Position;
            if (dir.LengthSquared() > 0)
            {
                dir.Normalize();
                SetPosition(Position + dir * speed * dt);
            }
        }
    }
}
