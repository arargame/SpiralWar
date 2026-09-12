using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Pooled particle emitter. Exposes named effects rather than raw particle parameters, so
    /// the look of "a ring exploded" is defined in exactly one place and every caller gets
    /// the same thing.
    /// </summary>
    public sealed class ParticleSystem
    {
        private readonly ObjectPool<Particle> _pool;
        private readonly Texture2D _pixel;
        private readonly Random _rng = new Random();
        private readonly Vector2 _arenaCenter;

        public int ActiveCount => _pool.ActiveCount;

        public ParticleSystem(Texture2D pixel, int capacity = GameConstants.MaxParticles)
        {
            _pixel = pixel;
            _pool = new ObjectPool<Particle>(capacity, () => new Particle());
            _arenaCenter = new Vector2(GameConstants.ScreenWidth * 0.5f, GameConstants.ScreenHeight * 0.5f);
        }

        // ── Named effects ──────────────────────────────────────────────────

        /// <summary>Small spark spray where a bullet met a ring.</summary>
        public void EmitHit(Vector2 position, Vector2 impactDirection, Color color, int count = 6)
        {
            float baseAngle = MathF.Atan2(impactDirection.Y, impactDirection.X);

            for (int i = 0; i < count; i++)
            {
                Particle? p = _pool.Rent();
                if (p == null) return;

                // Sparks bounce back toward where the shot came from, in a 120 degree cone.
                float angle = baseAngle + MathF.PI + NextFloat(-1.05f, 1.05f);
                float speed = NextFloat(70f, 230f);

                p.Spawn(position,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Color.Lerp(color, Color.White, NextFloat(0.25f, 0.75f)),
                    lifeSeconds: NextFloat(0.16f, 0.34f),
                    size: NextFloat(1.2f, 2.4f),
                    endSize: 0.3f,
                    shape: ParticleShape.Spark,
                    drag: 3.4f);
            }
        }

        /// <summary>Full break-up: shards, sparks and a shockwave ring.</summary>
        public void EmitDestruction(Vector2 position, Color color, float scale = 1f)
        {
            // Shockwave.
            Particle? ring = _pool.Rent();
            ring?.Spawn(position, Vector2.Zero, Color.Lerp(color, Color.White, 0.4f),
                lifeSeconds: 0.34f,
                size: 8f * scale,
                endSize: 52f * scale,
                shape: ParticleShape.Ring,
                drag: 0f);

            // Shards from the ring's shell.
            int shardCount = (int)(9 * scale);
            for (int i = 0; i < shardCount; i++)
            {
                Particle? p = _pool.Rent();
                if (p == null) return;

                float angle = NextFloat(0f, MathF.Tau);
                float speed = NextFloat(120f, 340f) * scale;

                p.Spawn(position,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    color,
                    lifeSeconds: NextFloat(0.35f, 0.65f),
                    size: NextFloat(3.5f, 7.5f) * scale,
                    endSize: 0.8f,
                    shape: ParticleShape.Shard,
                    drag: 1.8f,
                    spin: NextFloat(-7f, 7f));
            }

            // Hot core sparks.
            int sparkCount = (int)(12 * scale);
            for (int i = 0; i < sparkCount; i++)
            {
                Particle? p = _pool.Rent();
                if (p == null) return;

                float angle = NextFloat(0f, MathF.Tau);
                float speed = NextFloat(40f, 260f) * scale;

                p.Spawn(position,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Color.Lerp(color, Color.White, NextFloat(0.4f, 0.95f)),
                    lifeSeconds: NextFloat(0.25f, 0.55f),
                    size: NextFloat(1.4f, 3.0f),
                    endSize: 0.2f,
                    shape: ParticleShape.Spark,
                    drag: 2.6f);
            }
        }

        /// <summary>Collecting a supply: a tight inward fizz in the supply's colour.</summary>
        public void EmitPickup(Vector2 position, Color color)
        {
            for (int i = 0; i < 14; i++)
            {
                Particle? p = _pool.Rent();
                if (p == null) return;

                float angle = NextFloat(0f, MathF.Tau);
                float speed = NextFloat(90f, 210f);

                p.Spawn(position,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Color.Lerp(color, Color.White, 0.55f),
                    lifeSeconds: NextFloat(0.22f, 0.45f),
                    size: NextFloat(1.4f, 2.8f),
                    endSize: 0.2f,
                    shape: ParticleShape.Spark,
                    drag: 2.2f,
                    gravityToCenter: 420f);
            }

            Particle? ring = _pool.Rent();
            ring?.Spawn(position, Vector2.Zero, color,
                lifeSeconds: 0.28f, size: 6f, endSize: 34f,
                shape: ParticleShape.Ring, drag: 0f);
        }

        /// <summary>The ship taking a hit: a red shell and debris kicked away from the impact.</summary>
        public void EmitPlayerHurt(Vector2 position, Vector2 fromDirection)
        {
            Particle? ring = _pool.Rent();
            ring?.Spawn(position, Vector2.Zero, new Color(255, 60, 60),
                lifeSeconds: 0.30f, size: 10f, endSize: 66f,
                shape: ParticleShape.Ring, drag: 0f);

            float baseAngle = MathF.Atan2(fromDirection.Y, fromDirection.X);
            for (int i = 0; i < 16; i++)
            {
                Particle? p = _pool.Rent();
                if (p == null) return;

                float angle = baseAngle + NextFloat(-1.4f, 1.4f);
                float speed = NextFloat(110f, 300f);

                p.Spawn(position,
                    new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                    Color.Lerp(new Color(255, 90, 90), Color.White, NextFloat(0f, 0.5f)),
                    lifeSeconds: NextFloat(0.25f, 0.5f),
                    size: NextFloat(1.6f, 3.4f),
                    endSize: 0.3f,
                    shape: ParticleShape.Spark,
                    drag: 2.4f);
            }
        }

        /// <summary>Muzzle flash, deliberately tiny — it must not compete with the bullet.</summary>
        public void EmitMuzzle(Vector2 position, float angle, Color color)
        {
            for (int i = 0; i < 3; i++)
            {
                Particle? p = _pool.Rent();
                if (p == null) return;

                float a = angle + NextFloat(-0.35f, 0.35f);
                float speed = NextFloat(90f, 190f);

                p.Spawn(position,
                    new Vector2(MathF.Cos(a), MathF.Sin(a)) * speed,
                    Color.Lerp(color, Color.White, 0.6f),
                    lifeSeconds: NextFloat(0.07f, 0.14f),
                    size: NextFloat(1.2f, 2.2f),
                    endSize: 0.2f,
                    shape: ParticleShape.Spark,
                    drag: 6f);
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────────────

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Update(dt, _arenaCenter);

            _pool.ReclaimInactive();
        }

        /// <summary>Draws every particle. Must run in an additive pass.</summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            var active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Draw(spriteBatch, _pixel);
        }

        public void Clear() => _pool.Clear();

        private float NextFloat(float min, float max)
            => min + (float)_rng.NextDouble() * (max - min);
    }
}
