# Architecture

## Layers and dependency direction

Dependencies point downward only. Nothing in a lower layer knows a higher one exists.

```
                     SpiralWarGame            <- composition root, picks implementations
                            |
                        Screens               <- state machines, event wiring
                     /      |      \
              Systems   Rendering   Levels    <- gameplay rules / drawing / difficulty
                   \       |       /
                     Entities                 <- Player, Snake, SnakeSegment, Bullet, Supply
                            |
                 Core  +  Audio               <- pooling, config, input, viewport, synth
```

`SpiralWarGame` is the only file that names a concrete implementation. Everything below it
receives what it needs through an interface or a constructor parameter, which is what will let
the Android head project reuse `SpiralWar.Shared` unchanged.

## The interfaces that matter

| Interface | Why it exists | Current implementation |
|---|---|---|
| `IAudioService` | Swap the synthesizer for streamed assets without touching gameplay | `AudioManager` |
| `ILevelProvider` | Swap the formula for a JSON campaign or a daily challenge | `LevelDesigner` |
| `IConfigStore` | Swap the file system for isolated storage or a cloud save | `JsonConfigStore` |
| `IScreen` | Uniform lifecycle for every screen | `BaseScreen` and its subclasses |
| `IPoolable` | Recycling without forcing a base class on particles and bullets | `Bullet`, `Particle`, `FloatingText`, `Supply` |
| `IUpdatable` / `IRenderable` | Narrow contracts a system can implement without being a GameObject | several systems |

## Screen management

`ScreenManager` owns a stack, the shared resources and the transition.

| Operation | Effect | Used by |
|---|---|---|
| `SetRoot` | Clears the stack and installs the first screen | start-up |
| `Push` | Adds on top, keeps what is underneath | settings, credits, pause |
| `Pop` | Returns to the previous screen | every BACK |
| `Replace` | Swaps the top without growing the stack | menu to game, level to level |
| `PopToRoot` | Unwinds to the main menu | quit to menu, run over |

**Every one of those is deferred.** A request sets `_pendingChange` and starts a fade-out; the
change is applied at the midpoint and a fade-in follows. Two consequences:

1. A screen can request a change from inside its own `Update` without the manager mutating the
   list it is currently iterating. That removes an entire class of bug by construction.
2. Transitions are automatic. There is no way to write a hard cut by accident.

While a change is in flight, further requests are ignored - double-clicking CONTINUE cannot
queue two level loads.

### Back navigation

`ScreenManager.Update` reads one device-neutral `BackPressed` (Escape, gamepad B/Back, Android
back) and offers it to the top screen first via `IScreen.OnBack()`. A screen returning `true`
has consumed it; returning `false` lets the manager pop. When the stack would empty,
`StackEmptied` fires and the game saves and exits.

- `GameScreen.OnBack` pushes the pause overlay instead of quitting.
- `PauseScreen.OnBack` pops itself, so back resumes.
- `MenuScreen` does not override it, so back from the root exits.

### Popups

`IScreen.IsPopup` tells the manager to keep drawing the screen underneath and dim it.
`PauseScreen` is the only popup today; the frozen arena stays visible behind it.

## Rendering passes

`DrawContext` wraps the `SpriteBatch` and owns the blend state. Screens never call
`Begin`/`End`; they ask for a mode and the context restarts the batch only when something
actually changed:

| Call | Blend | Camera shake | Used for |
|---|---|---|---|
| `UseWorld()` | Additive | yes | neon, particles, bullets, the snake |
| `UseWorldAlpha()` | Alpha | yes | anything over the world that must darken |
| `UseHud()` | Alpha | no | HUD, menus, overlays |
| `UseHudGlow()` | Additive | no | HUD glow, selected buttons, titles |

Additive is what makes the neon read as light rather than as grey fog - see
[rendering.md](rendering.md).

## Class map

