# SpiralWar - Technical Documentation

This folder is the project's working memory. Before starting any piece of work, read the
document covering that area; after finishing it, record anything worth knowing next time.

## Index

| Document | What it covers |
|---|---|
| [goals.md](goals.md) | What this game is trying to be, and the rules we hold ourselves to |
| [architecture.md](architecture.md) | Layers, dependency direction, screen management, class map |
| [level-design.md](level-design.md) | The 100 level curve, the formulas, and the full generated table |
| [supply-system.md](supply-system.md) | Carrier rings, drop rates, the T / D / + pickups, weapon modes |
| [audio-design.md](audio-design.md) | The procedural synthesizer, every sound recipe, voice throttling |
| [rendering.md](rendering.md) | The neon look, additive passes, damage feedback, performance budget |
| [persistence.md](persistence.md) | GameSettings, PlayerProfile, the player GUID, file locations |
| [changelog.md](changelog.md) | What changed, when, and why |

## Ground rules for this repository

- **Language:** all code, comments and commit messages are written in **English**.
- **Principles:** SOLID, DRY, YAGNI. New behaviour goes behind an interface when there is a
  plausible second implementation, and not before.
- **Platforms:** desktop and mobile share `SpiralWar.Shared` entirely. Anything platform
  specific lives in the head project.
- **Resolution:** the game is authored at 1280x720 and letterboxed by `ViewportAdapter`. Never
  hard-code a real pixel size; work in virtual coordinates.
- **Transitions:** every screen change fades and settles. No hard cuts.
- **Assets:** there is no art and no audio on disk. Every visual is drawn from a 1x1 texel and
  every sound is synthesized at start-up. Keep it that way unless there is a strong reason.
