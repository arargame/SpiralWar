using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Levels;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// One ring of the snake — the thing the player actually shoots.
    ///
    /// Everything about this class exists to answer "did my shot land?" without the player
    /// having to think about it:
    ///   * the hit number drops by exactly the damage dealt,
    ///   * the ring flashes white for 120 ms,
    ///   * it is knocked backwards along its own axis and springs back,
    ///   * armored rings answer with a different flash colour AND a different sound.
    /// A ring that is carrying a supply wears the glyph it will drop, so the player can decide
    /// which part of the snake is worth the ammunition.
    /// </summary>
    public sealed class SnakeSegment : Enemy
    {
        public Color NeonColor { get; private set; }
        public bool IsHead { get; private set; }
        public bool IsArmored { get; private set; }

        /// <summary>Supply released on death, or null when this ring is empty.</summary>
        public SupplyType? CarriedSupply { get; private set; }

        /// <summary>Velocity estimated from the last movement, used to eject drops.</summary>
        public Vector2 Velocity { get; private set; }

        /// <summary>0..1 white-out driving the hit flash.</summary>
        public float HitFlash { get; private set; }

        private float _glowPulse;
        private float _badgePulse;
        private float _knock;              // px pushed back along the ring's own axis
        private float _timeSinceHit;
        private float _regenCarry;         // fractional hit points waiting to become whole

        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly bool _regenEnabled;
        private readonly float _regenDelay;
        private readonly float _regenPerSecond;

        private static readonly Color[] Palette =
        {
            new Color(0,   255, 190),
            new Color(255,  20, 200),
            new Color(170, 255,  30),
            new Color(255, 140,  20),
            new Color(140,  40, 255),
            new Color(0,   190, 255),
            new Color(255,  70,  90)
        };

        private static readonly Color ArmorColor = new Color(190, 210, 230);
        private static readonly Color HeadColor = new Color(255, 45, 45);

        /// <summary>Armored rings carry this multiple of a normal ring's hit points.</summary>
        public const int ArmorHealthMultiplier = 2;

        public SnakeSegment(bool isHead, LevelDefinition level, SpriteFont font,
                            Texture2D pixel, Random rng)
            : base(isHead ? int.MaxValue : 1, 0f)
        {
            IsHead = isHead;
            _font = font;
            _pixel = pixel;

            _regenEnabled = !isHead && level.RegenerationEnabled;
            _regenDelay = level.RegenerationDelay;
            _regenPerSecond = level.RegenerationPerSecond;

            if (isHead)
            {
                NeonColor = HeadColor;
                return;
            }

            IsArmored = rng.NextDouble() < level.ArmoredChance;
            NeonColor = IsArmored
                ? ArmorColor
                : Palette[rng.Next(Palette.Length)];

            // Armor is expressed as DOUBLE HIT POINTS, not as a damage reduction.
            // A bullet always deals exactly 1 damage, so halving incoming damage would round
            // to "1" and do nothing at all — a rule the player could never see working.
            // Doubling the number printed on the ring is honest and immediately legible.
            int health = RollHealth(level, rng);
            if (IsArmored) health *= ArmorHealthMultiplier;
            ResetHealth(health);

            CarriedSupply = RollSupply(rng);
        }

        /// <summary>
        /// Independent per-type rolls, in a fixed order. A ring can carry at most one supply,
        /// so the first roll that succeeds wins — see docs/supply-system.md for why the
        /// effective rates are 20% / 16% / 12.8% rather than a flat 20% each.
        /// </summary>
        private static SupplyType? RollSupply(Random rng)
        {
            if (rng.NextDouble() < GameConstants.SupplyChanceTriple) return SupplyType.TripleFire;
            if (rng.NextDouble() < GameConstants.SupplyChanceDiffused) return SupplyType.DiffusedFire;
            if (rng.NextDouble() < GameConstants.SupplyChanceHealth) return SupplyType.HealthPack;
            return null;
        }

        private static int RollHealth(LevelDefinition level, Random rng)
        {
            int min = Math.Max(1, level.MinSegmentHealth);
            int max = Math.Max(min, level.MaxSegmentHealth);
            return rng.Next(min, max + 1);
        }

        /// <summary>Rotation is driven by the Snake, which owns the path.</summary>
        public void UpdateRotation(float angle) => SetRotation(angle);

        /// <summary>Position is driven by the Snake; this also refreshes the velocity estimate.</summary>
        public void MoveTo(Vector2 position, float deltaTime)
        {
            if (deltaTime > 0.0001f)
                Velocity = (position - Position) / deltaTime;
            SetPosition(position);
        }

        public override int TakeDamage(int amount)
        {
            int applied = base.TakeDamage(amount);
            if (applied > 0)
            {
                HitFlash = 1f;
                _knock = GameConstants.HitKnockAmount;
                _timeSinceHit = 0f;
                _regenCarry = 0f;
            }
            return applied;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;

            _glowPulse = (float)Math.Sin(totalSeconds * 4.0) * 0.5f + 0.5f;
            _badgePulse = (float)Math.Sin(totalSeconds * 6.5 + Position.X * 0.01) * 0.5f + 0.5f;

            if (HitFlash > 0f)
            {
                HitFlash -= dt / GameConstants.HitFlashDuration;
                if (HitFlash < 0f) HitFlash = 0f;
            }

            // Knock springs back quickly — it should read as a jolt, not as displacement.
            if (_knock > 0f)
            {
                _knock -= dt * 42f;
                if (_knock < 0f) _knock = 0f;
            }

            _timeSinceHit += dt;
            TickRegeneration(dt);
        }

        private void TickRegeneration(float dt)
        {
            if (!_regenEnabled || !IsAlive) return;
            if (CurrentHealth >= MaxHealth) return;
            if (_timeSinceHit < _regenDelay) return;

            _regenCarry += _regenPerSecond * dt;
            if (_regenCarry < 1f) return;

            int whole = (int)_regenCarry;
            _regenCarry -= whole;
            Heal(whole);
        }

        /// <summary>Visual position including the hit knock-back.</summary>
        public Vector2 DrawPosition
        {
            get
            {
                if (_knock <= 0f) return Position;
                return Position - new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation)) * _knock;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsAlive) return;

            Vector2 drawPos = DrawPosition;

            if (CarriedSupply.HasValue)
            {
                NeonRenderer.DrawSupplyMarker(spriteBatch, _pixel, drawPos,
                    GameConstants.SegmentWidth, GameConstants.SegmentHeight, Rotation,
                    SupplyTypeInfo.ColorOf(CarriedSupply.Value), _badgePulse);
            }

            NeonRenderer.DrawNeonSegment(spriteBatch, _pixel, drawPos,
                GameConstants.SegmentWidth, GameConstants.SegmentHeight, Rotation,
                NeonColor,
                glowIntensity: 0.75f + _glowPulse * 0.45f,
                flash: HitFlash,
                armored: IsArmored);

            if (!IsHead)
                DrawLabels(spriteBatch, drawPos);
        }

        private void DrawLabels(SpriteBatch sb, Vector2 drawPos)
        {
            if (_font == null) return;

            float cos = MathF.Cos(Rotation);
            float sin = MathF.Sin(Rotation);

            Vector2 Local(float x, float y) =>
                new Vector2(drawPos.X + x * cos - y * sin,
                            drawPos.Y + x * sin + y * cos);

            // Hit points, slightly behind centre so the supply badge has room in front.
            string hp = CurrentHealth.ToString();
            Vector2 hpSize = _font.MeasureString(hp);
            Vector2 hpAnchor = CarriedSupply.HasValue ? Local(-7f, 0f) : drawPos;
            Vector2 hpPos = hpAnchor - hpSize * 0.5f;

            Color hpColor = HitFlash > 0.02f
                ? Color.White
                : Color.Lerp(NeonColor, Color.White, 0.7f);

            sb.DrawString(_font, hp, hpPos + new Vector2(-1f, 0f), hpColor * 0.3f);
            sb.DrawString(_font, hp, hpPos + new Vector2(1f, 0f), hpColor * 0.3f);
            sb.DrawString(_font, hp, hpPos, hpColor);

            if (!CarriedSupply.HasValue) return;

            // Supply glyph in front of the number.
            SupplyType type = CarriedSupply.Value;
            string glyph = SupplyTypeInfo.GlyphOf(type);
            Color glyphColor = Color.Lerp(SupplyTypeInfo.ColorOf(type), Color.White,
                0.35f + _badgePulse * 0.35f);

            Vector2 glyphSize = _font.MeasureString(glyph);
            Vector2 glyphPos = Local(13f, 0f) - glyphSize * 0.5f;

            sb.DrawString(_font, glyph, glyphPos + new Vector2(-1f, 0f), glyphColor * 0.4f);
            sb.DrawString(_font, glyph, glyphPos, glyphColor);
        }
    }
}
