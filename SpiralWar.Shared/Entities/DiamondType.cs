using System;
using Microsoft.Xna.Framework;

namespace SpiralWar.Entities
{
    /// <summary>
    /// The four neon diamonds. Where a supply changes the SHAPE of a volley, a diamond changes
    /// the STATS behind it, and the two stack freely - a triple-fire volley under a blue and a
    /// red diamond is a different weapon entirely.
    /// </summary>
    public enum DiamondType
    {
        /// <summary>Blue - attack speed. Cuts the fire cooldown.</summary>
        AttackSpeed = 0,

        /// <summary>Black - critical hits. A share of bullets land for triple damage.</summary>
        Critical = 1,

        /// <summary>Green - acidic rounds. Hits leave acid that keeps eating the ring.</summary>
        Acidic = 2,

        /// <summary>Red - power attack. Flat extra damage, and piercing at level 2 and above.</summary>
        Power = 3
    }

    /// <summary>
    /// Single source of truth for the diamonds: colour, glyph, name and the one-line
    /// description of what each stack is worth.
    ///
    /// Black is the awkward one. A genuinely black diamond is invisible on a near-black arena,
    /// so it is drawn as a very dark core inside a bright white-violet neon shell - it reads as
    /// "black diamond" while still being the brightest thing on screen when it drops.
    /// </summary>
    public static class DiamondTypeInfo
    {
        public const int Count = 4;

        private static readonly Color[] Colors =
        {
            new Color(60,  150, 255), // blue   - attack speed
            new Color(205, 190, 255), // black  - the SHELL colour; the core is drawn dark
            new Color(110, 255,  70), // green  - acidic
            new Color(255,  60,  60)  // red    - power
        };

        /// <summary>Colour of the stone itself, inside the neon shell.</summary>
        private static readonly Color[] CoreColors =
        {
            new Color(20,  60, 130),
            new Color(8,    6,  16), // the black stone
            new Color(30,  90,  20),
            new Color(110, 18,  18)
        };

        private static readonly string[] Glyphs = { "A", "C", "X", "P" };

        private static readonly string[] Names =
        {
            "ATTACK SPEED", "CRITICAL", "ACIDIC ROUNDS", "POWER ATTACK"
        };

        private static readonly string[] Effects =
        {
            "FIRE FASTER",
            "TRIPLE DAMAGE CRITS",
            "HITS KEEP BURNING",
            "HEAVIER ROUNDS, THEN PIERCING"
        };

        /// <summary>Even weights: no colour should feel like the one you never see.</summary>
        private static readonly int[] Weights = { 25, 25, 25, 25 };

        private static readonly int TotalWeight = ComputeTotalWeight();

        private static int ComputeTotalWeight()
        {
            int total = 0;
            for (int i = 0; i < Weights.Length; i++) total += Weights[i];
            return total;
        }

        public static Color ColorOf(DiamondType type) => Colors[IndexOf(type)];
        public static Color CoreColorOf(DiamondType type) => CoreColors[IndexOf(type)];
        public static string GlyphOf(DiamondType type) => Glyphs[IndexOf(type)];
        public static string NameOf(DiamondType type) => Names[IndexOf(type)];
        public static string EffectOf(DiamondType type) => Effects[IndexOf(type)];

        public static DiamondType Roll(Random rng)
        {
            int roll = rng.Next(TotalWeight);
            for (int i = 0; i < Weights.Length; i++)
            {
                roll -= Weights[i];
                if (roll < 0) return (DiamondType)i;
            }
            return DiamondType.AttackSpeed;
        }

        /// <summary>Short HUD label, e.g. "SPD 2" for a level 2 attack speed stack.</summary>
        public static string ShortLabel(DiamondType type, int level)
        {
            switch (type)
            {
                case DiamondType.AttackSpeed: return "SPD " + level;
                case DiamondType.Critical: return "CRIT " + level;
                case DiamondType.Acidic: return "ACID " + level;
                default: return "PWR " + level;
            }
        }

        private static int IndexOf(DiamondType type)
        {
            int index = (int)type;
            if (index < 0 || index >= Colors.Length) return 0;
            return index;
        }
    }
}
