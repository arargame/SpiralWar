@echo off
REM ===================================================================
REM  SpiralWar - stage, commit and push the v1.1 "Carrier" pass.
REM
REM  Double-click this file, or run it from a terminal in the repo root.
REM  It stages only the paths that were actually touched, shows you the
REM  diff summary, asks for confirmation, then commits and pushes.
REM ===================================================================
setlocal
cd /d "%~dp0"

echo.
echo === SpiralWar : staging changed files ===
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: git is not on PATH. Open "Git Bash" or a Developer Command Prompt and retry.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo ERROR: this folder is not a git repository.
    pause
    exit /b 1
)

REM --- Make sure the remote is the intended one -----------------------
git remote get-url origin >nul 2>nul
if errorlevel 1 (
    echo Adding origin remote...
    git remote add origin https://github.com/arargames/SpiralWar.git
)

echo Remote:
git remote -v
echo.

REM --- Stage the touched paths ---------------------------------------
git add README.md
git add push.bat
git add .github/copilot-instructions.md
git add docs
git add SpiralWar.Shared

echo.
echo === Staged changes ===
git status --short
echo.
git diff --cached --stat
echo.

set /p CONFIRM="Commit and push to origin/main? (y/n) "
if /i not "%CONFIRM%"=="y" (
    echo Aborted. Nothing was committed. The files stay staged.
    pause
    exit /b 0
)

git commit ^
 -m "feat: 100-level campaign, carrier supplies, procedural audio and neon pass" ^
 -m "Gameplay: add LevelDesigner/LevelDefinition/ILevelProvider driving a smooth 100-level curve with boss spirals every tenth level, ten colour tiers and milestone rules at 25 (armored rings), 50 (spiral surge) and 75 (regenerating rings). Give the ship a 100 point hull with gated contact damage. Rework supplies into carrier rings that display their glyph from spawn: T triple fire and D diffused fire as independent stacking timers, plus a repair pack." ^
 -m "Feel: hit flash, axial knock-back, spark cones, floating damage numbers and trauma-based camera shake on every connecting shot; shockwave, shards and heavier shake on destruction." ^
 -m "Audio: add a full procedural synthesizer (SoundBuilder, SoundBank, AudioManager) with fifteen recipes, per-sound voice throttling and random pitch variation. One fire sound per volley rather than per bullet. No audio assets on disk." ^
 -m "Collision: replace axis-aligned point tests with oriented bounding boxes and swept segment tests, with broad-phase radius rejection. A bullet crossing several rings now hits the first one." ^
 -m "Rendering: move the world to an additive pass so glow layers accumulate light, fix the line origin so strokes are centred on their segment, add an orbiting parallax starfield, a core danger ring, an extracted HudRenderer and off-screen ring culling." ^
 -m "Architecture: add ObjectPool/IPoolable (bullets, particles, floating text, supplies are all pre-allocated), DrawContext owning the SpriteBatch passes, ViewportAdapter for a letterboxed virtual canvas, a device-neutral InputState, and a rewritten ScreenManager whose stack changes are deferred to the midpoint of a fade. Add BaseScreen plus Settings, Credits and Pause screens with device-neutral back navigation." ^
 -m "Persistence: add GameSettings and PlayerProfile as JSON behind IConfigStore, including a per-installation GUID surfaced as a support id, lifetime statistics, atomic writes and a one-time migration of the legacy save.dat." ^
 -m "Docs: add docs/ with goals, architecture, the full level table and its arithmetic, the supply drop table, every sound recipe, the rendering approach, the persistence format and a changelog." ^
 -m "Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>" ^
 -m "Claude-Session: https://claude.ai/code/session_01DEey78zJG2X2HEAyWvXpXA"

if errorlevel 1 (
    echo.
    echo ERROR: commit failed. Nothing was pushed.
    pause
    exit /b 1
)

echo.
echo === Pushing to origin/main ===
git push -u origin main
if errorlevel 1 (
    echo.
    echo ERROR: push failed. The commit is safe locally; fix the remote/auth and run:
    echo     git push -u origin main
    pause
    exit /b 1
)

echo.
echo === Done ===
git log --oneline -1
pause
endlocal
