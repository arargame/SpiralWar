using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>Payload describing a missile that reached something.</summary>
    public sealed class MissileImpactEventArgs : EventArgs
    {
        public Vector2 Position { get; }
        public SnakeSegment Segment { get; }
        public int DirectDamage { get; }
        public int SplashKills { get; }

        public MissileImpactEventArgs(Vector2 position, SnakeSegment segment,
                                      int directDamage, int splashKills)
        {
            Position = position;
            Segment = segment;
            DirectDamage = directDamage;
            SplashKills = splashKills;
        }
    }

    /// <summary>
    /// Owns the homing missile pool, their steering and their detonation.
    ///
    /// Missiles are kept deliberately slow (430 px/s against a bullet's 660) and their turn
    /// rate is capped, so they are a tool for reaching a cluster the player cannot line up on
    /// rather than an auto-aim that plays the game for them. They retarget every frame, which
    /// matters on a spiral: the ring a missile was chasing is frequently dead by the time it
    /// arrives, and a missile that then flew on stupidly would look broken.
    /// </summary>
    public sealed class MissileSystem
    {
        private readonly ObjectPool<Missile> _pool;
        private readonly List<SnakeSegment> _splashBuffer = new List<SnakeSegment>(32);

        /// <summary>Raised when a missile connects, after its damage has been applied.</summary>
        public event EventHandler<MissileImpactEventArgs>? Impact;

        public int ActiveCount => _pool.ActiveCount;

        /// <summary>Missiles launched this level. Statistics only.</summary>
        public int Launched { get; private set; }

        public MissileSystem(Texture2D pixel, int capacity = GameConstants.MaxMissiles)
        {
            _pool = new ObjectPool<Missile>(capacity, () => new Missile(pixel));
        }

        /// <summary>
        /// Launches one missile. The heading is offset from the ship's aim so a stream of them
        /// fans out instead of flying in single file.
        /// </summary>
        public bool Launch(Vector2 origin, float heading)
        {
            Missile? missile = _pool.Rent();
            if (missile == null) return false;

            missile.Launch(origin, heading);
            Launched++;
            return true;
        }

        public void Update(GameTime gameTime, Snake? snake)
        {
            IReadOnlyList<Missile> active = _pool.Active;

            for (int i = 0; i < active.Count; i++)
            {
                Missile missile = active[i];
                if (!missile.IsActive) continue;

                SnakeSegment? target = snake?.FindNearestSegment(missile.Position);
                missile.Update(gameTime, target?.Position);

                if (!missile.IsActive) continue;
                if (snake == null) continue;

                TryDetonate(missile, snake);
            }

            _pool.ReclaimInactive();
        }

        private void TryDetonate(Missile missile, Snake snake)
        {
            // A missile is a fat projectile, so a simple radius test against ring centres is
            // both sufficient and cheaper than the swept box test bullets need.
            SnakeSegment? hit = null;
            float bestDistance = float.MaxValue;

            snake.CollectSegmentsInRadius(missile.Position, GameConstants.MissileBlastRadius * 0.45f,
                _splashBuffer);

            for (int i = 0; i < _splashBuffer.Count; i++)
            {
                float distance = Vector2.DistanceSquared(_splashBuffer[i].Position, missile.Position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                hit = _splashBuffer[i];
            }

            if (hit == null) return;

            int direct = hit.TakeDamage(missile.Damage);

            // Splash, excluding the ring that took the direct hit.
            snake.CollectSegmentsInRadius(missile.Position, GameConstants.MissileBlastRadius,
                _splashBuffer);

            int splashKills = 0;
            for (int i = 0; i < _splashBuffer.Count; i++)
            {
                SnakeSegment segment = _splashBuffer[i];
                if (ReferenceEquals(segment, hit) || !segment.IsAlive) continue;

                segment.TakeDamage(GameConstants.MissileBlastDamage);
                if (!segment.IsAlive) splashKills++;
            }

            Vector2 position = missile.Position;
            missile.Consume();

            Impact?.Invoke(this, new MissileImpactEventArgs(position, hit, direct, splashKills));
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            IReadOnlyList<Missile> active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Draw(spriteBatch, gameTime);
        }

        public void Clear()
        {
            _pool.Clear();
            _splashBuffer.Clear();
        }
    }
}
