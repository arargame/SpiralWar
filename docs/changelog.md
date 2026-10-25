# Changelog

## 2026-10-25 - v1.2 "Dynamite"

The 3D "AI Reposta" experiment was parked (`git stash`) and the game continues in 2D.

### Pacing

- **Every level is now three minutes.** `GameConstants.LevelDurationSeconds` is a constant 180s
  for the whole campaign, boss levels included. A fail timer the player can internalise beats
  one that shifts under them, and it makes the health pool a plain consequence of expected
  damage output x expected clear time.
- Health pool recurve: `700 + 4200 * t`, linear. The previous sub-linear curve left levels 6-9
  - after the pool had grown but before dynamite unlocked - as the only unwinnable stretch.
- **Fixed a real balance bug:** average ring health was derived without accounting for armored
  rings carrying double hit points, which inflated the true pool by up to 40% at the top of the
  campaign. `LevelDesigner` now resolves `armoredChance` first and divides by `(1 + armor)`.
- Angular speed is now DERIVED from the ring size rather than authored, so the radial gap
  between spiral turns stays a few ring-heights wide at every form.
- Verified against the damage model: every level clears in 131-163s against the 180s timer,
  average 155s, tightest margin 17s.

### Five snake forms

- New `SnakeForm` / `SnakeFormInfo`. The form is decided by which band of twenty levels you are
  in, so the snake changes character five times across the campaign:
  SWARM (many small fragile rings) -> FLIGHT -> COLUMN -> PHALANX -> BULWARK (a short column of
  huge, very heavy rings).
- Count factor x health factor is ~1 for every form, so the form changes how a level FEELS, not
  how long it takes. Ring size scales with the form too, which keeps the snake's physical
  length roughly constant as the count drops.
- Ring width/height are now per-level and read from the SEGMENT by the collision system, not
  from `GameConstants`.

### Neon dynamite

- New ring type: destroying it deals falloff damage in a 128px radius and chains into any
  dynamite caught in the blast, capped at 4 generations.
- `DetonationSystem` resolves the cascade **iteratively** rather than recursing through the kill
  event, so a hundred-ring chain does not unwind on the call stack inside a collision loop.
- Chained blasts use a shorter, quieter sound and shake less, so a cascade builds into something
  you can hear count instead of one clipped roar.
- Unlocks at level 10 with a banner.

### Weapons

- Supplies are now six: double fire, triple fire, diffused fire, **homing missiles**,
  **stasis field** and repair. Weighted drop table in `SupplyTypeInfo`.
- Volleys are ADDITIVE in bullet count with the widest active cone winning, so `2`+`D` is a
  genuinely different spray from either alone. Up to 8 bullets.
- `Missile` + `MissileSystem`: slow, capped turn rate, retargets every frame, 5 direct + 2
  splash. A weapon that cannot miss is a weapon the player stops aiming.
- Stasis scales the time the spiral INTEGRATES (`Snake.TimeScale`), so the fail timer, danger
  ring and HUD countdown all stay consistent for free.

### Neon diamonds

- New `DiamondType` / `Diamond` / `DiamondSystem`: blue attack speed, black critical, green
  acidic rounds, red power attack.
- Timed and STACKING to level 3, where supplies refresh instead. A diamond is a magnitude and
  needs levels; a supply is a shape and does not.
- The black diamond is drawn as a dark stone inside a bright white-violet shell, because a
  genuinely black diamond is invisible on a near-black arena.
- Acid keeps eating a ring after the player's aim has moved on, and is the only thing that stops
  a level 75+ ring from regenerating.
- **Criticals and power attacks bend the "a bullet deals 1" rule, openly:** a different bullet
  colour, a bigger number in its own colour, and its own layered sound.

### Player

- New `PlayerLoadout` owns every timed effect and the statistics derived from them, so `Player`
  stays about aiming and firing. One answer to "what is the fire cooldown right now".
- `BuildVolley` resolves angle, damage, critical, piercing and acid per bullet before spawning,
  so the collision system never has to know what the ship is carrying.
- Piercing bullets remember the ring they punched (`Bullet.IgnoredSegment`); without it a
  pierced round re-hits the same ring every frame and "pierce one extra ring" quietly means
  "deal four times the damage to one ring".
- The hull tints toward the strongest active diamond, so the loadout is readable off the ship.

### Presentation

- `NeonRenderer` gained the dynamite ring (hazard chevrons + orbiting fuse spark), the faceted
  tumbling diamond, the missile with its exhaust plume, the acid drip overlay, the stasis field
  and the HUD diamond pip.
- HUD now lists active supplies and diamond stacks down the left edge with their own timers, and
  shows the breach countdown as m:ss rather than a bare second count.
- Credits screen legend now draws all six supplies and all four diamonds with the REAL renderers
  and colour tables, so it cannot drift out of sync with the arena.

