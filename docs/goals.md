# Goals

## What the game is

A single ship, bolted to the centre of a dark neon arena. A segmented snake spirals in from
outside the screen. Every ring of that snake shows its own hit points. Destroy them all before
the head reaches the core - you have exactly three minutes.

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

**2. The numbers must stay accountable.**

A BASE bullet always deals exactly 1 damage, at every level, forever.

Critical hits, power attacks, acid, missiles and dynamite all exceed that, and every one of
them announces itself: its own bullet colour, its own number colour, its own sound. The rule
was never "the number is always 1" - it is **the player can always account for every point
that came off a ring**. A white-hot bullet that prints a yellow 12 does not break that; an
invisible multiplier would.

Ring hit points stay in the 3-80 range for the entire 100 level campaign - always two digits,
always printed on the ring. This is also why armor is expressed as double hit points rather
than as a damage reduction: the player can see it.

**3. Progression must be fair, and legible.**

The spiral is a three minute fail timer, at every level, boss or not. The health pool is then a
plain consequence: expected damage output x expected clear time. See
[level-design.md](level-design.md) for the arithmetic and the verification.

New rules are introduced with an on-screen banner, never silently. The snake's whole SHAPE
changes five times across the campaign, so progress is something you can see rather than only
something you can measure.

**4. Choice over luck.**

Carrier rings wear the glyph of what they will drop from the moment they appear. The player
looks at the incoming spiral and decides what is worth the ammunition. A drop rolled at the
moment of death would be a slot machine; a drop declared in advance is a decision.

**5. It must run the same everywhere.**

One shared project drives desktop and mobile. Input is device-neutral (`InputState`), the
canvas is virtual and letterboxed (`ViewportAdapter`), and a finger and a mouse follow the same
code path.

**6. It must be readable six months from now.**

Interfaces where a second implementation is plausible (`IAudioService`, `ILevelProvider`,
`IConfigStore`, `IScreen`). One responsibility per class. Tuning values in `GameConstants`,
`LevelDesigner` and `SnakeFormInfo`, not scattered through the code.

## Non-goals

- 3D. It was tried in October 2026 ("AI Reposta") and did not improve the game; the branch is
  parked in `git stash`. The neon 2D look is the identity.
- Story, cutscenes, dialogue.
- Ship movement or a second controllable unit.
- Online anything. The player GUID exists so that cloud features *could* be added later; it is
  not a commitment to build them.
- External art and audio assets. Procedural generation is a constraint we have chosen, and it
  is what keeps the build tiny and the look coherent.

## Current state

Implemented: the 100 level three-minute campaign with five snake forms, boss levels, four
milestone rules, carrier rings with six supplies and four diamonds, neon dynamite with chain
detonation, homing missiles, the stasis field, the ship's hull and contact damage, the
procedural sound bank, pooling throughout, oriented + swept collision, additive neon rendering,
the screen stack with back navigation and fades, settings, credits & support, and the JSON
profile with a per-installation GUID.

Next candidates, in rough priority order:

1. Android head project (the shared project is already mobile-ready; only the head is missing).
2. Music: a slow generative drone keyed to the tier colour and the danger level.
3. Per-level best times stored on the profile, shown on the level intro card.
4. A meta-progression pass - diamonds banked between runs rather than only within a level.
5. Localisation pass - all UI strings are currently inline English constants, and the
   spritefonts are ASCII only.
