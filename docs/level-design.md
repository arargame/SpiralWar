# Level Design - the 100 level campaign

Every level is produced by `LevelDesigner` from one number: normalized campaign progress
`t = (level - 1) / 99`. There is no hand-authored table, which means the curve can be retuned
by changing a handful of constants instead of editing a hundred rows.

## The design contract

1. **A bullet always deals 1 damage.** Every hit visibly ticks the number on a ring down by
   one, so the player can always read exactly how much work is left. Difficulty is expressed
   as more rings, tougher rings and a faster spiral - never as an invisible multiplier.
2. **The spiral is the fail timer.** `SpiralDuration` is always at least 45% longer than the
   time the curve expects a competent player to need. A clean run finishes with slack; a
   sloppy one gets eaten.
3. **Steps are smooth, rules are announced.** Sub-linear power curves everywhere, with three
   hard milestones that introduce a NEW rule rather than a bigger number. Each one is shown on
   the level intro card.
4. **Every tenth level is a boss.** ~30% more rings, tougher, faster, more armor - and ~30%
   more time, because otherwise it stops being a challenge and becomes an impossibility.
5. **Each tier of ten levels has its own accent colour**, so progress is legible at a glance.

## The formulas

With `t = (level - 1) / 99`:

| Parameter | Formula | Range |
|---|---|---|
| Rings | `round(22 + 78 * t^0.75)` | 22 -> 100 |
| Min ring HP | `round(2 + 2t)` | 2 -> 4 |
| Max ring HP | `round(5 + 5t)` | 5 -> 10 |
| Target clear time | `18 + 46 * t^0.90` | 18s -> 64s |
| Core breach timer | `targetClear * 1.45` | 26s -> 93s |
| Angular speed | `0.45 + 0.55t` rad/s | 0.45 -> 1.00 |
| Inward acceleration | `0.25 + 0.45t` (+0.35 from level 50) | 0.25 -> 1.05 |
| Armored chance | `0` below 25, then ramps to `0.22` | 0% -> 22% |
| Regeneration | from level 75 | `1 + (L-75)/12` HP/s after 3.5s untouched |

Boss levels (`level % 10 == 0`) then apply: rings x1.30, max HP x1.35, angular x1.15,
breach timer x1.30, armored chance +0.10 (capped at 40%).

### Why the curve exponents are below 1

`t^0.75` on ring count and `t^0.90` on clear time both mean the campaign gets harder **fast at
the start and gently at the end**. Level 1 to 10 roughly doubles the ring count; level 90 to
100 adds a few percent. That is deliberate: early levels are where a player decides whether the
game is worth learning, so the sense of escalation has to be obvious there. By level 90 the
player already has full mastery and the interesting pressure comes from the spiral speed and
the milestone rules, not from the count.

### Inward acceleration

The head's progress along the radius is eased:

```
u        = elapsed / spiralDuration
progress = (u + accel * u^2) / (1 + accel)
radius   = startRadius * (1 - progress)
```

At `u = 1` this is exactly 1 regardless of `accel`, so the fail timer is honest whatever the
acceleration is. What changes is the shape: the head starts at the authored pace and tightens
as it closes. From level 50 the surge bonus roughly doubles that tightening, which is what
makes the last third of a high level feel like it is closing in on you.

## Milestones

| Level | Rule | Banner |
|---|---|---|
| 25 | **Armored rings.** Silver-white, double hit points, an X brace and their own clank on impact. | `NEW: ARMORED RINGS CARRY DOUBLE PLATING` |
| 50 | **The surge.** Inward acceleration jumps by 0.35, so the spiral visibly speeds up as it closes. | `NEW: THE SPIRAL SURGES AS IT CLOSES` |
| 75 | **Regeneration.** A ring left alone for 3.5s heals. Spreading fire thinly across the snake stops working. | `NEW: RINGS REGENERATE IF YOU LET THEM BREATHE` |
| every 10th | Boss spiral. | `BOSS SPIRAL` |
| 100 | The finale. | `THE FINAL CORE` |

