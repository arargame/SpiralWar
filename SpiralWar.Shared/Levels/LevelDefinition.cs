using System;
using Microsoft.Xna.Framework;

namespace SpiralWar.Levels
{
    /// <summary>
    /// Everything that makes level N different from level N+1, resolved once when the level
    /// starts. Immutable on purpose: gameplay systems read it, nobody mutates it, so there is
    /// exactly one source of truth for difficulty.
    /// </summary>
    public sealed class LevelDefinition
    {
        /// <summary>1-based level number.</summary>
        public int Index { get; }

        /// <summary>Tier this level belongs to (0 = levels 1-10, 9 = levels 91-100).</summary>
        public int Tier { get; }

        /// <summary>Every tenth level is a boss: a longer snake, tougher rings, more dynamite.</summary>
        public bool IsBoss { get; }

        /// <summary>Short label shown on the HUD, e.g. "THE COIL" or "BOSS".</summary>
        public string Title { get; }

        // ── Form ───────────────────────────────────────────────────────────
        /// <summary>Which of the five shapes this level's spiral takes.</summary>
        public SnakeForm Form { get; }

        public SnakeFormInfo FormInfo { get; }

        /// <summary>True when this level is where the form changes, so it can be announced.</summary>
        public bool IsFormDebut { get; }

        // ── Snake shape ────────────────────────────────────────────────────
        public int SegmentCount { get; }
        public int MinSegmentHealth { get; }
        public int MaxSegmentHealth { get; }

        /// <summary>Drawn and collided ring size for this level, already scaled by the form.</summary>
        public int SegmentWidth { get; }
        public int SegmentHeight { get; }

        /// <summary>Distance between two ring centres along the trail.</summary>
        public float SegmentSpacing { get; }

        // ── Spiral motion ──────────────────────────────────────────────────
        /// <summary>Radians per second the head sweeps around the core.</summary>
        public float AngularSpeed { get; }

        /// <summary>
        /// Seconds the head needs to travel from the spawn radius to the core. This is the
        /// level's fail timer and is a constant three minutes for the whole campaign.
        /// </summary>
        public float SpiralDuration { get; }

        /// <summary>
        /// Extra inward speed accumulated across the level, as a fraction of the base speed.
        /// 0.4 means the head is moving 40% faster at the core than at the rim.
        /// </summary>
        public float InwardAcceleration { get; }

        // ── Ring composition ───────────────────────────────────────────────
        /// <summary>Probability a body ring spawns armored (double hit points).</summary>
        public float ArmoredChance { get; }

        /// <summary>Probability a body ring is a neon dynamite charge.</summary>
        public float DynamiteChance { get; }

        /// <summary>Probability a non-dynamite ring carries a weapon or repair supply.</summary>
        public float SupplyCarryChance { get; }

        /// <summary>Probability a non-dynamite ring carries a neon diamond.</summary>
        public float DiamondCarryChance { get; }

        /// <summary>Damage dealt at the centre of a dynamite blast on this level.</summary>
        public int DynamiteBlastDamage { get; }

        // ── Modifiers unlocked by milestones ───────────────────────────────
        /// <summary>Level 75+: undamaged rings slowly heal, punishing spread-thin fire.</summary>
        public bool RegenerationEnabled { get; }

        /// <summary>Seconds a ring must go without being hit before it starts healing.</summary>
        public float RegenerationDelay { get; }

        /// <summary>Hit points healed per second once regeneration kicks in.</summary>
        public float RegenerationPerSecond { get; }

        // ── Presentation ───────────────────────────────────────────────────
        /// <summary>Accent colour for the HUD, grid and background of this tier.</summary>
        public Color ThemeColor { get; }

        /// <summary>Intended time-to-clear used when the curve was authored. Docs/telemetry only.</summary>
        public float TargetClearSeconds { get; }

        /// <summary>Total hit points the player has to chew through, armor included.</summary>
        public float HealthPool { get; }

        public LevelDefinition(
            int index, int tier, bool isBoss, string title,
            SnakeForm form, bool isFormDebut,
            int segmentCount, int minSegmentHealth, int maxSegmentHealth,
            float angularSpeed, float spiralDuration, float inwardAcceleration,
            float armoredChance, float dynamiteChance,
            float supplyCarryChance, float diamondCarryChance,
            bool regenerationEnabled, float regenerationDelay, float regenerationPerSecond,
            Color themeColor, float targetClearSeconds)
        {
            Index = index;
            Tier = tier;
            IsBoss = isBoss;
            Title = title;

            Form = form;
            FormInfo = SnakeFormInfo.Of(form);
            IsFormDebut = isFormDebut;

            SegmentCount = segmentCount;
            MinSegmentHealth = minSegmentHealth;
            MaxSegmentHealth = maxSegmentHealth;

            SegmentWidth = FormInfo.RingWidth;
            SegmentHeight = FormInfo.RingHeight;
            SegmentSpacing = FormInfo.RingSpacing;

            AngularSpeed = angularSpeed;
            SpiralDuration = spiralDuration;
            InwardAcceleration = inwardAcceleration;

            ArmoredChance = armoredChance;
            DynamiteChance = dynamiteChance;
            SupplyCarryChance = supplyCarryChance;
            DiamondCarryChance = diamondCarryChance;

            RegenerationEnabled = regenerationEnabled;
            RegenerationDelay = regenerationDelay;
            RegenerationPerSecond = regenerationPerSecond;

            ThemeColor = themeColor;
            TargetClearSeconds = targetClearSeconds;

            DynamiteBlastDamage = Math.Max(1,
                (int)MathF.Round(maxSegmentHealth * GameConstants.DynamiteBlastFraction));

            AverageSegmentHealth = (minSegmentHealth + maxSegmentHealth) * 0.5f;
            HealthPool = segmentCount * AverageSegmentHealth * (1f + armoredChance);
        }

        /// <summary>Average hit points of a body ring. Used for HUD estimates and docs.</summary>
        public float AverageSegmentHealth { get; }
    }
}
