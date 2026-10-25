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
- **The game is 2D.** 3D was tried and parked in `git stash`; do not reintroduce it.

## Design invariants - do not break these casually

- **Every level is three minutes.** `GameConstants.LevelDurationSeconds`, constant for the whole
  campaign. The health pool is derived from it, not the other way round.
- **A BASE bullet deals exactly 1 damage.** Crits, power attacks, acid, missiles and dynamite
  exceed that, but each announces itself with its own bullet colour, number colour and sound.
  The rule is "the player can account for every point", not "the number is always 1".
- **Ring health stays two digits.** If a retune pushes printed HP past 99, the label layout
  needs revisiting first.
- **Armor is double hit points, never damage reduction.** `max(1, 1/2) == 1`, so reduction is
  silently dead code against a 1-damage bullet.
- **Resolve `armoredChance` before deriving average ring health**, and divide by `(1 + armor)`.
  Forgetting this inflates the real pool by up to 40% at the top of the campaign.
- **Count factor x health factor is ~1 for every SnakeForm.** A form changes how a level feels,
  not how long it takes.
- **Carrier rings show their payload glyph from spawn.** A drop rolled at death is a lottery;
  a drop declared in advance is a decision.
- **A ring is at most one special thing** - dynamite, or a supply, or a diamond, or plain. Armor
  is the only thing that combines.
- **One fire sound per volley, not per bullet**, and always via `PlayVaried`.
- **Dynamite chains iteratively, never recursively** through the kill event.
- **NEW GAME resets the level only.** Identity and lifetime statistics survive.

## Where things go

| Change | File |
|---|---|
| Gameplay tuning value | `GameConstants` |
| Per-level difficulty | `LevelDesigner` |
| How a snake form feels | `SnakeFormInfo` |
| A new sound | `SoundId` + `SoundBank` + a throttle in `AudioManager` |
| A new screen | subclass `BaseScreen` |
| A new supply | `SupplyType` + `SupplyTypeInfo` + `Player.ApplySupply` |
| A new diamond | `DiamondType` + `DiamondTypeInfo` + `PlayerLoadout` |
| A timed player effect | `PlayerLoadout`, never a loose timer on `Player` |
| Drawing a new shape | `PrimitiveDrawer` (generic) or `NeonRenderer` (styled) |
| HUD layout | `HudRenderer`, not `GameScreen` |

Anything added to `SpiralWar.Shared` must also be listed in `SpiralWar.Shared.projitems` -
the shared project does not glob.

## Verifying a change

There is no .NET SDK in the desktop workspace VM, but a full type check is possible:
stage the sources plus a `MonoGame.Framework.dll` into the cloud container and run `csc` from
`dotnet-sdk-8.0` against the net8 reference pack. A net9-targeted MonoGame DLL type-checks
fine. Do this at minimum before handing code over.

After any `LevelDesigner` retune, re-run the balance model in `docs/level-design.md` and
confirm every level still clears inside its 180 second timer.
