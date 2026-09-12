using System;
using Microsoft.Xna.Framework;

namespace SpiralWar.Levels
{
    /// <summary>
    /// Generates the 100 level campaign from a single normalized progress value.
    ///
    /// DESIGN CONTRACT (full rationale in docs/level-design.md)
    /// --------------------------------------------------------
    /// 1. A bullet always deals 1 damage. Every hit visibly ticks the number on a ring down
    ///    by one, so the player can always read exactly how much work is left. Difficulty is
    ///    expressed as more rings, tougher rings and a faster spiral — never as an invisible
    ///    damage multiplier.
    /// 2. The spiral is the fail timer. SpiralDuration is always ~45% longer than the time the
    ///    curve expects a competent player to need, so a clean run finishes with slack and a
    ///    sloppy run gets eaten.
    /// 3. Difficulty steps are smooth (sub-linear power curves), with three hard milestones
    ///    that introduce a NEW rule rather than a bigger number:
    ///       level 25  armored rings (halved damage)
    ///       level 50  the surge — the spiral accelerates noticeably as it closes
    ///       level 75  regeneration — rings you stop shooting heal back
    /// 4. Every tenth level is a boss: ~30% more rings, tougher, faster, extra armor.
    /// 5. Each tier of ten levels gets its own accent colour so progress is legible at a glance.
    /// </summary>
    public sealed class LevelDesigner : ILevelProvider
    {
        // ── Curve constants (all tunable; keep docs/level-design.md in sync) ──
        private const int   BaseSegmentCount   = 22;
        private const int   ExtraSegmentCount  = 78;    // added across the whole campaign
        private const float SegmentCountCurve  = 0.75f; // <1 => generous early, steep late

        // Ring hit points stay SMALL on purpose (see the design contract above): a ring should
        // always show a one- or low-two-digit number so each hit visibly ticks it down.
        // Difficulty comes from ring COUNT, not from inflating these.
        private const int   BaseMinHealth      = 2;
        private const int   ExtraMinHealth     = 2;
        private const int   BaseMaxHealth      = 5;
        private const int   ExtraMaxHealth     = 5;

        private const float BaseClearSeconds   = 18f;
        private const float ExtraClearSeconds  = 46f;
        private const float ClearSecondsCurve  = 0.90f;

        /// <summary>How much slack the fail timer gives over the intended clear time.</summary>
        private const float SpiralSlack        = 1.45f;

        private const float BaseAngularSpeed   = 0.45f;
        private const float ExtraAngularSpeed  = 0.55f;

        private const float BaseInwardAccel    = 0.25f;
        private const float ExtraInwardAccel   = 0.45f;

        // Milestones
        public const int ArmorMilestone  = 25;
        public const int SurgeMilestone  = 50;
        public const int RegenMilestone  = 75;

        // An armored ring carries DOUBLE hit points (see SnakeSegment). Each point of armor
        // chance therefore adds that fraction again to the level's total health pool, so this
        // ceiling is a real difficulty lever and not just decoration.
        private const float MaxArmoredChance = 0.22f;
        private const float SurgeAccelBonus  = 0.35f;

        // Boss multipliers
        private const float BossSegmentMultiplier = 1.30f;
        private const float BossHealthMultiplier  = 1.35f;
        private const float BossAngularMultiplier = 1.15f;
        // A boss carries ~30% more rings AND tougher ones, so it needs proportionally more
        // time on the clock or it stops being a challenge and becomes an impossibility.
        private const float BossDurationBonus     = 1.30f;
        private const float BossArmorBonus        = 0.10f;

        /// <summary>One accent colour per tier of ten levels.</summary>
        private static readonly Color[] TierColors =
        {
            new Color(0,   220, 255), // 1-10   ice cyan
            new Color(0,   255, 170), // 11-20  mint
            new Color(150, 255,  40), // 21-30  acid lime
            new Color(255, 210,   0), // 31-40  amber
            new Color(255, 130,   0), // 41-50  ember
            new Color(255,  60,  90), // 51-60  coral
            new Color(255,   0, 170), // 61-70  hot magenta
            new Color(190,  60, 255), // 71-80  violet
            new Color(110, 110, 255), // 81-90  indigo
            new Color(255, 255, 255)  // 91-100 white hot
        };

        private static readonly string[] TierNames =
        {
            "OUTER RIM", "DRIFT", "THE COIL", "ASHFALL", "EMBER GATE",
            "REDSHIFT", "SINGULARITY", "VIOLET DEEP", "EVENT HORIZON", "WHITE CORE"
        };

        private readonly LevelDefinition[] _cache;

