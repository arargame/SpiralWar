using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Pause overlay. It is a POPUP: the ScreenManager keeps drawing the frozen arena behind
    /// it and dims it, so the player never loses sight of the board they are about to go back
    /// to. Backing out resumes rather than quitting, which is what everyone expects.
    /// </summary>
    public sealed class PauseScreen : BaseScreen
    {
        private readonly int _level;

        public PauseScreen(ScreenManager manager, int level) : base(manager) => _level = level;

        public override bool IsPopup => true;

        public override void LoadContent(ContentManager content)
        {
            Buttons.Clear();
            Buttons.Add(new MenuButton("RESUME"));
            Buttons.Add(new MenuButton("SETTINGS"));
            Buttons.Add(new MenuButton("QUIT TO MENU", "PROGRESS IS SAVED"));
            SelectedIndex = 0;
        }

        public override bool OnBack()
        {
            Manager.Pop();
            return true;
        }

        protected override void OnButtonActivated(int index)
        {
            switch (index)
            {
                case 0:
                    Manager.Pop();
                    break;
                case 1:
                    Manager.Push(new SettingsScreen(Manager));
                    break;
                default:
                    Manager.PopToRoot();
                    break;
            }
        }

        public override void Draw(DrawContext ctx, GameTime gameTime)
        {
            DrawTitle(ctx, "PAUSED", ctx.Height * 0.26f, NeonCyan, 0.7f);

            ctx.UseHud();
            ctx.DrawTextCentered(ctx.GameFont, $"LEVEL {_level}",
                new Vector2(ctx.Width * 0.5f, ctx.Height * 0.26f + 44f), DimText, 0.82f);

            DrawButtons(ctx, ctx.Height * 0.48f, 66f, NeonPink);
            DrawFooter(ctx, "[ESC] RESUME");
        }
    }
}
