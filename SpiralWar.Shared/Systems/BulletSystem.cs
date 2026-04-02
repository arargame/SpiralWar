using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Entities;
using SpiralWar.Core;

namespace SpiralWar.Systems
{
    /// <summary>
    /// Bullet üretimi, güncellemesi ve temizliğini yönetir.
    /// SRP: Sadece bullet yaşam döngüsünden sorumludur.
    /// </summary>
    public sealed class BulletSystem : IUpdatable, IRenderable
    {
        private readonly List<Bullet> _bullets = new();
        private readonly Texture2D    _pixel;

        public IReadOnlyList<Bullet> Bullets => _bullets;

        public BulletSystem(Texture2D pixel)
        {
            _pixel = pixel;
        }

        public void SpawnBullet(Vector2 origin, Vector2 direction, int damage)
        {
            _bullets.Add(new Bullet(origin, direction, damage, _pixel));
        }

        public void Update(GameTime gameTime)
        {
            foreach (var b in _bullets) b.Update(gameTime);
            _bullets.RemoveAll(b => !b.IsActive);
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach (var b in _bullets) b.Draw(spriteBatch, gameTime);
        }
    }
}
