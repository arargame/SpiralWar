using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>Visual style of a pooled particle.</summary>
    public enum ParticleShape
    {
        /// <summary>Round glow dot. Sparks, embers, pickup fizz.</summary>
        Spark,

        /// <summary>Short oriented line. Debris flying off a shattered ring.</summary>
        Shard,

        /// <summary>Expanding hollow circle. Shockwaves.</summary>
        Ring
    }

    /// <summary>
    /// One pooled particle. Deliberately a plain class rather than a GameObject: there are up
    /// to 900 of these and the virtual Update/Draw dispatch of the entity hierarchy buys
    /// nothing here.
    /// </summary>
    public sealed class Particle : IPoolable
    {
        public bool IsActive { get; private set; }

        private Vector2 _position;
        private Vector2 _velocity;
        private float _drag;
        private float _life;
        private float _maxLife;
        private float _size;
        private float _endSize;
        private float _rotation;
        private float _spin;
        private Color _color;
        private ParticleShape _shape;
        private float _gravityToCenter;

        /// <summary>Arms a recycled particle.</summary>
        public void Spawn(Vector2 position, Vector2 velocity, Color color,
                          float lifeSeconds, float size, float endSize,
                          ParticleShape shape, float drag = 2.2f,
                          float spin = 0f, float gravityToCenter = 0f)
        {
            _position = position;
            _velocity = velocity;
            _color = color;
            _life = lifeSeconds;
            _maxLife = lifeSeconds <= 0f ? 0.001f : lifeSeconds;
            _size = size;
            _endSize = endSize;
            _shape = shape;
            _drag = drag;
            _spin = spin;
            _rotation = MathF.Atan2(velocity.Y, velocity.X);
            _gravityToCenter = gravityToCenter;
            IsActive = true;
        }

        public void Update(float deltaTime, Vector2 arenaCenter)
        {
            if (!IsActive) return;

            _life -= deltaTime;
            if (_life <= 0f)
            {
                IsActive = false;
                return;
            }

            if (_gravityToCenter != 0f)
            {
                Vector2 toCenter = arenaCenter - _position;
                float dist = toCenter.Length();
                if (dist > 0.01f)
                    _velocity += toCenter / dist * _gravityToCenter * deltaTime;
            }

            _position += _velocity * deltaTime;
            _velocity -= _velocity * MathF.Min(1f, _drag * deltaTime);
            _rotation += _spin * deltaTime;
        }

        /// <summary>Draws the particle. Must run in an additive pass.</summary>
        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            if (!IsActive) return;

            float t = 1f - _life / _maxLife;            // 0 at birth, 1 at death
            float fade = _life / _maxLife;              // linear alpha
            fade *= fade;                               // squared reads as a hotter core
            float size = MathHelper.Lerp(_size, _endSize, t);
            Color color = _color * fade;

            switch (_shape)
            {
                case ParticleShape.Shard:
                {
                    Vector2 dir = new Vector2(MathF.Cos(_rotation), MathF.Sin(_rotation));
                    Vector2 a = _position - dir * size;
                    Vector2 b = _position + dir * size;
                    PrimitiveDrawer.DrawLine(sb, pixel, a, b, color, 2f);
                    PrimitiveDrawer.DrawLine(sb, pixel, a, b, color * 0.35f, 5f);
                    break;
                }

                case ParticleShape.Ring:
                {
                    PrimitiveDrawer.DrawCircle(sb, pixel, _position, size, color, 26, 2f);
                    PrimitiveDrawer.DrawCircle(sb, pixel, _position, size * 0.82f, color * 0.4f, 22, 1f);
                    break;
                }

                default:
                {
                    PrimitiveDrawer.DrawFilledCircle(sb, pixel, _position, size * 1.9f, color * 0.25f);
                    PrimitiveDrawer.DrawFilledCircle(sb, pixel, _position, size, color);
                    break;
                }
            }
        }

        // ── IPoolable ──────────────────────────────────────────────────────
        public void OnSpawned() => IsActive = true;

        public void OnDespawned()
        {
            IsActive = false;
            _velocity = Vector2.Zero;
            _life = 0f;
        }
    }
}