        public int MaxLevel => GameConstants.MaxLevel;

        public LevelDesigner()
        {
            // The whole campaign is deterministic, so build it once up front. 100 small
            // objects cost nothing and every later lookup is free.
            _cache = new LevelDefinition[GameConstants.MaxLevel + 1];
            for (int i = 1; i <= GameConstants.MaxLevel; i++)
                _cache[i] = Compose(i);
        }

        public LevelDefinition GetLevel(int index)
        {
            if (index < 1) index = 1;
            if (index > GameConstants.MaxLevel) index = GameConstants.MaxLevel;
            return _cache[index];
        }

        // ── The curve ──────────────────────────────────────────────────────
        private static LevelDefinition Compose(int level)
        {
            // Normalized campaign progress: 0 at level 1, 1 at level 100.
            float t = (level - 1) / (float)(GameConstants.MaxLevel - 1);
            bool isBoss = level % 10 == 0;
            int tier = Math.Min(TierColors.Length - 1, (level - 1) / 10);

            // 1. How much snake.
            int segmentCount = (int)MathF.Round(
                BaseSegmentCount + ExtraSegmentCount * MathF.Pow(t, SegmentCountCurve));

            // 2. How tough each ring is.
            int minHealth = (int)MathF.Round(BaseMinHealth + ExtraMinHealth * t);
            int maxHealth = (int)MathF.Round(BaseMaxHealth + ExtraMaxHealth * t);

            // 3. How long the player should need, and how long they actually get.
            float targetClear = BaseClearSeconds + ExtraClearSeconds * MathF.Pow(t, ClearSecondsCurve);
            float spiralDuration = targetClear * SpiralSlack;

            // 4. How fast it whirls. Late levels spin hard while closing steadily —
            //    that reads as pressure without turning the level into a coin flip.
            float angularSpeed = BaseAngularSpeed + ExtraAngularSpeed * t;
            float inwardAccel = BaseInwardAccel + ExtraInwardAccel * t;

            // 5. Milestone rules.
            float armoredChance = 0f;
            if (level >= ArmorMilestone)
            {
                float armorProgress = (level - ArmorMilestone) /
                                      (float)(GameConstants.MaxLevel - ArmorMilestone);
                armoredChance = Clamp(armorProgress * MaxArmoredChance, 0f, MaxArmoredChance);
            }

            if (level >= SurgeMilestone)
                inwardAccel += SurgeAccelBonus;

            bool regen = level >= RegenMilestone;
            float regenDelay = 3.5f;
            float regenPerSecond = regen
                ? 1f + (level - RegenMilestone) / 12f
                : 0f;

            // 6. Boss pass.
            string title;
            if (isBoss)
            {
                segmentCount = (int)MathF.Round(segmentCount * BossSegmentMultiplier);
                maxHealth = (int)MathF.Round(maxHealth * BossHealthMultiplier);
                angularSpeed *= BossAngularMultiplier;
                spiralDuration *= BossDurationBonus;
                armoredChance = Clamp(armoredChance + BossArmorBonus, 0f, 0.40f);
                title = level == GameConstants.MaxLevel ? "FINAL CORE" : "BOSS";
            }
            else
            {
                title = TierNames[tier];
            }

            if (maxHealth < minHealth) maxHealth = minHealth;

            return new LevelDefinition(
                index: level,
                tier: tier,
                isBoss: isBoss,
                title: title,
                segmentCount: segmentCount,
                minSegmentHealth: minHealth,
                maxSegmentHealth: maxHealth,
                angularSpeed: angularSpeed,
                spiralDuration: spiralDuration,
                inwardAcceleration: inwardAccel,
                armoredChance: armoredChance,
                regenerationEnabled: regen,
                regenerationDelay: regenDelay,
                regenerationPerSecond: regenPerSecond,
                themeColor: TierColors[tier],
                targetClearSeconds: targetClear);
        }

        /// <summary>
        /// Human readable one-liner describing what changes at this level. Shown on the level
        /// intro card so a new rule is never a silent surprise.
        /// </summary>
        public static string GetMilestoneBanner(LevelDefinition level)
        {
            if (level.Index == GameConstants.MaxLevel) return "THE FINAL CORE";
            if (level.Index == RegenMilestone) return "NEW: RINGS REGENERATE IF YOU LET THEM BREATHE";
            if (level.Index == SurgeMilestone) return "NEW: THE SPIRAL SURGES AS IT CLOSES";
            if (level.Index == ArmorMilestone) return "NEW: ARMORED RINGS CARRY DOUBLE PLATING";
            if (level.IsBoss) return "BOSS SPIRAL";
            return string.Empty;
        }

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}