> Note: armor is **double hit points**, not damage reduction, and the banner says so. A damage
> reduction would round to nothing against 1-damage bullets and the rule would silently do
> nothing - and the player would have no way to see that it was broken. See
> `SnakeSegment.ArmorHealthMultiplier`.

## Tiers

| Tier | Levels | Name | Accent |
|---|---|---|---|
| 1 | 1-10 | OUTER RIM | ice cyan |
| 2 | 11-20 | DRIFT | mint |
| 3 | 21-30 | THE COIL | acid lime |
| 4 | 31-40 | ASHFALL | amber |
| 5 | 41-50 | EMBER GATE | ember |
| 6 | 51-60 | REDSHIFT | coral |
| 7 | 61-70 | SINGULARITY | hot magenta |
| 8 | 71-80 | VIOLET DEEP | violet |
| 9 | 81-90 | EVENT HORIZON | indigo |
| 10 | 91-100 | WHITE CORE | white hot |

The tier accent drives the grid, the starfield tint, the core ring and the HUD level readout,
so a screenshot alone tells you roughly how far in the player is.

## Balance check

The "health pool" column below is `rings x average HP x (1 + armoredChance)`, i.e. the total
number of bullets that must connect to clear the level.

Against it, the design assumes an effective damage output rising from ~4.7/s at level 1
(5.9 shots/s at 80% accuracy, no boosts yet) to ~13.7/s once supply uptime is high. Under that
model **every level in the campaign clears inside its breach timer**, with the two tightest
being level 10 (the first boss, ~5s of slack) and level 100 (~8s). Those two are supposed to be
walls.

If you retune anything in `LevelDesigner`, re-check that margin before committing.

## Generated table

