using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Pooling;

namespace SpiralWar.Entities
{
    /// <summary>
    /// A short-lived number or word that rises off a hit and fades — the damage readout.
    ///
    /// This exists for one reason: "did that shot do anything?" must never be a question.
    /// The ring flashes, the hit sound fires and this number floats off, so the feedback
    /// lands on three senses' worth of channels at once.
    /// </summary>
    public sealed class FloatingText : IPoolable
    {
        public bool IsActive { get; private set; }

        private string _text = string.Empty;
        private Vector2 _position;
        private Vector2 _velocity;
        private Color _color;
        private float _life;
        private float _maxLife;
        private float _scale;
        private float _popScale;

        public void Spawn(string text, Vector2 position, Color color,
                          float lifeSeconds = 0.75f, float scale = 1f, float riseSpeed = 46f)
        {
            _text = text ?? string.Empty;
            _position = position;
            _velocity = new Vector2(0f, -riseSpeed);
            _color = color;
            _life = lifeSeconds;
            _maxLife = lifeSeconds <= 0f ? 0.001f : lifeSeconds;
            _scale = scale;
            _popScale = 1.45f;   // snaps down to 1.0 in the first ~100 ms
            IsActive = true;
        }

        /// <summary>Gives the text a sideways drift so a burst of hits does not stack up.</summary>
        public void SetDrift(float horizontalSpeed) => _velocity.X = horizontalSpeed;

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            _life -= deltaTime;
            if (_life <= 0f)
            {
                IsActive = false;
                return;
            }

            _position += _velocity * deltaTime;
            _velocity.Y *= 1f - MathF.Min(1f, 1.9f * deltaTime);

            if (_popScale > 1f)
            {
                _popScale -= deltaTime * 5.5f;
                if (_popScale < 1f) _popScale = 1f;
            }
        }

        /// <summary>Draws the text. Runs in the additive world pass so it glows.</summary>
        public void Draw(SpriteBatch sb, SpriteFont font)
        {
            if (!IsActive || font == null || _text.Length == 0) return;

            float fade = _life / _maxLife;
            float alpha = fade < 0.35f ? fade / 0.35f : 1f;
            float scale = _scale * _popScale;

            Vector2 size = font.MeasureString(_text) * scale;
            Vector2 pos = _position - size * 0.5f;

            // Cheap halo, then the glyph.
            sb.DrawString(font, _text, pos + new Vector2(-1, 0), _color * (alpha * 0.30f),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sb.DrawString(font, _text, pos + new Vector2(1, 0), _color * (alpha * 0.30f),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            sb.DrawString(font, _text, pos, _color * alpha,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // ── IPoolable ──────────────────────────────────────────────────────
        public void OnSpawned() => IsActive = true;

        public void OnDespawned()
        {
            IsActive = false;
            _text = string.Empty;
            _life = 0f;
        }
    }
}
