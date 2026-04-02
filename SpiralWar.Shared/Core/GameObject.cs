using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Core
{
    /// <summary>
    /// Tüm oyun nesnelerinin temel soyut sınıfı.
    /// Position, IsActive durumunu yönetir; Update/Draw zorunludur.
    /// </summary>
    public abstract class GameObject : IUpdatable, IRenderable
    {
        public Vector2 Position { get; protected set; }
        public bool IsActive { get; protected set; } = true;

        public virtual void SetPosition(Vector2 position) => Position = position;
        public virtual void SetPosition(float x, float y) => Position = new Vector2(x, y);

        public virtual void Activate()   => IsActive = true;
        public virtual void Deactivate() => IsActive = false;

        public abstract void Update(GameTime gameTime);
        public abstract void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }
}
