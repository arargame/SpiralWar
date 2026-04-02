using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Supply yaratma, güncelleme ve oyuncuyla toplama kontrolü.
    /// </summary>
    public sealed class SupplySystem : IRenderable
    {
        private readonly List<Supply> _supplies = new();
        private readonly Texture2D   _pixel;
        private readonly Random      _rng = new Random();

        public SupplySystem(Texture2D pixel)
        {
            _pixel = pixel;
        }

        /// <summary>
        /// Segment ölüm pozisyonunda şans hesabıyla supply düşürür.
        /// </summary>
        public void TryDropSupply(Vector2 position)
        {
            if (_rng.NextDouble() < GameConstants.SupplyDropChance)
                _supplies.Add(new Supply(position, _pixel));
        }

        public void Update(GameTime gameTime, Player? player)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var s in _supplies)
            {
                s.Update(gameTime);
                if (player != null && s.IsActive)
                {
                    // Supply objelerini oyuncuya doğru çek
                    s.MoveTowards(player.Position, 250f, dt);
                }
            }
            _supplies.RemoveAll(s => !s.IsActive);
        }

        public void CheckCollection(Player player)
        {
            foreach (var s in _supplies)
            {
                if (!s.IsActive) continue;
                if (Vector2.Distance(s.Position, player.Position) < s.CollectRadius)
                    s.Collect(player);
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach (var s in _supplies) s.Draw(spriteBatch, gameTime);
        }
    }
}
