# Audio Design

There are no audio files in this repository. Every sound is synthesized into a float buffer at
start-up and handed to MonoGame as mono 16-bit PCM. Retuning a sound means editing numbers in
`SoundBank`, not opening a DAW.

## The synthesizer

`SoundBuilder` is a small offline synth with a fluent API:

| Method | What it does |
|---|---|
| `Tone(start, dur, freqFrom, freqTo, amp, env, wave)` | Oscillator with a frequency glide |
| `Noise(start, dur, amp, env, lowPassHz)` | White noise, low-passed while generated |
| `FmBody(start, dur, carrier, modHz, depth, amp, env)` | FM carrier - metallic impacts |
| `LowPass(hz)` / `HighPass(hz)` | One-pole filters |
| `SoftClip(drive)` | tanh saturation - warmth, and a hard ceiling |
| `Echo(delay, feedback, taps)` | Cheap feedback echo for stingers |
| `NormalizeTo(peak)` | Scales the buffer so its loudest sample hits `peak` |
| `FadeEdges(ms)` | Ramps the first and last few ms to zero |
| `Build(name)` | Converts to `SoundEffect` |

Envelopes are functions of normalized time: `Percussive(sharpness, attack)`,
`Ads(attack, release)`, `Swell(peak)`.

Waveforms are `Sine`, `Triangle` (`asin(sin(x))` - naturally rolled-off harmonics) and
`SoftSquare` (`tanh(3 sin x)` - punchy without the razor edges of a real square). There is no
raw saw or square on purpose.

## The rule that keeps it listenable

**Every sound ends with a low-pass, a soft clip and an edge fade.**

- The **low-pass** removes energy above ~7 kHz, which is the band that causes listening fatigue
  on a sound you hear five times a second.
- The **soft clip** guarantees the buffer never clips hard, and adds a little warmth.
- The **edge fade** removes the DC step at the buffer boundary. Without it, every single
  playback starts and ends with a click, and at six shots a second that click becomes the
  loudest thing in the mix.

This is the difference between the fire sound being a texture you stop noticing and it being
the reason someone turns the volume off.

## Impact design

An impact is always **a very short filtered click plus a low body**:

- the **click** (10-20 ms of band-limited noise) says *I hit something*
- the **body** (an FM tone falling from ~330 Hz) says *and it was solid*

`SegmentHit` uses a 330 Hz carrier; `SegmentArmorHit` uses 470 Hz with a second FM layer and a
wider filter, so an armored ring answers with an audibly harder, more metallic clank. The
player learns the difference without being told.

At play time the hit is **pitched by how much of the ring that hit took off**
(`damageApplied / maxHealth`), so finishing a nearly-dead ring sounds different from scratching
a fresh one.

## The sound bank

| Id | Length | Recipe summary | Peak |
|---|---|---|---|
| `Shoot` | 0.11s | Triangle 760->240 Hz + click, LP 2.6 kHz | 0.50 |
| `ShootWide` | 0.14s | Triangle 620->190 Hz, two layers, LP 2.3 kHz | 0.55 |
| `SegmentHit` | 0.10s | FM 330 Hz + 14 ms click, LP 6.2 kHz | 0.72 |
| `SegmentArmorHit` | 0.16s | FM 470 + 940 Hz, LP 7 kHz | 0.74 |
| `SegmentDestroy` | 0.34s | Two noise bursts + 260->58 Hz sweep | 0.85 |
| `SupplyDrop` | 0.20s | Quiet 1180 + 1770 Hz chime | 0.42 |
| `SupplyPickup` | 0.26s | Rising fifth, 659 -> 988 Hz | 0.62 |
| `HealthPickup` | 0.40s | Major triad 523/659/784 with echo | 0.62 |
| `PlayerHurt` | 0.30s | 210->62 Hz thud, LP 1.1 kHz | 0.80 |
| `PlayerDown` | 1.10s | 420->74 Hz sweep, noise wash, echo | 0.72 |
| `LevelClear` | 1.00s | Arpeggio 392/523/659/784 + echo | 0.68 |
| `BossWarning` | 1.20s | Three low swells at 88-110 Hz | 0.70 |
| `MenuMove` | 0.07s | 880 Hz blip | 0.34 |
| `MenuSelect` | 0.18s | 660 -> 1320 Hz | 0.50 |
| `MenuBack` | 0.16s | 620 -> 330 Hz (descending - the inverse of select) | 0.44 |

Fire sounds are the quietest things in the bank relative to their frequency of use. That is
deliberate: the mix should be led by impacts, because impacts carry information.

