using System;
using Microsoft.Xna.Framework;

namespace SpiralWar.Levels
{
    /// <summary>
    /// Generates the 100 level campaign from a single normalized progress value.
    ///
    /// DESIGN CONTRACT (full rationale in docs/level-design.md)
    /// --------------------------------------------------------
    /// 1. **Every level is three minutes.** The spiral always needs
    ///    <see cref="GameConstants.LevelDurationSeconds"/> to reach the core, at every level,
    ///    boss or not. A fail timer the player can internalise is worth more than one that
    ///    shifts under them, and it makes the health pool a straightforward consequence:
    ///    pool = expected damage output x expected clear time.
    /// 2. **The health pool is the design variable; the form splits it.** The curve decides how
    ///    many hit points a level is worth. The level's SnakeForm then decides whether that is
    ///    spent on a swarm of fragile rings or a short column of enormous ones. Count factor and
    ///    health factor are inverses, so switching form changes feel, not length.
    /// 3. **Numbers stay readable.** A base bullet still deals 1. Critical hits, power attacks
    ///    and acid can exceed that, but every one of them is announced on screen in its own
    ///    colour, so the player can always account for what came off a ring.
    /// 4. **New rules arrive with a banner, never silently:**
    ///       level 10  neon dynamite rings start appearing
    ///       level 25  armored rings (double plating)
    ///       level 50  the surge - the spiral accelerates as it closes
    ///       level 75  regeneration - rings you stop shooting heal back
    ///       every 20  a new SnakeForm
    /// 5. Every tenth level is a boss: a longer snake, tougher rings and far more dynamite.
    /// </summary>
    public sealed class LevelDesigner : ILevelProvider
    {
        // ── Curve constants (all tunable; keep docs/level-design.md in sync) ──

        /// <summary>
        /// Total hit points a level is worth, ARMOR INCLUDED. This is the number the whole
        /// curve is built around: pool = expected damage output x expected clear time.
        /// </summary>
        private const float BasePool = 700f;

        /// <summary>Extra hit points added across the whole campaign.</summary>
        private const float ExtraPool = 4200f;

        /// <summary>
        /// Linear on purpose. The pool has to track the player's rising damage output, and that
        /// output rises roughly linearly with the supply and diamond economy a level provides.
        /// A sub-linear curve here made levels 6 to 9 - after the pool has grown but before
        /// dynamite unlocks at 10 - the only unwinnable stretch in the campaign.
        /// </summary>
        private const float PoolCurve = 1.00f;

        /// <summary>Reference ring count (SnakeForm.Column) at level 1 and at level 100.</summary>
        private const float BaseRingCount = 75f;
        private const float ExtraRingCount = 45f;
        private const float RingCountCurve = 0.70f;

        /// <summary>Health spread around the average, so a snake is not uniform.</summary>
        private const float HealthSpreadLow = 0.60f;
        private const float HealthSpreadHigh = 1.45f;

        private const float BaseInwardAccel = 0.25f;
        private const float ExtraInwardAccel = 0.45f;

        /// <summary>Late levels wind the spiral a little tighter than early ones.</summary>
        private const float AngularLevelBoost = 0.25f;

        // Milestones
        public const int DynamiteMilestone = 10;
        public const int ArmorMilestone = 25;
        public const int SurgeMilestone = 50;
        public const int RegenMilestone = 75;

        private const float MaxArmoredChance = 0.22f;
        private const float MaxDynamiteChance = 0.075f;
        private const float SurgeAccelBonus = 0.35f;

        // Drop chances per ring. These are generous because a three minute level needs a
        // steady supply economy; see docs/supply-system.md.
        private const float BaseSupplyChance = 0.26f;
        private const float ExtraSupplyChance = 0.06f;
        private const float BaseDiamondChance = 0.08f;
        private const float ExtraDiamondChance = 0.07f;

        // Boss multipliers. The health bump is small on purpose: a boss already carries 25%
        // more rings AND an extra 10% armor chance, and all three compound into the pool.
        private const float BossRingMultiplier = 1.25f;
        private const float BossHealthMultiplier = 1.08f;
        private const float BossDynamiteBonus = 0.05f;
        private const float BossArmorBonus = 0.10f;

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

            SnakeForm form = SnakeFormInfo.ForLevel(level);
            SnakeFormInfo formInfo = SnakeFormInfo.Of(form);

            // 1. How much total health the level is worth, armor included.
            float pool = BasePool + ExtraPool * MathF.Pow(t, PoolCurve);

            // 2. How that pool is split, which is entirely the form's business.
            float referenceCount = BaseRingCount + ExtraRingCount * MathF.Pow(t, RingCountCurve);
            int segmentCount = (int)MathF.Round(referenceCount * formInfo.RingCountFactor);

            if (isBoss)
            {
                segmentCount = (int)MathF.Round(segmentCount * BossRingMultiplier);
                pool *= BossHealthMultiplier;
            }

            if (segmentCount < 8) segmentCount = 8;

