# Changelog

## 2026-09-12 - v1.1 "Carrier"

A large pass covering feel, progression, audio, architecture and persistence.

### Gameplay

- **100 level campaign.** `LevelDesigner` / `LevelDefinition` / `ILevelProvider` replace the
  old `10 + level * 4` ad-hoc scaling. Smooth sub-linear curves, boss levels every ten, three
  milestone rules at 25 / 50 / 75, ten colour tiers. Full table in
  [level-design.md](level-design.md).
- **The ship now has hull.** 100 hit points, a segmented neon bar in the top-left. Rings that
  overlap the ship tick 10 damage every 250 ms (40 dps). The head reaching the core is still an
  instant loss.
  - The tick gate matters: without it, six rings touching at once would delete the bar in a
    single frame and the player would never see what killed them.
- **Carrier rings.** Whether a ring drops a supply is decided when the snake is built and the
  ring wears the glyph from the start, so target priority becomes a real decision instead of a
  slot machine. Rates: 20% `T`, then 20% `D`, then 20% `+` - effectively 20 / 16 / 12.8.
- **Supply types reworked.** `T` triple fire and `D` diffused fire are now independent 10 second
  timers that stack into a 7-shot fan; `+` restores 35 hull. The old generic fire-rate and
  damage boosts are gone.
- **Armored rings** (level 25+) carry **double hit points**, in silver-white with an X brace and
  their own clank.
  - Implemented as double HP, not as damage reduction: a bullet deals 1 damage, and halving
    that rounds to 1, so a damage-reduction rule would have silently done nothing.
- **Regenerating rings** (level 75+) heal after 3.5 s untouched.
- **The surge** (level 50+) sharply increases how much the spiral tightens as it closes.
- **Bullets always deal 1 damage** at every level, so every hit visibly ticks a number down.

### Feel

- Hit flash, axial knock-back, spark cone, damage numbers, trauma-based screen shake and a
  pitched impact sound, all on every connecting shot.
- Destruction: shockwave ring, shards, core sparks, heavier shake.
- `ParticleSystem` (900 pooled), `FloatingTextSystem` (64 pooled), `CameraShake`.

### Audio (new)

- Full procedural synthesizer: `SoundBuilder` (oscillators, FM, noise, one-pole filters, soft
  clip, echo, normalize, edge fade) and `SoundBank` with 15 recipes. No audio files.
- `AudioManager` with per-sound voice throttling and random pitch variation.
- **One fire sound per volley, not per bullet** - seven stacked phase-aligned copies of the same
  waveform was the specific thing that made a wide volley unpleasant.
- Every sound ends with a low-pass, a soft clip and an edge fade. See
  [audio-design.md](audio-design.md).

### Collision

- Oriented bounding boxes replace axis-aligned ones. A 48x28 ring rotated 45 degrees has a
  ~35% error in hit area under an AABB, which the player feels as "my shots don't register".
- Swept segment tests replace point tests. A bullet covers ~11 px per frame and a ring is 28 px
  on its short axis, so point tests miss grazing shots and tunnel at higher frame times.
- Broad-phase bounding-radius rejection before the narrow phase.
- A bullet crossing several rings in one frame now hits the **first** one, not an arbitrary one.

### Rendering

- **World pass is now additive.** Glow layers accumulate light instead of averaging to grey.
  This is the single biggest visual change.
- Fixed `DrawLine` origin to `(0, 0.5)` so strokes are centred on their segment - thick glow
  layers were drifting downward.
- Rings gained corner studs, a hot filament and an armor brace; the ship gained an engine flare
  and a hurt shell; bullets gained a motion streak.
- `Starfield` with three orbiting parallax bands, tinted by tier.
- Core danger ring that pulses harder as the head closes.
- `HudRenderer` extracted: segmented hull bar, boost timers, level/sector readout, breach
  countdown, intro and outcome cards.
- Off-screen ring culling - a boss snake is ~130 rings at ~50 draws each.

### Architecture

- `ObjectPool<T>` + `IPoolable`. Bullets, particles, floating text and supplies are all
  pre-allocated. Zero allocations during play.
- `DrawContext` owns the SpriteBatch passes; screens never call Begin/End.
- `ViewportAdapter` - the game is authored at 1280x720 and letterboxed onto any back buffer.
  Desktop, ultrawide and phone share one layout.
- `InputState` unifies keyboard, mouse, touch and gamepad into one device-neutral view in
  virtual coordinates. Entities no longer call `Mouse.GetState()`.
- `ScreenManager` rewritten: push / pop / replace / popToRoot, all **deferred to the midpoint of
  a fade**, so a screen can request a change from inside its own Update without the manager
  mutating the list it is iterating.
- Device-neutral back navigation via `IScreen.OnBack()`; popup screens draw the stack beneath.
- `BaseScreen` + `MenuButton` - one implementation of menu layout, navigation and the neon
  button look, shared by all four menu screens.
- New screens: `SettingsScreen`, `CreditsScreen`, `PauseScreen`.

### Persistence

- `GameSettings` and `PlayerProfile` as JSON under `ArarGames/SpiralWar/`, behind
  `IConfigStore` / `JsonConfigStore` / `GameConfig`.
- **Per-installation GUID** created on first run. Shown as an 8-character `PROFILE ID` on the
  CREDITS & SUPPORT screen so bug reports can be tied to a profile without personal data.
- Lifetime statistics: rings destroyed, shots fired, supplies collected, deaths, play time,
  best score.
- Atomic writes (temp file + move); loads never throw.
- One-time migration of the old `SpiralWar/save.dat` integer save.
- NEW GAME resets the level only - identity and lifetime stats survive.

### Content

- Both spritefonts gained `<DefaultCharacter>?</DefaultCharacter>`, so a glyph outside
  ASCII 32-126 renders as `?` instead of throwing at draw time. All UI strings were also
  converted to plain ASCII.

### Notes for next time

- The fonts are ASCII-only. Before adding any localisation, extend `CharacterRegions` in both
  `.spritefont` files.
- `LevelDesigner`'s balance was checked against an effective-damage model rising from ~4.7/s to
  ~13.7/s; every level clears inside its breach timer, tightest at level 10 (~5 s of slack) and
  level 100 (~8 s). Re-run that check after any retune.
- `SoundEffect.Play` gives no handle, so `AudioManager.StopAll` mutes the master bus rather than
  stopping voices. If per-voice control is ever needed, move to `SoundEffectInstance` pools.
