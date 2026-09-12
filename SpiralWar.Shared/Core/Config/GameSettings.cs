namespace SpiralWar.Core.Config
{
    /// <summary>
    /// User adjustable options. Serialized to settings.json next to the player profile.
    /// Everything here is a plain value so the type stays trivially serializable.
    /// </summary>
    public sealed class GameSettings
    {
        /// <summary>Schema version so future migrations can be detected.</summary>
        public int Version { get; set; } = 1;

        // ── Audio ──────────────────────────────────────────────────────────
        public float MasterVolume { get; set; } = 0.85f;
        public float SfxVolume { get; set; } = 0.75f;
        public float MusicVolume { get; set; } = 0.55f;
        public bool Muted { get; set; }

        // ── Video ──────────────────────────────────────────────────────────
        public int WindowWidth { get; set; } = GameConstants.ScreenWidth;
        public int WindowHeight { get; set; } = GameConstants.ScreenHeight;
        public bool FullScreen { get; set; }
        public bool VSync { get; set; } = true;

        // ── Accessibility / feel ───────────────────────────────────────────
        /// <summary>Scales camera shake. 0 disables it entirely.</summary>
        public float ScreenShakeScale { get; set; } = 1f;

        /// <summary>Scales the additive bloom passes. Lower it on weak GPUs.</summary>
        public float GlowScale { get; set; } = 1f;

        /// <summary>Draws the floating damage numbers over hit segments.</summary>
        public bool ShowDamageNumbers { get; set; } = true;

        // ── Misc ───────────────────────────────────────────────────────────
        public string Language { get; set; } = "en";

        /// <summary>False until the player has seen the first-run flow at least once.</summary>
        public bool HasSeenIntro { get; set; }

        /// <summary>Clamps every field into a valid range after loading a foreign file.</summary>
        public void Sanitize()
        {
            MasterVolume = Clamp01(MasterVolume);
            SfxVolume = Clamp01(SfxVolume);
            MusicVolume = Clamp01(MusicVolume);
            ScreenShakeScale = Clamp(ScreenShakeScale, 0f, 2f);
            GlowScale = Clamp(GlowScale, 0.25f, 2f);

            if (WindowWidth < 640) WindowWidth = GameConstants.ScreenWidth;
            if (WindowHeight < 360) WindowHeight = GameConstants.ScreenHeight;
            if (string.IsNullOrWhiteSpace(Language)) Language = "en";
        }

        private static float Clamp01(float v) => Clamp(v, 0f, 1f);

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}
