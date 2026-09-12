namespace SpiralWar.Audio
{
    /// <summary>
    /// What gameplay code is allowed to know about audio. Entities and systems depend on
    /// this interface only, so the synthesizer can be replaced by streamed assets later
    /// without touching a single gameplay file (DIP).
    /// </summary>
    public interface IAudioService
    {
        /// <summary>Builds/loads every sound. Safe to call once.</summary>
        void Initialize();

        /// <summary>
        /// Plays a one-shot.
        /// </summary>
        /// <param name="id">Which sound.</param>
        /// <param name="volumeScale">0..1 multiplier applied on top of the user's SFX volume.</param>
        /// <param name="pitch">-1..1, 0 is the recorded pitch.</param>
        /// <param name="pan">-1 (left) .. 1 (right).</param>
        void Play(SoundId id, float volumeScale = 1f, float pitch = 0f, float pan = 0f);

        /// <summary>
        /// Plays a one-shot with a small random pitch offset, which is what stops repeated
        /// shots from turning into a harsh machine-gun buzz.
        /// </summary>
        void PlayVaried(SoundId id, float volumeScale = 1f, float pitchSpread = 0.08f, float pan = 0f);

        /// <summary>Re-reads the volumes from the settings document.</summary>
        void ApplySettings();

        /// <summary>Stops everything (screen change, app suspend).</summary>
        void StopAll();
    }
}