### Audio

- Eleven new procedural sounds: critical ping, dynamite blast, dynamite chain, acid tick,
  missile launch, missile impact, diamond drop / pickup / level-up, stasis start and end.
- New throttles for all of them. Acid in particular ticks twice a second on many rings at once
  and becomes a constant fizz under everything else without a hard floor.

### Notes for next time

- Ring counts now reach 190 (level 20 SWARM). Off-screen culling plus `GlowScale` carry it, but
  that is the number to watch if frame time ever becomes a problem.
- Max printed ring HP is 80, which still fits two digits. A future retune that pushes it past 99
  needs the label layout revisited.
- `git stash list` still holds the 3D AI Reposta experiment.

## 2026-09-12 - v1.1 "Carrier"

A large pass covering feel, progression, audio, architecture and persistence.

### Gameplay

- **100 level campaign.** `LevelDesigner` / `LevelDefinition` / `ILevelProvider` replace the old
  `10 + level * 4` ad-hoc scaling. Smooth sub-linear curves, boss levels every ten, three
  milestone rules at 25 / 50 / 75, ten colour tiers.
- **The ship now has hull.** 100 hit points, a segmented neon bar in the top-left. Rings that
  overlap the ship tick 10 damage every 250 ms (40 dps). The head reaching the core is still an
  instant loss.
  - The tick gate matters: without it, six rings touching at once would delete the bar in a
    single frame and the player would never see what killed them.
- **Carrier rings.** Whether a ring drops a supply is decided when the snake is built and the
  ring wears the glyph from the start, so target priority becomes a real decision instead of a
  slot machine.
- **Supply types reworked.** `T` triple fire and `D` diffused fire as independent stacking
  timers; `+` restores 35 hull.
- **Armored rings** (level 25+) carry **double hit points**, in silver-white with an X brace.
  - Implemented as double HP, not as damage reduction: a bullet deals 1 damage, and halving
    that rounds to 1, so a damage-reduction rule would have silently done nothing.
- **Regenerating rings** (level 75+) heal after 3.5 s untouched.
- **The surge** (level 50+) sharply increases how much the spiral tightens as it closes.

### Feel

- Hit flash, axial knock-back, spark cone, damage numbers, trauma-based screen shake and a
  pitched impact sound, all on every connecting shot.
- Destruction: shockwave ring, shards, core sparks, heavier shake.
- `ParticleSystem` (900 pooled), `FloatingTextSystem` (64 pooled), `CameraShake`.

### Audio (new)

- Full procedural synthesizer: `SoundBuilder` and `SoundBank` with 15 recipes. No audio files.
- `AudioManager` with per-sound voice throttling and random pitch variation.
- **One fire sound per volley, not per bullet** - stacked phase-aligned copies of the same
  waveform was the specific thing that made a wide volley unpleasant.
- Every sound ends with a low-pass, a soft clip and an edge fade.

### Collision

- Oriented bounding boxes replace axis-aligned ones. A 48x28 ring rotated 45 degrees has a ~35%
  error in hit area under an AABB, which the player feels as "my shots don't register".
- Swept segment tests replace point tests, which stops fast bullets tunnelling.
- Broad-phase bounding-radius rejection before the narrow phase.
- A bullet crossing several rings in one frame now hits the **first** one.

### Rendering

- **World pass is now additive.** Glow layers accumulate light instead of averaging to grey.
- Fixed `DrawLine` origin to `(0, 0.5)` so strokes are centred on their segment.
- Rings gained corner studs, a hot filament and an armor brace; the ship gained an engine flare
  and a hurt shell; bullets gained a motion streak.
- `Starfield` with three orbiting parallax bands, tinted by tier.
- `HudRenderer` extracted. Off-screen ring culling.

### Architecture

- `ObjectPool<T>` + `IPoolable`. Zero allocations during play.
- `DrawContext` owns the SpriteBatch passes; screens never call Begin/End.
- `ViewportAdapter` - authored at 1280x720 and letterboxed onto any back buffer.
- `InputState` unifies keyboard, mouse, touch and gamepad in virtual coordinates.
- `ScreenManager` rewritten: push / pop / replace / popToRoot, all **deferred to the midpoint of
  a fade**.
- `BaseScreen` + `MenuButton`; new `SettingsScreen`, `CreditsScreen`, `PauseScreen`.

### Persistence

- `GameSettings` and `PlayerProfile` as JSON behind `IConfigStore`.
- **Per-installation GUID** created on first run, shown as an 8-character `PROFILE ID`.
- Lifetime statistics, atomic writes, one-time migration of the old `save.dat`.
- NEW GAME resets the level only - identity and lifetime stats survive.

### Content

- Both spritefonts gained `<DefaultCharacter>?</DefaultCharacter>`, and all UI strings were
  converted to plain ASCII.
