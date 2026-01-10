@echo off
echo Building Wrangler Master (Flet Edition)...
echo.

REM Install dependencies
pip install -r requirements.txt

REM Build with Flet's built-in packaging
flet pack wrangler_master_flet.py --name "Wrangler Master" --icon icon.ico

echo.
echo Build complete! Check the 'dist' folder for the executable.
pause
