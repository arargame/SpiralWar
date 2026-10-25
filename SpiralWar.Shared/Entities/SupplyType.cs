using System;
using Microsoft.Xna.Framework;

namespace SpiralWar.Entities
{
    /// <summary>
    /// The pickups a ring can be carrying. Kept to six because the player has to recognise one
    /// at a glance, mid-fight, from a single glyph.
    /// </summary>
    public enum SupplyType
    {
        /// <summary>"2" - two tight shots per volley.</summary>
        DoubleFire = 0,

        /// <summary>"T" - three tight shots per volley.</summary>
        TripleFire = 1,

        /// <summary>"D" - a wide cone.</summary>
        DiffusedFire = 2,

        /// <summary>"M" - homing missiles launch alongside normal fire.</summary>
        HomingMissile = 3,

        /// <summary>"S" - the spiral crawls for a few seconds.</summary>
        Stasis = 4,

        /// <summary>"+" - restores hit points immediately.</summary>
        HealthPack = 5
    }

    /// <summary>
    /// Single source of truth for how each supply type looks, reads and how often it appears.
    /// Putting the glyph, the colour and the drop weight here (rather than in a switch inside
    /// the renderer and a second one inside the spawner) means the drop table, the pickup HUD
    /// and the carrier ring badge can never disagree with each other.
    /// </summary>
    public static class SupplyTypeInfo
    {
        public const int Count = 6;

        private static readonly Color[] Colors =
        {
            new Color(0,   235, 220), // double     - teal
            new Color(60,  255, 150), // triple     - mint
            new Color(255, 165,  45), // diffused   - amber
            new Color(120, 190, 255), // homing     - sky
            new Color(200, 160, 255), // stasis     - pale violet
            new Color(255,  70, 120)  // health     - pink
        };

        private static readonly string[] Glyphs = { "2", "T", "D", "M", "S", "+" };

        private static readonly string[] Names =
        {
            "DOUBLE FIRE", "TRIPLE FIRE", "DIFFUSED FIRE",
            "HOMING MISSILES", "STASIS FIELD", "REPAIR"
        };

        /// <summary>
        /// Relative likelihood of each type once a ring has been chosen to carry a supply.
        /// Repair is the most common because a three minute level is long enough that losing
        /// the hull to attrition is the realistic failure, not a single mistake.
        /// </summary>
        private static readonly int[] Weights = { 20, 18, 16, 14, 14, 18 };

        private static readonly int TotalWeight = ComputeTotalWeight();

        private static int ComputeTotalWeight()
        {
            int total = 0;
            for (int i = 0; i < Weights.Length; i++) total += Weights[i];
            return total;
        }

        public static Color ColorOf(SupplyType type) => Colors[IndexOf(type)];

        /// <summary>The glyph drawn inside the neon capsule and on the carrier ring.</summary>
        public static string GlyphOf(SupplyType type) => Glyphs[IndexOf(type)];

        /// <summary>Label used by the pickup toast and the credits/how-to screen.</summary>
        public static string NameOf(SupplyType type) => Names[IndexOf(type)];

        /// <summary>Picks a type from the weighted drop table.</summary>
        public static SupplyType Roll(Random rng)
        {
            int roll = rng.Next(TotalWeight);
            for (int i = 0; i < Weights.Length; i++)
            {
                roll -= Weights[i];
                if (roll < 0) return (SupplyType)i;
            }
            return SupplyType.HealthPack;
        }

        /// <summary>Percentage of all supply drops this type represents. Used by the docs.</summary>
        public static float ShareOf(SupplyType type)
            => Weights[IndexOf(type)] / (float)TotalWeight;

        private static int IndexOf(SupplyType type)
        {
            int index = (int)type;
            if (index < 0 || index >= Colors.Length) return (int)SupplyType.HealthPack;
            return index;
        }
    }
}
