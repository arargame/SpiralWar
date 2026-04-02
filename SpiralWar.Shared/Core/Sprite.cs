using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Core
{
    /// <summary>
    /// Texture tabanlı görsel rendering için temel sınıf.
    /// KI sprite encapsulation pattern'ından türetilmiştir.
    /// </summary>
    public abstract class Sprite : GameObject
    {
        public Texture2D? Texture    { get; private set; }
        public int        Width      { get; private set; }
        public int        Height     { get; private set; }
        public Vector2    Scale      { get; private set; } = Vector2.One;
        public Vector2    Origin     { get; private set; }
        public Rectangle? SourceRect { get; private set; }
        public Color      Color      { get; protected set; } = Color.White;
        public float      Rotation   { get; protected set; }
        public float      LayerDepth { get; protected set; }

        public void SetTexture(Texture2D texture)
        {
            Texture = texture;
            if (texture != null)
            {
                if (Width  == 0) Width  = texture.Width;
                if (Height == 0) Height = texture.Height;
            }
        }

        public void SetSize(int width, int height)
        {
            if (Texture != null)
                Scale = new Vector2((float)width / Texture.Width, (float)height / Texture.Height);
            Width  = width;
            Height = height;
        }

        public void SetOrigin(float x, float y)  => Origin = new Vector2(x, y);
        public void SetOriginCenter()             => Origin = new Vector2(Width / 2f, Height / 2f);
        public void SetColor(Color color)         => Color  = color;
        public void SetRotation(float rotation)   => Rotation   = rotation;
        public void SetLayerDepth(float depth)    => LayerDepth = depth;

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (Texture == null) return;
            var dest = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
            spriteBatch.Draw(Texture, dest, SourceRect, Color, Rotation, Origin, SpriteEffects.None, LayerDepth);
        }
    }
}
