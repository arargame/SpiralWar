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

        /// <summary>Every tenth level is a boss: far longer snake, tougher rings, faster spin.</summary>
        public bool IsBoss { get; }

        /// <summary>Short label shown on the HUD, e.g. "SECTOR 4" or "BOSS".</summary>
        public string Title { get; }

        // ── Snake shape ────────────────────────────────────────────────────
        public int SegmentCount { get; }
        public int MinSegmentHealth { get; }
        public int MaxSegmentHealth { get; }

        // ── Spiral motion ──────────────────────────────────────────────────
        /// <summary>Radians per second the head sweeps around the core.</summary>
        public float AngularSpeed { get; }

        /// <summary>
        /// Seconds the head needs to travel from the spawn radius to the core when it does
        /// not accelerate. This is the level's fail timer.
        /// </summary>
        public float SpiralDuration { get; }

        /// <summary>
        /// Extra inward speed accumulated across the level, as a fraction of the base speed.
        /// 0.4 means the head is moving 40% faster at the core than at the rim — the level
        /// tightens instead of running at a flat pace.
        /// </summary>
        public float InwardAcceleration { get; }

        // ── Modifiers unlocked by milestones ───────────────────────────────
        /// <summary>Probability a body segment spawns armored (halved incoming damage).</summary>
        public float ArmoredChance { get; }

        /// <summary>Level 75+: undamaged segments slowly heal, punishing spread-thin fire.</summary>
        public bool RegenerationEnabled { get; }

        /// <summary>Seconds a segment must go without being hit before it starts healing.</summary>
        public float RegenerationDelay { get; }

        /// <summary>Hit points healed per second once regeneration kicks in.</summary>
        public float RegenerationPerSecond { get; }

        // ── Presentation ───────────────────────────────────────────────────
        /// <summary>Accent colour for the HUD, grid and background of this tier.</summary>
        public Color ThemeColor { get; }

        /// <summary>Intended time-to-clear used when the curve was authored. Docs/telemetry only.</summary>
        public float TargetClearSeconds { get; }

        public LevelDefinition(
            int index, int tier, bool isBoss, string title,
            int segmentCount, int minSegmentHealth, int maxSegmentHealth,
            float angularSpeed, float spiralDuration, float inwardAcceleration,
            float armoredChance, bool regenerationEnabled,
            float regenerationDelay, float regenerationPerSecond,
            Color themeColor, float targetClearSeconds)
        {
            Index = index;
            Tier = tier;
            IsBoss = isBoss;
            Title = title;
            SegmentCount = segmentCount;
            MinSegmentHealth = minSegmentHealth;
            MaxSegmentHealth = maxSegmentHealth;
            AngularSpeed = angularSpeed;
            SpiralDuration = spiralDuration;
            InwardAcceleration = inwardAcceleration;
            ArmoredChance = armoredChance;
            RegenerationEnabled = regenerationEnabled;
            RegenerationDelay = regenerationDelay;
            RegenerationPerSecond = regenerationPerSecond;
            ThemeColor = themeColor;
            TargetClearSeconds = targetClearSeconds;
        }

        /// <summary>Average hit points of a body segment. Used for HUD estimates and docs.</summary>
        public float AverageSegmentHealth => (MinSegmentHealth + MaxSegmentHealth) * 0.5f;

        /// <summary>Total hit points the player has to chew through to clear the level.</summary>
        public float TotalHealthPool => SegmentCount * AverageSegmentHealth;
    }
}
