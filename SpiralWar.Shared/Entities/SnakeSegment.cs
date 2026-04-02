using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Rendering;

namespace SpiralWar.Entities
{
    /// <summary>
    /// Yılanın tek bir halkası. Random renk, random HP, neon glow ile çizilir.
    /// HP yazısı üzerinde gösterilir. Pozisyon Snake tarafından yönetilir.
    /// </summary>
    public sealed class SnakeSegment : Enemy
    {
        public Color  NeonColor { get; private set; }
        public bool   IsHead    { get; private set; }

        private readonly SpriteFont _font;
        private readonly Texture2D  _pixel;
        private float _glowPulse;

        private static readonly Random _rng = new Random();
        private static readonly Color[] _palette =
        {
            new Color(0,   255, 180), // cyan-green
            new Color(255,  0,  200), // hot magenta
            new Color(180, 255,   0), // lime
            new Color(255, 130,   0), // neon orange
            new Color(110,   0, 255), // violet
            new Color(0,   180, 255), // sky blue
            new Color(255,  60,  60), // coral red (non-head)
        };

        public SnakeSegment(bool isHead, int level, SpriteFont font, Texture2D pixel)
            : base(
                isHead ? 99999 : _rng.Next(level, 3 + (level * 2) + 1),
                0f)
        {
            IsHead   = isHead;
            _font    = font;
            _pixel   = pixel;

            // Kafa kırmızı, gövde random palette
            NeonColor = isHead
                ? new Color(255, 50, 50)
                : _palette[_rng.Next(_palette.Length)];
        }

        // Rotation Snake tarafından dışarıdan set edilir
        public void UpdateRotation(float angle) => SetRotation(angle);

        public override void Update(GameTime gameTime)
        {
            _glowPulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 4) * 0.5f + 0.5f;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            NeonRenderer.DrawNeonSegmentRect(
                spriteBatch, _pixel,
                Position,
                GameConstants.SegmentWidth,
                GameConstants.SegmentHeight,
                Rotation,
                NeonColor,
                glowIntensity: 0.7f + _glowPulse * 0.5f);

            // HP yazısı (kafa hariç)
            if (!IsHead && _font != null)
            {
                string hp     = CurrentHealth.ToString();
                Vector2 size  = _font.MeasureString(hp);
                Vector2 textPos = Position - size / 2f;
                // Glow efekti için aynı yazı biraz offset ile
                spriteBatch.DrawString(_font, hp, textPos + Vector2.One, Color.Black * 0.7f);
                spriteBatch.DrawString(_font, hp, textPos,
                    Color.Lerp(NeonColor, Color.White, 0.6f));
            }
        }
    }
}
