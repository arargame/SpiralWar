using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpiralWar.Rendering
{
    /// <summary>
    /// Draws every neon shape in the game.
    ///
    /// The look is built from three ingredients, always in this order:
    ///   1. a few expanding, very low alpha copies of the shape  -> the halo
    ///   2. the shape itself at full colour                      -> the tube
    ///   3. a smaller, whiter copy inside it                     -> the hot filament
    /// Because the world is drawn in an ADDITIVE pass, those layers accumulate light exactly
    /// the way a real neon tube does. The same code in an alpha pass looks like grey fog,
    /// which is what the first version of this renderer suffered from.
    /// </summary>
    public static class NeonRenderer
    {
        // ── Snake segment ──────────────────────────────────────────────────

        /// <param name="flash">0..1 white-out applied right after the ring takes a hit.</param>
        /// <param name="armored">Armored rings get a second shell and an X brace.</param>
        public static void DrawNeonSegment(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            Color neonColor, float glowIntensity = 1f, float flash = 0f,
            bool armored = false, float glowScale = 1f)
        {
            // 1. Halo
            int layers = GameConstants.GlowLayers;
            for (int i = layers; i >= 1; i--)
            {
                float expand = i * GameConstants.GlowExpand * glowIntensity * glowScale;
                float alpha = GameConstants.GlowAlpha / (i * 1.35f) * glowScale;
                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, width + expand * 2f, height + expand * 2f,
                    rotation, neonColor * alpha, 2f);
            }

            // 2. Tube
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width, height, rotation, neonColor, 2f);

            // 3. Filament
            Color inner = Color.Lerp(neonColor, Color.White, 0.55f) * 0.7f;
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width - 7f, height - 7f, rotation, inner, 1f);

            // Corner studs — a small detail that makes the rings read as machined parts
            // instead of plain rectangles.
            DrawCornerStuds(sb, pixel, center, width, height, rotation,
                Color.Lerp(neonColor, Color.White, 0.7f) * 0.85f);

            if (armored)
                DrawArmorBrace(sb, pixel, center, width, height, rotation, neonColor);

            // Hit flash: a bright shell that snaps outward and fades within ~120 ms.
            if (flash > 0.001f)
            {
                float punch = 1f + flash * 0.22f;
                Color flashColor = Color.White * (flash * 0.85f);

                PrimitiveDrawer.DrawRotatedFilledRect(sb, pixel, center,
                    width * 0.92f, height * 0.92f, rotation, Color.White * (flash * 0.32f));

                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, width * punch, height * punch, rotation, flashColor, 3f);

                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, width * punch + 10f, height * punch + 10f, rotation,
                    flashColor * 0.35f, 2f);
            }
        }

        private static readonly Vector2[] CornerBuffer = new Vector2[4];

        private static void DrawCornerStuds(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation, Color color)
        {
            PrimitiveDrawer.GetRotatedCorners(center, width, height, rotation, CornerBuffer);
            for (int i = 0; i < 4; i++)
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, CornerBuffer[i], 1.6f, color);
        }

        private static void DrawArmorBrace(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation, Color neonColor)
        {
            // Second shell.
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width + 7f, height + 7f, rotation,
                Color.Lerp(neonColor, Color.White, 0.3f) * 0.8f, 1f);

            // X brace, drawn in the ring's local space.
            PrimitiveDrawer.GetRotatedCorners(center, width - 12f, height - 10f, rotation, CornerBuffer);
            Color brace = Color.Lerp(neonColor, Color.White, 0.4f) * 0.55f;
            PrimitiveDrawer.DrawLine(sb, pixel, CornerBuffer[0], CornerBuffer[2], brace, 1f);
            PrimitiveDrawer.DrawLine(sb, pixel, CornerBuffer[1], CornerBuffer[3], brace, 1f);
        }

        /// <summary>
        /// Marks a ring that is carrying a supply: a pulsing outer ring in the supply's colour.
        /// The letter itself is drawn by the segment so it can use the shared font.
        /// </summary>
        public static void DrawSupplyMarker(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            Color supplyColor, float pulse)
        {
            float expand = 6f + pulse * 5f;
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width + expand * 2f, height + expand * 2f,
                rotation, supplyColor * (0.30f + pulse * 0.30f), 1.5f);

            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width + expand * 2f + 5f, height + expand * 2f + 5f,
                rotation, supplyColor * (0.10f + pulse * 0.12f), 1f);
        }

        // ── Player ─────────────────────────────────────────────────────────

        public static void DrawNeonPlayer(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float rotation, Color neonColor,
            float enginePulse, float hurtFlash, float glowScale = 1f)
        {
            float s = GameConstants.PlayerSize;
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            Vector2 Local(float x, float y) =>
                new Vector2(center.X + x * cos - y * sin,
                            center.Y + x * sin + y * cos);

            Vector2 tip = Local(s, 0f);
            Vector2 wingL = Local(-s * 0.62f, -s * 0.68f);
            Vector2 wingR = Local(-s * 0.62f, s * 0.68f);
            Vector2 tail = Local(-s * 0.22f, 0f);

            Color body = hurtFlash > 0f
                ? Color.Lerp(neonColor, new Color(255, 70, 70), hurtFlash)
                : neonColor;

            // Halo
            for (int i = 3; i >= 1; i--)
            {
                float a = 0.14f / i * glowScale;
                float w = i * 2.4f;
                PrimitiveDrawer.DrawLine(sb, pixel, tip, wingL, body * a, w);
                PrimitiveDrawer.DrawLine(sb, pixel, tip, wingR, body * a, w);
                PrimitiveDrawer.DrawLine(sb, pixel, wingL, tail, body * a, w);
                PrimitiveDrawer.DrawLine(sb, pixel, wingR, tail, body * a, w);
            }

            // Tube
            PrimitiveDrawer.DrawLine(sb, pixel, tip, wingL, body, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, tip, wingR, body, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, wingL, tail, body, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, wingR, tail, body, 2f);

            // Filament along the spine
            Color bright = Color.Lerp(body, Color.White, 0.6f);
            PrimitiveDrawer.DrawLine(sb, pixel, tip, tail, bright, 1f);

            // Engine flare at the back, pulsing
            Vector2 flare = Local(-s * 0.36f - enginePulse * 6f, 0f);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, flare, 2.2f + enginePulse * 1.8f,
                new Color(255, 180, 90) * (0.55f + enginePulse * 0.35f));

            // Hub
            PrimitiveDrawer.DrawCircle(sb, pixel, center, 4.5f, body * 0.9f, 14, 1f);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, center, 1.6f, bright);

            if (hurtFlash > 0.01f)
                PrimitiveDrawer.DrawCircle(sb, pixel, center, s * (1.2f + hurtFlash * 0.8f),
                    new Color(255, 60, 60) * (hurtFlash * 0.5f), 24, 2f);
        }

        // ── Bullets ────────────────────────────────────────────────────────

        public static void DrawNeonBullet(SpriteBatch sb, Texture2D pixel,
            Vector2 position, Vector2 previousPosition, Color color, float glowScale = 1f)
        {
            // Motion streak: the segment between last frame and this one.
            PrimitiveDrawer.DrawLine(sb, pixel, previousPosition, position, color * 0.45f, 2.5f);
            PrimitiveDrawer.DrawLine(sb, pixel, previousPosition, position, color * 0.18f, 6f);

            for (int i = 3; i >= 1; i--)
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, position, i * 2.1f,
                    color * (0.16f / i) * glowScale);

            PrimitiveDrawer.DrawFilledCircle(sb, pixel, position, 2.4f, color);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, position, 1.1f,
                Color.Lerp(color, Color.White, 0.8f));
        }

        // ── Supplies ───────────────────────────────────────────────────────

        /// <summary>
        /// Neon diamond capsule. The identifying letter is drawn by the caller so the
        /// renderer stays font-agnostic.
        /// </summary>
        public static void DrawNeonSupplyShell(SpriteBatch sb, Texture2D pixel,
            Vector2 center, Color color, float size, float spin, float pulse,
            float glowScale = 1f)
        {
            float s = size * (1f + pulse * 0.10f);

            for (int i = 3; i >= 1; i--)
            {
                float expand = i * 2.8f;
                float a = 0.20f / i * glowScale;
                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, s + expand * 2f, s + expand * 2f, spin, color * a, 2f);
            }

            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel, center, s, s, spin, color, 2f);
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel, center, s - 7f, s - 7f, spin,
                Color.Lerp(color, Color.White, 0.5f) * 0.7f, 1f);

            // Orbiting spark, so a supply reads as "live" even when it is far away.
            Vector2 orbit = center + new Vector2(MathF.Cos(spin * 2.4f), MathF.Sin(spin * 2.4f)) * (s * 0.85f);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, orbit, 1.6f, Color.Lerp(color, Color.White, 0.6f));
        }

        // ── Arena furniture ────────────────────────────────────────────────

        /// <summary>Background grid with a subtle radial falloff toward the core.</summary>
        public static void DrawGrid(SpriteBatch sb, Texture2D pixel,
            int screenW, int screenH, Color tint, int cellSize = 55, float intensity = 1f)
        {
            Color near = tint * (0.16f * intensity);
            Color far = tint * (0.05f * intensity);

            float cx = screenW * 0.5f;
            float cy = screenH * 0.5f;
            float maxDist = MathF.Sqrt(cx * cx + cy * cy);

            for (int x = 0; x <= screenW; x += cellSize)
            {
                float d = MathF.Abs(x - cx) / maxDist;
                Color c = Color.Lerp(near, far, d);
                PrimitiveDrawer.DrawLine(sb, pixel, new Vector2(x, 0), new Vector2(x, screenH), c, 1f);
            }

            for (int y = 0; y <= screenH; y += cellSize)
            {
                float d = MathF.Abs(y - cy) / maxDist;
                Color c = Color.Lerp(near, far, d);
                PrimitiveDrawer.DrawLine(sb, pixel, new Vector2(0, y), new Vector2(screenW, y), c, 1f);
            }
        }

        /// <summary>
        /// The core the snake is diving for. Pulses harder as the head closes in, which is
        /// the player's peripheral warning that time is running out.
        /// </summary>
        public static void DrawCoreRing(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float danger, float time, Color themeColor)
        {
            float pulse = MathF.Sin(time * (3f + danger * 7f)) * 0.5f + 0.5f;
            Color color = Color.Lerp(themeColor, new Color(255, 60, 60), danger);

            float baseRadius = GameConstants.HeadCenterRadius;
            PrimitiveDrawer.DrawCircle(sb, pixel, center,
                baseRadius + pulse * 4f, color * (0.16f + danger * 0.40f), 40, 1.5f);

            PrimitiveDrawer.DrawCircle(sb, pixel, center,
                baseRadius * 0.55f, color * (0.10f + danger * 0.30f), 28, 1f);

            // Four tick marks, rotating slowly.
            float spin = time * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                float a = spin + i * MathF.PI * 0.5f;
                Vector2 dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                PrimitiveDrawer.DrawLine(sb, pixel,
                    center + dir * (baseRadius + 6f),
                    center + dir * (baseRadius + 16f),
                    color * (0.25f + danger * 0.45f), 1.5f);
            }
        }

        // ── HUD bars ───────────────────────────────────────────────────────

        /// <summary>
        /// Segmented neon bar. Discrete ticks read far faster than a smooth gradient — the
        /// player can count what is left without parsing a number.
        /// </summary>
        public static void DrawNeonBar(SpriteBatch sb, Texture2D pixel,
            Vector2 topLeft, float width, float height,
            float percent, int tickCount, Color barColor, float glowScale = 1f)
        {
            if (percent < 0f) percent = 0f;
            if (percent > 1f) percent = 1f;
            if (tickCount < 1) tickCount = 1;

            float gap = 3f;
            float tickWidth = (width - gap * (tickCount - 1)) / tickCount;
            float filledTicks = percent * tickCount;

            for (int i = 0; i < tickCount; i++)
            {
                float x = topLeft.X + i * (tickWidth + gap);
                var rect = new Rectangle((int)x, (int)topLeft.Y, (int)MathF.Ceiling(tickWidth), (int)height);

                float fill = filledTicks - i;
                if (fill <= 0f)
                {
                    // Empty slot: faint outline only.
                    PrimitiveDrawer.DrawHollowRect(sb, pixel, rect, barColor * 0.14f, 1);
                    continue;
                }

                if (fill > 1f) fill = 1f;

                var filledRect = new Rectangle(rect.X, rect.Y,
                    Math.Max(1, (int)(rect.Width * fill)), rect.Height);

                // Halo then core.
                for (int g = 2; g >= 1; g--)
                {
                    var grow = new Rectangle(filledRect.X - g, filledRect.Y - g,
                        filledRect.Width + g * 2, filledRect.Height + g * 2);
                    PrimitiveDrawer.DrawFilledRect(sb, pixel, grow, barColor * (0.10f / g) * glowScale);
                }

                PrimitiveDrawer.DrawFilledRect(sb, pixel, filledRect, barColor * 0.85f);
                PrimitiveDrawer.DrawHollowRect(sb, pixel, rect,
                    Color.Lerp(barColor, Color.White, 0.4f) * 0.9f, 1);
            }
        }

        /// <summary>Thin countdown bar used for the weapon boost timers.</summary>
        public static void DrawTimerBar(SpriteBatch sb, Texture2D pixel,
            Vector2 topLeft, float width, float height, float percent, Color color)
        {
            if (percent < 0f) percent = 0f;
            if (percent > 1f) percent = 1f;

            var back = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)width, (int)height);
            PrimitiveDrawer.DrawFilledRect(sb, pixel, back, color * 0.12f);

            var fill = new Rectangle(back.X, back.Y, (int)(width * percent), back.Height);
            PrimitiveDrawer.DrawFilledRect(sb, pixel, fill, color * 0.9f);
            PrimitiveDrawer.DrawFilledRect(sb, pixel,
                new Rectangle(fill.X, fill.Y - 1, fill.Width, 1),
                Color.Lerp(color, Color.White, 0.6f) * 0.8f);
        }
    }
}
