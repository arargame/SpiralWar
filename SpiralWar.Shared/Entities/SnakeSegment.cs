using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Levels;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// One ring of the snake - the thing the player actually shoots.
    ///
    /// Everything about this class exists to answer "did my shot land?" without the player
    /// having to think about it: the number drops by exactly the damage dealt, the ring flashes,
    /// it is knocked backwards along its own axis, and armored, dynamite and acid-covered rings
    /// all look and sound different from a plain one.
    ///
    /// A ring is at most ONE special thing. It is a dynamite charge, or it carries a supply, or
    /// it carries a diamond, or it is plain - never two at once. Armor is the single exception
    /// because it is a property of the plating rather than a payload. That restraint is what
    /// keeps a wall of eighty rings readable at speed.
    /// </summary>
    public sealed class SnakeSegment : Enemy
    {
        public Color NeonColor { get; private set; }
        public bool IsHead { get; private set; }
        public bool IsArmored { get; private set; }

        /// <summary>True when this ring is a neon dynamite charge. Carries no payload.</summary>
        public bool IsDynamite { get; private set; }

        /// <summary>Supply released on death, or null.</summary>
        public SupplyType? CarriedSupply { get; private set; }

        /// <summary>Diamond released on death, or null.</summary>
        public DiamondType? CarriedDiamond { get; private set; }

        /// <summary>Drawn and collided size, scaled by the level's snake form.</summary>
        public int Width { get; }
        public int Height { get; }

        /// <summary>Velocity estimated from the last movement, used to eject drops.</summary>
        public Vector2 Velocity { get; private set; }

        /// <summary>0..1 white-out driving the hit flash.</summary>
        public float HitFlash { get; private set; }

        /// <summary>Acid stacks currently eating this ring.</summary>
        public int AcidStacks { get; private set; }

        /// <summary>Raised when acid eats hit points, so the screen can show the number.</summary>
        public event EventHandler<int>? AcidTicked;

        private float _glowPulse;
        private float _badgePulse;
        private float _fuse;
        private float _knock;
        private float _timeSinceHit;
        private float _regenCarry;
        private float _acidTimer;
        private float _acidRemaining;

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
        private static readonly Color DynamiteColor = new Color(255, 120, 0);

        /// <summary>Armored rings carry this multiple of a normal ring's hit points.</summary>
        public const int ArmorHealthMultiplier = 2;

        public SnakeSegment(bool isHead, LevelDefinition level, SpriteFont font,
                            Texture2D pixel, Random rng)
            : base(isHead ? int.MaxValue : 1, 0f)
        {
            IsHead = isHead;
            _font = font;
            _pixel = pixel;

            Width = level.SegmentWidth;
            Height = level.SegmentHeight;

            _regenEnabled = !isHead && level.RegenerationEnabled;
            _regenDelay = level.RegenerationDelay;
            _regenPerSecond = level.RegenerationPerSecond;

            if (isHead)
            {
                NeonColor = HeadColor;
                return;
            }

            RollPayload(level, rng);

            // Armor is expressed as DOUBLE HIT POINTS, not as a damage reduction. A base bullet
            // deals 1, so halving incoming damage would round to "1" and do nothing at all - a
            // rule the player could never see working. Doubling the printed number is honest.
            int health = RollHealth(level, rng);
            if (IsArmored) health *= ArmorHealthMultiplier;
            ResetHealth(health);
        }

        /// <summary>
        /// Decides what this ring is. Order matters and is the drop table: dynamite first
        /// (it is exclusive and the most dramatic), then a diamond, then a supply.
        /// </summary>
        private void RollPayload(LevelDefinition level, Random rng)
        {
            if (rng.NextDouble() < level.DynamiteChance)
            {
                IsDynamite = true;
                NeonColor = DynamiteColor;
                return;
            }

            if (rng.NextDouble() < level.DiamondCarryChance)
                CarriedDiamond = DiamondTypeInfo.Roll(rng);
            else if (rng.NextDouble() < level.SupplyCarryChance)
                CarriedSupply = SupplyTypeInfo.Roll(rng);

            IsArmored = rng.NextDouble() < level.ArmoredChance;
            NeonColor = IsArmored
                ? ArmorColor
                : Palette[rng.Next(Palette.Length)];
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

        /// <summary>
        /// Adds acid stacks and refreshes their clock. Acid is the green diamond's whole point:
        /// it keeps eating a ring after the player has moved their aim somewhere else, which is
        /// what makes it worth having against a long snake even though its raw numbers are low.
        /// </summary>
        public void ApplyAcid(int stacks)
        {
            if (stacks <= 0 || !IsAlive) return;

            AcidStacks = Math.Min(GameConstants.AcidMaxStacks, AcidStacks + stacks);
            _acidRemaining = GameConstants.AcidDuration;
            if (_acidTimer <= 0f) _acidTimer = GameConstants.AcidTickInterval;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;

            _glowPulse = (float)Math.Sin(totalSeconds * 4.0) * 0.5f + 0.5f;
            _badgePulse = (float)Math.Sin(totalSeconds * 6.5 + Position.X * 0.01) * 0.5f + 0.5f;

            if (IsDynamite)
                _fuse += dt * GameConstants.DynamiteFuseSpeed;

            if (HitFlash > 0f)
            {
                HitFlash -= dt / GameConstants.HitFlashDuration;
                if (HitFlash < 0f) HitFlash = 0f;
            }

            // Knock springs back quickly - it should read as a jolt, not as displacement.
            if (_knock > 0f)
            {
                _knock -= dt * 42f;
                if (_knock < 0f) _knock = 0f;
            }

            _timeSinceHit += dt;

            TickAcid(dt);
            TickRegeneration(dt);
        }

        private void TickAcid(float dt)
        {
            if (AcidStacks <= 0 || !IsAlive) return;

            _acidRemaining -= dt;
            if (_acidRemaining <= 0f)
            {
                AcidStacks = 0;
                _acidTimer = 0f;
                return;
            }

            _acidTimer -= dt;
            if (_acidTimer > 0f) return;

            _acidTimer = GameConstants.AcidTickInterval;

            // Two stacks are worth one point per tick, so a level 3 green diamond burns for
            // three a tick rather than six - strong, but not a replacement for aiming.
            int damage = Math.Max(GameConstants.AcidDamagePerTick,
                                  AcidStacks / 2 * GameConstants.AcidDamagePerTick);

            int applied = base.TakeDamage(damage);
            if (applied > 0) AcidTicked?.Invoke(this, applied);
        }

        private void TickRegeneration(float dt)
        {
            if (!_regenEnabled || !IsAlive) return;
            if (CurrentHealth >= MaxHealth) return;
            if (_timeSinceHit < _regenDelay) return;
            if (AcidStacks > 0) return; // acid keeps a ring from knitting back together

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

            if (IsDynamite)
            {
                NeonRenderer.DrawDynamiteRing(spriteBatch, _pixel, drawPos,
                    Width, Height, Rotation, _fuse, _glowPulse, HitFlash);
                DrawHealthLabel(spriteBatch, drawPos, Color.White);
                return;
            }

            Color? markerColor = CarriedDiamond.HasValue
                ? DiamondTypeInfo.ColorOf(CarriedDiamond.Value)
                : CarriedSupply.HasValue
                    ? SupplyTypeInfo.ColorOf(CarriedSupply.Value)
                    : (Color?)null;

            if (markerColor.HasValue)
            {
                NeonRenderer.DrawSupplyMarker(spriteBatch, _pixel, drawPos,
                    Width, Height, Rotation, markerColor.Value, _badgePulse);
            }

            NeonRenderer.DrawNeonSegment(spriteBatch, _pixel, drawPos,
                Width, Height, Rotation,
                NeonColor,
                glowIntensity: 0.75f + _glowPulse * 0.45f,
                flash: HitFlash,
                armored: IsArmored);

            if (AcidStacks > 0)
                NeonRenderer.DrawAcidOverlay(spriteBatch, _pixel, drawPos,
                    Width, Height, Rotation, AcidStacks, _badgePulse);

            if (!IsHead)
                DrawLabels(spriteBatch, drawPos);
        }

        private void DrawLabels(SpriteBatch sb, Vector2 drawPos)
        {
            if (_font == null) return;

            bool hasBadge = CarriedSupply.HasValue || CarriedDiamond.HasValue;

            float cos = MathF.Cos(Rotation);
            float sin = MathF.Sin(Rotation);

            Vector2 Local(float x, float y) =>
                new Vector2(drawPos.X + x * cos - y * sin,
                            drawPos.Y + x * sin + y * cos);

            // Hit points, slightly behind centre so the badge has room in front.
            float offset = Width * 0.15f;
            Vector2 hpAnchor = hasBadge ? Local(-offset, 0f) : drawPos;

            Color hpColor = HitFlash > 0.02f
                ? Color.White
                : AcidStacks > 0
                    ? Color.Lerp(NeonColor, new Color(150, 255, 90), 0.6f)
                    : Color.Lerp(NeonColor, Color.White, 0.7f);

            DrawCenteredGlyph(sb, CurrentHealth.ToString(), hpAnchor, hpColor, 1f);

            if (!hasBadge) return;

            string glyph;
            Color glyphColor;

            if (CarriedDiamond.HasValue)
            {
                DiamondType type = CarriedDiamond.Value;
                glyph = DiamondTypeInfo.GlyphOf(type);
                glyphColor = Color.Lerp(DiamondTypeInfo.ColorOf(type), Color.White,
                    0.35f + _badgePulse * 0.35f);
            }
            else
            {
                SupplyType type = CarriedSupply!.Value;
                glyph = SupplyTypeInfo.GlyphOf(type);
                glyphColor = Color.Lerp(SupplyTypeInfo.ColorOf(type), Color.White,
                    0.35f + _badgePulse * 0.35f);
            }

            DrawCenteredGlyph(sb, glyph, Local(Width * 0.27f, 0f), glyphColor, 0.9f);
        }

        private void DrawHealthLabel(SpriteBatch sb, Vector2 drawPos, Color color)
        {
            if (_font == null || IsHead) return;
            DrawCenteredGlyph(sb, CurrentHealth.ToString(), drawPos, color, 1f);
        }

        private void DrawCenteredGlyph(SpriteBatch sb, string text, Vector2 center,
                                       Color color, float scale)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;

            Vector2 size = _font.MeasureString(text) * scale;
            Vector2 pos = center - size * 0.5f;

            sb.DrawString(_font, text, pos + new Vector2(-1f, 0f), color * 0.3f,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sb.DrawString(_font, text, pos + new Vector2(1f, 0f), color * 0.3f,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sb.DrawString(_font, text, pos, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
