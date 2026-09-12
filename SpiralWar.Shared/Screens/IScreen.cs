using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SpiralWar.Core.Input;
using SpiralWar.Rendering;

namespace SpiralWar.Screens
{
    /// <summary>
    /// Contract every screen implements.
    ///
    /// The lifecycle is deliberately explicit — LoadContent once, then Enter/Leave every time
    /// the screen becomes or stops being the active one. A screen pushed on top of another
    /// (the pause menu, a confirmation) gets Leave on the one below it, so a screen always
    /// knows whether it currently owns the input.
    /// </summary>
    public interface IScreen
    {
        /// <summary>Called once, when the screen is first pushed.</summary>
        void LoadContent(ContentManager content);

        /// <summary>Called every time this screen becomes the top of the stack.</summary>
        void Enter();

        /// <summary>Called when this screen stops being the top of the stack.</summary>
        void Leave();

        void Update(GameTime gameTime, InputState input);

        void Draw(DrawContext context, GameTime gameTime);

        /// <summary>
        /// True when the screen below should still be DRAWN behind this one (pause overlays,
        /// confirmations). False for full screens.
        /// </summary>
        bool IsPopup { get; }

        /// <summary>
        /// Handles the back gesture (Escape, gamepad B, Android back).
        /// Return true when the screen consumed it; false lets the ScreenManager pop.
        /// </summary>
        bool OnBack();
    }
}
