# Supply System

## The core idea: carrier rings

Whether a ring carries a supply is decided **when the snake is built**, not when the ring dies.
From the moment it appears the ring wears the glyph it will drop and a pulsing outer ring in
that supply's colour.

This is the single most important decision in this system. A drop rolled at the moment of
death is pure luck - the player does nothing, and the game either rewards them or does not.
A drop declared in advance turns the incoming spiral into a **target priority problem**: the
`T` ring is eleven rings back and the head reaches the core in forty seconds, is it worth
spending the ammunition to dig it out now?

That is a decision. The other version is a slot machine.

## Drop table

Rolled per ring, in `SnakeSegment.RollSupply`, in a fixed order. A ring carries **at most one**
supply, so the first roll that succeeds wins:

```
20% -> T (triple fire)
else 20% -> D (diffused fire)
else 20% -> + (repair)
else nothing
```

Because the rolls are sequential, the effective rates are **not** a flat 20% each:

| Supply | Nominal | Effective | Reasoning |
|---|---|---|---|
| `T` triple fire | 20% | **20.0%** | first roll |
| `D` diffused fire | 20% | **16.0%** | 0.8 x 0.2 |
| `+` repair | 20% | **12.8%** | 0.8 x 0.8 x 0.2 |
| nothing | - | **51.2%** | 0.8 x 0.8 x 0.8 |

So roughly **half the rings carry something**, and the ordering deliberately makes the offensive
pickups more common than repair. That is the right bias: repair is the pickup a player wants
when they are already losing, and making it the rarest keeps a bad run from self-correcting for
free.

The three constants are `GameConstants.SupplyChanceTriple`, `...Diffused` and `...Health`. If
you want three genuinely equal rates, change the roll to a single uniform draw rather than
raising the individual numbers.

## The pickups

| Glyph | Name | Colour | Effect |
|---|---|---|---|
| `T` | TRIPLE FIRE | mint green `(60,255,150)` | 3 shots at +/-0.13 rad for 10s |
| `D` | DIFFUSED FIRE | amber `(255,165,45)` | 5 shots at +/-0.46 rad for 10s |
| `+` | REPAIR | pink `(255,70,120)` | restores 35 hull immediately |

`T` and `D` are **independent timers**, so they stack:

| State | Shots | Half-angle |
|---|---|---|
| neither | 1 | - |
| `T` only | 3 | 0.13 rad |
| `D` only | 5 | 0.46 rad |
| `T` + `D` | **7** | 0.46 rad |

Both active at once is the strongest the ship ever gets. It is reachable, it does not last, and
the ship's baseline is never permanently raised - which is what keeps carrier rings worth
prioritising for the whole campaign.

## Drift toward the core

A released supply inherits 35% of the ring's momentum (so the drop looks ejected rather than
teleported), that momentum decays, and the supply then drifts toward the core at
190 px/s, accelerating by up to 50% over the last 500 px so the final stretch does not feel
sluggish.

The ship sits at the core. It therefore never has to chase a pickup - collecting one is a
reward for killing the right ring, not a second skill test bolted onto the first. What the
player *does* have to do is survive the 3-6 seconds it takes to arrive.

A supply lives for 9 seconds and blinks out over the last 2, so its disappearance is never a
surprise.

## Rendering

- The ring wears a pulsing outer frame in the supply colour plus the glyph, drawn in front of
  the hit point number in the ring's own local space (`SnakeSegment.DrawLabels`).
- The released pickup is a spinning neon diamond with a halo, an orbiting spark, and the glyph
  in the middle (`NeonRenderer.DrawNeonSupplyShell` + `Supply.DrawGlyph`).
- The legend on the CREDITS & SUPPORT screen draws the real shells with the real colours, so it
  can never drift out of sync with the game.

All three come from `SupplyTypeInfo`, which is the single source of truth for a supply's
colour, glyph and display name.

## Pooling

Supplies are pooled (`ObjectPool<Supply>`, capacity 48) like bullets and particles. A boss
level can drop several dozen over its lifetime; none of them allocate.

## Tuning notes

- The 51% "nothing" rate is intentional. If most rings carried something, the glyphs would stop
  being a signal and become wallpaper.
- 10 seconds per boost against a 26-93 second level means uptime is high late and spotty early,
  which matches how the difficulty curve expects damage output to rise.
- Repair restores 35 of 100 hull. Three contact ticks cost 30, so one repair buys back roughly
  one bad mistake - not two.
