# Level Design - the 100 level campaign

Every level is produced by `LevelDesigner` from one number: normalized campaign progress
`t = (level - 1) / 99`. There is no hand-authored table, which means the curve can be retuned
by changing a handful of constants instead of editing a hundred rows.

## The design contract

1. **Every level is three minutes.** The spiral always needs
   `GameConstants.LevelDurationSeconds` (180s) to reach the core, at every level, boss or not.
   A fail timer the player can internalise is worth more than one that shifts under them, and
   it makes the health pool a straightforward consequence:
   **pool = expected damage output x expected clear time**.
2. **The health pool is the design variable; the form splits it.** The curve decides how many
   hit points a level is worth. The level's `SnakeForm` then decides whether that is spent on a
   swarm of fragile rings or a short column of enormous ones.
3. **A base bullet still deals 1.** Critical hits, power attacks, acid, missiles and dynamite
   can all exceed that, but every one of them announces itself on screen in its own colour, so
   the player can always account for what came off a ring.
4. **New rules arrive with a banner, never silently.**
5. **Every tenth level is a boss**: 25% more rings, 8% more health, far more dynamite.

## The five forms

The form is decided by which band of twenty levels you are in, so the snake visibly changes
character five times across the campaign instead of only getting numerically bigger.

| Form | Levels | Name | Ring count | Ring health | Ring scale | Character |
|---:|:---|:---|---:|---:|---:|:---|
| 1 | 1-20 | SWARM | x1.70 | x0.59 | x0.80 | many small fragile rings |
| 2 | 21-40 | FLIGHT | x1.35 | x0.74 | x0.90 | long and light |
| 3 | 41-60 | COLUMN | x1.00 | x1.00 | x1.00 | the reference shape |
| 4 | 61-80 | PHALANX | x0.78 | x1.28 | x1.22 | fewer, larger, tougher |
| 5 | 81-100 | BULWARK | x0.60 | x1.67 | x1.50 | a short column of huge rings |

The contract that makes this work: **count factor x health factor is approximately 1 for every
form.** Switching form changes how a level FEELS, not how long it takes.

`RingScale` is not decoration. A ring carrying four times the health has to look like it, and
scaling the ring also keeps the snake's physical length roughly constant as the count drops -
a Bulwark spiral of 70 rings at normal size would be a few dots on an empty screen rather than
a wall.

## The formulas

With `t = (level - 1) / 99`:

| Parameter | Formula | Range |
|---|---|---|
| Health pool (armor included) | `700 + 4200 * t` | 700 -> 4900 |
| Reference ring count | `75 + 45 * t^0.70` | 75 -> 120 |
| Ring count | `reference * form.CountFactor` | 68 -> 190 |
| Average ring HP | `pool / (count * (1 + armoredChance))` | 5 -> 55 |
| Ring HP range | `average * 0.60` .. `average * 1.45` | 3 -> 80 |
| Spiral duration | constant | 180s |
| Armored chance | 0 below 25, then ramps to 0.22 | 0% -> 22% |
| Dynamite chance | 0 below 10, then 0.075 * (0.35 + 0.65 * p) | 0% -> 7.5% |
| Supply carry chance | `0.26 + 0.06 * t` | 26% -> 32% |
| Diamond carry chance | `0.08 + 0.07 * t` | 8% -> 15% |
| Inward acceleration | `0.25 + 0.45t` (+0.35 from level 50) | 0.25 -> 1.05 |

Boss levels (`level % 10 == 0`) then apply: rings x1.25, pool x1.08, dynamite +5pp (cap 16%),
armor +10pp (cap 40%).

### Why the pool curve is linear

It tracks the player's damage output, and that output rises roughly linearly with the supply
and diamond economy a level hands out. A sub-linear curve made levels 6 to 9 - after the pool
has grown but **before dynamite unlocks at level 10** - the only unwinnable stretch in the
campaign. Linear removes that notch.

### Why armor is resolved before health

