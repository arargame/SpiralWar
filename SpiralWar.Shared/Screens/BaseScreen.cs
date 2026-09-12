using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SpiralWar.Audio;
using SpiralWar.Core.Input;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Shared behaviour for every menu-style screen: keyboard/gamepad navigation, pointer
    /// hover, the neon button look and the title/footer furniture.
    ///
    /// Every menu in the game inherits this, which is the only reason MenuScreen,
    /// SettingsScreen, CreditsScreen and PauseScreen can look and feel identical without
    /// four copies of the same layout code (DRY).
    /// </summary>
    public abstract class BaseScreen : IScreen
    {
        protected ScreenManager Manager { get; }
        protected List<MenuButton> Buttons { get; } = new List<MenuButton>();
        protected int SelectedIndex { get; set; }

        protected float Pulse { get; private set; }
        protected float TimeInScreen { get; private set; }

        protected static readonly Color NeonCyan = new Color(0, 220, 255);
        protected static readonly Color NeonPink = new Color(255, 0, 160);
        protected static readonly Color DimText = new Color(96, 126, 156);
        protected static readonly Color FaintText = new Color(56, 78, 100);

        private Vector2 _lastPointer;
        private bool _pointerHasMoved;

        protected BaseScreen(ScreenManager manager) => Manager = manager;

        public virtual bool IsPopup => false;

        public virtual void LoadContent(ContentManager content) { }

        public virtual void Enter()
        {
            TimeInScreen = 0f;
            _pointerHasMoved = false;
        }

        public virtual void Leave() { }

        public virtual bool OnBack() => false;

        public virtual void Update(GameTime gameTime, InputState input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            TimeInScreen += dt;
            Pulse = MathF.Sin((float)gameTime.TotalGameTime.TotalSeconds * 3f) * 0.5f + 0.5f;

            UpdateMenuNavigation(input);
        }

        // ── Navigation ─────────────────────────────────────────────────────

        protected void UpdateMenuNavigation(InputState input)
        {
            if (Buttons.Count == 0) return;

            if (input.MenuUpPressed) MoveSelection(-1);
            if (input.MenuDownPressed) MoveSelection(1);

            // Pointer hover only takes over once the pointer has actually moved, so a
            // stationary mouse never fights with the keyboard for the selection.
            if (input.PointerPosition != _lastPointer)
            {
                _lastPointer = input.PointerPosition;
                _pointerHasMoved = true;
            }

            if (_pointerHasMoved)
            {
                for (int i = 0; i < Buttons.Count; i++)
                {
                    if (!Buttons[i].Enabled) continue;
                    if (!Buttons[i].Bounds.Contains((int)input.PointerPosition.X,
                                                    (int)input.PointerPosition.Y)) continue;

                    if (SelectedIndex != i)
                    {
                        SelectedIndex = i;
                        Manager.Audio.Play(SoundId.MenuMove);
                    }
                    break;
                }
            }

            bool pointerActivated = input.PointerPressed &&
                                    SelectedIndex >= 0 && SelectedIndex < Buttons.Count &&
                                    Buttons[SelectedIndex].Bounds.Contains(
                                        (int)input.PointerPosition.X,
                                        (int)input.PointerPosition.Y);

            if (input.ConfirmPressed || pointerActivated)
            {
                if (SelectedIndex >= 0 && SelectedIndex < Buttons.Count && Buttons[SelectedIndex].Enabled)
                {
                    Manager.Audio.Play(SoundId.MenuSelect);
                    OnButtonActivated(SelectedIndex);
                }
            }
        }

        private void MoveSelection(int direction)
        {
            int count = Buttons.Count;
            if (count == 0) return;

            int index = SelectedIndex;
            for (int i = 0; i < count; i++)
            {
                index = (index + direction + count) % count;
                if (!Buttons[index].Enabled) continue;

                if (index != SelectedIndex)
                {
                    SelectedIndex = index;
                    Manager.Audio.Play(SoundId.MenuMove);
                }
                return;
            }
        }

        /// <summary>Called when the player confirms a row.</summary>
        protected abstract void OnButtonActivated(int index);

        // ── Drawing ────────────────────────────────────────────────────────

        public abstract void Draw(DrawContext context, GameTime gameTime);

        /// <summary>
        /// Lays out and draws the button column, centered horizontally.
        /// Recalculates hit boxes every frame so a window resize can never desync them.
        /// </summary>
        protected void DrawButtons(DrawContext ctx, float firstRowCenterY, float rowSpacing,
                                   Color accent)
        {
            SpriteFont font = ctx.GameFont;
            float widest = 0f;

            for (int i = 0; i < Buttons.Count; i++)
            {
                float w = font.MeasureString(Buttons[i].Label).X;
                if (w > widest) widest = w;
            }

            float buttonWidth = MathF.Min(widest + 96f, ctx.Width * 0.86f);
            float lineHeight = font.LineSpacing;

            for (int i = 0; i < Buttons.Count; i++)
            {
                MenuButton button = Buttons[i];
                bool hasDetail = !string.IsNullOrEmpty(button.Detail);
                float buttonHeight = lineHeight + 22f + (hasDetail ? lineHeight * 0.8f : 0f);

                float centerY = firstRowCenterY + i * rowSpacing;
                var bounds = new Rectangle(
                    (int)(ctx.Width * 0.5f - buttonWidth * 0.5f),
                    (int)(centerY - buttonHeight * 0.5f),
                    (int)buttonWidth,
                    (int)buttonHeight);

                button.Bounds = bounds;

                Color rowAccent = button.Accent == Color.Transparent ? accent : button.Accent;
                DrawSingleButton(ctx, button, bounds, i == SelectedIndex, rowAccent, hasDetail);
            }
        }

        private void DrawSingleButton(DrawContext ctx, MenuButton button, Rectangle bounds,
                                      bool selected, Color accent, bool hasDetail)
        {
            SpriteFont font = ctx.GameFont;

            // Panel (alpha pass — it has to be able to DARKEN the background).
            ctx.UseHud();

            Color fill = !button.Enabled
                ? new Color(8, 10, 16) * 0.75f
                : selected
                    ? new Color(24, 8, 26) * 0.92f
                    : new Color(8, 12, 22) * 0.85f;

            PrimitiveDrawer.DrawFilledRect(ctx.Batch, ctx.Pixel, bounds, fill);

            // Neon frame and glow (additive).
            ctx.UseHudGlow();

            Color frame = !button.Enabled
                ? new Color(40, 48, 60)
                : selected ? accent : new Color(48, 66, 92);

            if (selected && button.Enabled)
            {
                float grow = 1f + Pulse * 0.35f;
                for (int g = 3; g >= 1; g--)
                {
                    var halo = new Rectangle(
                        bounds.X - (int)(g * 2 * grow), bounds.Y - (int)(g * 2 * grow),
                        bounds.Width + (int)(g * 4 * grow), bounds.Height + (int)(g * 4 * grow));
                    PrimitiveDrawer.DrawHollowRect(ctx.Batch, ctx.Pixel, halo,
                        accent * (0.16f / g) * ctx.GlowScale, 1);
                }

                // Leading edge marker.
                PrimitiveDrawer.DrawFilledRect(ctx.Batch, ctx.Pixel,
                    new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), accent);
            }

            PrimitiveDrawer.DrawHollowRect(ctx.Batch, ctx.Pixel, bounds, frame, 1);

            // Label.
            Color labelColor = !button.Enabled
                ? new Color(70, 82, 96)
                : selected ? Color.White : new Color(132, 162, 190);

            float labelY = hasDetail
                ? bounds.Y + 12f
                : bounds.Center.Y - font.LineSpacing * 0.5f;

            Vector2 labelSize = font.MeasureString(button.Label);
            var labelPos = new Vector2(bounds.Center.X - labelSize.X * 0.5f, labelY);

            if (selected && button.Enabled)
            {
                ctx.Batch.DrawString(font, button.Label, labelPos + new Vector2(-1, 0), accent * 0.45f);
                ctx.Batch.DrawString(font, button.Label, labelPos + new Vector2(1, 0), accent * 0.45f);
            }

            ctx.UseHud();
            ctx.Batch.DrawString(font, button.Label, labelPos, labelColor);

            if (!hasDetail) return;

            Vector2 detailSize = font.MeasureString(button.Detail) * 0.75f;
            ctx.Batch.DrawString(font, button.Detail,
                new Vector2(bounds.Center.X - detailSize.X * 0.5f, labelY + font.LineSpacing * 0.95f),
                selected ? accent * 0.95f : DimText,
                0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        }

        /// <summary>Big glowing headline with a rule under it.</summary>
        protected void DrawTitle(DrawContext ctx, string title, float centerY, Color accent,
                                 float scale = 1f)
        {
            ctx.UseHudGlow();

            SpriteFont font = ctx.TitleFont;
            float pulseScale = scale * (1f + Pulse * 0.025f);

            ctx.DrawGlowTextCentered(font, title,
                new Vector2(ctx.Width * 0.5f, centerY), accent, pulseScale, 1.3f);

            Vector2 size = font.MeasureString(title) * pulseScale;
            float lineY = centerY + size.Y * 0.5f + 8f;
            float halfWidth = MathF.Min(size.X * 0.62f, ctx.Width * 0.42f);

            PrimitiveDrawer.DrawLine(ctx.Batch, ctx.Pixel,
                new Vector2(ctx.Width * 0.5f - halfWidth, lineY),
                new Vector2(ctx.Width * 0.5f + halfWidth, lineY),
                accent * 0.7f, 1.5f);
        }

        /// <summary>Small hint line at the bottom of the screen.</summary>
        protected void DrawFooter(DrawContext ctx, string text)
        {
            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, text,
                new Vector2(ctx.Width * 0.5f, ctx.Height - 34f), FaintText, 0.8f);
        }

        /// <summary>Draws the shared arena backdrop so menus and gameplay share one world.</summary>
        protected void DrawBackdrop(DrawContext ctx, Color accent, float gridIntensity = 1f)
        {
            ctx.UseWorld();
            Manager.Background.Draw(ctx.Batch, ctx.Pixel, accent, 0.9f);
            NeonRenderer.DrawGrid(ctx.Batch, ctx.Pixel, ctx.Width, ctx.Height,
                accent, 60, gridIntensity);
        }
    }
}
