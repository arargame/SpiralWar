using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Rendering
{
    /// <summary>
    /// Slow rotating star layer behind the arena.
    ///
    /// The stars orbit the core rather than scrolling sideways, which quietly reinforces the
    /// one idea the whole game is about: everything is being pulled into the middle. Three
    /// depth layers move at different rates so the background has real parallax instead of
    /// looking like a flat texture.
    /// </summary>
    public sealed class Starfield
    {
        private struct Star
        {
            public float Angle;       // current angle around the core
            public float Radius;      // distance from the core
            public float Speed;       // radians per second
            public float Size;
            public float Brightness;
            public float TwinklePhase;
            public float TwinkleRate;
        }

        private readonly Star[] _stars;
        private readonly Vector2 _center;
        private float _time;

        public Starfield(int count, int screenWidth, int screenHeight, int seed = 7)
        {
            var rng = new Random(seed);
            _center = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);
            _stars = new Star[Math.Max(1, count)];

            float maxRadius = MathF.Sqrt(_center.X * _center.X + _center.Y * _center.Y) + 80f;

            for (int i = 0; i < _stars.Length; i++)
            {
                // Three parallax bands: far/dim/slow to near/bright/fast.
                int band = i % 3;
                float bandScale = 0.45f + band * 0.32f;

                _stars[i] = new Star
                {
                    Angle = (float)(rng.NextDouble() * Math.Tau),
                    Radius = (float)(rng.NextDouble()) * maxRadius,
                    Speed = (0.012f + (float)rng.NextDouble() * 0.030f) * bandScale,
                    Size = 0.6f + band * 0.55f,
                    Brightness = 0.10f + (float)rng.NextDouble() * 0.22f * bandScale,
                    TwinklePhase = (float)(rng.NextDouble() * Math.Tau),
                    TwinkleRate = 0.6f + (float)rng.NextDouble() * 2.2f
                };
            }
        }

        public void Update(float deltaTime)
        {
            _time += deltaTime;
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].Angle += _stars[i].Speed * deltaTime;
        }

        /// <summary>
        /// Draws the field. Must run in an additive pass.
        /// </summary>
        /// <param name="tint">Tier accent colour, so the sky shifts with progression.</param>
        /// <param name="intensity">0 dims the field out entirely (used during transitions).</param>
        public void Draw(SpriteBatch sb, Texture2D pixel, Color tint, float intensity = 1f)
        {
            if (intensity <= 0.01f) return;

            for (int i = 0; i < _stars.Length; i++)
            {
                Star s = _stars[i];

                float twinkle = MathF.Sin(_time * s.TwinkleRate + s.TwinklePhase) * 0.35f + 0.65f;
                float alpha = s.Brightness * twinkle * intensity;
                if (alpha <= 0.01f) continue;

                var pos = new Vector2(
                    _center.X + MathF.Cos(s.Angle) * s.Radius,
                    _center.Y + MathF.Sin(s.Angle) * s.Radius);

                Color color = Color.Lerp(Color.White, tint, 0.55f) * alpha;

                if (s.Size <= 1f)
                    sb.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, 1, 1), color);
                else
                    PrimitiveDrawer.DrawFilledCircle(sb, pixel, pos, s.Size, color);
            }
        }
    }
}