An armored ring carries **double** hit points. Deriving the average ring health without
accounting for that silently inflates the real pool by up to 40% at the top of the campaign,
which is the difference between a hard level and an impossible one. `LevelDesigner` therefore
computes `armoredChance` first and divides by `(1 + armoredChance)`.

### Why the angular speed is derived, not authored

The radial gap between two turns of the spiral has to stay a few ring-heights wide or the turns
overlap and the arena becomes unreadable. Bigger rings need a wider corridor, so:

```
revolutions  = spawnRadius / (ringHeight * form.TurnGapInRingHeights)
angularSpeed = revolutions * 2*pi / 180s  * (1 + 0.25t)
```

A Swarm level winds 12.6 turns; a Bulwark level only 7.0. Both leave the same amount of room
to fly through.

### Inward acceleration

```
u        = elapsed / spiralDuration
progress = (u + accel * u^2) / (1 + accel)
radius   = startRadius * (1 - progress)
```

At `u = 1` this is exactly 1 regardless of `accel`, so the fail timer is honest whatever the
acceleration is. What changes is the shape: the head starts at the authored pace and tightens
as it closes. From level 50 the surge bonus roughly doubles that tightening.

## Milestones

| Level | Rule | Banner |
|---|---|---|
| 10 | **Neon dynamite.** Rings that detonate, damaging and chaining into their neighbours. | `NEW: NEON DYNAMITE - SHOOT IT, CLEAR THE CLUSTER` |
| 21 / 41 / 61 / 81 | **A new form.** The snake's whole shape changes. | `NEW FORM: <name> - <tagline>` |
| 25 | **Armored rings.** Silver-white, double hit points, an X brace and their own clank. | `NEW: ARMORED RINGS CARRY DOUBLE PLATING` |
| 50 | **The surge.** The spiral visibly speeds up as it closes. | `NEW: THE SPIRAL SURGES AS IT CLOSES` |
| 75 | **Regeneration.** A ring left alone for 3.5s heals - and acid stops it. | `NEW: RINGS REGENERATE IF YOU LET THEM BREATHE` |
| every 10 | Boss spiral. | `BOSS SPIRAL` |
| 100 | The finale. | `THE FINAL CORE` |

## Tiers

Ten accent colours, one per ten levels, driving the grid, the starfield tint, the core ring and
the HUD: OUTER RIM, DRIFT, THE COIL, ASHFALL, EMBER GATE, REDSHIFT, SINGULARITY, VIOLET DEEP,
EVENT HORIZON, WHITE CORE.

## Balance check

The model behind the curve assumes effective damage output of
`4.7 + 19 * t^0.9` points per second - 4.7/s at level 1 from the bare gun at 80% accuracy,
rising to ~24/s once the supply and diamond economy is running - plus dynamite relief of about
four rings per charge.

Under that model **every level in the campaign clears between 131 and 163 seconds**, against a
180 second fail timer. Average 155s. The tightest levels still leave 17 seconds of slack.

That is the intended shape: a three minute level that a competent player finishes with time to
spare, and a sloppy one does not finish at all.

**If you retune anything in `LevelDesigner`, re-run that check before committing.**

## Generated table

