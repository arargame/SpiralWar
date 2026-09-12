using System;
using Microsoft.Xna.Framework;
using SpiralWar.Core.Config;


namespace SpiralWar.Systems
{
    /// <summary>
    /// Trauma-based screen shake.
    ///
    /// Impacts add "trauma" (0..1) rather than setting an amplitude directly. The offset is
    /// trauma SQUARED, so small hits barely register while a destroyed ring lands hard, and
    /// stacking many small hits never snowballs into an unreadable screen. Trauma decays
    /// linearly, which feels sharper than an exponential falloff.
    ///
    /// Respects the player's ScreenShakeScale setting, including 0 for players who find shake
    /// uncomfortable.
    /// </summary>
    public sealed class CameraShake
    {
        private float _trauma;
        private float _time;

        /// <summary>Current offset in virtual pixels. Applied to world passes only.</summary>
        public Vector2 Offset { get; private set; }

        /// <summary>Maximum displacement at full trauma.</summary>
        public float MaxOffset { get; set; } = 14f;

        /// <summary>Trauma lost per second.</summary>
        public float DecayPerSecond { get; set; } = 1.8f;

        public float Trauma => _trauma;

        /// <summary>Adds trauma. Values are clamped so a chain of kills cannot blind the player.</summary>
        public void Add(float amount)
        {
            if (amount <= 0f) return;

            // The incoming amount is expressed in "shake units" for readability at the call
            // site (see GameConstants.ShakeOn*); convert to 0..1 trauma here.
            _trauma += amount / 20f;
            if (_trauma > 1f) _trauma = 1f;
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _time += dt;

            if (_trauma <= 0f)
            {
                Offset = Vector2.Zero;
                return;
            }

            _trauma -= DecayPerSecond * dt;
            if (_trauma < 0f) _trauma = 0f;

            float userScale = GameConfig.IsInitialized
                ? GameConfig.Settings.ScreenShakeScale
                : 1f;

            if (userScale <= 0.001f)
            {
                Offset = Vector2.Zero;
                return;
            }

            float amplitude = _trauma * _trauma * MaxOffset * userScale;

            // Two decorrelated noise channels; using time-based sine sums keeps the motion
            // smooth instead of the jittery look of per-frame random offsets.
            float x = Noise(_time * 34f, 0f);
            float y = Noise(_time * 29f, 17.3f);

            Offset = new Vector2(x, y) * amplitude;
        }

        /// <summary>Instantly stops shaking. Used on screen transitions.</summary>
        public void Reset()
        {
            _trauma = 0f;
            Offset = Vector2.Zero;
        }

        private static float Noise(float t, float seed)
        {
            return MathF.Sin(t + seed) * 0.6f +
                   MathF.Sin(t * 2.37f + seed * 1.7f) * 0.3f +
                   MathF.Sin(t * 4.91f + seed * 2.3f) * 0.1f;
        }
    }
}
