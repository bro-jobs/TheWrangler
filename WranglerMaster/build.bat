@echo off
REM Build script for Wrangler Master executable
REM Requires: pip install pyinstaller requests

echo Installing dependencies...
pip install pyinstaller requests

echo.
echo Building executable...
pyinstaller --onefile --windowed --name "WranglerMaster" --uac-admin wrangler_master.py

echo.
echo Build complete! Executable is in the 'dist' folder.
echo.
pause
