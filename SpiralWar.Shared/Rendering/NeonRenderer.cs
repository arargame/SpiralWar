using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Rendering
{
    /// <summary>
    /// Neon glow efektli çizim yardımcısı.
    /// Her şekli birkaç kez büyüterek ve alpha azaltarak layerlar; iç kenar parlak kalır.
    /// </summary>
    public static class NeonRenderer
    {
        // ── Neon hollow rotated rect (yılan segmenti) ─────────────────────────
        public static void DrawNeonSegmentRect(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            Color neonColor, float glowIntensity = 1f)
        {
            // Glow layers — dıştan içe
            for (int i = GameConstants.GlowLayers; i >= 1; i--)
            {
                float expand = i * GameConstants.GlowExpand * glowIntensity;
                float alpha  = GameConstants.GlowAlpha / i;
                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, width + expand * 2, height + expand * 2,
                    rotation, neonColor * alpha, 2);
            }

            // Ana kenar (parlak)
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width, height, rotation, neonColor, 2);

            // İç parlak kenar
            var inner = Color.Lerp(neonColor, Color.White, 0.45f) * 0.75f;
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width - 6, height - 6, rotation, inner, 1);
        }

        // ── Neon player (üçgen ok şekli) ─────────────────────────────────────
        public static void DrawNeonPlayer(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float rotation, Color neonColor)
        {
            float s   = GameConstants.PlayerSize;
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Vector2 Rot(float x, float y) =>
                new Vector2(center.X + x * cos - y * sin,
                            center.Y + x * sin + y * cos);

            // Üçgen köşeleri (yönünü gösteren ok)
            var tip  = Rot(s,       0);
            var bl   = Rot(-s * 0.6f, -s * 0.65f);
            var br   = Rot(-s * 0.6f,  s * 0.65f);
            var tail = Rot(-s * 0.2f,  0);

            // Glow
            for (int i = 3; i >= 1; i--)
            {
                float a = 0.12f / i;
                PrimitiveDrawer.DrawLine(sb, pixel, tip, bl,   neonColor * a, i * 2);
                PrimitiveDrawer.DrawLine(sb, pixel, tip, br,   neonColor * a, i * 2);
                PrimitiveDrawer.DrawLine(sb, pixel, bl,  tail, neonColor * a, i * 2);
                PrimitiveDrawer.DrawLine(sb, pixel, br,  tail, neonColor * a, i * 2);
            }

            // Ana kenar
            PrimitiveDrawer.DrawLine(sb, pixel, tip, bl,   neonColor, 2);
            PrimitiveDrawer.DrawLine(sb, pixel, tip, br,   neonColor, 2);
            PrimitiveDrawer.DrawLine(sb, pixel, bl,  tail, neonColor, 2);
            PrimitiveDrawer.DrawLine(sb, pixel, br,  tail, neonColor, 2);

            // Parlak iç
            var bright = Color.Lerp(neonColor, Color.White, 0.5f);
            PrimitiveDrawer.DrawLine(sb, pixel, tip, tail, bright, 1);

            // Merkez nokta (crosshair)
            PrimitiveDrawer.DrawCircle(sb, pixel, center, 4, neonColor, 12, 1);
        }

        // ── Neon bullet ──────────────────────────────────────────────────────
        public static void DrawNeonBullet(SpriteBatch sb, Texture2D pixel,
            Vector2 position, Color color)
        {
            for (int i = 3; i >= 1; i--)
            {
                int   r = i * 3;
                float a = 0.20f / i;
                PrimitiveDrawer.DrawCircle(sb, pixel, position, r, color * a, 8, i);
            }
            PrimitiveDrawer.DrawCircle(sb, pixel, position, 3, color, 8, 2);
            // Parlak merkez
            sb.Draw(pixel, new Rectangle((int)position.X - 1, (int)position.Y - 1, 3, 3),
                Color.Lerp(color, Color.White, 0.7f));
        }

        // ── Neon supply icon ─────────────────────────────────────────────────
        public static void DrawNeonSupply(SpriteBatch sb, Texture2D pixel,
            Vector2 center, Color color, float pulse)
        {
            float s = GameConstants.SupplySize * (1f + pulse * 0.12f);

            for (int i = 3; i >= 1; i--)
            {
                float expand = i * 2.5f;
                float a      = 0.15f / i;
                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, s + expand * 2, s + expand * 2,
                    MathF.PI / 4f, color * a, 2);
            }
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, s, s, MathF.PI / 4f, color, 2);

            // + işareti içine
            float h = s * 0.4f;
            PrimitiveDrawer.DrawLine(sb, pixel,
                center + new Vector2(-h, 0), center + new Vector2(h, 0), color, 2);
            PrimitiveDrawer.DrawLine(sb, pixel,
                center + new Vector2(0, -h), center + new Vector2(0, h), color, 2);
        }

        // ── Background grid ──────────────────────────────────────────────────
        public static void DrawGrid(SpriteBatch sb, Texture2D pixel,
            int screenW, int screenH, int cellSize = 60)
        {
            var gridColor = new Color(15, 25, 40);
            for (int x = 0; x <= screenW; x += cellSize)
                PrimitiveDrawer.DrawLine(sb, pixel,
                    new Vector2(x, 0), new Vector2(x, screenH), gridColor, 1);
            for (int y = 0; y <= screenH; y += cellSize)
                PrimitiveDrawer.DrawLine(sb, pixel,
                    new Vector2(0, y), new Vector2(screenW, y), gridColor, 1);
        }

        // ── HP bar ───────────────────────────────────────────────────────────
        public static void DrawHpBar(SpriteBatch sb, Texture2D pixel,
            Vector2 topLeft, float width, float height,
            float percent, Color barColor)
        {
            var bgRect   = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)width, (int)height);
            var fillRect = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(width * percent), (int)height);
            PrimitiveDrawer.DrawFilledRect(sb, pixel, bgRect, new Color(20, 20, 30));
            PrimitiveDrawer.DrawFilledRect(sb, pixel, fillRect, barColor);
            PrimitiveDrawer.DrawHollowRect(sb, pixel, bgRect,
                Color.Lerp(barColor, Color.White, 0.3f), 1);
        }
    }
}
