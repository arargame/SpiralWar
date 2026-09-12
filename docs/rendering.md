# Rendering

Everything is drawn from a single 1x1 white texel. There is no art on disk, no shader and no
render target.

## Why additive blending is the whole trick

The first version of this renderer drew its glow layers with alpha blending. Overlapping
translucent copies of a shape **average** toward grey, so a stack of halos produced fog rather
than light. Switching the world pass to `BlendState.Additive` makes overlapping layers
**accumulate**, which is exactly what a real neon tube does.

Every neon shape is then built from three ingredients, always in this order:

1. a few expanding, very low alpha copies of the shape - the **halo**
2. the shape itself at full colour - the **tube**
3. a smaller, whiter copy inside it - the **hot filament**

The same code in an alpha pass looks flat. The same code in an additive pass looks lit.

The consequence is that the world pass cannot *darken* anything, so there are no dark fills in
the arena - the near-black background shows through the rings. That is not a compromise; on a
neon wireframe look it is what you want. Anything that genuinely needs to darken (the pause
dim, the outcome overlay, the transition fade, menu panels) is drawn in an alpha HUD pass.

`DrawContext` owns this. Screens call `UseWorld()` / `UseWorldAlpha()` / `UseHud()` /
`UseHudGlow()` and the context restarts the `SpriteBatch` only when the mode actually changed.

## Line thickness

`PrimitiveDrawer.DrawLine` uses origin `(0, 0.5)`, so a line is **centred** on the segment it
connects. With the origin at `(0,0)` every stroke drifts half its thickness downward, which is
what made thick glow layers look lopsided in v1. Because glow layers are drawn at increasing
thickness, that error grows with the layer index and the halo ends up visibly offset from the
tube.

## Damage feedback

This is the part the game is built around. When a ring is hit:

| Channel | Implementation |
|---|---|
| The number | `SnakeSegment` prints `CurrentHealth`; a bullet always deals 1 |
| Flash | `HitFlash` 1 -> 0 over 120 ms; a white filled core, a snapping outer shell at +22% scale, and a second shell 10 px out |
| Knock-back | `_knock` = 5.5 px along the ring's own axis, springing back at 42 px/s |
| Sparks | `ParticleSystem.EmitHit` - 6 sparks in a 120 degree cone back along the bullet's path |
| Shake | 1.6 units of trauma |
| Sound | Pitched by `damageApplied / maxHealth` |
| Number | `FloatingTextSystem.ShowDamage`, with random sideways drift so a burst does not stack |

On destruction: a 52 px shockwave ring, ~9 shards, ~12 core sparks, 5.5 units of trauma (x1.25
for armored) and the destruction sound. Then the carried supply is released.

## Camera shake

`CameraShake` is trauma-based. Impacts **add trauma** rather than setting an amplitude, and the
offset is `trauma^2 * maxOffset`. Two consequences:

- small hits barely register while a destroyed ring lands hard (the squaring)
- stacking many small hits cannot snowball into an unreadable screen (trauma clamps at 1)

Motion is a sum of three sine waves rather than per-frame random offsets, which reads as a
camera being shoved rather than as video noise. Trauma decays linearly at 1.8/s - linear feels
sharper than exponential here.

Shake is applied to world passes only; the HUD never moves. It respects
`GameSettings.ScreenShakeScale`, including 0.

## Background

`Starfield` - 200 stars in three parallax bands, **orbiting the core** rather than scrolling
sideways. That quietly restates the one idea the game is about: everything is being pulled into
the middle. Stars are tinted 55% toward the tier accent colour, so the sky shifts as the
campaign progresses.

`NeonRenderer.DrawGrid` fades toward the edges of the screen and brightens with the danger
value, so the arena visibly tightens as the head closes.

`NeonRenderer.DrawCoreRing` pulses faster and redder as `Snake.Danger` rises - peripheral
warning that does not require reading the timer.

## HUD

`HudRenderer` keeps layout out of `GameScreen`. Positions are fractions of the virtual canvas
wherever possible, so the HUD survives letterboxing on any aspect ratio.

The hull bar is **segmented into 10 discrete ticks** rather than being a smooth gradient.
Discrete ticks can be counted at a glance; a smooth bar has to be estimated. Below 28% it
switches to red and breathes.

## Virtual resolution

The game is authored at 1280x720. `ViewportAdapter` computes a uniform scale and a centring
offset onto the real back buffer and hands `DrawContext` a transform matrix; `InputState`
inverts the same transform so a pointer position is always in virtual coordinates.

`ScreenManager.DrawLetterbox` paints the bars outside the canvas, because a resized window
otherwise shows whatever the previous frame left in those pixels.

## Transitions

Fade out over `TransitionDuration/2`, apply the pending stack change, fade in. The fade curve
is smoothstepped so the darkest moment is brief. `DrawContext.ContentScale` adds a small
counter-zoom - the outgoing screen pulls back to 0.96, the incoming settles from 1.04 - which
is what makes a cross-fade read as a deliberate move between places rather than a dropped
frame.

## Performance budget

A boss spiral is 100-130 rings. Each ring costs roughly 50 sprite draws once its four glow
layers, tube, filament and four corner studs are counted, so a fully visible boss snake would
be ~6,500 sprites per frame before particles.

Mitigations, in order of value:

1. **Off-screen culling** (`Snake.IsOnScreen`, 70 px margin). Most of a boss snake is still
   off-stage for the first half of the level, so this is the single cheapest win available.
2. **`GameSettings.GlowScale`** scales every additive halo, letting a weak GPU drop the most
   expensive part of the look without losing the shape.
3. **Pooling** - bullets (512), particles (900), floating text (64), supplies (48) are all
   pre-allocated, so frame time stays flat during the heaviest moments instead of spiking
   exactly when the player can least afford it.

If frame time becomes a problem beyond that, the next step is reducing `GlowLayers` from 4 to 3
for rings below a size threshold, not reducing ring counts.
