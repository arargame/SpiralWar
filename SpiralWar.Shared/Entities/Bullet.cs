using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Core.Pooling;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// A single projectile. Pooled: instances are created once at start-up and recycled
    /// forever after, so a held trigger produces zero allocations and therefore zero GC
    /// hitches during a run.
    ///
    /// The bullet remembers its previous position so the renderer can draw a motion streak
    /// and the collision system can do a swept test instead of a point test — at 660 px/s a
    /// point test tunnels straight through a 28 px tall ring roughly one frame in four.
    /// </summary>
    public sealed class Bullet : GameObject, IPoolable
    {
        public Vector2 Velocity { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        public int Damage { get; private set; }
        public Color Tint { get; private set; }

        private float _age;
        private readonly Texture2D _pixel;

        public static readonly Color DefaultColor = new Color(255, 236, 120);
        public static readonly Color WideColor = new Color(150, 230, 255);

        public Bullet(Texture2D pixel)
        {
            _pixel = pixel;
            Deactivate();
        }

        /// <summary>Arms a recycled bullet. Called by <see cref="Systems.BulletSystem"/>.</summary>
        public void Launch(Vector2 origin, Vector2 direction, int damage, Color tint)
        {
            SetPosition(origin);
            PreviousPosition = origin;
            Velocity = direction * GameConstants.BulletSpeed;
            Damage = damage;
            Tint = tint;
            _age = 0f;
            Activate();
        }

        public override void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            PreviousPosition = Position;
            SetPosition(Position + Velocity * dt);

            _age += dt;
            if (_age >= GameConstants.BulletLifetime)
            {
                Deactivate();
                return;
            }

            const float margin = 70f;
            if (Position.X < -margin || Position.X > GameConstants.ScreenWidth + margin ||
                Position.Y < -margin || Position.Y > GameConstants.ScreenHeight + margin)
                Deactivate();
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;
            NeonRenderer.DrawNeonBullet(spriteBatch, _pixel, Position, PreviousPosition, Tint);
        }

        /// <summary>Marks the bullet spent. Called on impact.</summary>
        public void Consume() => Deactivate();

        // ── IPoolable ──────────────────────────────────────────────────────
        public void OnSpawned() => Activate();

        public void OnDespawned()
        {
            Deactivate();
            Velocity = Vector2.Zero;
            Damage = 0;
            _age = 0f;
        }
    }
}