```
Core/
  GameObject, Sprite, IUpdatable, IRenderable   base types (unchanged from v1)
  SaveManager                                   progression facade + legacy save.dat migration
  ViewportAdapter                               virtual canvas -> real back buffer
  Input/InputState                              keyboard + mouse + touch + gamepad, unified
  Pooling/IPoolable, ObjectPool<T>              fixed-capacity, allocation-free recycling
  Config/GameSettings, PlayerProfile,
         IConfigStore, JsonConfigStore,
         GameConfig                             persisted state, single entry point

Audio/
  SoundId, IAudioService
  SoundBuilder                                  offline synthesizer (oscillators, filters)
  SoundBank                                     one recipe per sound
  AudioManager                                  playback, volume, voice throttling

Levels/
  LevelDefinition                               immutable per-level parameters
  ILevelProvider, LevelDesigner                 the 100 level curve

Entities/
  Entity, Enemy                                 hit points, damage routing
  Player                                        aim, fire pattern, hull, boosts
  Snake, SnakeSegment                           the spiral, arc-length trail, rings
  Bullet, Supply, SupplyType(+Info)
  Particle, FloatingText                        pooled feedback objects

Rendering/
  PrimitiveDrawer                               lines, boxes, circles from one texel
  NeonRenderer                                  the neon look
  Starfield, HudRenderer, DrawContext

Screens/
  IScreen, BaseScreen, MenuButton, ScreenManager
  MenuScreen, SettingsScreen, CreditsScreen, PauseScreen, GameScreen

Systems/
  CollisionMath, CollisionSystem                oriented + swept tests
  BulletSystem, SupplySystem
  ParticleSystem, FloatingTextSystem, CameraShake
```

## Event flow of a single shot

This is the clearest illustration of how the layers talk to each other. Nothing calls down
into a lower layer to produce an effect; the lower layer raises an event and `GameScreen`
decides what it means.

```
InputState.FireHeld
  -> Player.Update              cooldown expires, raises Fired
    -> GameScreen.OnPlayerFired builds the volley from Player.GetShotAngles,
                                spawns pooled bullets, one muzzle flash, ONE sound
      -> BulletSystem.Update    moves bullets, records PreviousPosition
        -> CollisionSystem      swept test vs oriented rings, applies damage,
                                raises BulletImpact
          -> GameScreen         sparks, shake, damage number, pitched hit sound
            -> SnakeSegment     flash, knock-back, HP text updates
              -> Snake          raises SegmentKilled on death
                -> GameScreen   destruction particles, shake, destroy sound
                  -> SupplySystem.ReleaseFrom  drops the ring's carried supply
```

## v1.2 additions to the class map

```
Levels/
  SnakeForm, SnakeFormInfo        the five shapes and their count/health/scale factors

Entities/
  PlayerLoadout                   every timed effect + the stats derived from them
  DiamondType(+Info), Diamond     the four neon diamonds
  Missile                         pooled homing projectile

Systems/
  DiamondSystem                   twin of SupplySystem for the stones
  MissileSystem                   steering, retargeting and detonation
  DetonationSystem                dynamite blasts and the iterative chain
```

`PlayerLoadout` is the one worth explaining. Weapon supplies, four diamond stacks and the
stasis field are all "a timer plus what that timer is worth". Keeping them in one class means
there is exactly one answer to "what is the fire cooldown right now" instead of four classes
each holding a piece of it, and `Player` gets to stay about aiming and firing.

### Event flow of a dynamite chain

```
CollisionSystem        bullet kills a dynamite ring
  -> Snake.SegmentKilled
    -> GameScreen      sees IsDynamite, calls DetonationSystem.Enqueue (depth 0)
GameScreen             after collisions: DetonationSystem.Resolve(snake, level)
  -> dequeue blast     apply falloff damage to every ring in radius
    -> Snake.SegmentKilled for each ring that dies
      -> GameScreen    another dynamite? Enqueue at currentDepth + 1
  -> Detonated event   GameScreen plays blast/chain sound, particles, shake
  ... loop until the queue empties or the depth cap is hit
```

The loop is explicit and the depth is a plain number. Recursing through the kill event instead
would mean a hundred-ring cascade unwinding on the call stack inside a collision loop, with
every handler running at an unpredictable depth.

## Adding something new

- **A new sound:** add to `SoundId`, write a recipe in `SoundBank`, give it a throttle in
  `AudioManager.ConfigureThrottles` if it can fire more than a few times a second.
- **A new screen:** inherit `BaseScreen`, implement `OnButtonActivated` and `Draw`, push it.
  Back navigation and the fade come for free.
- **A new pickup:** add to `SupplyType`, give it a colour, glyph and name in `SupplyTypeInfo`,
  handle it in `Supply.Collect` and in the roll in `SnakeSegment.RollSupply`.
- **A new level rule:** add a field to `LevelDefinition`, compute it in `LevelDesigner.Compose`,
  and give it a milestone banner if the player needs to be told about it.
