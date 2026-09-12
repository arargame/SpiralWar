using System;
using Microsoft.Xna.Framework.Audio;
using SpiralWar.Core.Config;

namespace SpiralWar.Audio
{
    /// <summary>
    /// Runtime side of the audio system: owns the synthesized buffers, applies the user's
    /// volume settings and — importantly — limits how often each sound may retrigger.
    ///
    /// Voice throttling is the difference between "satisfying" and "unbearable". A diffused
    /// volley fires seven bullets in one frame; without the per-sound minimum interval that
    /// would be seven stacked copies of the same waveform, which is both 7x too loud and
    /// phase-cancels into a harsh click.
    /// </summary>
    public sealed class AudioManager : IAudioService
    {
        private readonly SoundEffect?[] _sounds = new SoundEffect?[(int)SoundId.Count];

        /// <summary>Minimum seconds between two plays of the same sound.</summary>
        private readonly float[] _minInterval = new float[(int)SoundId.Count];

        /// <summary>Wall-clock timestamp of the last play, per sound.</summary>
        private readonly double[] _lastPlayed = new double[(int)SoundId.Count];

        private readonly Random _rng = new Random();
        private bool _initialized;
        private float _sfxVolume = 0.75f;

        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                SoundEffect[] built = SoundBank.BuildAll();
                for (int i = 0; i < _sounds.Length && i < built.Length; i++)
                    _sounds[i] = built[i];

                ConfigureThrottles();
                ApplySettings();
                _initialized = true;
            }
            catch (Exception ex)
            {
                // A machine without an audio device must still be able to play the game.
                Console.WriteLine($"[Audio] Initialization failed, continuing muted: {ex.Message}");
                _initialized = false;
            }
        }

        private void ConfigureThrottles()
        {
            // Default: allow rapid retrigger.
            for (int i = 0; i < _minInterval.Length; i++)
                _minInterval[i] = 0f;

            _minInterval[(int)SoundId.Shoot]           = 0.045f; // ~22 shots/s ceiling
            _minInterval[(int)SoundId.ShootWide]       = 0.060f;
            _minInterval[(int)SoundId.SegmentHit]      = 0.022f; // several hits read as one texture
            _minInterval[(int)SoundId.SegmentArmorHit] = 0.030f;
            _minInterval[(int)SoundId.SegmentDestroy]  = 0.040f;
            _minInterval[(int)SoundId.SupplyDrop]      = 0.070f;
            _minInterval[(int)SoundId.PlayerHurt]      = 0.220f;
            _minInterval[(int)SoundId.MenuMove]        = 0.050f;
        }

        public void ApplySettings()
        {
            GameSettings settings = GameConfig.Settings;
            float master = settings.Muted ? 0f : settings.MasterVolume;

            _sfxVolume = master * settings.SfxVolume;

            try
            {
                SoundEffect.MasterVolume = Clamp01(master);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Audio] Volume update failed: {ex.Message}");
            }
        }

        public void Play(SoundId id, float volumeScale = 1f, float pitch = 0f, float pan = 0f)
        {
            if (!_initialized) return;

            int index = (int)id;
            if (index < 0 || index >= _sounds.Length) return;

            SoundEffect? sound = _sounds[index];
            if (sound == null) return;

            float volume = Clamp01(_sfxVolume * volumeScale);
            if (volume <= 0.001f) return;

            // Throttle: silently drop a retrigger that arrives too soon.
            double now = NowSeconds();
            if (now - _lastPlayed[index] < _minInterval[index]) return;
            _lastPlayed[index] = now;

            try
            {
                sound.Play(volume, Clamp(pitch, -1f, 1f), Clamp(pan, -1f, 1f));
            }
            catch (Exception)
            {
                // Voice limit reached at the platform level — dropping the sound is correct.
            }
        }

        public void PlayVaried(SoundId id, float volumeScale = 1f, float pitchSpread = 0.08f, float pan = 0f)
        {
            float pitch = (float)(_rng.NextDouble() * 2.0 - 1.0) * pitchSpread;
            Play(id, volumeScale, pitch, pan);
        }

        public void StopAll()
        {
            // One-shots created via SoundEffect.Play are managed by the platform; there is
            // no handle to stop them individually. Muting the master bus is the honest
            // equivalent and is restored by ApplySettings.
            try { SoundEffect.MasterVolume = 0f; }
            catch (Exception) { /* no audio device */ }
        }

        /// <summary>
        /// Converts a world X position into a stereo pan value so hits on the left of the
        /// arena are heard on the left.
        /// </summary>
        public static float PanFor(float virtualX)
        {
            float half = GameConstants.ScreenWidth * 0.5f;
            float pan = (virtualX - half) / half;
            return Clamp(pan, -1f, 1f) * 0.65f; // 0.65 keeps it subtle rather than gimmicky
        }

        private static double NowSeconds() =>
            Environment.TickCount64 / 1000.0;

        private static float Clamp01(float v) => Clamp(v, 0f, 1f);

        private static float Clamp(float v, float min, float max)
            => v < min ? min : (v > max ? max : v);
    }
}