## Voice throttling

`AudioManager` enforces a minimum interval per sound and silently drops a retrigger that
arrives too early:

| Sound | Minimum interval |
|---|---|
| `Shoot` | 45 ms (~22/s ceiling) |
| `ShootWide` | 60 ms |
| `SegmentHit` | 22 ms |
| `SegmentArmorHit` | 30 ms |
| `SegmentDestroy` | 40 ms |
| `SupplyDrop` | 70 ms |
| `PlayerHurt` | 220 ms |
| `MenuMove` | 50 ms |

This matters more than it looks. A diffused+triple volley fires seven bullets in one frame. If
each bullet triggered the fire sound, that would be seven phase-aligned copies of the same
waveform: seven times too loud, and the phase alignment turns a soft "thup" into a harsh click.

Two rules follow from that and are enforced at the call site in `GameScreen`:

1. **One fire sound per volley, not per bullet.**
2. **Every one-shot is played through `PlayVaried`**, which applies a small random pitch offset
   (+/-0.10 to +/-0.14). Identical repeated waveforms are what the ear hears as a machine gun
   buzz; a few percent of pitch scatter is what turns the same samples into a texture.

Impacts are also panned by their X position (`AudioManager.PanFor`, scaled to 65% so it reads
as space rather than as a gimmick).

## Settings and failure

Volumes come from `GameConfig.Settings` (`MasterVolume`, `SfxVolume`, `Muted`) and are applied
through `SoundEffect.MasterVolume` plus a per-play multiplier. `ApplySettings()` is called from
the settings screen so the player hears their change immediately.

Initialization is wrapped in try/catch: **a machine with no audio device must still be able to
play the game**. If the bank fails to build, `_initialized` stays false and every `Play` call
becomes a no-op.

## v1.2 additions

Eleven sounds were added for dynamite, missiles, diamonds and status effects.

| Id | Length | Recipe summary | Peak |
|---|---|---|---|
| `CriticalHit` | 0.22s | 2640 + 3960 Hz glassy ping over the normal impact | 0.58 |
| `DynamiteBlast` | 0.85s | 150->34 Hz sub drop + long filtered noise + FM crack, echo | 0.95 |
| `DynamiteChain` | 0.46s | Shorter, drier, 15% quieter version of the blast | 0.80 |
| `AcidTick` | 0.09s | High-passed noise fizz + a faint 1700 Hz tail | 0.26 |
| `MissileLaunch` | 0.26s | Rising hiss with a 220->520 Hz thump underneath | 0.45 |
| `MissileImpact` | 0.34s | Compact version of the blast | 0.78 |
| `DiamondDrop` | 0.26s | 2093 + 3136 Hz glass chime | 0.38 |
| `DiamondPickup` | 0.46s | Crystalline major triad with a long echoed tail | 0.60 |
| `DiamondLevelUp` | 0.56s | The pickup plus an extra octave on top | 0.64 |
| `StasisStart` | 0.70s | 1760->440 Hz descending shimmer | 0.60 |
| `StasisEnd` | 0.42s | The same shimmer running back up | 0.44 |

### Design notes

**A critical plays ON TOP of the normal impact** rather than replacing it, so a crit sounds
like the same hit with something extra. Replacing it would have made criticals read as a
different weapon rather than as a better outcome from the same one.

**Dynamite chains get their own shorter, quieter sound.** A six-charge cascade fires inside two
frames. With one sound at one volume it is a single clipped roar; with a distinct chain sound
and a 70ms floor, it is something you can hear count.

**Acid needed the harshest throttle in the bank (110ms).** It ticks twice a second on every
burning ring at once, and at level 3 that can be a dozen rings - without a hard floor it
becomes a constant fizz sitting under everything else in the mix.

**The stasis pair is pitch direction as information.** Falling pitch means time just got
slower; rising pitch means it is back. Neither needs a label.

### New throttles

| Sound | Minimum interval |
|---|---|
| `DynamiteBlast` | 90 ms |
| `DynamiteChain` | 70 ms |
| `AcidTick` | 110 ms |
| `CriticalHit` | 55 ms |
| `MissileLaunch` | 90 ms |
| `MissileImpact` | 60 ms |
| `DiamondDrop` | 70 ms |

## Ideas not yet implemented

- A slow generative drone keyed to the tier accent colour and the danger value.
- Ducking the fire bus while a destruction sound plays.
- A stereo width pass on the destruction sound using two decorrelated noise seeds.
