using Microsoft.Xna.Framework;

namespace SpiralWar.Entities
{
    /// <summary>
    /// The three pickups a ring can be carrying. Kept deliberately small: the player has to
    /// recognise one at a glance, mid-fight, from a single glyph.
    /// </summary>
    public enum SupplyType
    {
        /// <summary>"T" — triple fire for a while.</summary>
        TripleFire = 0,

        /// <summary>"D" — diffused (wide cone) fire for a while.</summary>
        DiffusedFire = 1,

        /// <summary>"+" — restores hit points immediately.</summary>
        HealthPack = 2
    }

    /// <summary>
    /// Single source of truth for how each supply type looks and reads. Putting the glyph and
    /// the colour here (rather than in a switch inside the renderer) means the drop table,
    /// the pickup HUD and the carrier ring badge can never disagree with each other.
    /// </summary>
    public static class SupplyTypeInfo
    {
        private static readonly Color TripleColor = new Color(60, 255, 150);
        private static readonly Color DiffusedColor = new Color(255, 165, 45);
        private static readonly Color HealthColor = new Color(255, 70, 120);

        public static Color ColorOf(SupplyType type)
        {
            switch (type)
            {
                case SupplyType.TripleFire: return TripleColor;
                case SupplyType.DiffusedFire: return DiffusedColor;
                default: return HealthColor;
            }
        }

        /// <summary>The glyph drawn inside the neon capsule and on the carrier ring.</summary>
        public static string GlyphOf(SupplyType type)
        {
            switch (type)
            {
                case SupplyType.TripleFire: return "T";
                case SupplyType.DiffusedFire: return "D";
                default: return "+";
            }
        }

        /// <summary>Label used by the pickup toast and the credits/how-to screen.</summary>
        public static string NameOf(SupplyType type)
        {
            switch (type)
            {
                case SupplyType.TripleFire: return "TRIPLE FIRE";
                case SupplyType.DiffusedFire: return "DIFFUSED FIRE";
                default: return "REPAIR";
            }
        }
    }
}
