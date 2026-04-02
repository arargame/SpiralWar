using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Doğrusal hareket eden mermi. Sınır dışına çıkınca deaktive olur.
    /// </summary>
    public sealed class Bullet : Entity
    {
        public Vector2 Velocity { get; private set; }
        public int     Damage   { get; private set; }

        private readonly Texture2D _pixel;
        private static readonly Color BulletColor = new Color(255, 240, 80);

        public Bullet(Vector2 position, Vector2 direction, int damage, Texture2D pixel)
            : base(1)
        {
            _pixel   = pixel;
            Damage   = damage;
            Velocity = direction * GameConstants.BulletSpeed;
            SetPosition(position);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            SetPosition(Position + Velocity * dt);

            if (Position.X < -60 || Position.X > GameConstants.ScreenWidth  + 60 ||
                Position.Y < -60 || Position.Y > GameConstants.ScreenHeight + 60)
                Deactivate();
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            NeonRenderer.DrawNeonBullet(spriteBatch, _pixel, Position, BulletColor);
        }
    }
}
