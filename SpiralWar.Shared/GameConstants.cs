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

        // ── Level pacing ───────────────────────────────────────────────────
        // Every level is a three minute engagement. The spiral always needs this
        // long to reach the core, so the fail timer is a constant the player can
        // internalise instead of a number that shifts under them.
        public const float LevelDurationSeconds = 180f;

        /// <summary>Time the curve expects a competent player to need. Design target only.</summary>
        public const float TargetClearSeconds = 150f;

        // ── Snake geometry (base size; each level scales it by its form) ────
        public const int   SegmentWidth      = 48;
        public const int   SegmentHeight     = 28;
        public const float SegmentSpacing    = 0.72f; // of the scaled segment width
        public const float HeadCenterRadius  = 42f;   // head "reached the core" distance

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
        public const float WeaponBoostDuration   = 12f;   // seconds granted by a weapon supply
        public const float StasisDuration        = 8f;    // seconds the spiral is slowed
        public const float StasisTimeScale       = 0.40f; // spiral advances at 40% speed
        public const int   HealthPackRestore     = 35;    // hit points restored by a "+" supply

        public const float DoubleSpreadRadians   = 0.065f;
        public const float TripleSpreadRadians   = 0.13f;
        public const float DiffusedSpreadRadians = 0.46f;
        public const int   DiffusedBulletCount   = 5;
        public const int   MaxVolleySize         = 9;     // double + diffused, the widest fan

        // ── Homing missiles ────────────────────────────────────────────────
        public const float MissileInterval    = 0.55f; // seconds between launches while active
        public const float MissileSpeed       = 430f;  // px/s
        public const float MissileTurnRate    = 4.6f;  // rad/s steering limit
        public const float MissileLifetime    = 4.5f;
        public const int   MissileDamage      = 5;
        public const float MissileBlastRadius = 72f;
        public const int   MissileBlastDamage = 2;
        public const int   MaxMissiles        = 64;

        // ── Neon diamonds (timed, stacking stat buffs) ─────────────────────
        public const float DiamondDuration   = 14f;  // seconds granted per pickup
        public const int   DiamondMaxLevel   = 3;    // stacking cap per colour
        public const float DiamondLifetime   = 11f;  // seconds before it fades away
        public const int   DiamondSize       = 22;
        public const float DiamondFallSpeed  = 165f; // px/s toward the core
        public const float DiamondSpinSpeed  = 1.3f; // rad/s

        /// <summary>Blue: fire cooldown multiplier removed per stack. Lv3 => x0.46.</summary>
        public const float AttackSpeedPerLevel = 0.18f;

        /// <summary>Black: critical chance added per stack. Lv3 => 30%.</summary>
        public const float CritChancePerLevel  = 0.10f;
        public const int   CritDamageMultiplier = 3;

        /// <summary>Red: flat damage added per stack. Lv3 => 4 damage per bullet.</summary>
        public const int   PowerDamagePerLevel = 1;

        /// <summary>Red Lv2+: bullets carry on through one extra ring.</summary>
        public const int   PowerPierceLevel    = 2;

        /// <summary>Green: acid stacks applied per hit, and what each stack is worth.</summary>
        public const int   AcidStacksPerLevel  = 2;   // Lv3 => up to 6 stacks on a ring
        public const int   AcidMaxStacks       = 6;
        public const float AcidTickInterval    = 0.5f;
        public const int   AcidDamagePerTick   = 1;
        public const float AcidDuration        = 3.5f;

        // ── Supplies ───────────────────────────────────────────────────────
        public const float SupplyLifetime    = 11f;  // seconds before it fades away
        public const float SupplyFallSpeed   = 190f; // px/s toward the core
        public const int   SupplySize        = 24;
        public const float SupplySpinSpeed   = 1.6f; // rad/s

        // ── Neon dynamite ──────────────────────────────────────────────────
        /// <summary>World-space radius of a dynamite ring's blast.</summary>
        public const float DynamiteBlastRadius = 128f;

        /// <summary>Damage at the centre of the blast, as a fraction of the level's max ring HP.</summary>
        public const float DynamiteBlastFraction = 0.80f;

        /// <summary>Damage still dealt at the very edge of the blast, as a fraction of the centre.</summary>
        public const float DynamiteEdgeFalloff = 0.35f;

        /// <summary>How many generations a chain reaction may travel before it stops.</summary>
        public const int DynamiteChainDepthLimit = 4;

        public const float DynamiteFuseSpeed = 9f; // rad/s of the fuse spark orbit

        // ── Feedback / juice ───────────────────────────────────────────────
        public const float HitFlashDuration   = 0.12f;
        public const float HitKnockAmount     = 5.5f;  // px the segment is pushed on hit
        public const float ShakeOnSegmentHit  = 1.6f;
        public const float ShakeOnSegmentKill = 5.5f;
        public const float ShakeOnPlayerHurt  = 9f;
        public const float ShakeOnDynamite    = 16f;

        public const int MaxParticles    = 1200;
        public const int MaxFloatingText = 96;
        public const int MaxBullets      = 640;
        public const int MaxSupplies     = 64;
        public const int MaxDiamonds     = 48;

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
