using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;
using SpiralWar.Core.Pooling;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// A homing missile, granted by the "M" supply.
    ///
    /// It steers rather than snapping: the turn rate is capped at
    /// <see cref="GameConstants.MissileTurnRate"/>, so a missile can genuinely miss a ring that
    /// crosses in front of it and has to loop back. That is the point - a weapon that cannot
    /// miss is a weapon the player stops aiming, and the whole game is aiming.
    ///
    /// On impact it deals its own damage plus a small splash, which is what makes it the
    /// natural answer to a cluster the player cannot reach with a straight line.
    /// </summary>
    public sealed class Missile : GameObject, IPoolable
    {
        public Vector2 Velocity { get; private set; }
        public Vector2 PreviousPosition { get; private set; }
        public float Heading { get; private set; }
        public int Damage { get; private set; }

        private float _age;
        private float _wobble;
        private readonly Texture2D _pixel;

        private static readonly Color MissileColor = new Color(130, 200, 255);

        public Missile(Texture2D pixel)
        {
            _pixel = pixel;
            Deactivate();
        }

        public void Launch(Vector2 origin, float heading)
        {
            SetPosition(origin);
            PreviousPosition = origin;
            Heading = heading;
            Velocity = new Vector2(MathF.Cos(heading), MathF.Sin(heading)) * GameConstants.MissileSpeed;
            Damage = GameConstants.MissileDamage;
            _age = 0f;
            _wobble = 0f;
            Activate();
        }

        /// <summary>
        /// Steers toward <paramref name="target"/> within the turn rate limit, then advances.
        /// Pass null when there is nothing left to chase; the missile flies straight and times
        /// out on its own.
        /// </summary>
        public void Update(GameTime gameTime, Vector2? target)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _age += dt;
            if (_age >= GameConstants.MissileLifetime)
            {
                Deactivate();
                return;
            }

            if (target.HasValue)
            {
                Vector2 toTarget = target.Value - Position;
                if (toTarget.LengthSquared() > 1f)
                {
                    float desired = MathF.Atan2(toTarget.Y, toTarget.X);
                    float delta = WrapAngle(desired - Heading);
                    float maxTurn = GameConstants.MissileTurnRate * dt;

                    if (delta > maxTurn) delta = maxTurn;
                    else if (delta < -maxTurn) delta = -maxTurn;

                    Heading = WrapAngle(Heading + delta);
                    Velocity = new Vector2(MathF.Cos(Heading), MathF.Sin(Heading)) *
                               GameConstants.MissileSpeed;
                }
            }

            PreviousPosition = Position;
            SetPosition(Position + Velocity * dt);
            _wobble += dt * 22f;

            const float margin = 140f;
            if (Position.X < -margin || Position.X > GameConstants.ScreenWidth + margin ||
                Position.Y < -margin || Position.Y > GameConstants.ScreenHeight + margin)
                Deactivate();
        }

        public override void Update(GameTime gameTime) => Update(gameTime, null);

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;
            NeonRenderer.DrawNeonMissile(spriteBatch, _pixel, Position, Heading,
                MissileColor, _wobble);
        }

        public void Consume() => Deactivate();

        private static float WrapAngle(float radians)
        {
            while (radians > MathF.PI) radians -= MathF.Tau;
            while (radians < -MathF.PI) radians += MathF.Tau;
            return radians;
        }

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
