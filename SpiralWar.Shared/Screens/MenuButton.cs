using Microsoft.Xna.Framework;

namespace SpiralWar.Screens
{
    /// <summary>
    /// One selectable row in a menu. A plain data holder: the screen owns the list, the
    /// <see cref="MenuLayout"/> lays it out and draws it, and neither of them has to know
    /// what the button actually does.
    /// </summary>
    public sealed class MenuButton
    {
        public string Label { get; set; }

        /// <summary>Optional smaller line under the label (current value, hint, level number).</summary>
        public string Detail { get; set; }

        /// <summary>False draws the row dimmed and skips it during navigation.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Accent colour. Defaults to the menu's own accent when left transparent.</summary>
        public Color Accent { get; set; } = Color.Transparent;

        /// <summary>Hit area, recalculated by the layout every frame.</summary>
        public Rectangle Bounds { get; set; }

        public MenuButton(string label, string detail = "")
        {
            Label = label;
            Detail = detail;
        }
    }
}
