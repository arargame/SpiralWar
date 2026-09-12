using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core;

namespace SpiralWar.Rendering
{
    /// <summary>Which blend state the current SpriteBatch pass uses.</summary>
    public enum BlendMode
    {
        /// <summary>Standard alpha. Correct for UI, dimming overlays and text.</summary>
        Alpha,

        /// <summary>
        /// Additive. This is what turns flat outlines into real neon: overlapping glow layers
        /// accumulate light instead of averaging out to grey.
        /// </summary>
        Additive
    }

    /// <summary>
    /// Everything a screen needs in order to draw, plus control over the SpriteBatch passes.
    ///
    /// Screens never call Begin/End themselves. They ask for a mode — <see cref="UseWorld"/>,
    /// <see cref="UseWorldAlpha"/>, <see cref="UseHud"/> — and the context restarts the batch
    /// only when something actually changed. That keeps the draw-call count honest while
    /// letting the world glow additively and the HUD render normally.
    /// </summary>
    public sealed class DrawContext
    {
        public SpriteBatch Batch { get; }
        public Texture2D Pixel { get; }
        public SpriteFont GameFont { get; }
        public SpriteFont TitleFont { get; }
        public ViewportAdapter Viewport { get; }

        /// <summary>Camera shake applied to world passes only, in virtual pixels.</summary>
        public Vector2 ShakeOffset { get; set; }

        /// <summary>Scales every additive glow pass. Driven by the user's GlowScale setting.</summary>
        public float GlowScale { get; set; } = 1f;

        /// <summary>
        /// Uniform zoom about the centre of the canvas, used by screen transitions. A screen
        /// leaving pulls back to ~0.96 while it fades out and the incoming one settles from
        /// 1.04 to 1.0 — the small push is what makes a plain cross-fade read as a deliberate
        /// move between places rather than a dropped frame.
        /// </summary>
        public float ContentScale { get; set; } = 1f;

        public int Width => Viewport.VirtualWidth;
        public int Height => Viewport.VirtualHeight;
        public Vector2 Center => Viewport.VirtualCenter;

        private BlendMode _mode = BlendMode.Alpha;
        private bool _shakeApplied;
        private bool _open;

        public DrawContext(SpriteBatch batch, Texture2D pixel,
                           SpriteFont gameFont, SpriteFont titleFont,
                           ViewportAdapter viewport)
        {
            Batch = batch;
            Pixel = pixel;
            GameFont = gameFont;
            TitleFont = titleFont;
            Viewport = viewport;
        }

        // ── Pass control ───────────────────────────────────────────────────

        /// <summary>Opens the first pass of the frame. Called by the ScreenManager.</summary>
        public void BeginFrame()
        {
            _open = false;
            Begin(BlendMode.Alpha, applyShake: false);
        }

        /// <summary>Closes whatever pass is open. Called by the ScreenManager.</summary>
        public void EndFrame()
        {
            if (!_open) return;
            Batch.End();
            _open = false;
        }

        /// <summary>Additive world pass — neon, particles, bullets, the snake.</summary>
        public void UseWorld() => Switch(BlendMode.Additive, applyShake: true);

        /// <summary>Alpha world pass — text on top of world objects, dark cores.</summary>
        public void UseWorldAlpha() => Switch(BlendMode.Alpha, applyShake: true);

        /// <summary>Alpha pass without camera shake — HUD, menus, overlays.</summary>
        public void UseHud() => Switch(BlendMode.Alpha, applyShake: false);

        /// <summary>Additive pass without camera shake — HUD glow.</summary>
        public void UseHudGlow() => Switch(BlendMode.Additive, applyShake: false);

        private void Switch(BlendMode mode, bool applyShake)
        {
            if (_open && _mode == mode && _shakeApplied == applyShake) return;
            Begin(mode, applyShake);
        }

        private void Begin(BlendMode mode, bool applyShake)
        {
            if (_open) Batch.End();

            Matrix transform = Viewport.GetScaleMatrix();

            if (applyShake && ShakeOffset != Vector2.Zero)
                transform = Matrix.CreateTranslation(ShakeOffset.X, ShakeOffset.Y, 0f) * transform;

            if (MathF.Abs(ContentScale - 1f) > 0.0005f)
            {
                float cx = Width * 0.5f;
                float cy = Height * 0.5f;
                transform =
                    Matrix.CreateTranslation(-cx, -cy, 0f) *
                    Matrix.CreateScale(ContentScale, ContentScale, 1f) *
                    Matrix.CreateTranslation(cx, cy, 0f) *
                    transform;
            }

            Batch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: mode == BlendMode.Additive ? BlendState.Additive : BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp,
                depthStencilState: DepthStencilState.None,
                rasterizerState: RasterizerState.CullNone,
                effect: null,
                transformMatrix: transform);

            _mode = mode;
            _shakeApplied = applyShake;
            _open = true;
        }

        // ── Text helpers ───────────────────────────────────────────────────

        public void DrawText(SpriteFont font, string text, Vector2 position, Color color)
            => Batch.DrawString(font, text, position, color);

        public void DrawTextCentered(SpriteFont font, string text, Vector2 center, Color color,
                                     float scale = 1f)
        {
            Vector2 size = font.MeasureString(text) * scale;
            Batch.DrawString(font, text, center - size * 0.5f, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Text with an additive halo behind it. Must be called inside an additive pass
        /// for the halo to read as light rather than as a grey smear.
        /// </summary>
        public void DrawGlowTextCentered(SpriteFont font, string text, Vector2 center,
                                         Color color, float scale = 1f, float intensity = 1f)
        {
            Vector2 size = font.MeasureString(text) * scale;
            Vector2 pos = center - size * 0.5f;

            float glow = 0.16f * intensity * GlowScale;
            for (int i = 3; i >= 1; i--)
            {
                float a = glow / i;
                Batch.DrawString(font, text, pos + new Vector2(-i, 0), color * a,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                Batch.DrawString(font, text, pos + new Vector2(i, 0), color * a,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                Batch.DrawString(font, text, pos + new Vector2(0, -i), color * a,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                Batch.DrawString(font, text, pos + new Vector2(0, i), color * a,
                    0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            Batch.DrawString(font, text, pos, color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>Full-screen tint. Always drawn in the HUD alpha pass.</summary>
        public void FillScreen(Color color)
        {
            UseHud();
            PrimitiveDrawer.DrawFilledRect(Batch, Pixel,
                new Rectangle(0, 0, Width, Height), color);
        }
    }
}
