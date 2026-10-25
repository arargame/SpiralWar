using System;

namespace SpiralWar.Levels
{
    /// <summary>
    /// The five shapes the spiral can take. The form a level uses is decided by which band of
    /// twenty levels it falls in, so the snake visibly changes character five times across the
    /// campaign instead of only getting numerically bigger.
    ///
    /// Form 1 is a swarm: a very long tail of small, fragile rings that melts under sustained
    /// fire. Form 5 is a bulwark: a short column of large, heavily plated rings where every
    /// single one is a commitment. The forms in between walk from one to the other.
    /// </summary>
    public enum SnakeForm
    {
        /// <summary>Levels 1-20. Many small rings, very little health each.</summary>
        Swarm = 1,

        /// <summary>Levels 21-40. Still long, slightly heavier.</summary>
        Flight = 2,

        /// <summary>Levels 41-60. The reference shape every factor is measured against.</summary>
        Column = 3,

        /// <summary>Levels 61-80. Fewer, larger, noticeably tougher rings.</summary>
        Phalanx = 4,

        /// <summary>Levels 81-100. A short column of huge, very heavy rings.</summary>
        Bulwark = 5
    }

    /// <summary>
    /// Per-form tuning. The contract that makes this work is that
    /// <see cref="RingCountFactor"/> x <see cref="RingHealthFactor"/> is approximately 1 for
    /// every form: the level's total health pool is decided by the difficulty curve, and the
    /// form only decides how that pool is SPLIT between "how many rings" and "how tough each
    /// one is". Changing form therefore changes how a level feels without changing how long it
    /// takes to clear.
    ///
    /// <see cref="RingScale"/> exists because a ring carrying four times the health has to look
    /// like it. It also keeps the snake's physical length roughly constant as the ring count
    /// drops, so a Bulwark spiral still reads as a continuous wall rather than a few dots on an
    /// empty screen.
    /// </summary>
    public sealed class SnakeFormInfo
    {
        public SnakeForm Form { get; }
        public string Name { get; }
        public string Tagline { get; }

        /// <summary>Multiplies the reference ring count.</summary>
        public float RingCountFactor { get; }

        /// <summary>Multiplies the reference ring health. Inverse of the count factor.</summary>
        public float RingHealthFactor { get; }

        /// <summary>Multiplies the ring's drawn and collided size.</summary>
        public float RingScale { get; }

        /// <summary>
        /// How many ring-heights of radial gap to leave between two turns of the spiral.
        /// Bigger rings need a wider corridor or the turns overlap into an unreadable mess.
        /// </summary>
        public float TurnGapInRingHeights { get; }

        private SnakeFormInfo(SnakeForm form, string name, string tagline,
                              float ringCountFactor, float ringHealthFactor,
                              float ringScale, float turnGapInRingHeights)
        {
            Form = form;
            Name = name;
            Tagline = tagline;
            RingCountFactor = ringCountFactor;
            RingHealthFactor = ringHealthFactor;
            RingScale = ringScale;
            TurnGapInRingHeights = turnGapInRingHeights;
        }

        private static readonly SnakeFormInfo[] All =
        {
            new SnakeFormInfo(SnakeForm.Swarm,   "SWARM",   "MANY RINGS, LITTLE HEALTH",
                ringCountFactor: 1.70f, ringHealthFactor: 0.59f, ringScale: 0.80f, turnGapInRingHeights: 3.2f),

            new SnakeFormInfo(SnakeForm.Flight,  "FLIGHT",  "LONG AND LIGHT",
                ringCountFactor: 1.35f, ringHealthFactor: 0.74f, ringScale: 0.90f, turnGapInRingHeights: 3.2f),

            new SnakeFormInfo(SnakeForm.Column,  "COLUMN",  "BALANCED",
                ringCountFactor: 1.00f, ringHealthFactor: 1.00f, ringScale: 1.00f, turnGapInRingHeights: 3.2f),

            new SnakeFormInfo(SnakeForm.Phalanx, "PHALANX", "FEWER RINGS, HEAVY PLATING",
                ringCountFactor: 0.78f, ringHealthFactor: 1.28f, ringScale: 1.22f, turnGapInRingHeights: 3.1f),

            new SnakeFormInfo(SnakeForm.Bulwark, "BULWARK", "FEW RINGS, ENORMOUS HEALTH",
                ringCountFactor: 0.60f, ringHealthFactor: 1.67f, ringScale: 1.50f, turnGapInRingHeights: 3.0f)
        };

        public static SnakeFormInfo Of(SnakeForm form)
        {
            int index = (int)form - 1;
            if (index < 0) index = 0;
            if (index >= All.Length) index = All.Length - 1;
            return All[index];
        }

        /// <summary>Levels 1-20 are form 1, 21-40 form 2, and so on up to 81-100 form 5.</summary>
        public static SnakeForm ForLevel(int level)
        {
            int band = (level - 1) / 20 + 1;
            if (band < 1) band = 1;
            if (band > 5) band = 5;
            return (SnakeForm)band;
        }

        /// <summary>True on the first level of a band, where the form changes under the player.</summary>
        public static bool IsFormDebutLevel(int level)
            => level > 1 && (level - 1) % 20 == 0;

        public int RingWidth => (int)MathF.Round(GameConstants.SegmentWidth * RingScale);
        public int RingHeight => (int)MathF.Round(GameConstants.SegmentHeight * RingScale);

        /// <summary>Distance between two consecutive ring centres along the trail.</summary>
        public float RingSpacing => RingWidth * GameConstants.SegmentSpacing;
    }
}
