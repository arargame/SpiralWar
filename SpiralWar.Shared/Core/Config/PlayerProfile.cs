using System;

namespace SpiralWar.Core.Config
{
    /// <summary>
    /// Identity and progression of a single player, persisted as profile.json.
    ///
    /// PlayerId is a GUID generated once on this device and never changed. It is the key
    /// every later feature hangs off: cloud save, leaderboards, IAP entitlement, crash
    /// reports and analytics. It contains no personal data by design.
    /// </summary>
    public sealed class PlayerProfile
    {
        /// <summary>Schema version so future migrations can be detected.</summary>
        public int Version { get; set; } = 1;

        /// <summary>Stable anonymous identity for this installation.</summary>
        public Guid PlayerId { get; set; } = Guid.Empty;

        /// <summary>Optional display name. Empty means "use the default".</summary>
        public string DisplayName { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastPlayedUtc { get; set; } = DateTime.UtcNow;

        // ── Progression ────────────────────────────────────────────────────
        /// <summary>Highest level ever reached. Drives the CONTINUE button.</summary>
        public int HighestLevelReached { get; set; } = 1;

        /// <summary>Level the run should resume from.</summary>
        public int CurrentLevel { get; set; } = 1;

        // ── Lifetime statistics ────────────────────────────────────────────
        public long TotalSegmentsDestroyed { get; set; }
        public long TotalShotsFired { get; set; }
        public long TotalSuppliesCollected { get; set; }
        public long TotalDeaths { get; set; }
        public double TotalPlaySeconds { get; set; }
        public long BestScore { get; set; }

        /// <summary>Ensures the profile is usable: assigns an id on first run, clamps levels.</summary>
        public void EnsureValid()
        {
            if (PlayerId == Guid.Empty)
                PlayerId = Guid.NewGuid();

            if (CurrentLevel < 1) CurrentLevel = 1;
            if (CurrentLevel > GameConstants.MaxLevel) CurrentLevel = GameConstants.MaxLevel;

            if (HighestLevelReached < CurrentLevel) HighestLevelReached = CurrentLevel;
            if (HighestLevelReached > GameConstants.MaxLevel) HighestLevelReached = GameConstants.MaxLevel;

            if (CreatedUtc == default) CreatedUtc = DateTime.UtcNow;
            DisplayName ??= string.Empty;
        }

        /// <summary>Short, human readable id for the credits / support screen.</summary>
        public string ShortId => PlayerId == Guid.Empty
            ? "--------"
            : PlayerId.ToString("N").Substring(0, 8).ToUpperInvariant();
    }
}
