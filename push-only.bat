@echo off
REM ===================================================================
REM  SpiralWar - push the already-committed work to GitHub.
REM
REM  The v1.2 "Dynamite" commit is already made locally. This script
REM  only pushes it, using the Windows credential manager.
REM  Double-click it, or run it from a terminal in the repo root.
REM ===================================================================
setlocal
cd /d "%~dp0"

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: git is not on PATH. Open "Git Bash" or a Developer Command Prompt and retry.
    pause
    exit /b 1
)

echo.
echo === Commits waiting to be pushed ===
git log --oneline origin/main..HEAD
echo.

git status --short
echo.

set /p CONFIRM="Push to origin/main? (y/n) "
if /i not "%CONFIRM%"=="y" (
    echo Aborted. The commit stays safe locally.
    pause
    exit /b 0
)

git push origin main
if errorlevel 1 (
    echo.
    echo ERROR: push failed. The commit is safe locally; fix the remote/auth and run:
    echo     git push origin main
    pause
    exit /b 1
)

echo.
echo === Done ===
git log --oneline -1
pause
endlocal
