# SpiralWar - instructions for AI assistants

Read `docs/README.md` and the document covering the area you are about to touch **before**
writing code. Update that document when you are done.

## Hard rules

- **English only** for code, identifiers, comments, commit messages and UI strings.
- **SOLID, DRY, YAGNI.** Put new behaviour behind an interface when a second implementation is
  genuinely plausible, and not before.
- **Stage deliberately.** Add the paths you changed, not `git add -A` from habit.
- **Desktop and mobile share `SpiralWar.Shared` entirely.** Platform-specific code belongs in a
  head project. Never call `Mouse`, `Keyboard` or `TouchPanel` outside `InputState`.
- **Work in virtual coordinates.** The canvas is 1280x720 and letterboxed by `ViewportAdapter`.
  Never hard-code a real pixel size.
- **No hard cuts.** Every screen change goes through `ScreenManager`, which fades.
- **No assets on disk.** Visuals are drawn from one white texel; sounds are synthesized at
  start-up. Do not add art or audio files without a strong reason.
- **The spritefonts are ASCII 32-126 only.** Do not put a non-ASCII character in a drawn string.

## Design invariants - do not break these casually

- A bullet always deals exactly **1 damage**, at every level. Difficulty is more rings, tougher
  rings and a faster spiral - never an invisible multiplier. Ring hit points stay small enough
  to read at a glance.
- **Every hit must be felt**: number, flash, knock-back, sparks, sound. If a change makes any of
  those less legible, the change is wrong.
- **Carrier rings show their glyph from spawn.** A drop rolled at the moment of death is a
  lottery; a drop declared in advance is a decision.
- **One fire sound per volley, not per bullet**, and always via `PlayVaried`.
- **NEW GAME resets the level only.** Identity and lifetime statistics survive.

## Where things go

| Change | File |
|---|---|
| Gameplay tuning value | `GameConstants` |
| Per-level difficulty | `LevelDesigner` |
| A new sound | `SoundId` + `SoundBank` + a throttle in `AudioManager` |
| A new screen | subclass `BaseScreen` |
| A new pickup | `SupplyType` + `SupplyTypeInfo` + `Supply.Collect` + `SnakeSegment.RollSupply` |
| Drawing a new shape | `PrimitiveDrawer` (generic) or `NeonRenderer` (styled) |
| HUD layout | `HudRenderer`, not `GameScreen` |

Anything added to `SpiralWar.Shared` must also be listed in `SpiralWar.Shared.projitems`.
