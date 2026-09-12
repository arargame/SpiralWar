# Goals

## What the game is

A single ship, bolted to the centre of a dark neon arena. A segmented snake spirals in from
outside the screen. Every ring of that snake shows its own hit points. Destroy them all before
the head reaches the core.

The ship never moves. That is the whole design: there is no dodging, no positioning, no
movement skill. Every decision the player makes is **which ring to shoot next, and with what**.
Everything else in the game exists to make that one decision interesting.

## What we are optimising for

**1. Every hit must be felt.**

This is the first-order goal and the reason most of the feedback code exists. When a bullet
connects, five things happen in the same frame:

- the number printed on the ring drops by exactly the damage dealt
- the ring flashes white for 120 ms
- the ring is knocked backwards along its own axis and springs back
- sparks fly back along the bullet's path
- a short impact sound plays, pitched by how much of the ring that hit took off

A player should never have to ask "did that land?". If a change makes any of those five less
legible, the change is wrong.

**2. The numbers must stay readable.**

A bullet always deals exactly 1 damage, at every level, forever. Ring hit points stay in the
2-14 range for the entire 100 level campaign. Difficulty comes from **more rings, tougher rings
and a faster spiral**, never from an invisible damage multiplier. This is also why armor is
expressed as double hit points rather than as a damage reduction - the player can see it.

**3. Progression must be fair.**

The spiral is a fail timer, and it is always meaningfully longer than the time the curve
expects a competent player to need. See [level-design.md](level-design.md) for the arithmetic.
New rules are introduced at three milestones with an on-screen banner, never silently.

**4. It must run the same everywhere.**

One shared project drives desktop and mobile. Input is device-neutral (`InputState`), the
canvas is virtual and letterboxed (`ViewportAdapter`), and a finger and a mouse follow the same
code path.

**5. It must be readable six months from now.**

Interfaces where a second implementation is plausible (`IAudioService`, `ILevelProvider`,
`IConfigStore`, `IScreen`). One responsibility per class. Tuning values in
`GameConstants` and `LevelDesigner`, not scattered through the code.

## Non-goals

- Story, cutscenes, dialogue.
- Ship movement or a second controllable unit.
- Online anything. The player GUID exists so that cloud features *could* be added later; it is
  not a commitment to build them.
- External art and audio assets. Procedural generation is a constraint we have chosen, and it
  is what keeps the build tiny and the look coherent.

## Current state

Implemented: the 100 level curve, boss levels, the three milestone rules, carrier rings and
the T / D / + supplies, the ship's hull bar and contact damage, the procedural sound bank,
bullet and particle pooling, oriented + swept collision, additive neon rendering, the screen
stack with back navigation and fades, settings, credits & support, and the JSON profile with a
per-installation GUID.

Next candidates, in rough priority order:

1. Android head project (the shared project is already mobile-ready; only the head is missing).
2. A second weapon axis - something that rewards holding fire rather than spraying.
3. Music: a slow generative drone keyed to the tier colour and the danger level.
4. Per-level best times stored on the profile, shown on the level intro card.
5. Localisation pass - all UI strings are currently inline English constants.
