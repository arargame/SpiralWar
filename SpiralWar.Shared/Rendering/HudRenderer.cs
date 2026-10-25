using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Entities;
using SpiralWar.Levels;

namespace SpiralWar.Rendering
{
    /// <summary>
    /// Everything drawn on top of the arena. Kept out of GameScreen so the screen can stay
    /// about game STATE and this file can stay about LAYOUT (SRP).
    ///
    /// Positions are fractions of the virtual canvas wherever they can be, so the HUD survives
    /// the letterboxing on any aspect ratio without a second set of coordinates for phones.
    /// </summary>
    public sealed class HudRenderer
    {
        private const float Margin = 18f;
        private const float HealthBarWidth = 250f;
        private const float HealthBarHeight = 16f;
        private const int HealthTicks = 10;
        private const float RowHeight = 26f;

        private static readonly Color HealthHigh = new Color(0, 255, 170);
        private static readonly Color HealthMid = new Color(255, 200, 0);
        private static readonly Color HealthLow = new Color(255, 60, 70);
        private static readonly Color LabelColor = new Color(96, 126, 156);
        private static readonly Color FaintColor = new Color(52, 74, 96);

        /// <summary>Supplies in the order they are listed down the left edge.</summary>
        private static readonly SupplyType[] TimedSupplies =
        {
            SupplyType.DoubleFire,
            SupplyType.TripleFire,
            SupplyType.DiffusedFire,
            SupplyType.HomingMissile,
            SupplyType.Stasis
        };

        // ── Ship status, top left ──────────────────────────────────────────

