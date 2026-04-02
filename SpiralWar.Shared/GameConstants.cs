namespace SpiralWar
{
    /// <summary>
    /// Merkezi sabit değerler — tüm game-play parametreleri buradan ayarlanır.
    /// </summary>
    public static class GameConstants
    {
        // ── Screen ─────────────────────────────────────────
        public const int ScreenWidth  = 1280;
        public const int ScreenHeight = 720;

        // ── Snake ──────────────────────────────────────────
        public const int   SnakeSegmentCount   = 20;
        public const int   SegmentWidth        = 46;
        public const int   SegmentHeight       = 26;
        public const float SpiralAngularSpeed  = 0.5f;   // rad/sn
        public const float SpiralInwardSpeed   = 20f;    // px/sn (spiral daralma)
        public const int   SegmentTrailSteps   = 9;      // segment arası geçmiş adım sayısı
        public const int   SegmentMinHealth    = 2;
        public const int   SegmentMaxHealth    = 8;

        // ── Player ─────────────────────────────────────────
        public const int   PlayerSize          = 28;
        public const float DefaultFireCooldown = 0.22f;  // sn
        public const int   DefaultBulletDamage = 1;
        public const float BulletSpeed         = 620f;   // px/sn

        // ── Supply ─────────────────────────────────────────
        public const float SupplyDropChance         = 0.30f; // %30
        public const float SupplyLifetime           = 7f;    // sn
        public const float SupplyBoostDuration      = 9f;    // sn
        public const float FireRateBoostMultiplier  = 0.40f; // cooldown çarpanı (düşük = hızlı)
        public const int   DamageBoostAmount        = 2;     // ekstra hasar
        public const int   SupplySize               = 22;

        // ── Neon / Rendering ───────────────────────────────
        public const int   GlowLayers   = 4;
        public const float GlowExpand   = 3.5f;
        public const float GlowAlpha    = 0.18f;
    }
}
