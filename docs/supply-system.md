# Ring Payloads - supplies, diamonds and dynamite

Every body ring on the snake is at most **one** special thing: a dynamite charge, or a supply
carrier, or a diamond carrier, or plain. Armor is the single exception, because it is a
property of the plating rather than a payload.

That restraint is the whole reason a wall of a hundred and ninety rings stays readable at
speed. Two payloads on one ring would need two glyphs, and two glyphs on a 38x22 pixel box at
the edge of vision is nothing at all.

## The core idea: carrier rings

Whether a ring carries something is decided **when the snake is built**, not when the ring
dies. From the moment it appears the ring wears the glyph it will drop and a pulsing outer ring
in that payload's colour.

This is the most important decision in this system. A drop rolled at the moment of death is
pure luck - the player does nothing, and the game either rewards them or does not. A drop
declared in advance turns the incoming spiral into a **target priority problem**: the `T` ring
is eleven rings back and the head reaches the core in ninety seconds, is it worth spending the
ammunition to dig it out now?

That is a decision. The other version is a slot machine.

## The roll

Per ring, in `SnakeSegment.RollPayload`, in this order:

```
dynamiteChance        -> neon dynamite   (exclusive; carries nothing)
else diamondChance    -> a neon diamond  (weighted 25/25/25/25)
else supplyChance     -> a supply        (weighted table below)
then armoredChance    -> independent, unless the ring is dynamite
```

Dynamite is first because it is the most dramatic and has to be unmistakable. Diamonds are
rolled before supplies because they are the rarer, more valuable class and would otherwise be
crowded out.

Level-dependent chances are in [level-design.md](level-design.md). At level 1 roughly a third
of rings carry something; at level 100 roughly half, plus 12.5% dynamite on a boss.

---

## Supplies - they change the gun

| Glyph | Name | Colour | Effect | Drop weight |
|:---:|:---|:---|:---|---:|
| `2` | DOUBLE FIRE | teal | +1 bullet, +/-0.065 rad | 20 |
| `T` | TRIPLE FIRE | mint | +2 bullets, +/-0.13 rad | 18 |
| `D` | DIFFUSED FIRE | amber | +4 bullets, +/-0.46 rad | 16 |
| `M` | HOMING MISSILES | sky | a missile every 0.55s | 14 |
| `S` | STASIS FIELD | pale violet | the spiral runs at 40% speed | 14 |
| `+` | REPAIR | pink | +35 hull, immediately | 18 |

All except repair run for 12 seconds (stasis: 8). Collecting one again **refreshes** its timer
rather than stacking - a supply changes what the gun does and does not need a magnitude.

### Volleys are additive

Weapon supplies add bullets and the **widest active cone wins**:

| Active | Bullets | Half-angle |
|:---|---:|---:|
| none | 1 | - |
| `2` | 2 | 0.065 |
| `T` | 3 | 0.13 |
| `D` | 5 | 0.46 |
| `2`+`T` | 4 | 0.13 |
| `2`+`T`+`D` | **8** | 0.46 |

Additive rather than "strongest wins" means `2`+`D` is a genuinely different spray from either
alone, instead of one silently overriding the other.

### Stasis

`Snake.TimeScale` scales the time the spiral **integrates**, not its speed constants. The fail
timer, the danger ring and the HUD countdown therefore all stay consistent with each other for
free. On a three minute level, eight seconds of stasis is worth roughly four seconds of
breathing room and a lot of panic avoided.

### Homing missiles

