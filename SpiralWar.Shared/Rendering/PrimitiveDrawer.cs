using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Rendering
{
    /// <summary>
    /// Texture kullanmadan 2D primitive çizimi (çizgi, dolu/boş rect).
    /// SpriteBatch + 1×1 pixel texture tekniği.
    /// </summary>
    public static class PrimitiveDrawer
    {
        // ── Line ────────────────────────────────────────────────────────────
        public static void DrawLine(SpriteBatch sb, Texture2D pixel,
            Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 delta     = end - start;
            float   length    = delta.Length();
            if (length < 0.01f) return;
            float   angle     = MathF.Atan2(delta.Y, delta.X);

            sb.Draw(pixel, start, null, color, angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None, 0f);
        }

        // ── Filled rect ──────────────────────────────────────────────────────
        public static void DrawFilledRect(SpriteBatch sb, Texture2D pixel,
            Rectangle rect, Color color)
        {
            sb.Draw(pixel, rect, color);
        }

        // ── Hollow rect (axis-aligned) ────────────────────────────────────────
        public static void DrawHollowRect(SpriteBatch sb, Texture2D pixel,
            Rectangle rect, Color color, int thickness = 1)
        {
            // Top
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        // ── Rotated hollow rect (4 lines around center with rotation) ─────────
        public static void DrawRotatedHollowRect(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            Color color, int thickness = 1)
        {
            float hw = width  / 2f;
            float hh = height / 2f;
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Vector2 Rot(float x, float y) =>
                new Vector2(center.X + x * cos - y * sin,
                            center.Y + x * sin + y * cos);

            var tl = Rot(-hw, -hh);
            var tr = Rot( hw, -hh);
            var br = Rot( hw,  hh);
            var bl = Rot(-hw,  hh);

            DrawLine(sb, pixel, tl, tr, color, thickness);
            DrawLine(sb, pixel, tr, br, color, thickness);
            DrawLine(sb, pixel, br, bl, color, thickness);
            DrawLine(sb, pixel, bl, tl, color, thickness);
        }

        // ── Circle (approximated with line segments) ──────────────────────────
        public static void DrawCircle(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float radius, Color color,
            int segments = 32, int thickness = 1)
        {
            float step = MathF.Tau / segments;
            for (int i = 0; i < segments; i++)
            {
                float a0 = step * i;
                float a1 = step * (i + 1);
                var   p0 = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * radius;
                var   p1 = center + new Vector2(MathF.Cos(a1), MathF.Sin(a1)) * radius;
                DrawLine(sb, pixel, p0, p1, color, thickness);
            }
        }
    }
}
