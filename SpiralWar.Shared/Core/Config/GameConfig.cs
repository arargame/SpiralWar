using System;

namespace SpiralWar.Core.Config
{
    /// <summary>
    /// Single entry point for persisted state. Initialized once at start-up from
    /// <see cref="SpiralWarGame"/>; every other system reads <see cref="Settings"/> and
    /// <see cref="Profile"/> from here instead of touching the file system itself.
    ///
    /// The backing store is injected, so tests (or a future cloud save) can supply their own.
    /// </summary>
    public static class GameConfig
    {
        public const string SettingsDocument = "settings";
        public const string ProfileDocument  = "profile";

        private static IConfigStore _store = new JsonConfigStore();
        private static GameSettings _settings = new GameSettings();
        private static PlayerProfile _profile = new PlayerProfile();
        private static bool _initialized;

        public static GameSettings Settings => _settings;
        public static PlayerProfile Profile => _profile;
        public static IConfigStore Store => _store;
        public static bool IsInitialized => _initialized;

        /// <summary>Raised after any Save so HUD elements can refresh.</summary>
        public static event EventHandler? Changed;

        /// <summary>
        /// Loads both documents, creating them with defaults on first run.
        /// Safe to call more than once.
        /// </summary>
        public static void Initialize(IConfigStore? store = null)
        {
            if (store != null) _store = store;

            _settings = _store.Load<GameSettings>(SettingsDocument) ?? new GameSettings();
            _settings.Sanitize();

            bool isFirstRun = !_store.Exists(ProfileDocument);
            _profile = _store.Load<PlayerProfile>(ProfileDocument) ?? new PlayerProfile();

            // Assigns the GUID on first run.
            _profile.EnsureValid();
            _profile.LastPlayedUtc = DateTime.UtcNow;

            _initialized = true;

            // Always write back so the identity file exists from the very first launch.
            SaveAll();

            if (isFirstRun)
                Console.WriteLine($"[Config] New player profile created: {_profile.PlayerId}");
        }

        public static void SaveSettings()
        {
            _settings.Sanitize();
            _store.Save(SettingsDocument, _settings);
            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void SaveProfile()
        {
            _profile.EnsureValid();
            _store.Save(ProfileDocument, _profile);
            Changed?.Invoke(null, EventArgs.Empty);
        }

        public static void SaveAll()
        {
            _settings.Sanitize();
            _profile.EnsureValid();
            _store.Save(SettingsDocument, _settings);
            _store.Save(ProfileDocument, _profile);
            Changed?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Resets progression but keeps the identity (PlayerId, creation date, lifetime stats).
        /// This is what the NEW GAME button does.
        /// </summary>
        public static void ResetProgress()
        {
            _profile.CurrentLevel = 1;
            SaveProfile();
        }
    }
}
