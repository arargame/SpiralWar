using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SpiralWar.Audio;
using SpiralWar.Core.Config;
using SpiralWar.Core.Input;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Options. Every row is a cycling value rather than a slider, so the same control works
    /// with a pointer, a keyboard and a gamepad without a second input path.
    ///
    /// Changes are written to settings.json immediately on leaving, and applied live, so the
    /// player hears the volume they picked while they are still on this screen.
    /// </summary>
    public sealed class SettingsScreen : BaseScreen
    {
        private static readonly float[] VolumeSteps = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        private static readonly float[] ShakeSteps = { 0f, 0.5f, 1f, 1.5f };
        private static readonly float[] GlowSteps = { 0.5f, 0.75f, 1f, 1.35f };

        private const int RowMaster = 0;
        private const int RowSfx = 1;
        private const int RowShake = 2;
        private const int RowGlow = 3;
        private const int RowDamageNumbers = 4;
        private const int RowFullScreen = 5;
        private const int RowBack = 6;

        public SettingsScreen(ScreenManager manager) : base(manager) { }

        public override void LoadContent(ContentManager content) => RebuildButtons();

        public override void Enter()
        {
            base.Enter();
            RebuildButtons();
            SelectedIndex = 0;
        }

        public override void Leave()
        {
            GameConfig.SaveSettings();
            base.Leave();
        }

        private void RebuildButtons()
        {
            GameSettings s = GameConfig.Settings;

            Buttons.Clear();
            Buttons.Add(new MenuButton("MASTER VOLUME", Percent(s.MasterVolume)));
            Buttons.Add(new MenuButton("SFX VOLUME", Percent(s.SfxVolume)));
            Buttons.Add(new MenuButton("SCREEN SHAKE", ShakeLabel(s.ScreenShakeScale)));
            Buttons.Add(new MenuButton("NEON GLOW", Percent(s.GlowScale / 1.35f)));
            Buttons.Add(new MenuButton("DAMAGE NUMBERS", s.ShowDamageNumbers ? "ON" : "OFF"));
            Buttons.Add(new MenuButton("FULL SCREEN", s.FullScreen ? "ON" : "OFF"));
            Buttons.Add(new MenuButton("BACK"));
        }

        private void RefreshLabels()
        {
            GameSettings s = GameConfig.Settings;
            Buttons[RowMaster].Detail = Percent(s.MasterVolume);
            Buttons[RowSfx].Detail = Percent(s.SfxVolume);
            Buttons[RowShake].Detail = ShakeLabel(s.ScreenShakeScale);
            Buttons[RowGlow].Detail = Percent(s.GlowScale / 1.35f);
            Buttons[RowDamageNumbers].Detail = s.ShowDamageNumbers ? "ON" : "OFF";
            Buttons[RowFullScreen].Detail = s.FullScreen ? "ON" : "OFF";
        }

        public override void Update(GameTime gameTime, InputState input)
        {
            base.Update(gameTime, input);

            // Left/right also cycles, which is what a gamepad user will reach for first.
            if (input.WasPressed(Microsoft.Xna.Framework.Input.Keys.Left))
                Cycle(SelectedIndex, -1);
            if (input.WasPressed(Microsoft.Xna.Framework.Input.Keys.Right))
                Cycle(SelectedIndex, 1);
        }

        protected override void OnButtonActivated(int index)
        {
            if (index == RowBack)
            {
                Manager.Pop();
                return;
            }

            Cycle(index, 1);
        }

        private void Cycle(int row, int direction)
        {
            GameSettings s = GameConfig.Settings;

            switch (row)
            {
                case RowMaster:
                    s.MasterVolume = Step(VolumeSteps, s.MasterVolume, direction);
                    break;
                case RowSfx:
                    s.SfxVolume = Step(VolumeSteps, s.SfxVolume, direction);
                    break;
                case RowShake:
                    s.ScreenShakeScale = Step(ShakeSteps, s.ScreenShakeScale, direction);
                    break;
                case RowGlow:
                    s.GlowScale = Step(GlowSteps, s.GlowScale, direction);
                    break;
                case RowDamageNumbers:
                    s.ShowDamageNumbers = !s.ShowDamageNumbers;
                    break;
                case RowFullScreen:
                    s.FullScreen = !s.FullScreen;
                    break;
                default:
                    return;
            }

            Manager.Audio.ApplySettings();
            Manager.Audio.Play(SoundId.MenuMove);
            RefreshLabels();
        }

        private static float Step(float[] steps, float current, int direction)
        {
            int nearest = 0;
            float bestDelta = float.MaxValue;

            for (int i = 0; i < steps.Length; i++)
            {
                float delta = MathF.Abs(steps[i] - current);
                if (delta >= bestDelta) continue;
                bestDelta = delta;
                nearest = i;
            }

            int next = (nearest + direction + steps.Length) % steps.Length;
            return steps[next];
        }

        private static string Percent(float value)
            => ((int)MathF.Round(MathHelper.Clamp(value, 0f, 1f) * 100f)) + "%";

        private static string ShakeLabel(float value)
        {
            if (value <= 0.01f) return "OFF";
            if (value < 0.75f) return "LOW";
            if (value < 1.25f) return "NORMAL";
            return "HIGH";
        }

        public override void Draw(DrawContext ctx, GameTime gameTime)
        {
            DrawBackdrop(ctx, NeonCyan, 0.6f);
            DrawTitle(ctx, "SETTINGS", ctx.Height * 0.16f, NeonCyan, 0.72f);

            float firstRow = ctx.Height * 0.32f;
            float spacing = MathF.Min(62f, (ctx.Height - firstRow - 64f) / Buttons.Count);
            DrawButtons(ctx, firstRow, spacing, NeonCyan);

            DrawFooter(ctx, "LEFT / RIGHT TO CHANGE   -   [ESC] BACK");
        }
    }
}
