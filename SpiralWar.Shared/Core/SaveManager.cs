using System;
using System.IO;
using SpiralWar.Core.Config;

namespace SpiralWar.Core
{
    /// <summary>
    /// Progression facade over <see cref="GameConfig"/>.
    ///
    /// Gameplay code asks this class about levels and never touches the profile document
    /// directly. It also migrates the pre-1.1 plain text "save.dat" into the new profile
    /// exactly once, so existing players keep their progress.
    /// </summary>
    public static class SaveManager
    {
        private static readonly string LegacyFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpiralWar");

        private static readonly string LegacyFile = Path.Combine(LegacyFolder, "save.dat");

        /// <summary>Level the CONTINUE button should resume from.</summary>
        public static int LoadLevel()
        {
            MigrateLegacySaveIfNeeded();
            return GameConfig.Profile.CurrentLevel;
        }

        public static int HighestLevel => GameConfig.Profile.HighestLevelReached;

        /// <summary>Stores the level the player is about to play and bumps the personal best.</summary>
        public static void SaveLevel(int level)
        {
            if (level < 1) level = 1;
            if (level > GameConstants.MaxLevel) level = GameConstants.MaxLevel;

            PlayerProfile profile = GameConfig.Profile;
            profile.CurrentLevel = level;

            if (level > profile.HighestLevelReached)
                profile.HighestLevelReached = level;

            GameConfig.SaveProfile();
        }

        /// <summary>Records the outcome of a finished run into the lifetime statistics.</summary>
        public static void RecordRun(int segmentsDestroyed, int shotsFired, int suppliesCollected,
                                     double playSeconds, bool died, long score)
        {
            PlayerProfile profile = GameConfig.Profile;
            profile.TotalSegmentsDestroyed += segmentsDestroyed;
            profile.TotalShotsFired += shotsFired;
            profile.TotalSuppliesCollected += suppliesCollected;
            profile.TotalPlaySeconds += playSeconds;
            if (died) profile.TotalDeaths++;
            if (score > profile.BestScore) profile.BestScore = score;

            GameConfig.SaveProfile();
        }

        /// <summary>Sends the player back to level 1 while keeping their identity and stats.</summary>
        public static void ClearSave() => GameConfig.ResetProgress();

        // ── Legacy migration ───────────────────────────────────────────────
        private static bool _migrationChecked;

        private static void MigrateLegacySaveIfNeeded()
        {
            if (_migrationChecked) return;
            _migrationChecked = true;

            try
            {
                if (!File.Exists(LegacyFile)) return;

                string content = File.ReadAllText(LegacyFile);
                if (int.TryParse(content, out int legacyLevel) && legacyLevel > 1)
                {
                    PlayerProfile profile = GameConfig.Profile;
                    if (legacyLevel > profile.CurrentLevel)
                    {
                        profile.CurrentLevel = Math.Min(legacyLevel, GameConstants.MaxLevel);
                        if (profile.CurrentLevel > profile.HighestLevelReached)
                            profile.HighestLevelReached = profile.CurrentLevel;
                        GameConfig.SaveProfile();
                        Console.WriteLine($"[Save] Migrated legacy save (level {legacyLevel}).");
                    }
                }

                File.Delete(LegacyFile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Save] Legacy migration skipped: {ex.Message}");
            }
        }
    }
}
