using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>Payload for a collected supply, carrying how much hull the repair restored.</summary>
    public sealed class SupplyCollectedEventArgs : EventArgs
    {
        public SupplyType Type { get; }
        public Vector2 Position { get; }
        public int HealthRestored { get; }

        public SupplyCollectedEventArgs(SupplyType type, Vector2 position, int healthRestored)
        {
            Type = type;
            Position = position;
            HealthRestored = healthRestored;
        }
    }

    /// <summary>
    /// Spawns, moves and collects supplies.
    ///
    /// Crucially, this system does NOT roll the drop table. Whether a ring carries a supply is
    /// decided when the snake is built, and the ring wears its glyph from that moment on. The
    /// player can therefore look at the incoming spiral and decide which rings are worth
    /// shooting first - a drop rolled at the moment of death would be pure luck, which is a
    /// much weaker decision to give somebody.
    /// </summary>
    public sealed class SupplySystem
    {
        private readonly ObjectPool<Supply> _pool;
        private readonly Vector2 _corePosition;

        /// <summary>Raised when the ship collects a supply, so the screen can play audio/FX.</summary>
        public event EventHandler<SupplyCollectedEventArgs>? Collected;

        /// <summary>Raised when a supply is released by a destroyed ring.</summary>
        public event EventHandler<Supply>? Dropped;

        public int ActiveCount => _pool.ActiveCount;

        /// <summary>Supplies collected this run, for the end-of-run statistics.</summary>
        public int CollectedCount { get; private set; }

        public SupplySystem(Texture2D pixel, SpriteFont font,
                            int capacity = GameConstants.MaxSupplies)
        {
            _pool = new ObjectPool<Supply>(capacity, () => new Supply(pixel, font));
            _corePosition = new Vector2(GameConstants.ScreenWidth * 0.5f,
                                        GameConstants.ScreenHeight * 0.5f);
        }

        /// <summary>Releases the supply a destroyed ring was carrying. No-op for empty rings.</summary>
        public void ReleaseFrom(SnakeSegment segment)
        {
            if (segment == null || !segment.CarriedSupply.HasValue) return;

            Supply? supply = _pool.Rent();
            if (supply == null) return;

            supply.Spawn(segment.Position, segment.CarriedSupply.Value, segment.Velocity);
            Dropped?.Invoke(this, supply);
        }

        public void Update(GameTime gameTime, Player? player)
        {
            IReadOnlyList<Supply> active = _pool.Active;

            for (int i = 0; i < active.Count; i++)
                active[i].Update(gameTime, _corePosition);

            if (player != null && player.IsAlive)
                CheckCollection(player);

            _pool.ReclaimInactive();
        }

        private void CheckCollection(Player player)
        {
            IReadOnlyList<Supply> active = _pool.Active;

            for (int i = 0; i < active.Count; i++)
            {
                Supply supply = active[i];
                if (!supply.IsActive) continue;

                float radius = supply.CollectRadius + GameConstants.PlayerSize * 0.5f;
                if (CollisionMath.DistanceSquared(supply.Position, player.Position) > radius * radius)
                    continue;

                // Capture the type and position before Collect() retires the instance.
                SupplyType type = supply.Type;
                Vector2 position = supply.Position;

                int healed = supply.Collect(player);
                CollectedCount++;

                Collected?.Invoke(this, new SupplyCollectedEventArgs(type, position, healed));
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            IReadOnlyList<Supply> active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Draw(spriteBatch, gameTime);
        }

        public void Clear() => _pool.Clear();
    }
}
