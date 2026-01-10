@echo off
REM Build script for Wrangler Master executable
REM Requires: pip install pyinstaller requests customtkinter Pillow

echo Installing dependencies...
pip install pyinstaller requests customtkinter Pillow

echo.
echo Building executable...

REM Get the customtkinter package location
for /f "delims=" %%i in ('python -c "import customtkinter; import os; print(os.path.dirname(customtkinter.__file__))"') do set CTK_PATH=%%i

echo CustomTkinter path: %CTK_PATH%

REM Build with customtkinter assets included
pyinstaller --onefile --windowed ^
    --name "WranglerMaster" ^
    --uac-admin ^
    --add-data "%CTK_PATH%;customtkinter/" ^
    --add-data "themes;themes/" ^
    --hidden-import PIL ^
    --hidden-import PIL._tkinter_finder ^
    --collect-all customtkinter ^
    wrangler_master.py

echo.
echo Build complete! Executable is in the 'dist' folder.
echo.
pause
