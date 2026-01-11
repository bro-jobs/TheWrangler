@echo off
echo ============================================
echo Building Wrangler Master (Flet 0.19.0)
echo ============================================
echo.

REM Set variables
set VENV_DIR=.flet_build_env
set SCRIPT_NAME=wrangler_master_flet.py
set APP_NAME=Wrangler Master
set ICON_FILE=icon.ico
set SETTINGS_FILE=app_settings.json
set CONFIG_FILE=wrangler_config.json

REM Check for --clean flag to force recreate venv
if "%1"=="--clean" (
    echo Cleaning build environment...
    if exist "%VENV_DIR%" rmdir /s /q "%VENV_DIR%"
)

REM Backup settings files before build
echo.
echo Backing up settings files...
if exist "dist\%SETTINGS_FILE%" copy "dist\%SETTINGS_FILE%" "%SETTINGS_FILE%.bak" >nul
if exist "dist\%CONFIG_FILE%" copy "dist\%CONFIG_FILE%" "%CONFIG_FILE%.bak" >nul

:CREATE_VENV
REM Check if virtual environment exists, create if not
if exist "%VENV_DIR%\Scripts\activate.bat" (
    echo Using existing build environment...
) else (
    echo Creating isolated build environment...
    if exist "%VENV_DIR%" rmdir /s /q "%VENV_DIR%"
    python -m venv %VENV_DIR%
    if errorlevel 1 (
        echo ERROR: Failed to create virtual environment.
        echo Make sure Python is installed and in your PATH.
        pause
        exit /b 1
    )
)

REM Activate virtual environment
echo Activating build environment...
call %VENV_DIR%\Scripts\activate.bat

REM Upgrade pip first to avoid issues
echo.
echo Upgrading pip...
python -m pip install --upgrade pip --quiet

REM Uninstall any conflicting packages first
echo.
echo Cleaning up any conflicting packages...
pip uninstall flet flet-cli flet-core flet-desktop flet-runtime flet-web -y >nul 2>&1

REM Install exact versions needed for Flet 0.19.0
echo.
echo Installing Flet 0.19.0 and dependencies...
pip install flet==0.19.0 requests pyinstaller pillow --quiet
if errorlevel 1 (
    echo.
    echo Installation failed. Recreating build environment...
    call deactivate
    rmdir /s /q "%VENV_DIR%"
    goto CREATE_VENV
)

REM Build the application
echo.
echo Building application...
flet pack %SCRIPT_NAME% --name "%APP_NAME%" --icon %ICON_FILE%
if errorlevel 1 (
    echo ERROR: Build failed.
    call deactivate
    pause
    exit /b 1
)

REM Deactivate virtual environment
call deactivate

REM Restore settings files after build
echo.
echo Restoring settings files...
if exist "%SETTINGS_FILE%.bak" (
    copy "%SETTINGS_FILE%.bak" "dist\%SETTINGS_FILE%" >nul
    del "%SETTINGS_FILE%.bak" >nul
    echo  - Restored %SETTINGS_FILE%
)
if exist "%CONFIG_FILE%.bak" (
    copy "%CONFIG_FILE%.bak" "dist\%CONFIG_FILE%" >nul
    del "%CONFIG_FILE%.bak" >nul
    echo  - Restored %CONFIG_FILE%
)

echo.
echo ============================================
echo Build complete!
echo ============================================
echo.
echo Output: dist\%APP_NAME%.exe
echo.
echo Flet 0.19.0 benefits:
echo  - Small executable size (~11MB compressed)
echo  - Fast startup (near-instant)
echo  - No runtime dependencies
echo.
echo Settings files are preserved between builds.
echo.
echo TIP: Run "build_flet.bat --clean" to force recreate the build environment.
echo.
pause