| Level | Tier | Rings | Ring HP | Health pool | Target clear | Core breach | Angular (rad/s) | Inward accel | Armored | Regen |
|------:|:----:|------:|:-------:|------------:|-------------:|------------:|----------------:|-------------:|--------:|:-----:|
| 1 | OUTER RIM | 22 | 2-5 | 77 | 18s | 26s | 0.45 | 0.25 | 0% | - |
| 2 | OUTER RIM | 24 | 2-5 | 84 | 19s | 27s | 0.46 | 0.25 | 0% | - |
| 3 | OUTER RIM | 26 | 2-5 | 91 | 19s | 28s | 0.46 | 0.26 | 0% | - |
| 4 | OUTER RIM | 28 | 2-5 | 98 | 20s | 29s | 0.47 | 0.26 | 0% | - |
| 5 | OUTER RIM | 29 | 2-5 | 102 | 21s | 30s | 0.47 | 0.27 | 0% | - |
| 6 | OUTER RIM | 30 | 2-5 | 105 | 21s | 31s | 0.48 | 0.27 | 0% | - |
| 7 | OUTER RIM | 32 | 2-5 | 112 | 22s | 31s | 0.48 | 0.28 | 0% | - |
| 8 | OUTER RIM | 33 | 2-5 | 116 | 22s | 32s | 0.49 | 0.28 | 0% | - |
| 9 | OUTER RIM | 34 | 2-5 | 119 | 23s | 33s | 0.49 | 0.29 | 0% | - |
| 10 | **BOSS** | 46 | 2-7 | 228 | 23s | 44s | 0.57 | 0.29 | 10% | - |
| 11 | DRIFT | 36 | 2-6 | 144 | 24s | 35s | 0.51 | 0.30 | 0% | - |
| 12 | DRIFT | 37 | 2-6 | 148 | 24s | 35s | 0.51 | 0.30 | 0% | - |
| 13 | DRIFT | 38 | 2-6 | 152 | 25s | 36s | 0.52 | 0.30 | 0% | - |
| 14 | DRIFT | 39 | 2-6 | 156 | 25s | 37s | 0.52 | 0.31 | 0% | - |
| 15 | DRIFT | 40 | 2-6 | 160 | 26s | 38s | 0.53 | 0.31 | 0% | - |
| 16 | DRIFT | 41 | 2-6 | 164 | 26s | 38s | 0.53 | 0.32 | 0% | - |
| 17 | DRIFT | 42 | 2-6 | 168 | 27s | 39s | 0.54 | 0.32 | 0% | - |
| 18 | DRIFT | 43 | 2-6 | 172 | 27s | 40s | 0.54 | 0.33 | 0% | - |
| 19 | DRIFT | 44 | 2-6 | 176 | 28s | 40s | 0.55 | 0.33 | 0% | - |
| 20 | **BOSS** | 58 | 2-8 | 319 | 28s | 54s | 0.64 | 0.34 | 10% | - |
| 21 | THE COIL | 46 | 2-6 | 184 | 29s | 42s | 0.56 | 0.34 | 0% | - |
| 22 | THE COIL | 46 | 2-6 | 184 | 29s | 43s | 0.57 | 0.35 | 0% | - |
| 23 | THE COIL | 47 | 2-6 | 188 | 30s | 43s | 0.57 | 0.35 | 0% | - |
| 24 | THE COIL | 48 | 2-6 | 192 | 30s | 44s | 0.58 | 0.35 | 0% | - |
| 25 | THE COIL | 49 | 2-6 | 196 | 31s | 45s | 0.58 | 0.36 | 0% | - |
| 26 | THE COIL | 50 | 3-6 | 226 | 31s | 45s | 0.59 | 0.36 | 0% | - |
| 27 | THE COIL | 51 | 3-6 | 231 | 32s | 46s | 0.59 | 0.37 | 1% | - |
| 28 | THE COIL | 51 | 3-6 | 232 | 32s | 47s | 0.60 | 0.37 | 1% | - |
| 29 | THE COIL | 52 | 3-6 | 237 | 33s | 48s | 0.61 | 0.38 | 1% | - |
| 30 | **BOSS** | 69 | 3-8 | 423 | 33s | 63s | 0.70 | 0.38 | 11% | - |
| 31 | ASHFALL | 54 | 3-7 | 275 | 34s | 49s | 0.62 | 0.39 | 2% | - |
| 32 | ASHFALL | 55 | 3-7 | 281 | 34s | 50s | 0.62 | 0.39 | 2% | - |
| 33 | ASHFALL | 55 | 3-7 | 281 | 35s | 50s | 0.63 | 0.40 | 2% | - |
| 34 | ASHFALL | 56 | 3-7 | 287 | 35s | 51s | 0.63 | 0.40 | 3% | - |
| 35 | ASHFALL | 57 | 3-7 | 293 | 36s | 52s | 0.64 | 0.40 | 3% | - |
| 36 | ASHFALL | 58 | 3-7 | 299 | 36s | 52s | 0.64 | 0.41 | 3% | - |
| 37 | ASHFALL | 59 | 3-7 | 305 | 37s | 53s | 0.65 | 0.41 | 4% | - |
| 38 | ASHFALL | 59 | 3-7 | 306 | 37s | 54s | 0.66 | 0.42 | 4% | - |
| 39 | ASHFALL | 60 | 3-7 | 312 | 37s | 54s | 0.66 | 0.42 | 4% | - |
| 40 | **BOSS** | 79 | 3-9 | 542 | 38s | 71s | 0.77 | 0.43 | 14% | - |
| 41 | EMBER GATE | 62 | 3-7 | 325 | 38s | 56s | 0.67 | 0.43 | 5% | - |
| 42 | EMBER GATE | 62 | 3-7 | 325 | 39s | 56s | 0.68 | 0.44 | 5% | - |
| 43 | EMBER GATE | 63 | 3-7 | 332 | 39s | 57s | 0.68 | 0.44 | 5% | - |
| 44 | EMBER GATE | 64 | 3-7 | 338 | 40s | 58s | 0.69 | 0.45 | 6% | - |
| 45 | EMBER GATE | 64 | 3-7 | 339 | 40s | 58s | 0.69 | 0.45 | 6% | - |
| 46 | EMBER GATE | 65 | 3-7 | 345 | 41s | 59s | 0.70 | 0.45 | 6% | - |
| 47 | EMBER GATE | 66 | 3-7 | 351 | 41s | 60s | 0.71 | 0.46 | 6% | - |
| 48 | EMBER GATE | 67 | 3-7 | 358 | 42s | 60s | 0.71 | 0.46 | 7% | - |
| 49 | EMBER GATE | 67 | 3-7 | 359 | 42s | 61s | 0.72 | 0.47 | 7% | - |
| 50 | **BOSS** | 88 | 3-9 | 620 | 42s | 80s | 0.83 | 0.82 | 17% | - |
| 51 | REDSHIFT | 69 | 3-8 | 408 | 43s | 62s | 0.73 | 0.83 | 8% | - |
| 52 | REDSHIFT | 69 | 3-8 | 410 | 43s | 63s | 0.73 | 0.83 | 8% | - |
| 53 | REDSHIFT | 70 | 3-8 | 417 | 44s | 63s | 0.74 | 0.84 | 8% | - |
| 54 | REDSHIFT | 71 | 3-8 | 424 | 44s | 64s | 0.74 | 0.84 | 9% | - |
| 55 | REDSHIFT | 72 | 3-8 | 431 | 45s | 65s | 0.75 | 0.85 | 9% | - |
| 56 | REDSHIFT | 72 | 3-8 | 432 | 45s | 65s | 0.76 | 0.85 | 9% | - |
| 57 | REDSHIFT | 73 | 3-8 | 439 | 46s | 66s | 0.76 | 0.85 | 9% | - |
| 58 | REDSHIFT | 74 | 3-8 | 446 | 46s | 67s | 0.77 | 0.86 | 10% | - |
| 59 | REDSHIFT | 74 | 3-8 | 448 | 46s | 67s | 0.77 | 0.86 | 10% | - |
| 60 | **BOSS** | 98 | 3-11 | 825 | 47s | 88s | 0.89 | 0.87 | 20% | - |
| 61 | SINGULARITY | 76 | 3-8 | 462 | 47s | 69s | 0.78 | 0.87 | 11% | - |
| 62 | SINGULARITY | 76 | 3-8 | 463 | 48s | 69s | 0.79 | 0.88 | 11% | - |
| 63 | SINGULARITY | 77 | 3-8 | 471 | 48s | 70s | 0.79 | 0.88 | 11% | - |
| 64 | SINGULARITY | 78 | 3-8 | 478 | 49s | 71s | 0.80 | 0.89 | 11% | - |
| 65 | SINGULARITY | 78 | 3-8 | 479 | 49s | 71s | 0.81 | 0.89 | 12% | - |
| 66 | SINGULARITY | 79 | 3-8 | 487 | 49s | 72s | 0.81 | 0.90 | 12% | - |
| 67 | SINGULARITY | 80 | 3-8 | 494 | 50s | 72s | 0.82 | 0.90 | 12% | - |
| 68 | SINGULARITY | 80 | 3-8 | 495 | 50s | 73s | 0.82 | 0.90 | 13% | - |
| 69 | SINGULARITY | 81 | 3-8 | 503 | 51s | 74s | 0.83 | 0.91 | 13% | - |
| 70 | **BOSS** | 105 | 3-11 | 906 | 51s | 97s | 0.96 | 0.91 | 23% | - |
| 71 | VIOLET DEEP | 82 | 3-9 | 558 | 52s | 75s | 0.84 | 0.92 | 13% | - |
| 72 | VIOLET DEEP | 83 | 3-9 | 567 | 52s | 76s | 0.84 | 0.92 | 14% | - |
| 73 | VIOLET DEEP | 83 | 3-9 | 568 | 53s | 76s | 0.85 | 0.93 | 14% | - |
| 74 | VIOLET DEEP | 84 | 3-9 | 576 | 53s | 77s | 0.86 | 0.93 | 14% | - |
| 75 | VIOLET DEEP | 85 | 3-9 | 585 | 53s | 77s | 0.86 | 0.94 | 15% | yes |
| 76 | VIOLET DEEP | 85 | 4-9 | 635 | 54s | 78s | 0.87 | 0.94 | 15% | yes |
| 77 | VIOLET DEEP | 86 | 4-9 | 644 | 54s | 79s | 0.87 | 0.95 | 15% | yes |
| 78 | VIOLET DEEP | 87 | 4-9 | 653 | 55s | 79s | 0.88 | 0.95 | 16% | yes |
| 79 | VIOLET DEEP | 87 | 4-9 | 655 | 55s | 80s | 0.88 | 0.95 | 16% | yes |
| 80 | **BOSS** | 114 | 4-12 | 1150 | 56s | 105s | 1.02 | 0.96 | 26% | yes |
| 81 | EVENT HORIZON | 88 | 4-9 | 666 | 56s | 81s | 0.89 | 0.96 | 16% | yes |
| 82 | EVENT HORIZON | 89 | 4-9 | 675 | 56s | 82s | 0.90 | 0.97 | 17% | yes |
| 83 | EVENT HORIZON | 90 | 4-9 | 685 | 57s | 82s | 0.91 | 0.97 | 17% | yes |
| 84 | EVENT HORIZON | 90 | 4-9 | 686 | 57s | 83s | 0.91 | 0.98 | 17% | yes |
| 85 | EVENT HORIZON | 91 | 4-9 | 696 | 58s | 84s | 0.92 | 0.98 | 18% | yes |
| 86 | EVENT HORIZON | 92 | 4-9 | 705 | 58s | 84s | 0.92 | 0.99 | 18% | yes |
| 87 | EVENT HORIZON | 92 | 4-9 | 707 | 59s | 85s | 0.93 | 0.99 | 18% | yes |
| 88 | EVENT HORIZON | 93 | 4-9 | 716 | 59s | 85s | 0.93 | 1.00 | 18% | yes |
| 89 | EVENT HORIZON | 93 | 4-9 | 718 | 59s | 86s | 0.94 | 1.00 | 19% | yes |
| 90 | **BOSS** | 122 | 4-12 | 1260 | 60s | 113s | 1.09 | 1.00 | 29% | yes |
| 91 | WHITE CORE | 95 | 4-10 | 794 | 60s | 87s | 0.95 | 1.01 | 19% | yes |
| 92 | WHITE CORE | 95 | 4-10 | 796 | 61s | 88s | 0.96 | 1.01 | 20% | yes |
| 93 | WHITE CORE | 96 | 4-10 | 806 | 61s | 89s | 0.96 | 1.02 | 20% | yes |
| 94 | WHITE CORE | 96 | 4-10 | 808 | 61s | 89s | 0.97 | 1.02 | 20% | yes |
| 95 | WHITE CORE | 97 | 4-10 | 818 | 62s | 90s | 0.97 | 1.03 | 21% | yes |
| 96 | WHITE CORE | 98 | 4-10 | 829 | 62s | 90s | 0.98 | 1.03 | 21% | yes |
| 97 | WHITE CORE | 98 | 4-10 | 831 | 63s | 91s | 0.98 | 1.04 | 21% | yes |
| 98 | WHITE CORE | 99 | 4-10 | 841 | 63s | 92s | 0.99 | 1.04 | 21% | yes |
| 99 | WHITE CORE | 99 | 4-10 | 843 | 64s | 92s | 0.99 | 1.05 | 22% | yes |
| 100 | **BOSS** | 130 | 4-14 | 1544 | 64s | 121s | 1.15 | 1.05 | 32% | yes |
