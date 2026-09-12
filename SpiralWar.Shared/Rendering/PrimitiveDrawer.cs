using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Rendering
{
    /// <summary>
    /// 2D primitives drawn with a single 1x1 white texture. No content pipeline assets, no
    /// shaders — which is exactly why the game boots identically on DesktopGL, WindowsDX and
    /// mobile.
    ///
    /// Note on line thickness: the origin is (0, 0.5) so a line is CENTERED on the segment it
    /// connects. With the origin at (0,0) every stroke drifts half its thickness downward,
    /// which is what made thick glow layers look lopsided.
    /// </summary>
    public static class PrimitiveDrawer
    {
        private static readonly Vector2 LineOrigin = new Vector2(0f, 0.5f);

        // ── Lines ──────────────────────────────────────────────────────────
        public static void DrawLine(SpriteBatch sb, Texture2D pixel,
            Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 0.01f) return;

            float angle = MathF.Atan2(delta.Y, delta.X);

            sb.Draw(pixel, start, null, color, angle,
                LineOrigin,
                new Vector2(length, thickness),
                SpriteEffects.None, 0f);
        }

        /// <summary>Connects an array of points. Used for trails and arcs.</summary>
        public static void DrawPolyline(SpriteBatch sb, Texture2D pixel,
            Vector2[] points, int count, Color color, float thickness = 1f)
        {
            for (int i = 1; i < count && i < points.Length; i++)
                DrawLine(sb, pixel, points[i - 1], points[i], color, thickness);
        }

        // ── Rectangles ─────────────────────────────────────────────────────
        public static void DrawFilledRect(SpriteBatch sb, Texture2D pixel,
            Rectangle rect, Color color)
        {
            sb.Draw(pixel, rect, color);
        }

        public static void DrawHollowRect(SpriteBatch sb, Texture2D pixel,
            Rectangle rect, Color color, int thickness = 1)
        {
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        /// <summary>Solid rotated rectangle. Cheaper than four lines for filled cores.</summary>
        public static void DrawRotatedFilledRect(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation, Color color)
        {
            sb.Draw(pixel, center, null, color, rotation,
                new Vector2(0.5f, 0.5f),
                new Vector2(width, height),
                SpriteEffects.None, 0f);
        }

        public static void DrawRotatedHollowRect(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            Color color, float thickness = 1f)
        {
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Vector2 tl = Rotate(center, -hw, -hh, cos, sin);
            Vector2 tr = Rotate(center, hw, -hh, cos, sin);
            Vector2 br = Rotate(center, hw, hh, cos, sin);
            Vector2 bl = Rotate(center, -hw, hh, cos, sin);

            DrawLine(sb, pixel, tl, tr, color, thickness);
            DrawLine(sb, pixel, tr, br, color, thickness);
            DrawLine(sb, pixel, br, bl, color, thickness);
            DrawLine(sb, pixel, bl, tl, color, thickness);
        }

        /// <summary>Writes the four rotated corners of a box into <paramref name="corners"/>.</summary>
        public static void GetRotatedCorners(Vector2 center, float width, float height,
            float rotation, Vector2[] corners)
        {
            if (corners == null || corners.Length < 4) return;

            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            corners[0] = Rotate(center, -hw, -hh, cos, sin);
            corners[1] = Rotate(center, hw, -hh, cos, sin);
            corners[2] = Rotate(center, hw, hh, cos, sin);
            corners[3] = Rotate(center, -hw, hh, cos, sin);
        }

        // ── Circles ────────────────────────────────────────────────────────
        public static void DrawCircle(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float radius, Color color,
            int segments = 24, float thickness = 1f)
        {
            if (radius <= 0.5f) return;
            if (segments < 3) segments = 3;

            float step = MathF.Tau / segments;
            Vector2 prev = center + new Vector2(radius, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float a = step * i;
                Vector2 next = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
                DrawLine(sb, pixel, prev, next, color, thickness);
                prev = next;
            }
        }

        /// <summary>Partial circle. Used for boost timers and the core danger ring.</summary>
        public static void DrawArc(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float radius, float startAngle, float sweepAngle,
            Color color, int segments = 32, float thickness = 1f)
        {
            if (radius <= 0.5f || MathF.Abs(sweepAngle) < 0.001f) return;
            if (segments < 2) segments = 2;

            float step = sweepAngle / segments;
            Vector2 prev = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;

            for (int i = 1; i <= segments; i++)
            {
                float a = startAngle + step * i;
                Vector2 next = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius;
                DrawLine(sb, pixel, prev, next, color, thickness);
                prev = next;
            }
        }

        /// <summary>
        /// Solid disc built from horizontal scanlines. Only meant for small radii
        /// (particles, bullet cores, supply centers) where a real mesh would be overkill.
        /// </summary>
        public static void DrawFilledCircle(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float radius, Color color)
        {
            if (radius <= 0.5f)
            {
                sb.Draw(pixel, new Rectangle((int)center.X, (int)center.Y, 1, 1), color);
                return;
            }

            int r = (int)MathF.Ceiling(radius);
            int cx = (int)center.X;
            int cy = (int)center.Y;

            for (int dy = -r; dy <= r; dy++)
            {
                float halfWidth = MathF.Sqrt(MathF.Max(0f, radius * radius - dy * dy));
                int w = (int)MathF.Round(halfWidth * 2f);
                if (w <= 0) continue;

                sb.Draw(pixel, new Rectangle(cx - w / 2, cy + dy, w, 1), color);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static Vector2 Rotate(Vector2 center, float x, float y, float cos, float sin)
            => new Vector2(center.X + x * cos - y * sin,
                           center.Y + x * sin + y * cos);
    }
}
