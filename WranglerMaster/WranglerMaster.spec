# -*- mode: python ; coding: utf-8 -*-
# PyInstaller spec file for WranglerMaster
# Build with: pyinstaller WranglerMaster.spec

import os

block_cipher = None

# Check if custom icon exists
icon_file = 'wrangler.ico' if os.path.exists('wrangler.ico') else None

a = Analysis(
    ['wrangler_master.py'],
    pathex=[],
    binaries=[],
    datas=[],
    hiddenimports=['requests'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    win_no_prefer_redirects=False,
    win_private_assemblies=False,
    cipher=block_cipher,
    noarchive=False,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.zipfiles,
    a.datas,
    [],
    name='WranglerMaster',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,  # No console window (GUI app)
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=icon_file,  # Use custom icon if available
    uac_admin=True,  # Request admin privileges
    uac_uiaccess=False,
)
