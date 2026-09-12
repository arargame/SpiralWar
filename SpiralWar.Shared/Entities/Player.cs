using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Input;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// The ship. It never moves — it is bolted to the core of the arena and only rotates —
    /// so every decision the player makes is about WHERE to aim and WHICH ring to kill next.
    ///
    /// Two independent, timed weapon boosts stack:
    ///   T (triple)   three tight shots
    ///   D (diffused) a wide cone
    ///   T + D        a seven-shot fan, the strongest state in the game
    /// Both are granted by supplies and expire on their own, which keeps the ship's baseline
    /// honest and makes carrier rings worth prioritising.
    /// </summary>
    public sealed class Player : Entity
    {
        public float Rotation { get; private set; }
        public int BulletDamage { get; private set; }
        public float FireCooldown { get; private set; }

        private float _cooldownTimer;
        private float _tripleTimer;
        private float _diffusedTimer;
        private float _contactTimer;
        private float _hurtFlash;
        private float _enginePulse;

        private readonly Texture2D _pixel;

        private static readonly Color ShipColor = new Color(0, 220, 255);

        /// <summary>Raised when a volley leaves the barrel. The screen turns it into bullets.</summary>
        public event EventHandler? Fired;

        public bool IsTripleActive => _tripleTimer > 0f;
        public bool IsDiffusedActive => _diffusedTimer > 0f;
        public float TripleTimeLeft => _tripleTimer;
        public float DiffusedTimeLeft => _diffusedTimer;
        public float HurtFlash => _hurtFlash;

        /// <summary>Largest volley this ship can currently emit. Sizes the caller's buffer.</summary>
        public static int MaxShotsPerVolley => GameConstants.DiffusedTripleCount;

        public Player(Texture2D pixel) : base(GameConstants.PlayerMaxHealth)
        {
            _pixel = pixel;
            FireCooldown = GameConstants.DefaultFireCooldown;
            BulletDamage = GameConstants.DefaultBulletDamage;
            SetPosition(GameConstants.ScreenWidth / 2f, GameConstants.ScreenHeight / 2f);
        }

        /// <summary>Restores the ship for a fresh level without reallocating it.</summary>
        public void ResetForLevel(bool restoreHealth)
        {
            if (restoreHealth) ResetHealth(GameConstants.PlayerMaxHealth);
            _cooldownTimer = 0f;
            _tripleTimer = 0f;
            _diffusedTimer = 0f;
            _contactTimer = 0f;
            _hurtFlash = 0f;
            Activate();
        }

        /// <summary>
        /// Drives aim and fire from the device-neutral input state, so the same code runs on
        /// a mouse and on a touchscreen.
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
                _cooldownTimer = FireCooldown;
                Fired?.Invoke(this, EventArgs.Empty);
            }

            TickTimers(dt);

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
            if (_tripleTimer > 0f) _tripleTimer -= dt;
            if (_diffusedTimer > 0f) _diffusedTimer -= dt;
            if (_contactTimer > 0f) _contactTimer -= dt;
            if (_hurtFlash > 0f) _hurtFlash -= dt / GameConstants.PlayerHurtFlashTime;
            if (_hurtFlash < 0f) _hurtFlash = 0f;
        }

        // ── Weapon pattern ─────────────────────────────────────────────────

        /// <summary>
        /// Writes the absolute angle of every shot in this volley into <paramref name="buffer"/>
        /// and returns how many were written. Buffer must hold <see cref="MaxShotsPerVolley"/>.
        /// </summary>
        public int GetShotAngles(float[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return 0;

            bool triple = IsTripleActive;
            bool diffused = IsDiffusedActive;

            if (triple && diffused)
                return Fan(buffer, GameConstants.DiffusedTripleCount, GameConstants.DiffusedSpreadRadians);

            if (diffused)
                return Fan(buffer, GameConstants.DiffusedBulletCount, GameConstants.DiffusedSpreadRadians);

            if (triple)
                return Fan(buffer, 3, GameConstants.TripleSpreadRadians);

            buffer[0] = Rotation;
            return 1;
        }

        /// <summary>Spreads <paramref name="count"/> shots evenly across ±halfAngle.</summary>
        private int Fan(float[] buffer, int count, float halfAngle)
        {
            if (count > buffer.Length) count = buffer.Length;
            if (count <= 1)
            {
                buffer[0] = Rotation;
                return 1;
            }

            float step = halfAngle * 2f / (count - 1);
            for (int i = 0; i < count; i++)
                buffer[i] = Rotation - halfAngle + step * i;

            return count;
        }

        /// <summary>True when the current volley should use the wider, lower fire sound.</summary>
        public bool UsesWideFireSound => IsTripleActive || IsDiffusedActive;

        // ── Supplies ───────────────────────────────────────────────────────

        public void ApplyTripleFire() => _tripleTimer = GameConstants.TripleFireDuration;

        public void ApplyDiffusedFire() => _diffusedTimer = GameConstants.DiffusedFireDuration;

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

        /// <summary>Unblockable damage — the head reaching the core.</summary>
        public void Obliterate()
        {
            if (!IsAlive) return;
            _hurtFlash = 1f;
            TakeDamage(CurrentHealth);
        }

        public override int Heal(int amount)
        {
            int applied = base.Heal(amount);
            return applied;
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
            NeonRenderer.DrawNeonPlayer(spriteBatch, _pixel, Position, Rotation,
                ShipColor, _enginePulse, _hurtFlash);
        }
    }
}
