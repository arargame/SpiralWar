using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>Payload for a collected diamond, carrying the stack level it produced.</summary>
    public sealed class DiamondCollectedEventArgs : EventArgs
    {
        public DiamondType Type { get; }
        public Vector2 Position { get; }
        public int Level { get; }

        /// <summary>True when this pickup raised the stack rather than only refreshing it.</summary>
        public bool Levelled { get; }

        public DiamondCollectedEventArgs(DiamondType type, Vector2 position, int level, bool levelled)
        {
            Type = type;
            Position = position;
            Level = level;
            Levelled = levelled;
        }
    }

    /// <summary>
    /// Spawns, moves and collects neon diamonds.
    ///
    /// Structurally this is the twin of <see cref="SupplySystem"/>, and for the same reason:
    /// whether a ring carries a diamond is decided when the snake is built, so the ring wears
    /// the stone's glyph from the moment it appears and the player can choose to dig it out.
    /// </summary>
    public sealed class DiamondSystem
    {
        private readonly ObjectPool<Diamond> _pool;
        private readonly Vector2 _corePosition;

        /// <summary>Raised when the ship collects a diamond.</summary>
        public event EventHandler<DiamondCollectedEventArgs>? Collected;

        /// <summary>Raised when a destroyed ring releases a diamond.</summary>
        public event EventHandler<Diamond>? Dropped;

        public int ActiveCount => _pool.ActiveCount;

        /// <summary>Diamonds collected this run, for the end-of-run statistics.</summary>
        public int CollectedCount { get; private set; }

        public DiamondSystem(Texture2D pixel, SpriteFont font,
                             int capacity = GameConstants.MaxDiamonds)
        {
            _pool = new ObjectPool<Diamond>(capacity, () => new Diamond(pixel, font));
            _corePosition = new Vector2(GameConstants.ScreenWidth * 0.5f,
                                        GameConstants.ScreenHeight * 0.5f);
        }

        /// <summary>Releases the diamond a destroyed ring was carrying. No-op for empty rings.</summary>
        public void ReleaseFrom(SnakeSegment segment)
        {
            if (segment == null || !segment.CarriedDiamond.HasValue) return;

            Diamond? diamond = _pool.Rent();
            if (diamond == null) return;

            diamond.Spawn(segment.Position, segment.CarriedDiamond.Value, segment.Velocity);
            Dropped?.Invoke(this, diamond);
        }

        public void Update(GameTime gameTime, Player? player)
        {
            IReadOnlyList<Diamond> active = _pool.Active;

            for (int i = 0; i < active.Count; i++)
                active[i].Update(gameTime, _corePosition);

            if (player != null && player.IsAlive)
                CheckCollection(player);

            _pool.ReclaimInactive();
        }

        private void CheckCollection(Player player)
        {
            IReadOnlyList<Diamond> active = _pool.Active;

            for (int i = 0; i < active.Count; i++)
            {
                Diamond diamond = active[i];
                if (!diamond.IsActive) continue;

                float radius = diamond.CollectRadius + GameConstants.PlayerSize * 0.5f;
                if (CollisionMath.DistanceSquared(diamond.Position, player.Position) > radius * radius)
                    continue;

                DiamondType type = diamond.Type;
                Vector2 position = diamond.Position;
                int before = player.Loadout.DiamondLevel(type);

                int level = diamond.Collect(player);
                CollectedCount++;

                Collected?.Invoke(this,
                    new DiamondCollectedEventArgs(type, position, level, level > before));
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            IReadOnlyList<Diamond> active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Draw(spriteBatch, gameTime);
        }

        public void Clear() => _pool.Clear();
    }
}