| Level | Form | Rings | Ring HP | Health pool | Ring size | Turns | Angular | Dynamite | Armored | Supply | Diamond | Blast |
|------:|:-----|------:|:-------:|------------:|:---------:|------:|--------:|---------:|--------:|-------:|--------:|------:|
| 1 | SWARM | 128 | 3-8 | 704 | 38x22 | 12.6 | 0.44 | 0.0% | 0% | 26% | 8% | 6 |
| 2 | SWARM | 131 | 3-8 | 720 | 38x22 | 12.6 | 0.44 | 0.0% | 0% | 26% | 8% | 6 |
| 3 | SWARM | 132 | 4-9 | 858 | 38x22 | 12.6 | 0.44 | 0.0% | 0% | 26% | 8% | 7 |
| 4 | SWARM | 134 | 4-9 | 871 | 38x22 | 12.6 | 0.44 | 0.0% | 0% | 26% | 8% | 7 |
| 5 | SWARM | 136 | 4-9 | 884 | 38x22 | 12.6 | 0.44 | 0.0% | 0% | 26% | 8% | 7 |
| 6 | SWARM | 137 | 4-10 | 959 | 38x22 | 12.6 | 0.44 | 0.0% | 0% | 26% | 8% | 8 |
| 7 | SWARM | 138 | 4-10 | 966 | 38x22 | 12.6 | 0.45 | 0.0% | 0% | 26% | 8% | 8 |
| 8 | SWARM | 139 | 4-10 | 973 | 38x22 | 12.6 | 0.45 | 0.0% | 0% | 26% | 8% | 8 |
| 9 | SWARM | 141 | 4-11 | 1058 | 38x22 | 12.6 | 0.45 | 0.0% | 0% | 26% | 9% | 9 |
| 10 | **SWARM BOSS** | 178 | 4-9 | 1273 | 38x22 | 12.6 | 0.45 | 7.6% | 10% | 27% | 9% | 7 |
| 11 | SWARM | 143 | 5-11 | 1144 | 38x22 | 12.6 | 0.45 | 2.7% | 0% | 27% | 9% | 9 |
| 12 | SWARM | 144 | 5-12 | 1224 | 38x22 | 12.6 | 0.45 | 2.7% | 0% | 27% | 9% | 10 |
| 13 | SWARM | 145 | 5-12 | 1232 | 38x22 | 12.6 | 0.45 | 2.8% | 0% | 27% | 9% | 10 |
| 14 | SWARM | 146 | 5-12 | 1241 | 38x22 | 12.6 | 0.45 | 2.8% | 0% | 27% | 9% | 10 |
| 15 | SWARM | 147 | 5-13 | 1323 | 38x22 | 12.6 | 0.45 | 2.9% | 0% | 27% | 9% | 10 |
| 16 | SWARM | 148 | 5-13 | 1332 | 38x22 | 12.6 | 0.46 | 2.9% | 0% | 27% | 9% | 10 |
| 17 | SWARM | 149 | 6-13 | 1416 | 38x22 | 12.6 | 0.46 | 3.0% | 0% | 27% | 9% | 10 |
| 18 | SWARM | 150 | 6-14 | 1500 | 38x22 | 12.6 | 0.46 | 3.1% | 0% | 27% | 9% | 11 |
| 19 | SWARM | 151 | 6-14 | 1510 | 38x22 | 12.6 | 0.46 | 3.1% | 0% | 27% | 9% | 11 |
| 20 | **SWARM BOSS** | 190 | 5-11 | 1672 | 38x22 | 12.6 | 0.46 | 8.2% | 10% | 27% | 9% | 9 |
| 21 | FLIGHT | 121 | 8-19 | 1634 | 43x25 | 11.1 | 0.41 | 3.2% | 0% | 27% | 9% | 15 |
| 22 | FLIGHT | 122 | 8-19 | 1647 | 43x25 | 11.1 | 0.41 | 3.3% | 0% | 27% | 9% | 15 |
| 23 | FLIGHT | 122 | 8-19 | 1647 | 43x25 | 11.1 | 0.41 | 3.3% | 0% | 27% | 10% | 15 |
| 24 | FLIGHT | 123 | 8-20 | 1722 | 43x25 | 11.1 | 0.41 | 3.4% | 0% | 27% | 10% | 16 |
| 25 | FLIGHT | 124 | 8-20 | 1736 | 43x25 | 11.1 | 0.41 | 3.4% | 0% | 27% | 10% | 16 |
| 26 | FLIGHT | 124 | 8-21 | 1803 | 43x25 | 11.1 | 0.41 | 3.5% | 0% | 28% | 10% | 17 |
| 27 | FLIGHT | 125 | 9-21 | 1886 | 43x25 | 11.1 | 0.41 | 3.5% | 1% | 28% | 10% | 17 |
| 28 | FLIGHT | 126 | 9-21 | 1907 | 43x25 | 11.1 | 0.41 | 3.6% | 1% | 28% | 10% | 17 |
| 29 | FLIGHT | 126 | 9-21 | 1912 | 43x25 | 11.1 | 0.41 | 3.7% | 1% | 28% | 10% | 17 |
| 30 | **FLIGHT BOSS** | 159 | 7-17 | 2127 | 43x25 | 11.1 | 0.41 | 8.7% | 11% | 28% | 10% | 14 |
| 31 | FLIGHT | 128 | 9-22 | 2019 | 43x25 | 11.1 | 0.42 | 3.8% | 2% | 28% | 10% | 18 |
| 32 | FLIGHT | 128 | 9-22 | 2025 | 43x25 | 11.1 | 0.42 | 3.8% | 2% | 28% | 10% | 18 |
| 33 | FLIGHT | 129 | 9-23 | 2112 | 43x25 | 11.1 | 0.42 | 3.9% | 2% | 28% | 10% | 18 |
| 34 | FLIGHT | 129 | 10-23 | 2185 | 43x25 | 11.1 | 0.42 | 3.9% | 3% | 28% | 10% | 18 |
| 35 | FLIGHT | 130 | 10-23 | 2208 | 43x25 | 11.1 | 0.42 | 4.0% | 3% | 28% | 10% | 18 |
| 36 | FLIGHT | 131 | 10-23 | 2231 | 43x25 | 11.1 | 0.42 | 4.0% | 3% | 28% | 10% | 18 |
| 37 | FLIGHT | 131 | 10-24 | 2305 | 43x25 | 11.1 | 0.42 | 4.1% | 4% | 28% | 11% | 19 |
| 38 | FLIGHT | 132 | 10-24 | 2330 | 43x25 | 11.1 | 0.42 | 4.1% | 4% | 28% | 11% | 19 |
| 39 | FLIGHT | 132 | 10-24 | 2336 | 43x25 | 11.1 | 0.42 | 4.2% | 4% | 28% | 11% | 19 |
| 40 | **FLIGHT BOSS** | 166 | 8-19 | 2564 | 43x25 | 11.1 | 0.42 | 9.2% | 14% | 28% | 11% | 15 |
| 41 | COLUMN | 99 | 14-34 | 2488 | 48x28 | 9.9 | 0.38 | 4.3% | 5% | 28% | 11% | 27 |
| 42 | COLUMN | 99 | 14-34 | 2494 | 48x28 | 9.9 | 0.38 | 4.4% | 5% | 28% | 11% | 27 |
| 43 | COLUMN | 100 | 14-34 | 2527 | 48x28 | 9.9 | 0.38 | 4.4% | 5% | 29% | 11% | 27 |
| 44 | COLUMN | 100 | 14-35 | 2587 | 48x28 | 9.9 | 0.38 | 4.5% | 6% | 29% | 11% | 28 |
| 45 | COLUMN | 101 | 14-35 | 2620 | 48x28 | 9.9 | 0.38 | 4.5% | 6% | 29% | 11% | 28 |
| 46 | COLUMN | 101 | 15-35 | 2681 | 48x28 | 9.9 | 0.38 | 4.6% | 6% | 29% | 11% | 28 |
| 47 | COLUMN | 101 | 15-36 | 2742 | 48x28 | 9.9 | 0.38 | 4.6% | 6% | 29% | 11% | 29 |
| 48 | COLUMN | 102 | 15-36 | 2776 | 48x28 | 9.9 | 0.39 | 4.7% | 7% | 29% | 11% | 29 |
| 49 | COLUMN | 102 | 15-36 | 2784 | 48x28 | 9.9 | 0.39 | 4.7% | 7% | 29% | 11% | 29 |
| 50 | **COLUMN BOSS** | 129 | 12-29 | 3103 | 48x28 | 9.9 | 0.39 | 9.8% | 17% | 29% | 11% | 23 |
| 51 | COLUMN | 103 | 15-37 | 2882 | 48x28 | 9.9 | 0.39 | 4.8% | 8% | 29% | 12% | 30 |
| 52 | COLUMN | 103 | 15-37 | 2890 | 48x28 | 9.9 | 0.39 | 4.9% | 8% | 29% | 12% | 30 |
| 53 | COLUMN | 104 | 15-37 | 2926 | 48x28 | 9.9 | 0.39 | 5.0% | 8% | 29% | 12% | 30 |
| 54 | COLUMN | 104 | 16-38 | 3047 | 48x28 | 9.9 | 0.39 | 5.0% | 9% | 29% | 12% | 30 |
| 55 | COLUMN | 104 | 16-38 | 3055 | 48x28 | 9.9 | 0.39 | 5.1% | 9% | 29% | 12% | 30 |
| 56 | COLUMN | 105 | 16-38 | 3093 | 48x28 | 9.9 | 0.39 | 5.1% | 9% | 29% | 12% | 30 |
| 57 | COLUMN | 105 | 16-39 | 3159 | 48x28 | 9.9 | 0.39 | 5.2% | 9% | 29% | 12% | 31 |
| 58 | COLUMN | 106 | 16-39 | 3197 | 48x28 | 9.9 | 0.39 | 5.2% | 10% | 29% | 12% | 31 |
| 59 | COLUMN | 106 | 16-39 | 3206 | 48x28 | 9.9 | 0.40 | 5.3% | 10% | 30% | 12% | 31 |
| 60 | **COLUMN BOSS** | 132 | 13-32 | 3572 | 48x28 | 9.9 | 0.40 | 10.3% | 20% | 30% | 12% | 26 |
| 61 | PHALANX | 83 | 21-51 | 3304 | 59x34 | 8.4 | 0.34 | 5.4% | 11% | 30% | 12% | 41 |
| 62 | PHALANX | 84 | 21-51 | 3352 | 59x34 | 8.4 | 0.34 | 5.4% | 11% | 30% | 12% | 41 |
| 63 | PHALANX | 84 | 21-52 | 3408 | 59x34 | 8.4 | 0.34 | 5.5% | 11% | 30% | 12% | 42 |
| 64 | PHALANX | 84 | 22-52 | 3464 | 59x34 | 8.4 | 0.34 | 5.5% | 11% | 30% | 12% | 42 |
| 65 | PHALANX | 84 | 22-53 | 3520 | 59x34 | 8.4 | 0.34 | 5.6% | 12% | 30% | 13% | 42 |
| 66 | PHALANX | 85 | 22-53 | 3571 | 59x34 | 8.4 | 0.34 | 5.7% | 12% | 30% | 13% | 42 |
| 67 | PHALANX | 85 | 22-53 | 3580 | 59x34 | 8.4 | 0.34 | 5.7% | 12% | 30% | 13% | 42 |
| 68 | PHALANX | 85 | 22-54 | 3637 | 59x34 | 8.4 | 0.34 | 5.8% | 13% | 30% | 13% | 43 |
| 69 | PHALANX | 85 | 22-54 | 3647 | 59x34 | 8.4 | 0.34 | 5.8% | 13% | 30% | 13% | 43 |
| 70 | **PHALANX BOSS** | 108 | 18-43 | 4058 | 59x34 | 8.4 | 0.34 | 10.9% | 23% | 30% | 13% | 34 |
| 71 | PHALANX | 86 | 23-55 | 3807 | 59x34 | 8.4 | 0.34 | 5.9% | 13% | 30% | 13% | 44 |
| 72 | PHALANX | 86 | 23-55 | 3816 | 59x34 | 8.4 | 0.35 | 6.0% | 14% | 30% | 13% | 44 |
| 73 | PHALANX | 87 | 23-55 | 3871 | 59x34 | 8.4 | 0.35 | 6.0% | 14% | 30% | 13% | 44 |
| 74 | PHALANX | 87 | 23-55 | 3881 | 59x34 | 8.4 | 0.35 | 6.1% | 14% | 30% | 13% | 44 |
| 75 | PHALANX | 87 | 23-56 | 3941 | 59x34 | 8.4 | 0.35 | 6.1% | 15% | 30% | 13% | 45 |
| 76 | PHALANX | 87 | 23-56 | 3951 | 59x34 | 8.4 | 0.35 | 6.2% | 15% | 31% | 13% | 45 |
| 77 | PHALANX | 88 | 23-56 | 4006 | 59x34 | 8.4 | 0.35 | 6.3% | 15% | 31% | 13% | 45 |
| 78 | PHALANX | 88 | 23-57 | 4067 | 59x34 | 8.4 | 0.35 | 6.3% | 16% | 31% | 13% | 46 |
| 79 | PHALANX | 88 | 24-57 | 4129 | 59x34 | 8.4 | 0.35 | 6.4% | 16% | 31% | 14% | 46 |
| 80 | **PHALANX BOSS** | 110 | 19-46 | 4509 | 59x34 | 8.4 | 0.35 | 11.4% | 26% | 31% | 14% | 37 |
| 81 | BULWARK | 68 | 31-75 | 4196 | 72x42 | 7.0 | 0.29 | 6.5% | 16% | 31% | 14% | 60 |
| 82 | BULWARK | 68 | 31-76 | 4246 | 72x42 | 7.0 | 0.30 | 6.5% | 17% | 31% | 14% | 61 |
| 83 | BULWARK | 69 | 31-75 | 4279 | 72x42 | 7.0 | 0.30 | 6.6% | 17% | 31% | 14% | 60 |
| 84 | BULWARK | 69 | 31-76 | 4330 | 72x42 | 7.0 | 0.30 | 6.6% | 17% | 31% | 14% | 61 |
| 85 | BULWARK | 69 | 32-76 | 4382 | 72x42 | 7.0 | 0.30 | 6.7% | 18% | 31% | 14% | 61 |
| 86 | BULWARK | 69 | 32-77 | 4433 | 72x42 | 7.0 | 0.30 | 6.7% | 18% | 31% | 14% | 62 |
| 87 | BULWARK | 69 | 32-77 | 4444 | 72x42 | 7.0 | 0.30 | 6.8% | 18% | 31% | 14% | 62 |
| 88 | BULWARK | 70 | 32-77 | 4520 | 72x42 | 7.0 | 0.30 | 6.8% | 18% | 31% | 14% | 62 |
| 89 | BULWARK | 70 | 32-77 | 4531 | 72x42 | 7.0 | 0.30 | 6.9% | 19% | 31% | 14% | 62 |
| 90 | **BULWARK BOSS** | 88 | 26-62 | 4997 | 72x42 | 7.0 | 0.30 | 12.0% | 29% | 31% | 14% | 50 |
| 91 | BULWARK | 70 | 32-78 | 4595 | 72x42 | 7.0 | 0.30 | 7.0% | 19% | 31% | 14% | 62 |
| 92 | BULWARK | 70 | 33-79 | 4690 | 72x42 | 7.0 | 0.30 | 7.1% | 20% | 32% | 14% | 63 |
| 93 | BULWARK | 71 | 32-78 | 4684 | 72x42 | 7.0 | 0.30 | 7.1% | 20% | 32% | 15% | 62 |
| 94 | BULWARK | 71 | 33-79 | 4781 | 72x42 | 7.0 | 0.30 | 7.2% | 20% | 32% | 15% | 63 |
| 95 | BULWARK | 71 | 33-79 | 4792 | 72x42 | 7.0 | 0.30 | 7.2% | 21% | 32% | 15% | 63 |
| 96 | BULWARK | 71 | 33-80 | 4847 | 72x42 | 7.0 | 0.30 | 7.3% | 21% | 32% | 15% | 64 |
| 97 | BULWARK | 71 | 33-80 | 4859 | 72x42 | 7.0 | 0.30 | 7.3% | 21% | 32% | 15% | 64 |
| 98 | BULWARK | 72 | 33-80 | 4939 | 72x42 | 7.0 | 0.31 | 7.4% | 21% | 32% | 15% | 64 |
| 99 | BULWARK | 72 | 33-80 | 4951 | 72x42 | 7.0 | 0.31 | 7.4% | 22% | 32% | 15% | 64 |
| 100 | **BULWARK BOSS** | 90 | 27-65 | 5465 | 72x42 | 7.0 | 0.31 | 12.5% | 32% | 32% | 15% | 52 |
