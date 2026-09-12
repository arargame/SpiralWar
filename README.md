# SpiralWar

SpiralWar is an arcade survival shooter built with **C#** and **MonoGame**. One ship, bolted to
the core of a neon arena. A segmented snake spirals in from off-screen. Destroy every ring
before the head reaches the middle.

![Gameplay Screenshot](screenshot.png)

## Features

* **100 hand-tuned levels** generated from one smooth difficulty curve - boss spirals every ten
  levels, ten colour tiers, and three milestone rules that each introduce something new:
  armored rings at 25, the accelerating surge at 50, regenerating rings at 75.
* **Readable damage.** A bullet always deals exactly 1. Every hit ticks the number printed on a
  ring down by one, flashes it white, knocks it back, throws sparks and plays a sound pitched
  by how much of that ring it took off. You never have to wonder whether a shot landed.
* **Carrier rings.** Rings that are carrying a supply wear its glyph from the moment they
  appear, so choosing what to shoot is a real decision instead of a lottery.
  `T` triple fire, `D` diffused fire, `+` repair - and `T` and `D` stack into a seven-shot fan.
* **Fully procedural presentation.** Every visual is drawn from a single white texel, and every
  sound is synthesized at start-up. There is not one art or audio file in this repository.
* **Real neon.** Additive glow layers that accumulate light like a tube instead of averaging to
  grey, orbiting parallax starfield, trauma-based camera shake.
* **Desktop and mobile from one project.** The game is authored at 1280x720 and letterboxed onto
  any back buffer; mouse, touch, keyboard and gamepad all arrive through one device-neutral
  input layer.
* **Persistent profile.** Settings and progression as readable JSON, with a per-installation
  GUID for support and future cloud features.

## Controls

| | |
|---|---|
| Aim | Move the pointer / touch |
| Fire | Hold left button / hold touch |
| Navigate menus | Arrows, W/S, D-pad, or pointer |
| Confirm | Enter, Space, A |
| Back / pause | Escape, B, Android back |

## Technologies

* **Language:** C# (.NET 9)
* **Framework:** MonoGame 3.8
* **Architecture:** shared project (`SpiralWar.Shared`) driving platform head projects

## Getting started

```bash
git clone https://github.com/arargames/SpiralWar.git
```

1. Open `SpiralWar.sln` in Visual Studio or Rider.
2. Set `SpiralWar.Desktop` as the startup project.
3. Build and run.

## Project layout

```
SpiralWar.Shared/     all gameplay, rendering, audio and UI
  Core/               pooling, config, input, virtual viewport
  Audio/              procedural synthesizer and sound bank
  Levels/             the 100 level curve
  Entities/           player, snake, rings, bullets, supplies, particles
  Rendering/          primitives, neon, HUD, draw context
  Screens/            screen stack and every screen
  Systems/            collision, bullets, supplies, particles, shake
SpiralWar.Desktop/    Windows head project
docs/                 technical documentation - read before changing an area
```

## Documentation

Detailed technical notes live in [`docs/`](docs/README.md): design goals, architecture, the full
level table and its arithmetic, the supply drop table, every sound recipe, the rendering
approach and the persistence format.

## Contributing

Code, comments and commit messages are English. SOLID, DRY and YAGNI are taken seriously here.
Before working on an area, read its document in `docs/`; after finishing, update it.
