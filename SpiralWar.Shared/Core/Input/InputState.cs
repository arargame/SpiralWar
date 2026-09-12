using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace SpiralWar.Core.Input
{
    /// <summary>
    /// One place that reads every input device and publishes a device neutral view of it
    /// in VIRTUAL coordinates.
    ///
    /// Screens and entities never call Mouse/Keyboard/TouchPanel directly. That keeps the
    /// desktop and the mobile build on exactly the same code path: a finger on a phone and
    /// a mouse on a PC both produce Pointer* state, and Escape / the Android back button
    /// both produce <see cref="BackPressed"/>.
    /// </summary>
    public sealed class InputState
    {
        private readonly ViewportAdapter _viewport;

        private KeyboardState _keys, _prevKeys;
        private MouseState _mouse, _prevMouse;
        private GamePadState _pad, _prevPad;

        private bool _pointerDown, _prevPointerDown;
        private Vector2 _pointerPosition;

        public InputState(ViewportAdapter viewport) => _viewport = viewport;

        // ── Pointer (mouse on desktop, first finger on touch) ──────────────
        public Vector2 PointerPosition => _pointerPosition;
        public bool PointerDown => _pointerDown;
        public bool PointerPressed => _pointerDown && !_prevPointerDown;
        public bool PointerReleased => !_pointerDown && _prevPointerDown;

        /// <summary>True while the player is asking to shoot (hold to fire).</summary>
        public bool FireHeld => _pointerDown;

        // ── Navigation ─────────────────────────────────────────────────────
        public bool BackPressed =>
            WasPressed(Keys.Escape) ||
            WasPressed(Keys.Back) ||
            (_pad.Buttons.B == ButtonState.Pressed && _prevPad.Buttons.B == ButtonState.Released) ||
            (_pad.Buttons.Back == ButtonState.Pressed && _prevPad.Buttons.Back == ButtonState.Released);

        public bool MenuUpPressed =>
            WasPressed(Keys.Up) || WasPressed(Keys.W) ||
            (_pad.DPad.Up == ButtonState.Pressed && _prevPad.DPad.Up == ButtonState.Released);

        public bool MenuDownPressed =>
            WasPressed(Keys.Down) || WasPressed(Keys.S) ||
            (_pad.DPad.Down == ButtonState.Pressed && _prevPad.DPad.Down == ButtonState.Released);

        public bool ConfirmPressed =>
            WasPressed(Keys.Enter) || WasPressed(Keys.Space) ||
            (_pad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A == ButtonState.Released);

        public bool PausePressed => WasPressed(Keys.P) ||
            (_pad.Buttons.Start == ButtonState.Pressed && _prevPad.Buttons.Start == ButtonState.Released);

        // ── Raw helpers ────────────────────────────────────────────────────
        public bool IsKeyDown(Keys key) => _keys.IsKeyDown(key);
        public bool WasPressed(Keys key) => _keys.IsKeyDown(key) && _prevKeys.IsKeyUp(key);

        /// <summary>Reads every device. Call exactly once per frame, before any screen update.</summary>
        public void Update()
        {
            _prevKeys = _keys;
            _prevMouse = _mouse;
            _prevPad = _pad;
            _prevPointerDown = _pointerDown;

            _keys = Keyboard.GetState();
            _mouse = Mouse.GetState();
            _pad = GamePad.GetState(PlayerIndex.One);

            // Touch wins when a finger is on the glass, otherwise fall back to the mouse.
            if (!TryReadTouch())
            {
                _pointerPosition = _viewport.ToVirtual(_mouse.X, _mouse.Y);
                _pointerDown = _mouse.LeftButton == ButtonState.Pressed;
            }
        }

        private bool TryReadTouch()
        {
            TouchCollection touches = TouchPanel.GetState();
            if (touches.Count == 0) return false;

            for (int i = 0; i < touches.Count; i++)
            {
                TouchLocation t = touches[i];
                if (t.State == TouchLocationState.Invalid || t.State == TouchLocationState.Released)
                    continue;

                _pointerPosition = _viewport.ToVirtual(t.Position.X, t.Position.Y);
                _pointerDown = true;
                return true;
            }

            // All fingers lifted this frame.
            _pointerDown = false;
            return true;
        }

        /// <summary>
        /// Drops the "just pressed" edges. Call after a screen change so the click that
        /// opened a screen is not also consumed by the new one.
        /// </summary>
        public void ConsumeEdges()
        {
            _prevKeys = _keys;
            _prevMouse = _mouse;
            _prevPad = _pad;
            _prevPointerDown = _pointerDown;
        }
    }
}
