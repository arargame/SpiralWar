namespace SpiralWar
{
    /// <summary>
    /// Central tuning values. Every gameplay parameter that is NOT level dependent lives here.
    /// Level dependent values are produced by <see cref="SpiralWar.Levels.LevelDesigner"/>.
    /// </summary>
    public static class GameConstants
    {
        // ── Virtual (design) resolution ────────────────────────────────────
        // The game is authored at this resolution and letterboxed to whatever
        // the real back buffer is, so desktop and mobile share one layout.
        public const int ScreenWidth  = 1280;
        public const int ScreenHeight = 720;

        // ── Snake geometry ─────────────────────────────────────────────────
        public const int   SegmentWidth      = 48;
        public const int   SegmentHeight     = 28;
        public const int   SegmentTrailSteps = 9;    // history steps between two segments
        public const float HeadCenterRadius  = 42f;  // head "reached the core" distance

        // ── Player ─────────────────────────────────────────────────────────
        public const int   PlayerSize            = 28;
        public const int   PlayerMaxHealth       = 100;
        public const float DefaultFireCooldown   = 0.17f; // seconds (~5.9 shots/s)
        public const int   DefaultBulletDamage   = 1;
        public const float BulletSpeed           = 660f;  // px/s
        public const float BulletLifetime        = 2.2f;  // seconds (pool recycling safety net)

        // Contact damage: a segment overlapping the player ticks damage.
        public const float ContactTickInterval   = 0.25f; // seconds between ticks
        public const int   ContactDamagePerTick  = 10;    // => 40 dps while overlapped
        public const float PlayerHurtFlashTime   = 0.35f;

        // ── Weapon modes (supply driven) ───────────────────────────────────
        public const float TripleFireDuration    = 10f;   // seconds granted by a "T" supply
        public const float DiffusedFireDuration  = 10f;   // seconds granted by a "D" supply
        public const int   HealthPackRestore     = 35;    // hit points restored by an "H" supply

        public const float TripleSpreadRadians   = 0.13f; // half angle between triple shots
        public const float DiffusedSpreadRadians = 0.46f; // half angle of the diffused cone
        public const int   DiffusedBulletCount   = 5;
        public const int   DiffusedTripleCount   = 7;     // when both boosts are active

        // ── Supplies ───────────────────────────────────────────────────────
        // Per-segment carry chance. A segment carries at most one supply.
        public const float SupplyChanceTriple   = 0.20f;
        public const float SupplyChanceDiffused = 0.20f;
        public const float SupplyChanceHealth   = 0.20f;

        public const float SupplyLifetime    = 9f;   // seconds before it fades away
        public const float SupplyFallSpeed   = 190f; // px/s toward the core
        public const int   SupplySize        = 24;
        public const float SupplySpinSpeed   = 1.6f; // rad/s

        // ── Feedback / juice ───────────────────────────────────────────────
        public const float HitFlashDuration   = 0.12f;
        public const float HitKnockAmount     = 5.5f;  // px the segment is pushed on hit
        public const float ShakeOnSegmentHit  = 1.6f;
        public const float ShakeOnSegmentKill = 5.5f;
        public const float ShakeOnPlayerHurt  = 9f;

        public const int MaxParticles    = 900;
        public const int MaxFloatingText = 64;
        public const int MaxBullets      = 512;

        // ── Neon rendering ─────────────────────────────────────────────────
        public const int   GlowLayers = 4;
        public const float GlowExpand = 3.4f;
        public const float GlowAlpha  = 0.22f;

        // ── Screen transitions ─────────────────────────────────────────────
        public const float TransitionDuration = 0.28f; // seconds, fade in/out half each

        // ── Meta ───────────────────────────────────────────────────────────
        public const int    MaxLevel     = 100;
        public const string GameTitle    = "SPIRAL WAR";
        public const string StudioName   = "ARAR GAMES";
        public const string SupportEmail = "korayarar@gmail.com";
        public const string RepositoryUrl = "https://github.com/arargames/SpiralWar";
    }
}
