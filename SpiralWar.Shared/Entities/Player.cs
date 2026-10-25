using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Input;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>One bullet of a volley, fully resolved before it is spawned.</summary>
    public struct ShotSpec
    {
        public float Angle;
        public int Damage;
        public bool IsCritical;
        public int Pierce;
        public int AcidStacks;
    }

    /// <summary>
    /// The ship. It never moves - it is bolted to the core of the arena and only rotates - so
    /// every decision the player makes is about WHERE to aim and WHICH ring to kill next.
    ///
    /// Its firepower comes from two independent sources that stack:
    ///   SUPPLIES change the SHAPE of a volley   (double, triple, diffused, homing)
    ///   DIAMONDS change the STATS behind it     (attack speed, critical, acid, power)
    /// Both are timed and both expire, which keeps the ship's baseline honest and makes carrier
    /// rings worth prioritising for the whole three minutes of a level.
    ///
    /// Critical hits and power attacks are the one place the game's "a bullet deals 1" rule
    /// bends. It bends openly: a critical is a different bullet colour, a bigger number in its
    /// own colour and its own sound, so the player can still account for everything that comes
    /// off a ring.
    /// </summary>
    public sealed class Player : Entity
    {
        public float Rotation { get; private set; }
        public float BaseFireCooldown { get; private set; }

        /// <summary>Every timed effect the ship is carrying. Read-only to the outside world.</summary>
        public PlayerLoadout Loadout { get; } = new PlayerLoadout();

        private float _cooldownTimer;
        private float _missileTimer;
        private float _contactTimer;
        private float _hurtFlash;
        private float _enginePulse;

        private readonly Texture2D _pixel;
        private readonly Random _rng = new Random();

        private static readonly Color ShipColor = new Color(0, 220, 255);

        /// <summary>Raised when a volley leaves the barrel. The screen turns it into bullets.</summary>
        public event EventHandler? Fired;

        /// <summary>Raised when a homing missile should launch.</summary>
        public event EventHandler? MissileReady;

        public float HurtFlash => _hurtFlash;

        /// <summary>Effective cooldown after the attack speed diamond has had its say.</summary>
        public float CurrentFireCooldown => BaseFireCooldown * Loadout.FireCooldownMultiplier;

        /// <summary>Largest volley this ship can emit. Sizes the caller's buffer.</summary>
        public static int MaxShotsPerVolley => GameConstants.MaxVolleySize;

        public Player(Texture2D pixel) : base(GameConstants.PlayerMaxHealth)
        {
            _pixel = pixel;
            BaseFireCooldown = GameConstants.DefaultFireCooldown;
            SetPosition(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);
        }

        /// <summary>Restores the ship for a fresh level without reallocating it.</summary>
        public void ResetForLevel(bool restoreHealth)
        {
            if (restoreHealth) ResetHealth(GameConstants.PlayerMaxHealth);
            _cooldownTimer = 0f;
            _missileTimer = 0f;
            _contactTimer = 0f;
            _hurtFlash = 0f;
            Loadout.Reset();
            Activate();
        }

        /// <summary>
        /// Drives aim and fire from the device-neutral input state, so the same code runs on a
        /// mouse and on a touchscreen.
        /// </summary>
        public void Update(GameTime gameTime, InputState input)
        {
            if (!IsAlive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Aim at the pointer.
            Vector2 toPointer = input.PointerPosition - Position;
            if (toPointer.LengthSquared() > 1f)
                Rotation = MathF.Atan2(toPointer.Y, toPointer.X);

            // Fire.
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;
            if (input.FireHeld && _cooldownTimer <= 0f)
            {
                _cooldownTimer = CurrentFireCooldown;
                Fired?.Invoke(this, EventArgs.Empty);
            }

            TickTimers(dt);

            // Homing missiles launch on their own cadence, independent of the trigger, so the
            // supply is worth something even while the player is lining up a dynamite ring.
            if (Loadout.IsActive(SupplyType.HomingMissile))
            {
                _missileTimer -= dt;
                if (_missileTimer <= 0f)
                {
                    _missileTimer = GameConstants.MissileInterval;
                    MissileReady?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                _missileTimer = 0f;
            }

            _enginePulse = MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 11f) * 0.5f + 0.5f;
        }

        public override void Update(GameTime gameTime)
        {
            // Input-free overload kept so Player still satisfies GameObject. The screen always
            // calls the InputState overload.
            TickTimers((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        private void TickTimers(float dt)
        {
            Loadout.Update(dt);

            if (_contactTimer > 0f) _contactTimer -= dt;
            if (_hurtFlash > 0f) _hurtFlash -= dt / GameConstants.PlayerHurtFlashTime;
            if (_hurtFlash < 0f) _hurtFlash = 0f;
        }

        // ── Volley construction ────────────────────────────────────────────

        /// <summary>
        /// Resolves the whole volley - angles, damage, criticals, piercing and acid - into
        /// <paramref name="buffer"/> and returns how many shots were written. The buffer must
        /// hold <see cref="MaxShotsPerVolley"/> entries.
        ///
        /// Weapon supplies are ADDITIVE in bullet count and the widest active cone wins, so
        /// double + diffused is a genuinely different spray from either alone rather than one
        /// silently overriding the other.
        /// </summary>
        public int BuildVolley(ShotSpec[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return 0;

            int count = 1;
            float halfAngle = 0f;

            if (Loadout.IsActive(SupplyType.DoubleFire))
            {
                count += 1;
                halfAngle = MathF.Max(halfAngle, GameConstants.DoubleSpreadRadians);
            }

            if (Loadout.IsActive(SupplyType.TripleFire))
            {
                count += 2;
                halfAngle = MathF.Max(halfAngle, GameConstants.TripleSpreadRadians);
            }

            if (Loadout.IsActive(SupplyType.DiffusedFire))
            {
                count += GameConstants.DiffusedBulletCount - 1;
                halfAngle = MathF.Max(halfAngle, GameConstants.DiffusedSpreadRadians);
            }

            if (count > buffer.Length) count = buffer.Length;

            int baseDamage = GameConstants.DefaultBulletDamage + Loadout.BonusDamage;
            float critChance = Loadout.CritChance;
            int pierce = Loadout.PierceCount;
            int acid = Loadout.AcidStacksPerHit;

            float step = count > 1 ? halfAngle * 2f / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = count > 1
                    ? Rotation - halfAngle + step * i
                    : Rotation;

                bool crit = critChance > 0f && _rng.NextDouble() < critChance;
                int damage = crit
                    ? baseDamage * GameConstants.CritDamageMultiplier
                    : baseDamage;

                buffer[i] = new ShotSpec
                {
                    Angle = angle,
                    Damage = damage,
                    IsCritical = crit,
                    Pierce = pierce,
                    AcidStacks = acid
                };
            }

            return count;
        }

        /// <summary>True when the current volley should use the wider, lower fire sound.</summary>
        public bool UsesWideFireSound =>
            Loadout.IsActive(SupplyType.TripleFire) ||
            Loadout.IsActive(SupplyType.DiffusedFire);

        // ── Supplies and diamonds ──────────────────────────────────────────

        /// <summary>
        /// Applies a collected supply. Health is immediate, everything else is a timer.
        /// Returns the hit points restored, which is zero for every non-repair pickup.
        /// </summary>
        public int ApplySupply(SupplyType type)
        {
            if (type == SupplyType.HealthPack)
                return Heal(GameConstants.HealthPackRestore);

            Loadout.GrantSupply(type);
            return 0;
        }

        /// <summary>Applies a collected diamond and returns its new stack level.</summary>
        public int ApplyDiamond(DiamondType type) => Loadout.GrantDiamond(type);

        // ── Damage ─────────────────────────────────────────────────────────

        /// <summary>
        /// Applies contact damage from an overlapping ring, but only once per
        /// <see cref="GameConstants.ContactTickInterval"/>. Without that gate, six rings
        /// touching the ship at once would delete the health bar in a single frame and the
        /// player would never see what killed them.
        /// </summary>
        /// <returns>Damage actually applied, or 0 when the gate rejected it.</returns>
        public int TakeContactDamage()
        {
            if (!IsAlive) return 0;
            if (_contactTimer > 0f) return 0;

            _contactTimer = GameConstants.ContactTickInterval;
            int applied = TakeDamage(GameConstants.ContactDamagePerTick);
            if (applied > 0) _hurtFlash = 1f;
            return applied;
        }

        /// <summary>Unblockable damage - the head reaching the core.</summary>
        public void Obliterate()
        {
            if (!IsAlive) return;
            _hurtFlash = 1f;
            TakeDamage(CurrentHealth);
        }

        // ── Presentation ───────────────────────────────────────────────────

        public Vector2 GetFireDirection() =>
            new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

        /// <summary>Muzzle position, so bullets leave the nose instead of the hub.</summary>
        public Vector2 GetMuzzlePosition()
            => Position + GetFireDirection() * (GameConstants.PlayerSize * 0.85f);

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            // The hull tints toward whichever diamond is strongest, so the player can read
            // their own loadout from the ship without looking at the HUD.
            Color hull = ShipColor;
            int best = 0;
            for (int i = 0; i < DiamondTypeInfo.Count; i++)
            {
                var type = (DiamondType)i;
                int level = Loadout.DiamondLevel(type);
                if (level <= best) continue;
                best = level;
                hull = Color.Lerp(ShipColor, DiamondTypeInfo.ColorOf(type), 0.45f);
            }

            NeonRenderer.DrawNeonPlayer(spriteBatch, _pixel, Position, Rotation,
                hull, _enginePulse, _hurtFlash);
        }
    }
}
