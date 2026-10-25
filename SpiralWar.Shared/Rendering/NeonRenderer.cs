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

            // Corner studs - a small detail that makes the rings read as machined parts
            // instead of plain rectangles.
            DrawCornerStuds(sb, pixel, center, width, height, rotation,
                Color.Lerp(neonColor, Color.White, 0.7f) * 0.85f);

            if (armored)
                DrawArmorBrace(sb, pixel, center, width, height, rotation, neonColor);

            if (flash > 0.001f)
                DrawHitFlash(sb, pixel, center, width, height, rotation, flash);
        }

        private static void DrawHitFlash(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation, float flash)
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
        /// Marks a ring that is carrying something: a pulsing outer ring in the payload's
        /// colour. The glyph itself is drawn by the segment so it can use the shared font.
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

        // ── Neon dynamite ──────────────────────────────────────────────────

        /// <summary>
        /// A dynamite ring. It has to be unmistakable from across the arena and from the corner
        /// of the eye, so it gets three signals no other ring has: a hot orange double shell, a
        /// hazard chevron brace, and a spark running around its outside like a lit fuse.
        /// </summary>
        public static void DrawDynamiteRing(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            float fuse, float pulse, float flash)
        {
            var hot = new Color(255, 120, 0);
            var core = new Color(255, 220, 120);

            float breathe = 1f + pulse * 0.10f;

            // Halo, noticeably bigger than a normal ring's.
            for (int i = GameConstants.GlowLayers + 1; i >= 1; i--)
            {
                float expand = i * GameConstants.GlowExpand * 1.35f * breathe;
                float alpha = 0.26f / (i * 1.2f);
                PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                    center, width + expand * 2f, height + expand * 2f,
                    rotation, hot * alpha, 2f);
            }

            // Double shell.
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width * breathe, height * breathe, rotation, hot, 2.5f);
            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width * breathe - 8f, height * breathe - 8f, rotation,
                core * 0.85f, 1.5f);

            // Hazard chevrons: two short diagonals meeting at the centre.
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            Vector2 Local(float x, float y) =>
                new Vector2(center.X + x * cos - y * sin, center.Y + x * sin + y * cos);

            float hw = width * 0.32f;
            float hh = height * 0.26f;
            Color chevron = core * (0.55f + pulse * 0.35f);
            PrimitiveDrawer.DrawLine(sb, pixel, Local(-hw, -hh), Local(0f, 0f), chevron, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, Local(-hw, hh), Local(0f, 0f), chevron, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, Local(hw * 0.2f, -hh), Local(hw, 0f), chevron, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, Local(hw * 0.2f, hh), Local(hw, 0f), chevron, 2f);

            // The fuse: a bright spark orbiting the ring.
            float orbitX = width * 0.62f;
            float orbitY = height * 0.62f;
            Vector2 spark = Local(MathF.Cos(fuse) * orbitX, MathF.Sin(fuse) * orbitY);

            PrimitiveDrawer.DrawFilledCircle(sb, pixel, spark, 5.5f, hot * 0.35f);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, spark, 2.6f, Color.White);

            if (flash > 0.001f)
                DrawHitFlash(sb, pixel, center, width, height, rotation, flash);
        }

        // ── Acid ───────────────────────────────────────────────────────────

        /// <summary>
        /// Green corrosion crawling over a ring. Drawn as short vertical drips hanging off the
        /// ring's lower edge in its own local space, so the acid rotates with it.
        /// </summary>
        public static void DrawAcidOverlay(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float width, float height, float rotation,
            int stacks, float pulse)
        {
            if (stacks <= 0) return;

            var acid = new Color(130, 255, 80);
            float intensity = MathF.Min(1f, stacks / (float)GameConstants.AcidMaxStacks);

            PrimitiveDrawer.DrawRotatedHollowRect(sb, pixel,
                center, width + 3f, height + 3f, rotation,
                acid * (0.25f + intensity * 0.35f), 1.5f);

            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);
            Vector2 Local(float x, float y) =>
                new Vector2(center.X + x * cos - y * sin, center.Y + x * sin + y * cos);

            int drips = 2 + stacks / 2;
            float half = width * 0.40f;

            for (int i = 0; i < drips; i++)
            {
                float t = drips == 1 ? 0.5f : i / (float)(drips - 1);
                float x = -half + half * 2f * t;

                // Each drip bobs on its own phase so the ring looks wet rather than striped.
                float phase = pulse + i * 0.7f;
                float length = height * (0.28f + 0.22f * (MathF.Sin(phase * MathF.Tau) * 0.5f + 0.5f));

                Vector2 top = Local(x, height * 0.42f);
                Vector2 bottom = Local(x, height * 0.42f + length);

                PrimitiveDrawer.DrawLine(sb, pixel, top, bottom, acid * (0.35f + intensity * 0.4f), 2f);
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, bottom, 1.8f, acid * (0.6f + intensity * 0.4f));
            }
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

        // ── Projectiles ────────────────────────────────────────────────────

        /// <param name="scale">Criticals are drawn larger, so they read before the number does.</param>
        public static void DrawNeonBullet(SpriteBatch sb, Texture2D pixel,
            Vector2 position, Vector2 previousPosition, Color color,
            float scale = 1f, float glowScale = 1f)
        {
            // Motion streak: the segment between last frame and this one.
            PrimitiveDrawer.DrawLine(sb, pixel, previousPosition, position,
                color * 0.45f, 2.5f * scale);
            PrimitiveDrawer.DrawLine(sb, pixel, previousPosition, position,
                color * 0.18f, 6f * scale);

            for (int i = 3; i >= 1; i--)
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, position, i * 2.1f * scale,
                    color * (0.16f / i) * glowScale);

            PrimitiveDrawer.DrawFilledCircle(sb, pixel, position, 2.4f * scale, color);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, position, 1.1f * scale,
                Color.Lerp(color, Color.White, 0.8f));
        }

        /// <summary>
        /// A homing missile: a small dart with an exhaust plume behind it. The plume is what
        /// makes a missile legible as "mine and moving" in a frame already full of enemy neon.
        /// </summary>
        public static void DrawNeonMissile(SpriteBatch sb, Texture2D pixel,
            Vector2 position, float heading, Color color, float wobble,
            float glowScale = 1f)
        {
            float cos = MathF.Cos(heading);
            float sin = MathF.Sin(heading);

            Vector2 Local(float x, float y) =>
                new Vector2(position.X + x * cos - y * sin, position.Y + x * sin + y * cos);

            Vector2 nose = Local(9f, 0f);
            Vector2 tailL = Local(-6f, -4f);
            Vector2 tailR = Local(-6f, 4f);

            // Exhaust: three shrinking puffs behind the dart.
            for (int i = 1; i <= 3; i++)
            {
                float back = -8f - i * 5f;
                float jitter = MathF.Sin(wobble + i) * 1.6f;
                Vector2 puff = Local(back, jitter);
                float radius = 3.4f - i * 0.8f;
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, puff, radius,
                    new Color(255, 170, 90) * (0.30f / i) * glowScale);
            }

            for (int i = 3; i >= 1; i--)
            {
                float a = 0.16f / i * glowScale;
                PrimitiveDrawer.DrawLine(sb, pixel, nose, tailL, color * a, i * 2f);
                PrimitiveDrawer.DrawLine(sb, pixel, nose, tailR, color * a, i * 2f);
            }

            PrimitiveDrawer.DrawLine(sb, pixel, nose, tailL, color, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, nose, tailR, color, 2f);
            PrimitiveDrawer.DrawLine(sb, pixel, tailL, tailR, color * 0.8f, 1.5f);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, nose, 1.8f,
                Color.Lerp(color, Color.White, 0.8f));
        }

        // ── Pickups ────────────────────────────────────────────────────────

        /// <summary>
        /// Neon diamond capsule used by supplies. The identifying glyph is drawn by the caller
        /// so the renderer stays font-agnostic.
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

        private static readonly Vector2[] DiamondPoints = new Vector2[4];

        /// <summary>
        /// A faceted neon diamond: a tall kite outline with an inner facet and a dark stone in
        /// the middle. The silhouette is deliberately nothing like the supply capsule, so the
        /// player can tell a weapon from a stat at a glance without reading the glyph.
        ///
        /// <paramref name="coreColor"/> is what lets the black diamond exist at all: a black
        /// stone on a black arena would be invisible, so the stone is dark and the SHELL is
        /// bright.
        /// </summary>
        public static void DrawNeonDiamond(SpriteBatch sb, Texture2D pixel,
            Vector2 center, Color color, Color coreColor,
            float size, float spin, float pulse, float glowScale = 1f)
        {
            float breathe = 1f + pulse * 0.12f;
            float halfW = size * 0.52f * breathe;
            float topH = size * 0.72f * breathe;
            float bottomH = size * 0.92f * breathe;

            // Tumbles slowly around its vertical axis: squash the width with a cosine so it
            // reads as a 3D stone turning rather than a 2D shape spinning.
            float turn = MathF.Abs(MathF.Cos(spin));
            halfW *= 0.35f + 0.65f * turn;

            DiamondPoints[0] = center + new Vector2(0f, -topH);
            DiamondPoints[1] = center + new Vector2(halfW, -topH * 0.15f);
            DiamondPoints[2] = center + new Vector2(0f, bottomH);
            DiamondPoints[3] = center + new Vector2(-halfW, -topH * 0.15f);

            // Halo
            for (int layer = 3; layer >= 1; layer--)
            {
                float a = 0.22f / layer * glowScale;
                float thickness = layer * 2.2f;
                for (int i = 0; i < 4; i++)
                    PrimitiveDrawer.DrawLine(sb, pixel,
                        DiamondPoints[i], DiamondPoints[(i + 1) % 4], color * a, thickness);
            }

            // The stone. Drawn before the outline so the edges stay crisp over it.
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, center + new Vector2(0f, bottomH * 0.05f),
                halfW * 0.78f, coreColor);

            // Outline
            for (int i = 0; i < 4; i++)
                PrimitiveDrawer.DrawLine(sb, pixel,
                    DiamondPoints[i], DiamondPoints[(i + 1) % 4], color, 2f);

            // Facets: the girdle plus two lines down to the point.
            Color facet = Color.Lerp(color, Color.White, 0.55f) * 0.75f;
            PrimitiveDrawer.DrawLine(sb, pixel, DiamondPoints[1], DiamondPoints[3], facet, 1f);
            PrimitiveDrawer.DrawLine(sb, pixel, DiamondPoints[0], DiamondPoints[2], facet * 0.7f, 1f);

            // Highlight spark that slides across the top facet as it turns.
            Vector2 spark = Vector2.Lerp(DiamondPoints[3], DiamondPoints[1],
                MathF.Sin(spin) * 0.5f + 0.5f);
            PrimitiveDrawer.DrawFilledCircle(sb, pixel, spark, 1.9f,
                Color.Lerp(color, Color.White, 0.9f));
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
        /// The core the snake is diving for. Pulses harder as the head closes in, which is the
        /// player's peripheral warning that time is running out.
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

        /// <summary>
        /// Full-screen tint drawn while the stasis field is up. Cold, faint and animated, so
        /// the player can feel the slowdown without the arena becoming harder to read.
        /// </summary>
        public static void DrawStasisField(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float time, float strength)
        {
            if (strength <= 0.01f) return;

            var color = new Color(170, 200, 255);
            float pulse = MathF.Sin(time * 2.2f) * 0.5f + 0.5f;

            for (int i = 1; i <= 4; i++)
            {
                float radius = 120f + i * 130f + pulse * 14f;
                PrimitiveDrawer.DrawCircle(sb, pixel, center, radius,
                    color * (0.05f * strength / i), 54, 1.5f);
            }

            // Twelve spokes, slowly counter-rotating.
            float spin = -time * 0.22f;
            for (int i = 0; i < 12; i++)
            {
                float a = spin + i * MathF.Tau / 12f;
                var dir = new Vector2(MathF.Cos(a), MathF.Sin(a));
                PrimitiveDrawer.DrawLine(sb, pixel,
                    center + dir * 150f, center + dir * 620f,
                    color * (0.035f * strength), 1f);
            }
        }

        // ── HUD bars ───────────────────────────────────────────────────────

        /// <summary>
        /// Segmented neon bar. Discrete ticks read far faster than a smooth gradient - the
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

        /// <summary>Thin countdown bar used for the weapon and diamond timers.</summary>
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

        /// <summary>
        /// Small diamond pip used by the HUD to show a stack level. Three pips, the filled ones
        /// lit, is faster to read than the number 3.
        /// </summary>
        public static void DrawDiamondPip(SpriteBatch sb, Texture2D pixel,
            Vector2 center, float size, Color color, bool filled)
        {
            Vector2 top = center + new Vector2(0f, -size);
            Vector2 right = center + new Vector2(size * 0.65f, 0f);
            Vector2 bottom = center + new Vector2(0f, size);
            Vector2 left = center + new Vector2(-size * 0.65f, 0f);

            if (filled)
            {
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, center, size * 0.75f, color * 0.85f);
                PrimitiveDrawer.DrawFilledCircle(sb, pixel, center, size * 0.35f,
                    Color.Lerp(color, Color.White, 0.7f));
            }

            Color edge = filled ? color : color * 0.28f;
            PrimitiveDrawer.DrawLine(sb, pixel, top, right, edge, 1f);
            PrimitiveDrawer.DrawLine(sb, pixel, right, bottom, edge, 1f);
            PrimitiveDrawer.DrawLine(sb, pixel, bottom, left, edge, 1f);
            PrimitiveDrawer.DrawLine(sb, pixel, left, top, edge, 1f);
        }
    }
}
