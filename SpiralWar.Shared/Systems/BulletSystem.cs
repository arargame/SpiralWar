using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Core.Pooling;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Owns the bullet pool and nothing else (SRP).
    ///
    /// Every bullet in the game is pre-allocated at construction. Firing is then a stack pop
    /// and a field write, so a seven-shot diffused volley at 5 volleys a second produces zero
    /// garbage — which is what keeps the frame time flat during the heaviest moments instead
    /// of spiking exactly when the player can least afford it.
    /// </summary>
    public sealed class BulletSystem : IUpdatable, IRenderable
    {
        private readonly ObjectPool<Bullet> _pool;

        public IReadOnlyList<Bullet> Bullets => _pool.Active;
        public int ActiveCount => _pool.ActiveCount;
        public int Capacity => _pool.Capacity;

        /// <summary>Total shots fired this run, for the end-of-run statistics.</summary>
        public int ShotsFired { get; private set; }

        public BulletSystem(Texture2D pixel, int capacity = GameConstants.MaxBullets)
        {
            _pool = new ObjectPool<Bullet>(capacity, () => new Bullet(pixel));
        }

        /// <summary>
        /// Fires one bullet. Returns false when the pool is exhausted, in which case the shot
        /// is silently dropped — a missing bullet at 512 concurrent projectiles is invisible,
        /// a mid-frame allocation is not.
        /// </summary>
        public bool Spawn(Vector2 origin, Vector2 direction, int damage, Color tint)
        {
            Bullet? bullet = _pool.Rent();
            if (bullet == null) return false;

            bullet.Launch(origin, direction, damage, tint);
            ShotsFired++;
            return true;
        }

        public void Update(GameTime gameTime)
        {
            IReadOnlyList<Bullet> active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Update(gameTime);

            _pool.ReclaimInactive();
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            IReadOnlyList<Bullet> active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Draw(spriteBatch, gameTime);
        }

        public void Clear() => _pool.Clear();
    }
}
