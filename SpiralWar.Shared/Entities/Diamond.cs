using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Core.Pooling;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// A neon diamond released by a destroyed carrier ring.
    ///
    /// Diamonds share the supply's drift-to-the-core behaviour but are deliberately a different
    /// SHAPE: a slowly tumbling faceted stone rather than a square capsule. At a glance across a
    /// crowded arena the player should be able to tell "that is a weapon" from "that is a stat"
    /// without reading the glyph at all.
    /// </summary>
    public sealed class Diamond : GameObject, IPoolable
    {
        public DiamondType Type { get; private set; }
        public Color Tint { get; private set; }
        public Color CoreTint { get; private set; }

        private float _lifetime;
        private float _spin;
        private float _pulse;
        private float _birthScale;
        private Vector2 _drift;

        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        public Diamond(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
            Deactivate();
        }

        /// <summary>Distance at which the ship vacuums the diamond up.</summary>
        public float CollectRadius => GameConstants.DiamondSize * 1.2f;

        public float LifePercent => _lifetime / GameConstants.DiamondLifetime;

        public void Spawn(Vector2 position, DiamondType type, Vector2 inheritedVelocity)
        {
            SetPosition(position);
            Type = type;
            Tint = DiamondTypeInfo.ColorOf(type);
            CoreTint = DiamondTypeInfo.CoreColorOf(type);
            _lifetime = GameConstants.DiamondLifetime;
            _spin = 0f;
            _pulse = 0f;
            _birthScale = 0f;
            _drift = inheritedVelocity * 0.35f;
            Activate();
        }

        public void Update(GameTime gameTime, Vector2 corePosition)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _lifetime -= dt;
            if (_lifetime <= 0f)
            {
                Deactivate();
                return;
            }

            _spin += GameConstants.DiamondSpinSpeed * dt;
            _pulse = MathF.Sin(_spin * 3.4f) * 0.5f + 0.5f;

            if (_birthScale < 1f)
            {
                _birthScale += dt * 4.5f;
                if (_birthScale > 1f) _birthScale = 1f;
            }

            _drift -= _drift * MathF.Min(1f, 2.6f * dt);

            Vector2 toCore = corePosition - Position;
            float distance = toCore.Length();
            if (distance > 0.01f)
            {
                Vector2 direction = toCore / distance;
                float speed = GameConstants.DiamondFallSpeed *
                              (1f + (1f - MathF.Min(1f, distance / 500f)) * 0.5f);
                SetPosition(Position + (direction * speed + _drift) * dt);
            }
        }

        public override void Update(GameTime gameTime)
            => Update(gameTime, new Vector2(GameConstants.ScreenWidth * 0.5f,
                                            GameConstants.ScreenHeight * 0.5f));

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            // Blink out over the last 2 seconds so its disappearance is never a surprise.
            if (_lifetime < 2f && (int)(_lifetime * 7f) % 2 == 0) return;

            float size = GameConstants.DiamondSize * (0.25f + _birthScale * 0.75f);

            NeonRenderer.DrawNeonDiamond(spriteBatch, _pixel, Position, Tint, CoreTint,
                size, _spin, _pulse);

            DrawGlyph(spriteBatch);
        }

        private void DrawGlyph(SpriteBatch sb)
        {
            if (_font == null || _birthScale < 0.6f) return;

            string glyph = DiamondTypeInfo.GlyphOf(Type);
            Color glyphColor = Color.Lerp(Tint, Color.White, 0.7f);

            Vector2 size = _font.MeasureString(glyph) * 0.8f;
            Vector2 pos = Position - size * 0.5f;

            sb.DrawString(_font, glyph, pos, glyphColor,
                0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        /// <summary>Applies the stack to the ship and retires the pickup. Returns the new level.</summary>
        public int Collect(Player player)
        {
            if (!IsActive || player == null) return 0;

            int level = player.ApplyDiamond(Type);
            Deactivate();
            return level;
        }

        // ── IPoolable ──────────────────────────────────────────────────────
        public void OnSpawned() => Activate();

        public void OnDespawned()
        {
            Deactivate();
            _lifetime = 0f;
            _drift = Vector2.Zero;
        }
    }
}