        public void DrawHealth(DrawContext ctx, Player player, float time)
        {
            float percent = player.HealthPercent;

            // Colour communicates urgency before the player has read anything.
            Color color = percent > 0.55f
                ? HealthHigh
                : percent > 0.28f
                    ? HealthMid
                    : HealthLow;

            // Below a quarter the bar breathes, which catches the eye mid-fight.
            if (percent <= 0.28f)
            {
                float blink = MathF.Sin(time * 9f) * 0.5f + 0.5f;
                color = Color.Lerp(color * 0.55f, Color.White, blink * 0.35f);
            }

            var origin = new Vector2(Margin, Margin);

            ctx.UseHud();
            ctx.Batch.DrawString(ctx.GameFont, "HULL", origin, LabelColor,
                0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);

            var barPos = new Vector2(Margin, Margin + ctx.GameFont.LineSpacing * 0.72f + 4f);

            ctx.UseHudGlow();
            NeonRenderer.DrawNeonBar(ctx.Batch, ctx.Pixel, barPos,
                HealthBarWidth, HealthBarHeight, percent, HealthTicks, color, ctx.GlowScale);

            ctx.UseHud();
            string value = player.CurrentHealth + " / " + player.MaxHealth;
            ctx.Batch.DrawString(ctx.GameFont, value,
                new Vector2(barPos.X + HealthBarWidth + 12f, barPos.Y - 2f),
                color * 0.9f, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Active weapon supplies and diamond stacks, stacked down the left edge under the hull
        /// bar. Supplies come first because they change what the gun DOES; diamonds change how
        /// hard it hits, which matters less in the half second before a ring reaches you.
        /// </summary>
        public void DrawLoadout(DrawContext ctx, Player player)
        {
            PlayerLoadout loadout = player.Loadout;
            float y = Margin + ctx.GameFont.LineSpacing * 0.72f + HealthBarHeight + 16f;

            for (int i = 0; i < TimedSupplies.Length; i++)
            {
                SupplyType type = TimedSupplies[i];
                if (!loadout.IsActive(type)) continue;

                DrawSupplyRow(ctx, y, type, loadout);
                y += RowHeight;
            }

            if (loadout.ActiveDiamondCount == 0) return;

            y += 6f;

            for (int i = 0; i < DiamondTypeInfo.Count; i++)
            {
                var type = (DiamondType)i;
                int level = loadout.DiamondLevel(type);
                if (level <= 0) continue;

                DrawDiamondRow(ctx, y, type, level, loadout);
                y += RowHeight;
            }
        }

        private void DrawSupplyRow(DrawContext ctx, float y, SupplyType type, PlayerLoadout loadout)
        {
            Color color = SupplyTypeInfo.ColorOf(type);
            string label = SupplyTypeInfo.GlyphOf(type) + "  " + SupplyTypeInfo.NameOf(type);

            ctx.UseHud();
            ctx.Batch.DrawString(ctx.GameFont, label, new Vector2(Margin, y),
                color, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            ctx.Batch.DrawString(ctx.GameFont, loadout.TimeLeft(type).ToString("F1") + "s",
                new Vector2(Margin + 168f, y),
                color * 0.75f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            ctx.UseHudGlow();
            NeonRenderer.DrawTimerBar(ctx.Batch, ctx.Pixel,
                new Vector2(Margin, y + ctx.GameFont.LineSpacing * 0.7f + 2f),
                206f, 3f, loadout.Fraction(type), color);
        }

        private void DrawDiamondRow(DrawContext ctx, float y, DiamondType type, int level,
                                    PlayerLoadout loadout)
        {
            Color color = DiamondTypeInfo.ColorOf(type);

            // Three pips: a stack level is a quantity, and a row of lit pips is read faster
            // than a digit at the edge of vision.
            ctx.UseHudGlow();
            for (int pip = 0; pip < GameConstants.DiamondMaxLevel; pip++)
            {
                var center = new Vector2(Margin + 6f + pip * 13f, y + 7f);
                NeonRenderer.DrawDiamondPip(ctx.Batch, ctx.Pixel, center, 4.5f,
                    color, pip < level);
            }

            ctx.UseHud();
            ctx.Batch.DrawString(ctx.GameFont, DiamondTypeInfo.NameOf(type),
                new Vector2(Margin + 50f, y),
                color, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            ctx.Batch.DrawString(ctx.GameFont, loadout.DiamondTimeLeft(type).ToString("F1") + "s",
                new Vector2(Margin + 168f, y),
                color * 0.75f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            ctx.UseHudGlow();
            NeonRenderer.DrawTimerBar(ctx.Batch, ctx.Pixel,
                new Vector2(Margin, y + ctx.GameFont.LineSpacing * 0.7f + 2f),
                206f, 3f, loadout.DiamondFraction(type), color);
        }

        // ── Level status ───────────────────────────────────────────────────

        public void DrawLevelStatus(DrawContext ctx, LevelDefinition level,
                                    int aliveSegments, float secondsRemaining, float danger)
        {
            SpriteFont font = ctx.GameFont;

            // Level, sector and form, top right.
            ctx.UseHud();
            string levelText = "LEVEL " + level.Index + " / " + GameConstants.MaxLevel;
            Vector2 levelSize = font.MeasureString(levelText) * 0.85f;
            var levelPos = new Vector2(ctx.Width - Margin - levelSize.X, Margin);

            ctx.Batch.DrawString(font, levelText, levelPos, level.ThemeColor,
                0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

            string sector = level.IsBoss ? "** " + level.Title : level.Title;
            Vector2 sectorSize = font.MeasureString(sector) * 0.7f;
            ctx.Batch.DrawString(font, sector,
                new Vector2(ctx.Width - Margin - sectorSize.X, Margin + levelSize.Y + 2f),
                level.IsBoss ? new Color(255, 90, 90) : LabelColor,
                0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            string form = "FORM " + (int)level.Form + "  " + level.FormInfo.Name;
            Vector2 formSize = font.MeasureString(form) * 0.66f;
            ctx.Batch.DrawString(font, form,
                new Vector2(ctx.Width - Margin - formSize.X, Margin + levelSize.Y + sectorSize.Y + 4f),
                FaintColor, 0f, Vector2.Zero, 0.66f, SpriteEffects.None, 0f);

            // Remaining rings, bottom centre.
            string remaining = "RINGS  " + aliveSegments + " / " + level.SegmentCount;
            ctx.DrawTextCentered(font, remaining,
                new Vector2(ctx.Width * 0.5f, ctx.Height - 30f),
                Color.Lerp(LabelColor, level.ThemeColor, 0.35f), 0.82f);

            DrawBreachTimer(ctx, secondsRemaining, danger);
        }

        /// <summary>
        /// Countdown to the core breach. Turns red and grows as the head closes, so the player
        /// can feel the deadline without taking their eyes off the spiral.
        /// </summary>
        private void DrawBreachTimer(DrawContext ctx, float secondsRemaining, float danger)
        {
            Color color = Color.Lerp(FaintColor, new Color(255, 60, 60), danger);
            float scale = 0.7f + danger * 0.25f;

            // Three minute levels need minutes and seconds; a bare "137s" is not a time.
            float clamped = MathF.Max(0f, secondsRemaining);
            int minutes = (int)(clamped / 60f);
            int seconds = (int)(clamped % 60f);
            string text = "CORE BREACH IN " + minutes + ":" + seconds.ToString("00");

            var position = new Vector2(ctx.Width * 0.5f, ctx.Height - 56f);

            if (danger > 0.7f)
            {
                ctx.UseHudGlow();
                ctx.DrawGlowTextCentered(ctx.GameFont, text, position, color, scale, danger);
                return;
            }

            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, text, position, color, scale);
        }

        // ── Full-screen cards ──────────────────────────────────────────────

        /// <summary>Level intro card. Fades in, holds, fades out; never blocks input.</summary>
        public void DrawIntro(DrawContext ctx, LevelDefinition level, string banner, float alpha)
        {
            if (alpha <= 0.01f) return;

            var center = new Vector2(ctx.Width * 0.5f, ctx.Height * 0.32f);

            ctx.UseHudGlow();
            ctx.DrawGlowTextCentered(ctx.TitleFont, "LEVEL " + level.Index,
                center, level.ThemeColor * alpha, 0.9f, 1.4f);

            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, level.Title,
                center + new Vector2(0f, 46f), Color.White * (alpha * 0.85f), 0.95f);

            ctx.DrawTextCentered(ctx.GameFont,
                "FORM " + (int)level.Form + " - " + level.FormInfo.Name,
                center + new Vector2(0f, 74f),
                Color.Lerp(LabelColor, level.ThemeColor, 0.5f) * alpha, 0.78f);

            if (!string.IsNullOrEmpty(banner))
            {
                ctx.DrawTextCentered(ctx.GameFont, banner,
                    center + new Vector2(0f, 104f),
                    new Color(255, 200, 60) * alpha, 0.78f);
            }

            string brief = level.SegmentCount + " RINGS   -   " +
                           level.MinSegmentHealth + "-" + level.MaxSegmentHealth + " HP   -   " +
                           (level.SpiralDuration / 60f).ToString("0.0") + " MIN TO THE CORE";

            ctx.DrawTextCentered(ctx.GameFont, brief,
                center + new Vector2(0f, 134f), LabelColor * alpha, 0.72f);
        }

        /// <summary>Win/lose card shown while the screen waits to advance.</summary>
        public void DrawOutcome(DrawContext ctx, bool won, LevelDefinition level, float alpha,
                                int ringsDestroyed, int suppliesCollected, int diamondsCollected,
                                int chainKills)
        {
            if (alpha <= 0.01f) return;

            ctx.UseHud();
            PrimitiveDrawer.DrawFilledRect(ctx.Batch, ctx.Pixel,
                new Rectangle(0, 0, ctx.Width, ctx.Height),
                new Color(2, 4, 12) * (alpha * 0.78f));

            string headline = won
                ? (level.Index >= GameConstants.MaxLevel ? "CORE SECURED" : "LEVEL " + level.Index + " CLEARED")
                : "CORE BREACHED";

            Color color = won ? new Color(0, 255, 170) : new Color(255, 60, 60);
            var center = new Vector2(ctx.Width * 0.5f, ctx.Height * 0.40f);

            ctx.UseHudGlow();
            ctx.DrawGlowTextCentered(ctx.TitleFont, headline, center, color * alpha, 0.85f, 1.5f);

            ctx.UseHud();

            string line1 = "RINGS " + ringsDestroyed + "   -   CHAIN KILLS " + chainKills;
            ctx.DrawTextCentered(ctx.GameFont, line1,
                center + new Vector2(0f, 52f), LabelColor * alpha, 0.8f);

            string line2 = "SUPPLIES " + suppliesCollected + "   -   DIAMONDS " + diamondsCollected;
            ctx.DrawTextCentered(ctx.GameFont, line2,
                center + new Vector2(0f, 78f), LabelColor * alpha, 0.8f);

            string next = won
                ? (level.Index >= GameConstants.MaxLevel ? "RETURNING TO MENU" : "NEXT LEVEL STARTING")
                : "RETURNING TO MENU";

            ctx.DrawTextCentered(ctx.GameFont, next,
                center + new Vector2(0f, 110f), FaintColor * alpha, 0.74f);
        }

        /// <summary>Bottom-left control hint, faded out after the first few seconds of a run.</summary>
        public void DrawHint(DrawContext ctx, float alpha)
        {
            if (alpha <= 0.01f) return;

            ctx.UseHud();
            ctx.Batch.DrawString(ctx.GameFont, "[ESC] PAUSE",
                new Vector2(Margin, ctx.Height - 34f),
                FaintColor * alpha, 0f, Vector2.Zero, 0.72f, SpriteEffects.None, 0f);
        }
    }
}
