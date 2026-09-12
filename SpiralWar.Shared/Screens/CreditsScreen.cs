using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Core.Config;
using SpiralWar.Entities;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Credits and support.
    ///
    /// The support half matters more than the credits half: it shows the player's anonymous
    /// PROFILE ID. When somebody reports a bug or a lost save, that eight character code is
    /// what ties their report to their profile document without ever asking them for personal
    /// information.
    /// </summary>
    public sealed class CreditsScreen : BaseScreen
    {
        private readonly string[] _creditLines;

        public CreditsScreen(ScreenManager manager) : base(manager)
        {
            _creditLines = new[]
            {
                "DESIGN  -  CODE  -  AUDIO",
                GameConstants.StudioName,
                "",
                "BUILT WITH MONOGAME AND .NET",
                "ALL VISUALS AND SOUNDS GENERATED AT RUNTIME",
                "NO EXTERNAL ART OR AUDIO ASSETS"
            };
        }

        public override void LoadContent(ContentManager content)
        {
            Buttons.Clear();
            Buttons.Add(new MenuButton("BACK"));
            SelectedIndex = 0;
        }

        protected override void OnButtonActivated(int index) => Manager.Pop();

        public override void Draw(DrawContext ctx, GameTime gameTime)
        {
            DrawBackdrop(ctx, NeonPink, 0.55f);
            DrawTitle(ctx, "CREDITS & SUPPORT", ctx.Height * 0.15f, NeonPink, 0.6f);

            float y = ctx.Height * 0.27f;
            y = DrawCredits(ctx, y);
            y = DrawSupply(ctx, y + 14f);
            DrawSupport(ctx, y + 18f);

            float buttonY = ctx.Height - 96f;
            DrawButtons(ctx, buttonY, 60f, NeonPink);

            DrawFooter(ctx, "[ESC] BACK");
        }

        private float DrawCredits(DrawContext ctx, float y)
        {
            ctx.UseHud();
            SpriteFont font = ctx.GameFont;

            for (int i = 0; i < _creditLines.Length; i++)
            {
                string line = _creditLines[i];
                if (line.Length > 0)
                {
                    Color color = i == 1 ? NeonCyan : DimText;
                    float scale = i == 1 ? 1f : 0.82f;
                    ctx.DrawTextCentered(font, line, new Vector2(ctx.Width * 0.5f, y), color, scale);
                }
                y += font.LineSpacing * 0.9f;
            }

            return y;
        }

        /// <summary>Legend for the three pickups — this doubles as the game's how-to-play.</summary>
        private float DrawSupply(DrawContext ctx, float y)
        {
            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, "SUPPLY DROPS",
                new Vector2(ctx.Width * 0.5f, y), NeonPink * 0.9f, 0.85f);

            y += ctx.GameFont.LineSpacing * 1.4f;

            var types = new[] { SupplyType.TripleFire, SupplyType.DiffusedFire, SupplyType.HealthPack };
            float spacing = MathF.Min(210f, ctx.Width / 4f);
            float startX = ctx.Width * 0.5f - spacing;

            for (int i = 0; i < types.Length; i++)
            {
                SupplyType type = types[i];
                Color color = SupplyTypeInfo.ColorOf(type);
                var center = new Vector2(startX + spacing * i, y);

                ctx.UseWorld();
                NeonRenderer.DrawNeonSupplyShell(ctx.Batch, ctx.Pixel, center, color,
                    GameConstants.SupplySize, MathF.PI / 4f, Pulse);

                string glyph = SupplyTypeInfo.GlyphOf(type);
                ctx.DrawTextCentered(ctx.GameFont, glyph, center,
                    Color.Lerp(color, Color.White, 0.7f));

                ctx.UseHud();
                ctx.DrawTextCentered(ctx.GameFont, SupplyTypeInfo.NameOf(type),
                    center + new Vector2(0f, 32f), DimText, 0.68f);
            }

            return y + 56f;
        }

        private void DrawSupport(DrawContext ctx, float y)
        {
            ctx.UseHud();
            SpriteFont font = ctx.GameFont;
            PlayerProfile profile = GameConfig.Profile;

            ctx.DrawTextCentered(font, "SUPPORT",
                new Vector2(ctx.Width * 0.5f, y), NeonPink * 0.9f, 0.85f);

            y += font.LineSpacing * 1.2f;

            ctx.DrawTextCentered(font, GameConstants.SupportEmail,
                new Vector2(ctx.Width * 0.5f, y), NeonCyan * 0.9f, 0.8f);

            y += font.LineSpacing * 0.95f;

            ctx.DrawTextCentered(font, GameConstants.RepositoryUrl,
                new Vector2(ctx.Width * 0.5f, y), DimText, 0.72f);

            y += font.LineSpacing * 1.25f;

            // The line that makes a support ticket actionable.
            ctx.DrawTextCentered(font, $"PROFILE ID   {profile.ShortId}",
                new Vector2(ctx.Width * 0.5f, y), new Color(150, 180, 210), 0.78f);

            y += font.LineSpacing * 0.9f;

            string stats = $"BEST LEVEL {profile.HighestLevelReached}   -   " +
                           $"RINGS DESTROYED {profile.TotalSegmentsDestroyed}   -   " +
                           $"RUNS LOST {profile.TotalDeaths}";

            ctx.DrawTextCentered(font, stats,
                new Vector2(ctx.Width * 0.5f, y), FaintText, 0.68f);
        }
    }
}
