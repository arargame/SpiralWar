using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Core
{
    /// <summary>
    /// Maps the fixed design resolution (<see cref="GameConstants.ScreenWidth"/> x
    /// <see cref="GameConstants.ScreenHeight"/>) onto whatever back buffer the platform
    /// gives us, preserving the aspect ratio and letterboxing the remainder.
    ///
    /// All gameplay code keeps working in virtual coordinates: the same layout is correct
    /// on a 1920x1080 desktop window, an ultrawide monitor and a 20:9 phone.
    /// </summary>
    public sealed class ViewportAdapter
    {
        private readonly GraphicsDevice _device;

        public int VirtualWidth { get; }
        public int VirtualHeight { get; }

        /// <summary>Uniform scale currently applied to the virtual canvas.</summary>
        public float Scale { get; private set; } = 1f;

        /// <summary>Top-left of the drawn area inside the real back buffer, in real pixels.</summary>
        public Vector2 Offset { get; private set; }

        /// <summary>The letterboxed destination rectangle in real back buffer pixels.</summary>
        public Rectangle Bounds { get; private set; }

        public ViewportAdapter(GraphicsDevice device,
                               int virtualWidth = GameConstants.ScreenWidth,
                               int virtualHeight = GameConstants.ScreenHeight)
        {
            _device = device;
            VirtualWidth = virtualWidth;
            VirtualHeight = virtualHeight;
            Refresh();
        }

        /// <summary>Recomputes the transform. Call whenever the window is resized.</summary>
        public void Refresh()
        {
            int realWidth = _device.PresentationParameters.BackBufferWidth;
            int realHeight = _device.PresentationParameters.BackBufferHeight;
            if (realWidth <= 0 || realHeight <= 0) return;

            float scaleX = (float)realWidth / VirtualWidth;
            float scaleY = (float)realHeight / VirtualHeight;
            Scale = scaleX < scaleY ? scaleX : scaleY;

            float drawnWidth = VirtualWidth * Scale;
            float drawnHeight = VirtualHeight * Scale;

            Offset = new Vector2(
                (realWidth - drawnWidth) * 0.5f,
                (realHeight - drawnHeight) * 0.5f);

            Bounds = new Rectangle(
                (int)Offset.X, (int)Offset.Y,
                (int)drawnWidth, (int)drawnHeight);
        }

        /// <summary>Transform to pass to SpriteBatch.Begin.</summary>
        public Matrix GetScaleMatrix() =>
            Matrix.CreateScale(Scale, Scale, 1f) *
            Matrix.CreateTranslation(Offset.X, Offset.Y, 0f);

        /// <summary>Converts a real pointer position (mouse / touch) into virtual space.</summary>
        public Vector2 ToVirtual(float realX, float realY)
        {
            if (Scale <= 0f) return Vector2.Zero;
            return new Vector2(
                (realX - Offset.X) / Scale,
                (realY - Offset.Y) / Scale);
        }

        public Vector2 ToVirtual(Point realPoint) => ToVirtual(realPoint.X, realPoint.Y);

        /// <summary>Center of the virtual canvas — the core the snake spirals into.</summary>
        public Vector2 VirtualCenter => new Vector2(VirtualWidth * 0.5f, VirtualHeight * 0.5f);
    }
}