Deliberately slow (430 px/s against a bullet's 660) with a capped turn rate, so a missile can
genuinely miss and has to loop back. A weapon that cannot miss is a weapon the player stops
aiming, and the whole game is aiming. They retarget every frame, because on a spiral the ring a
missile was chasing is frequently dead by the time it arrives.

5 direct damage plus 2 splash in a 72px radius. They fire on their own cadence, independent of
the trigger, so the supply is worth something while the player is lining up a dynamite ring.

---

## Neon diamonds - they change the numbers

Where a supply changes the SHAPE of a volley, a diamond changes the STATS behind it, and the
two stack freely. A triple-fire volley under a blue and a red diamond is a different weapon.

| Glyph | Colour | Name | Per stack | At level 3 |
|:---:|:---|:---|:---|:---|
| `A` | blue | ATTACK SPEED | fire cooldown -18% | ~2x fire rate |
| `C` | black | CRITICAL | +10% crit chance | 30% of bullets deal triple |
| `X` | green | ACIDIC ROUNDS | +2 acid stacks per hit | hits burn for 3/tick |
| `P` | red | POWER ATTACK | +1 damage, piercing from Lv2 | 4 damage, pierces one ring |

Diamonds **stack**: collecting a second blue takes it to level 2 and restarts the clock, up to
level 3. The whole stack expires together after 14 seconds - decaying one level at a time would
make the player's damage output drift without them noticing why.

That asymmetry with supplies is deliberate: a diamond IS a magnitude and nothing else, so it
needs levels; a supply is a shape and does not.

### Why black is drawn the way it is

A genuinely black diamond is invisible on a near-black arena. The stone is dark
(`CoreColorOf`) and the **shell** is bright white-violet, so it reads as "black diamond" while
still being the brightest thing on screen when it drops.

### Acid

Acid is the green diamond's whole point: it keeps eating a ring after the player has moved
their aim somewhere else. Two stacks are worth one point per 0.5s tick, capped at six stacks,
for 3.5 seconds. Modest raw numbers - but it is also the only thing that stops a level 75+ ring
from regenerating, which is where it earns its place.

### Why criticals bend the "one damage" rule

They bend it openly. A critical is a **different bullet colour** (white-hot, drawn 1.6x
larger), a **bigger number in its own colour**, and **its own sound** layered on top of the
normal impact. The rule exists so the player can account for every point that comes off a ring;
a crit that announces itself three ways does not break that.

---

## Neon dynamite

A dynamite ring is the one ring on the snake worth going out of your way for. Destroying it
damages everything within 128px, and any dynamite caught in that blast goes off too.

| Parameter | Value |
|---|---|
| Blast radius | 128 px |
| Centre damage | 80% of the level's max ring HP |
| Edge damage | 35% of centre (linear falloff) |
| Chain depth cap | 4 generations |
| Blasts per frame cap | 64 |

On a tightly wound spiral a well-placed shot can unzip a quarter of the snake. Two things keep
that from being a free win: damage falls off toward the edge so a chain peters out, and the
chain is capped.

### How the chain is resolved

**Iteratively, not recursively.** `DetonationSystem` keeps a queue; a ring killed by a blast
raises `SegmentKilled`, whose handler calls `Enqueue` on the same instance, which appends at
`currentDepth + 1`. The whole cascade lands inside one frame with the depth as a plain number.

Recursing through the kill event would mean a hundred-ring cascade unwinding on the call stack
inside a collision loop, with every handler running at an unpredictable depth.

`Resolve` is called **after** collisions each frame, so a charge the player just shot goes off
in the same frame, and a chain started by a missile is picked up too.

### Audio

A chained charge uses a different, shorter, 15% quieter sound than the one the player shot, and
both are throttled (90ms / 70ms). A six-charge cascade should build into something you can hear
count, not one clipped roar. Chained blasts also shake less, divided by `1 + depth * 0.6`, so a
long cascade does not accumulate into an unreadable screen.

---

## Drift toward the core

Supplies and diamonds both inherit 35% of the ring's momentum (so the drop looks ejected rather
than teleported), that momentum decays, and they then drift toward the core, accelerating by up
to 50% over the last 500px so the final stretch does not feel sluggish.

The ship sits at the core. It therefore never has to chase a pickup - collecting one is a
reward for killing the right ring, not a second skill test bolted onto the first. What the
player does have to do is survive the few seconds it takes to arrive.

Supplies live 11 seconds, diamonds 11, and both blink out over the last 2 so their
disappearance is never a surprise.

## Pooling

Bullets (640), missiles (64), supplies (64), diamonds (48), particles (1200) and floating text
(96) are all pre-allocated via `ObjectPool<T>`. Nothing in this system allocates during play.

## Tuning notes

- The ~50% "nothing" rate at high levels is intentional. If most rings carried something, the
  glyphs would stop being a signal and become wallpaper.
- Repair is weighted joint-highest because on a three minute level, losing the hull to attrition
  is the realistic failure, not a single mistake.
- 12 seconds per weapon supply against a 180 second level means uptime is spotty early and high
  late, which is exactly what the difficulty curve expects of damage output.