            // 3. Ring composition. Armor has to be resolved BEFORE health, because an armored
            //    ring carries double hit points: deriving the average without accounting for
            //    that silently inflates the real pool by up to 40% at the top of the campaign,
            //    which is the difference between a hard level and an impossible one.
            float armoredChance = 0f;
            if (level >= ArmorMilestone)
            {
                float progress = (level - ArmorMilestone) /
                                 (float)(GameConstants.MaxLevel - ArmorMilestone);
                armoredChance = Clamp(progress * MaxArmoredChance, 0f, MaxArmoredChance);
            }

            float dynamiteChance = 0f;
            if (level >= DynamiteMilestone)
            {
                float progress = (level - DynamiteMilestone) /
                                 (float)(GameConstants.MaxLevel - DynamiteMilestone);
                // Starts at a third of the ceiling so the rule is visible the moment it unlocks.
                dynamiteChance = Clamp(
                    MaxDynamiteChance * (0.35f + 0.65f * progress), 0f, MaxDynamiteChance);
            }

            float supplyChance = BaseSupplyChance + ExtraSupplyChance * t;
            float diamondChance = BaseDiamondChance + ExtraDiamondChance * t;

            if (isBoss)
            {
                dynamiteChance = Clamp(dynamiteChance + BossDynamiteBonus, 0f, 0.16f);
                armoredChance = Clamp(armoredChance + BossArmorBonus, 0f, 0.40f);
            }

            // 4. Health per ring, so that count x average x (1 + armor) lands back on the pool.
            float averageHealth = pool / (segmentCount * (1f + armoredChance));
            int minHealth = Math.Max(1, (int)MathF.Round(averageHealth * HealthSpreadLow));
            int maxHealth = Math.Max(minHealth, (int)MathF.Round(averageHealth * HealthSpreadHigh));

            // 5. How tightly the spiral winds.
            //
            // The radial gap between two turns has to stay a few ring-heights wide or the
            // turns overlap and the arena becomes unreadable. Bigger rings therefore get a
            // looser spiral, which is why this is derived rather than authored.
            float spawnRadius = ComputeSpawnRadius();
            float turnGap = formInfo.RingHeight * formInfo.TurnGapInRingHeights;
            float revolutions = spawnRadius / turnGap;
            float angularSpeed = revolutions * MathF.Tau / GameConstants.LevelDurationSeconds;
            angularSpeed *= 1f + AngularLevelBoost * t;

            float inwardAccel = BaseInwardAccel + ExtraInwardAccel * t;
            if (level >= SurgeMilestone) inwardAccel += SurgeAccelBonus;

            // 6. Milestone rules.
            bool regen = level >= RegenMilestone;
            float regenDelay = 3.5f;
            float regenPerSecond = regen ? 1f + (level - RegenMilestone) / 12f : 0f;

            string title = isBoss
                ? (level == GameConstants.MaxLevel ? "FINAL CORE" : "BOSS")
                : TierNames[tier];

            return new LevelDefinition(
                index: level,
                tier: tier,
                isBoss: isBoss,
                title: title,
                form: form,
                isFormDebut: SnakeFormInfo.IsFormDebutLevel(level),
                segmentCount: segmentCount,
                minSegmentHealth: minHealth,
                maxSegmentHealth: maxHealth,
                angularSpeed: angularSpeed,
                spiralDuration: GameConstants.LevelDurationSeconds,
                inwardAcceleration: inwardAccel,
                armoredChance: armoredChance,
                dynamiteChance: dynamiteChance,
                supplyCarryChance: supplyChance,
                diamondCarryChance: diamondChance,
                regenerationEnabled: regen,
                regenerationDelay: regenDelay,
                regenerationPerSecond: regenPerSecond,
                themeColor: TierColors[tier],
                targetClearSeconds: GameConstants.TargetClearSeconds);
        }

        /// <summary>
        /// Radius the head spawns at: just beyond the screen corner, so the snake always
        /// arrives from off-stage. Shared with <see cref="Entities.Snake"/>.
        /// </summary>
        public static float ComputeSpawnRadius()
        {
            float halfW = GameConstants.ScreenWidth / 2f + 110f;
            float halfH = GameConstants.ScreenHeight / 2f + 110f;
            return MathF.Sqrt(halfW * halfW + halfH * halfH);
        }

        /// <summary>
        /// Human readable one-liner describing what changes at this level. Shown on the level
        /// intro card so a new rule is never a silent surprise.
        /// </summary>
        public static string GetMilestoneBanner(LevelDefinition level)
        {
            if (level.Index == GameConstants.MaxLevel) return "THE FINAL CORE";
            if (level.IsFormDebut)
                return "NEW FORM: " + level.FormInfo.Name + " - " + level.FormInfo.Tagline;
            if (level.Index == RegenMilestone) return "NEW: RINGS REGENERATE IF YOU LET THEM BREATHE";
            if (level.Index == SurgeMilestone) return "NEW: THE SPIRAL SURGES AS IT CLOSES";
            if (level.Index == ArmorMilestone) return "NEW: ARMORED RINGS CARRY DOUBLE PLATING";
            if (level.Index == DynamiteMilestone) return "NEW: NEON DYNAMITE - SHOOT IT, CLEAR THE CLUSTER";
            if (level.IsBoss) return "BOSS SPIRAL";
            return string.Empty;
        }

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}
