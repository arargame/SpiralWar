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
    ///
    /// The pickup legend in the middle doubles as the game's how-to-play, and it is drawn with
    /// the REAL renderers and the REAL colour tables, so it can never drift out of sync with
    /// what the arena actually shows.
    /// </summary>
    public sealed class CreditsScreen : BaseScreen
    {
        private static readonly SupplyType[] Supplies =
        {
            SupplyType.DoubleFire, SupplyType.TripleFire, SupplyType.DiffusedFire,
            SupplyType.HomingMissile, SupplyType.Stasis, SupplyType.HealthPack
        };

        private static readonly DiamondType[] Diamonds =
        {
            DiamondType.AttackSpeed, DiamondType.Critical,
            DiamondType.Acidic, DiamondType.Power
        };

        public CreditsScreen(ScreenManager manager) : base(manager) { }

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
            DrawTitle(ctx, "CREDITS & SUPPORT", ctx.Height * 0.11f, NeonPink, 0.58f);

            float y = ctx.Height * 0.21f;
            y = DrawSupplyLegend(ctx, y);
            y = DrawDiamondLegend(ctx, y + 18f);
            DrawSupport(ctx, y + 22f);

            DrawButtons(ctx, ctx.Height - 72f, 60f, NeonPink);
            DrawFooter(ctx, "[ESC] BACK");
        }

        private float DrawSupplyLegend(DrawContext ctx, float y)
        {
            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, "SUPPLY DROPS - THEY CHANGE THE GUN",
                new Vector2(ctx.Width * 0.5f, y), NeonPink * 0.9f, 0.78f);

            y += ctx.GameFont.LineSpacing * 1.5f;
            DrawRow(ctx, y, Supplies.Length, (i, center) =>
            {
                SupplyType type = Supplies[i];
                Color color = SupplyTypeInfo.ColorOf(type);

                ctx.UseWorld();
                NeonRenderer.DrawNeonSupplyShell(ctx.Batch, ctx.Pixel, center, color,
                    GameConstants.SupplySize * 0.9f, MathF.PI / 4f, Pulse);

                ctx.DrawTextCentered(ctx.GameFont, SupplyTypeInfo.GlyphOf(type), center,
                    Color.Lerp(color, Color.White, 0.7f));

                ctx.UseHud();
                ctx.DrawTextCentered(ctx.GameFont, SupplyTypeInfo.NameOf(type),
                    center + new Vector2(0f, 28f), DimText, 0.6f);
            });

            return y + 48f;
        }

        private float DrawDiamondLegend(DrawContext ctx, float y)
        {
            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, "NEON DIAMONDS - THEY CHANGE THE NUMBERS",
                new Vector2(ctx.Width * 0.5f, y), NeonCyan * 0.9f, 0.78f);

            y += ctx.GameFont.LineSpacing * 1.5f;
            DrawRow(ctx, y, Diamonds.Length, (i, center) =>
            {
                DiamondType type = Diamonds[i];
                Color color = DiamondTypeInfo.ColorOf(type);

                ctx.UseWorld();
                NeonRenderer.DrawNeonDiamond(ctx.Batch, ctx.Pixel, center, color,
                    DiamondTypeInfo.CoreColorOf(type),
                    GameConstants.DiamondSize * 0.95f, Pulse * MathF.PI, Pulse);

                ctx.UseHud();
                ctx.DrawTextCentered(ctx.GameFont, DiamondTypeInfo.NameOf(type),
                    center + new Vector2(0f, 30f), color * 0.9f, 0.62f);
                ctx.DrawTextCentered(ctx.GameFont, DiamondTypeInfo.EffectOf(type),
                    center + new Vector2(0f, 46f), FaintText, 0.55f);
            });

            return y + 64f;
        }

        /// <summary>Evenly spaced row of icons, centred and clamped to the canvas width.</summary>
        private static void DrawRow(DrawContext ctx, float y, int count, Action<int, Vector2> draw)
        {
            if (count <= 0) return;

            float spacing = MathF.Min(190f, (ctx.Width * 0.82f) / count);
            float startX = ctx.Width * 0.5f - spacing * (count - 1) * 0.5f;

            for (int i = 0; i < count; i++)
                draw(i, new Vector2(startX + spacing * i, y));
        }

        private void DrawSupport(DrawContext ctx, float y)
        {
            ctx.UseHud();
            SpriteFont font = ctx.GameFont;
            PlayerProfile profile = GameConfig.Profile;

            ctx.DrawTextCentered(font, GameConstants.StudioName,
                new Vector2(ctx.Width * 0.5f, y), NeonCyan, 0.9f);

            y += font.LineSpacing * 1.1f;

            ctx.DrawTextCentered(font,
                GameConstants.SupportEmail + "   -   " + GameConstants.RepositoryUrl,
                new Vector2(ctx.Width * 0.5f, y), DimText, 0.68f);

            y += font.LineSpacing * 1.2f;

            // The line that makes a support ticket actionable.
            ctx.DrawTextCentered(font, "PROFILE ID   " + profile.ShortId,
                new Vector2(ctx.Width * 0.5f, y), new Color(150, 180, 210), 0.74f);

            y += font.LineSpacing * 0.9f;

            string stats = "BEST LEVEL " + profile.HighestLevelReached +
                           "   -   RINGS DESTROYED " + profile.TotalSegmentsDestroyed +
                           "   -   RUNS LOST " + profile.TotalDeaths;

            ctx.DrawTextCentered(font, stats,
                new Vector2(ctx.Width * 0.5f, y), FaintText, 0.64f);
        }
    }
}
