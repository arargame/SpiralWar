# Persistence

## Where things live

```
%LOCALAPPDATA%\ArarGames\SpiralWar\
    settings.json
    profile.json
```

On other platforms the root is
`Environment.GetFolderPath(SpecialFolder.LocalApplicationData)/ArarGames/SpiralWar`.

Both files are human-readable indented JSON on purpose - a player can inspect or back them up,
and a support request can be answered by asking for one file.

## The two documents

### settings.json - `GameSettings`

How the player wants the game to behave. Audio volumes, window size, full screen, vsync,
screen shake scale, glow scale, damage numbers, language, and a first-run flag.

`Sanitize()` clamps every field after loading, so a hand-edited or corrupt file can never put
the game into an unusable state (a zero-size window, a negative volume).

### profile.json - `PlayerProfile`

Who the player is and how far they have got.

```json
{
  "Version": 1,
  "PlayerId": "3f2b8c14-....",
  "DisplayName": "",
  "CreatedUtc": "2026-09-12T...",
  "LastPlayedUtc": "2026-09-12T...",
  "HighestLevelReached": 12,
  "CurrentLevel": 12,
  "TotalSegmentsDestroyed": 1843,
  "TotalShotsFired": 9021,
  "TotalSuppliesCollected": 214,
  "TotalDeaths": 7,
  "TotalPlaySeconds": 4210.5,
  "BestScore": 14400
}
```

## The player GUID

`PlayerId` is generated once, on first run, by `PlayerProfile.EnsureValid()` and never changes.

It is the key every later feature hangs off:

- cloud save and cross-device restore
- leaderboards and daily challenges
- IAP entitlement
- crash reports and analytics
- **support** - the CREDITS & SUPPORT screen shows the first 8 characters as `PROFILE ID`, so a
  bug report can be tied to a profile without ever asking the player for personal information

It contains no personal data by design. It is a random v4 GUID, not a device identifier, not a
hardware hash, and not derived from anything about the machine. That matters both ethically and
practically - store review processes treat device identifiers very differently from
per-installation random ids.

`DisplayName` is present and empty; nothing writes to it yet.

## How writes work

`JsonConfigStore.Save` writes to `<name>.json.tmp` first and then moves it over the real file.
A crash mid-save can therefore never leave a half-written profile behind - the worst case is a
stale file plus an orphaned `.tmp`.

Neither `Load` nor `Save` ever throws. A corrupt document logs and returns `null`, and the
caller falls back to defaults. **A bad save file must never stop the game from starting.**

## Access pattern

`GameConfig` is the single entry point. It is initialized in the `SpiralWarGame` constructor -
before the graphics device is configured, because the window size and full-screen flag come
from the settings - and every other system reads `GameConfig.Settings` / `GameConfig.Profile`
rather than touching the file system.

```
GameConfig.Initialize()            // once, at start-up; creates the GUID on first run
GameConfig.Settings                // live GameSettings
GameConfig.Profile                 // live PlayerProfile
GameConfig.SaveSettings()          // after the settings screen
GameConfig.SaveProfile()           // after a level or a finished run
GameConfig.ResetProgress()         // NEW GAME - keeps identity and lifetime stats
```

The store is injected (`Initialize(IConfigStore)`), so tests or a future cloud backend can
supply their own.

## Progression

`SaveManager` is a thin facade over `GameConfig` for the things gameplay cares about:

| Call | Effect |
|---|---|
| `LoadLevel()` | Level the CONTINUE button resumes from |
| `SaveLevel(n)` | Stores the next level and bumps `HighestLevelReached` |
| `RecordRun(...)` | Folds a finished run into the lifetime statistics |
| `ClearSave()` | NEW GAME - resets `CurrentLevel` to 1 only |

**NEW GAME keeps the player's identity, creation date and lifetime statistics.** Only
`CurrentLevel` is reset. Wiping someone's lifetime numbers because they wanted to replay from
level 1 would be a hostile thing to do.

## Legacy migration

Versions before this one stored progress as a plain integer in
`%LOCALAPPDATA%\SpiralWar\save.dat`. `SaveManager.MigrateLegacySaveIfNeeded()` runs once per
session, on the first `LoadLevel()`, and folds that number into the new profile before deleting
the old file. Existing players keep their progress and never see the transition.

The check is latched with a static bool, so it costs one `File.Exists` per session.

## Versioning

Both documents carry a `Version` field, currently 1. When a breaking schema change happens,
branch on it inside `GameConfig.Initialize` and migrate forward before the game touches the
data. Do not remove the field.
