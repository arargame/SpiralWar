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
    /// The bullet remembers its previous position so the renderer can draw a motion streak and
    /// the collision system can do a swept test instead of a point test - at 660 px/s a point
    /// test tunnels straight through a 28 px tall ring roughly one frame in four.
    ///
    /// Criticals, piercing and acid are all resolved by <see cref="Player.BuildVolley"/> and
    /// carried here, so the collision system never has to know what the ship's loadout is.
    /// </summary>
    public sealed class Bullet : GameObject, IPoolable
    {
        public Vector2 Velocity { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        public int Damage { get; private set; }
        public Color Tint { get; private set; }

        /// <summary>True when this round rolled a critical. Drawn hotter, reported louder.</summary>
        public bool IsCritical { get; private set; }

        /// <summary>Extra rings this round may carry on through before it is spent.</summary>
        public int PierceRemaining { get; private set; }

        /// <summary>Acid stacks applied to whatever this round hits. 0 when no green diamond.</summary>
        public int AcidStacks { get; private set; }

        /// <summary>
        /// The ring this round has already passed through. A piercing bullet is physically
        /// still inside the ring it just punched, so without this it would re-hit the same ring
        /// every frame until it exits and "pierce one extra ring" would quietly mean "deal
        /// three or four times the damage to one ring".
        /// </summary>
        public SnakeSegment? IgnoredSegment { get; private set; }

        private float _age;
        private readonly Texture2D _pixel;

        public static readonly Color DefaultColor = new Color(255, 236, 120);
        public static readonly Color WideColor = new Color(150, 230, 255);
        public static readonly Color CriticalColor = new Color(255, 255, 255);
        public static readonly Color AcidColor = new Color(140, 255, 90);

        public Bullet(Texture2D pixel)
        {
            _pixel = pixel;
            Deactivate();
        }

        /// <summary>Arms a recycled bullet. Called by <see cref="Systems.BulletSystem"/>.</summary>
        public void Launch(Vector2 origin, Vector2 direction, in ShotSpec spec, Color tint)
        {
            SetPosition(origin);
            PreviousPosition = origin;
            Velocity = direction * GameConstants.BulletSpeed;

            Damage = spec.Damage;
            IsCritical = spec.IsCritical;
            PierceRemaining = spec.Pierce;
            AcidStacks = spec.AcidStacks;
            IgnoredSegment = null;

            // A critical is white-hot; an acid round is green. Both have to be readable at a
            // glance against a screen already full of neon.
            Tint = spec.IsCritical
                ? CriticalColor
                : spec.AcidStacks > 0
                    ? AcidColor
                    : tint;

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

            float scale = IsCritical ? 1.6f : 1f;
            NeonRenderer.DrawNeonBullet(spriteBatch, _pixel, Position, PreviousPosition,
                Tint, scale);
        }

        /// <summary>
        /// Spends one hit on <paramref name="segment"/>. Returns true when the round still has
        /// piercing left and should keep flying, false when it is consumed.
        /// </summary>
        public bool ConsumeHit(SnakeSegment segment)
        {
            if (PierceRemaining > 0)
            {
                PierceRemaining--;
                IgnoredSegment = segment;
                return true;
            }

            Deactivate();
            return false;
        }

        // ── IPoolable ──────────────────────────────────────────────────────
        public void OnSpawned() => Activate();

        public void OnDespawned()
        {
            Deactivate();
            Velocity = Vector2.Zero;
            Damage = 0;
            IsCritical = false;
            PierceRemaining = 0;
            AcidStacks = 0;
            IgnoredSegment = null;
            _age = 0f;
        }
    }
}
