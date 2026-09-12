using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Core.Pooling;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// A pickup released by a destroyed carrier ring.
    ///
    /// Supplies always drift toward the core, where the ship sits. That is a deliberate
    /// design choice: the player never has to leave the centre to chase a pickup, so
    /// collecting one is a reward for killing the right ring rather than a second skill test.
    /// The player still has to survive long enough for it to arrive.
    /// </summary>
    public sealed class Supply : GameObject, IPoolable
    {
        public SupplyType Type { get; private set; }
        public Color Tint { get; private set; }

        private float _lifetime;
        private float _spin;
        private float _pulse;
        private float _birthScale;
        private Vector2 _drift;

        private readonly Texture2D _pixel;
        private readonly SpriteFont _font;

        public Supply(Texture2D pixel, SpriteFont font)
        {
            _pixel = pixel;
            _font = font;
            Deactivate();
        }

        /// <summary>Distance at which the ship vacuums the supply up.</summary>
        public float CollectRadius => GameConstants.SupplySize * 1.15f;

        /// <summary>Fraction of lifetime remaining, 1 at spawn.</summary>
        public float LifePercent => _lifetime / GameConstants.SupplyLifetime;

        /// <summary>Arms a recycled supply at the position of the ring that dropped it.</summary>
        public void Spawn(Vector2 position, SupplyType type, Vector2 inheritedVelocity)
        {
            SetPosition(position);
            Type = type;
            Tint = SupplyTypeInfo.ColorOf(type);
            _lifetime = GameConstants.SupplyLifetime;
            _spin = 0f;
            _pulse = 0f;
            _birthScale = 0f;

            // A little of the ring's momentum, so the drop feels ejected rather than teleported.
            _drift = inheritedVelocity * 0.35f;
            Activate();
        }

        /// <summary>
        /// Moves the supply toward <paramref name="corePosition"/> and ages it out.
        /// </summary>
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

            _spin += GameConstants.SupplySpinSpeed * dt;
            _pulse = MathF.Sin(_spin * 3.2f) * 0.5f + 0.5f;

            if (_birthScale < 1f)
            {
                _birthScale += dt * 5f;
                if (_birthScale > 1f) _birthScale = 1f;
            }

            // Ejection momentum decays, then the pull toward the core dominates.
            _drift -= _drift * MathF.Min(1f, 2.6f * dt);

            Vector2 toCore = corePosition - Position;
            float distance = toCore.Length();
            if (distance > 0.01f)
            {
                Vector2 direction = toCore / distance;

                // Accelerate slightly as it closes so the last stretch does not feel sluggish.
                float speed = GameConstants.SupplyFallSpeed * (1f + (1f - MathF.Min(1f, distance / 500f)) * 0.5f);
                SetPosition(Position + (direction * speed + _drift) * dt);
            }
        }

        public override void Update(GameTime gameTime)
            => Update(gameTime, new Vector2(GameConstants.ScreenWidth * 0.5f, GameConstants.ScreenHeight * 0.5f));

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            // Blink out over the last 2 seconds so its disappearance is never a surprise.
            if (_lifetime < 2f && (int)(_lifetime * 7f) % 2 == 0) return;

            float size = GameConstants.SupplySize * (0.2f + _birthScale * 0.8f);

            NeonRenderer.DrawNeonSupplyShell(spriteBatch, _pixel, Position, Tint,
                size, _spin, _pulse);

            DrawGlyph(spriteBatch);
        }

        private void DrawGlyph(SpriteBatch sb)
        {
            if (_font == null || _birthScale < 0.5f) return;

            string glyph = SupplyTypeInfo.GlyphOf(Type);
            Color glyphColor = Color.Lerp(Tint, Color.White, 0.65f);

            Vector2 size = _font.MeasureString(glyph);
            Vector2 pos = Position - size * 0.5f;

            // Halo then glyph — the letter has to survive being read at speed.
            sb.DrawString(_font, glyph, pos + new Vector2(-1f, 0f), glyphColor * 0.35f);
            sb.DrawString(_font, glyph, pos + new Vector2(1f, 0f), glyphColor * 0.35f);
            sb.DrawString(_font, glyph, pos, glyphColor);
        }

        /// <summary>Applies the effect to the ship and retires the pickup.</summary>
        public void Collect(Player player)
        {
            if (!IsActive || player == null) return;

            switch (Type)
            {
                case SupplyType.TripleFire:
                    player.ApplyTripleFire();
                    break;
                case SupplyType.DiffusedFire:
                    player.ApplyDiffusedFire();
                    break;
                default:
                    player.Heal(GameConstants.HealthPackRestore);
                    break;
            }

            Deactivate();
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
