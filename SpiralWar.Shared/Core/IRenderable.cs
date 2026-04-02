using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Core
{
    public interface IRenderable
    {
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }
}
