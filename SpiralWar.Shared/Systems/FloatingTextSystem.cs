using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;
using SpiralWar.Entities;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Pooled damage readouts and pickup toasts.
    ///
    /// Numbers are offset sideways by a small random amount because a burst of hits on the
    /// same ring would otherwise render several identical strings on top of each other, which
    /// reads as one bold number rather than as several hits.
    /// </summary>
    public sealed class FloatingTextSystem
    {
        private readonly ObjectPool<FloatingText> _pool;
        private readonly Random _rng = new Random();

        public FloatingTextSystem(int capacity = GameConstants.MaxFloatingText)
        {
            _pool = new ObjectPool<FloatingText>(capacity, () => new FloatingText());
        }

        /// <summary>Damage number over a ring that was hit.</summary>
        public void ShowDamage(Vector2 position, int amount, Color color)
        {
            FloatingText? text = _pool.Rent();
            if (text == null) return;

            text.Spawn(amount.ToString(),
                position + new Vector2(NextFloat(-9f, 9f), NextFloat(-6f, 4f)),
                color,
                lifeSeconds: 0.62f,
                scale: 0.85f,
                riseSpeed: 54f);

            text.SetDrift(NextFloat(-26f, 26f));
        }

        /// <summary>Bigger, slower label — pickups, milestones, warnings.</summary>
        public void ShowLabel(Vector2 position, string label, Color color, float scale = 1f)
        {
            FloatingText? text = _pool.Rent();
            if (text == null) return;

            text.Spawn(label, position, color,
                lifeSeconds: 1.05f,
                scale: scale,
                riseSpeed: 34f);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Update(dt);

            _pool.ReclaimInactive();
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            var active = _pool.Active;
            for (int i = 0; i < active.Count; i++)
                active[i].Draw(spriteBatch, font);
        }

        public void Clear() => _pool.Clear();

        private float NextFloat(float min, float max)
            => min + (float)_rng.NextDouble() * (max - min);
    }
}
