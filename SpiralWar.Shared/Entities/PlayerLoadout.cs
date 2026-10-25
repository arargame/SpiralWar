using System;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Every timed effect the ship is currently carrying, and the derived statistics that fall
    /// out of them.
    ///
    /// This exists so <see cref="Player"/> can stay about aiming and firing. Weapon supplies,
    /// the four diamond stacks and the stasis field are all "a timer plus what that timer is
    /// worth", and keeping them in one place means there is exactly one answer to "what is the
    /// fire cooldown right now" instead of four classes each holding a piece of it.
    ///
    /// Supplies REFRESH (collecting a second triple-fire restarts its 12 seconds).
    /// Diamonds STACK (collecting a second blue takes it to level 2 and restarts the clock),
    /// up to <see cref="GameConstants.DiamondMaxLevel"/>. That asymmetry is deliberate: a
    /// supply changes what the gun does and does not need a magnitude, while a diamond is a
    /// magnitude and nothing else.
    /// </summary>
    public sealed class PlayerLoadout
    {
        private readonly float[] _supplyTimers = new float[SupplyTypeInfo.Count];
        private readonly float[] _diamondTimers = new float[DiamondTypeInfo.Count];
        private readonly int[] _diamondLevels = new int[DiamondTypeInfo.Count];

        /// <summary>Raised when a diamond stack reaches a level it has not been at before.</summary>
        public event EventHandler<DiamondType>? DiamondLevelled;

        // ── Lifecycle ──────────────────────────────────────────────────────

        public void Reset()
        {
            Array.Clear(_supplyTimers, 0, _supplyTimers.Length);
            Array.Clear(_diamondTimers, 0, _diamondTimers.Length);
            Array.Clear(_diamondLevels, 0, _diamondLevels.Length);
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < _supplyTimers.Length; i++)
            {
                if (_supplyTimers[i] <= 0f) continue;
                _supplyTimers[i] -= deltaTime;
                if (_supplyTimers[i] < 0f) _supplyTimers[i] = 0f;
            }

            for (int i = 0; i < _diamondTimers.Length; i++)
            {
                if (_diamondTimers[i] <= 0f) continue;

                _diamondTimers[i] -= deltaTime;
                if (_diamondTimers[i] > 0f) continue;

                // The whole stack expires together. Decaying one level at a time would make
                // the player's damage output drift without them noticing why.
                _diamondTimers[i] = 0f;
                _diamondLevels[i] = 0;
            }
        }

        // ── Granting ───────────────────────────────────────────────────────

        /// <summary>Starts or refreshes a timed supply. Health packs are not timed and are ignored.</summary>
        public void GrantSupply(SupplyType type)
        {
            if (type == SupplyType.HealthPack) return;

            float duration = type == SupplyType.Stasis
                ? GameConstants.StasisDuration
                : GameConstants.WeaponBoostDuration;

            _supplyTimers[(int)type] = duration;
        }

        /// <summary>Adds a diamond stack and restarts its clock. Returns the new level.</summary>
        public int GrantDiamond(DiamondType type)
        {
            int index = (int)type;
            int before = _diamondLevels[index];

            if (_diamondLevels[index] < GameConstants.DiamondMaxLevel)
                _diamondLevels[index]++;

            _diamondTimers[index] = GameConstants.DiamondDuration;

            if (_diamondLevels[index] > before)
                DiamondLevelled?.Invoke(this, type);

            return _diamondLevels[index];
        }

        // ── Queries ────────────────────────────────────────────────────────

        public bool IsActive(SupplyType type) => _supplyTimers[(int)type] > 0f;

        public float TimeLeft(SupplyType type) => _supplyTimers[(int)type];

        public float Fraction(SupplyType type)
        {
            float duration = type == SupplyType.Stasis
                ? GameConstants.StasisDuration
                : GameConstants.WeaponBoostDuration;

            return duration <= 0f ? 0f : _supplyTimers[(int)type] / duration;
        }

        public int DiamondLevel(DiamondType type) => _diamondLevels[(int)type];

        public float DiamondTimeLeft(DiamondType type) => _diamondTimers[(int)type];

        public float DiamondFraction(DiamondType type)
            => _diamondTimers[(int)type] / GameConstants.DiamondDuration;

        public int ActiveDiamondCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _diamondLevels.Length; i++)
                    if (_diamondLevels[i] > 0) count++;
                return count;
            }
        }

        public int ActiveSupplyCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _supplyTimers.Length; i++)
                    if (_supplyTimers[i] > 0f) count++;
                return count;
            }
        }

        // ── Derived statistics ─────────────────────────────────────────────

        /// <summary>
        /// Multiplier applied to the base fire cooldown. Blue diamonds subtract from it, so
        /// level 3 fires at roughly twice the base rate.
        /// </summary>
        public float FireCooldownMultiplier
        {
            get
            {
                float reduction = DiamondLevel(DiamondType.AttackSpeed) *
                                  GameConstants.AttackSpeedPerLevel;
                float multiplier = 1f - reduction;
                return multiplier < 0.25f ? 0.25f : multiplier;
            }
        }

        /// <summary>Flat damage added to every bullet by red diamonds.</summary>
        public int BonusDamage
            => DiamondLevel(DiamondType.Power) * GameConstants.PowerDamagePerLevel;

        /// <summary>Chance a bullet lands as a critical, from black diamonds.</summary>
        public float CritChance
            => DiamondLevel(DiamondType.Critical) * GameConstants.CritChancePerLevel;

        /// <summary>
        /// Extra rings a bullet carries on through. Red diamonds grant this from level 2, which
        /// is what turns Power from "bigger number" into a different way of aiming.
        /// </summary>
        public int PierceCount
            => DiamondLevel(DiamondType.Power) >= GameConstants.PowerPierceLevel ? 1 : 0;

        /// <summary>Acid stacks applied per hit by green diamonds. Zero when none is active.</summary>
        public int AcidStacksPerHit
            => DiamondLevel(DiamondType.Acidic) * GameConstants.AcidStacksPerLevel;

        /// <summary>True while the stasis field is holding the spiral back.</summary>
        public bool IsStasisActive => IsActive(SupplyType.Stasis);

        /// <summary>Time scale to apply to the snake: 1 normally, slower under stasis.</summary>
        public float SpiralTimeScale
            => IsStasisActive ? GameConstants.StasisTimeScale : 1f;
    }
}
