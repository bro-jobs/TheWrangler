@echo off
echo Building Wrangler Master (Flet Edition - v0.19.0)
echo.

REM Install/update dependencies (with specific Flet version)
echo Installing dependencies...
pip install flet==0.19.0 requests

REM Build with Flet's built-in packaging
echo.
echo Building with flet pack...
flet pack wrangler_master_flet.py --name "Wrangler Master" --icon icon.ico

echo.
echo Build complete! Check the 'dist' folder for the executable.
echo.
echo NOTE: Flet 0.19.0 produces much smaller executables (~11MB compressed)
echo compared to newer versions (~140MB+).
pause
