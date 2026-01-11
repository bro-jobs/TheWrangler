#!/usr/bin/env python3
"""
Wrangler Master Control Program - Flet Edition (0.19.0 Compatible)
===================================================================

A modern GUI application using Flet (Flutter) to control multiple TheWrangler instances.

Designed for Flet 0.19.0 for optimal size (~11MB) and fast startup.

Features:
- Discord-like dark theme with sharp edges
- Background image support with opacity control
- Display Wrangler instances as status panels
- Start/Stop individual instances
- Master Start All / Stop All controls
- Auto-refresh status every 10 seconds
- Save/Load instance configuration
"""

import json
import logging
import os
import sys
import threading
import time
from dataclasses import dataclass, asdict
from datetime import datetime, timedelta
from pathlib import Path
from typing import Optional, List, Dict

import flet as ft

# Setup debug logging - use exe directory for compiled apps
def get_app_dir():
    """Get the application directory (works for both script and compiled exe)."""
    if getattr(sys, 'frozen', False):
        # Running as compiled exe
        return Path(sys.executable).parent
    else:
        # Running as script
        return Path(__file__).parent

APP_DIR = get_app_dir()
LOG_FILE = APP_DIR / "debug.log"

logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler(LOG_FILE, mode='w'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

try:
    import requests
except ImportError:
    print("ERROR: 'requests' library is required.")
    print("Install it with: pip install requests")
    sys.exit(1)


# =============================================================================
# Configuration
# =============================================================================

CONFIG_FILENAME = "wrangler_config.json"
SETTINGS_FILENAME = "app_settings.json"
POLL_INTERVAL_SECONDS = 10
REQUEST_TIMEOUT = 5


def get_base_path() -> Path:
    """Get the base path for resources."""
    if getattr(sys, 'frozen', False) and hasattr(sys, '_MEIPASS'):
        return Path(sys._MEIPASS)
    return Path(__file__).parent.resolve()


def get_config_dir() -> Path:
    """Get the directory for config files."""
    if getattr(sys, 'frozen', False):
        return Path(sys.executable).parent
    return Path(__file__).parent.resolve()


SCRIPT_DIR = get_base_path()
CONFIG_DIR = get_config_dir()


# =============================================================================
# Discord Color Scheme
# =============================================================================

class Colors:
    """Discord-inspired color palette."""
    # Backgrounds
    BG_DARKEST = "#202225"      # Sidebar, darkest areas
    BG_DARK = "#2f3136"         # Secondary panels
    BG_PRIMARY = "#36393f"      # Main content area
    BG_LIGHT = "#40444b"        # Input fields, hover
    BG_LIGHTER = "#4f545c"      # Secondary buttons

    # Accent colors
    BLURPLE = "#5865f2"         # Primary accent
    BLURPLE_DARK = "#4752c4"    # Hover state
    GREEN = "#57f287"           # Success/online
    YELLOW = "#fee75c"          # Warning/pending
    RED = "#ed4245"             # Danger/error

    # Text
    TEXT_PRIMARY = "#dcddde"    # Main text
    TEXT_MUTED = "#72767d"      # Secondary text
    TEXT_DARK = "#000000"       # Text on light backgrounds


# =============================================================================
# Data Classes
# =============================================================================

@dataclass
class AdvancedRunConfig:
    """Configuration for advanced run modes."""
    mode: str = "none"
    timer_hours: int = 0
    timer_minutes: int = 30
    schedule_start_hour: int = 8
    schedule_start_minute: int = 0
    schedule_end_hour: int = 22
    schedule_end_minute: int = 0
    use_resume: bool = False


@dataclass
class WranglerInstance:
    """Represents a Wrangler instance configuration."""
    name: str
    host: str
    port: int
    enabled: bool = True
    go_home_after_session: bool = False
    advanced_config: Optional[dict] = None

    @property
    def base_url(self) -> str:
        return f"http://{self.host}:{self.port}"

    def get_advanced_config(self) -> AdvancedRunConfig:
        if self.advanced_config is None:
            return AdvancedRunConfig()
        return AdvancedRunConfig(**self.advanced_config)

    def set_advanced_config(self, config: AdvancedRunConfig):
        self.advanced_config = asdict(config)


@dataclass
class InstanceStatus:
    """Current status of a Wrangler instance."""
    state: str = "unknown"
    is_executing: bool = False
    has_pending_order: bool = False
    has_incomplete_orders: bool = False
    current_file: str = "None"
    api_status: str = "Unknown"
    bot_running: bool = False
    reachable: bool = False
    error: Optional[str] = None
    character_name: str = "Unknown"
    world_name: str = "Unknown"
    runtime_seconds: int = 0


@dataclass
class AppSettings:
    """Application-wide settings."""
    background_image: str = ""
    background_opacity: float = 0.3
    font_size: int = 14


# =============================================================================
# API Client
# =============================================================================

class WranglerClient:
    """HTTP client for communicating with Wrangler instances."""

    @staticmethod
    def get_status(instance: WranglerInstance) -> InstanceStatus:
        status = InstanceStatus()
        try:
            response = requests.get(f"{instance.base_url}/status", timeout=REQUEST_TIMEOUT)
            if response.status_code == 200:
                data = response.json()
                status.state = data.get("state", "unknown")
                status.is_executing = data.get("isExecuting", False)
                status.has_pending_order = data.get("hasPendingOrder", False)
                status.has_incomplete_orders = data.get("hasIncompleteOrders", False)
                status.current_file = data.get("currentFile", "None")
                status.api_status = data.get("apiStatus", "Unknown")
                status.bot_running = data.get("botRunning", False)
                status.character_name = data.get("characterName", "Unknown")
                status.world_name = data.get("worldName", "Unknown")
                status.runtime_seconds = data.get("runtimeSeconds", 0)
                status.reachable = True
            else:
                status.error = f"HTTP {response.status_code}"
        except requests.exceptions.ConnectionError:
            status.error = "Connection refused"
        except requests.exceptions.Timeout:
            status.error = "Timeout"
        except Exception as e:
            status.error = str(e)
        return status

    @staticmethod
    def run_order(instance: WranglerInstance, json_path: str) -> tuple:
        try:
            response = requests.post(
                f"{instance.base_url}/run",
                json={"jsonPath": json_path},
                timeout=REQUEST_TIMEOUT
            )
            data = response.json()
            return data.get("success", False), data.get("message", data.get("error", "Unknown"))
        except Exception as e:
            return False, str(e)

    @staticmethod
    def stop_gently(instance: WranglerInstance) -> tuple:
        try:
            response = requests.post(f"{instance.base_url}/stop", timeout=REQUEST_TIMEOUT)
            data = response.json()
            return data.get("success", False), data.get("message", data.get("error", "Unknown"))
        except Exception as e:
            return False, str(e)

    @staticmethod
    def resume_orders(instance: WranglerInstance) -> tuple:
        try:
            response = requests.post(f"{instance.base_url}/resume", timeout=REQUEST_TIMEOUT)
            data = response.json()
            return data.get("success", False), data.get("message", data.get("error", "Unknown"))
        except Exception as e:
            return False, str(e)

    @staticmethod
    def go_home(instance: WranglerInstance) -> tuple:
        try:
            response = requests.post(f"{instance.base_url}/gohome", timeout=REQUEST_TIMEOUT)
            data = response.json()
            return data.get("success", False), data.get("message", data.get("error", "Unknown"))
        except Exception as e:
            return False, str(e)


# =============================================================================
# Instance Panel
# =============================================================================

class InstancePanel(ft.UserControl):
    """A panel displaying a single Wrangler instance."""

    STATUS_COLORS = {
        "unreachable": Colors.TEXT_MUTED,
        "stopped": Colors.TEXT_MUTED,
        "idle": Colors.BLURPLE,
        "pending": Colors.YELLOW,
        "executing": Colors.GREEN,
    }

    def __init__(
        self,
        instance: WranglerInstance,
        on_run,
        on_stop,
        on_resume,
        on_remove,
        on_settings_changed,
    ):
        super().__init__()
        self.instance = instance
        self.status = InstanceStatus()
        self._on_run = on_run
        self._on_stop = on_stop
        self._on_resume = on_resume
        self._on_remove = on_remove
        self._on_settings_changed = on_settings_changed

    def build(self):
        # Status indicator
        self.status_dot = ft.Container(
            width=12,
            height=12,
            border_radius=6,
            bgcolor=Colors.TEXT_MUTED,
        )

        self.status_text = ft.Text(
            "Unknown",
            size=13,
            color=Colors.TEXT_PRIMARY,
        )

        self.character_text = ft.Text(
            "Character: Unknown",
            size=12,
            color=Colors.TEXT_MUTED,
        )

        self.runtime_text = ft.Text(
            "",
            size=12,
            color=Colors.GREEN,
        )

        self.file_text = ft.Text(
            "File: None",
            size=11,
            color=Colors.TEXT_MUTED,
        )

        # Button style helper
        def btn_style(bg_color, text_color=Colors.TEXT_PRIMARY):
            return ft.ButtonStyle(
                shape=ft.RoundedRectangleBorder(radius=0),
                bgcolor=bg_color,
                color=text_color,
            )

        # Buttons
        self.run_btn = ft.ElevatedButton(
            "Run",
            style=btn_style(Colors.BLURPLE),
            on_click=lambda _: self._on_run(self.instance),
        )

        self.resume_btn = ft.ElevatedButton(
            "Resume",
            style=btn_style(Colors.BLURPLE),
            on_click=lambda _: self._on_resume(self.instance),
        )

        self.stop_btn = ft.ElevatedButton(
            "Stop",
            style=btn_style(Colors.BG_LIGHTER),
            on_click=lambda _: self._on_stop(self.instance),
        )

        self.remove_btn = ft.IconButton(
            icon=ft.icons.CLOSE,
            icon_color=Colors.RED,
            icon_size=18,
            on_click=lambda _: self._on_remove(self.instance),
        )

        # Go home checkbox
        self.go_home_cb = ft.Checkbox(
            label="Go Home after session",
            value=self.instance.go_home_after_session,
            on_change=self._on_go_home_change,
        )

        # Layout
        return ft.Container(
            bgcolor=Colors.BG_DARK,
            padding=15,
            border_radius=0,
            content=ft.Column(
                spacing=8,
                controls=[
                    # Header
                    ft.Row(
                        alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
                        controls=[
                            ft.Text(
                                self.instance.name,
                                size=15,
                                weight=ft.FontWeight.BOLD,
                                color=Colors.TEXT_PRIMARY,
                            ),
                            ft.Text(
                                f"{self.instance.host}:{self.instance.port}",
                                size=12,
                                color=Colors.TEXT_MUTED,
                            ),
                        ],
                    ),
                    # Status row
                    ft.Row(
                        spacing=8,
                        controls=[self.status_dot, self.status_text],
                    ),
                    self.character_text,
                    self.runtime_text,
                    self.file_text,
                    self.go_home_cb,
                    # Buttons
                    ft.Row(
                        spacing=5,
                        controls=[
                            self.run_btn,
                            self.resume_btn,
                            self.stop_btn,
                            ft.Container(expand=True),
                            self.remove_btn,
                        ],
                    ),
                ],
            ),
        )

    def _on_go_home_change(self, e):
        self.instance.go_home_after_session = e.control.value
        self._on_settings_changed()

    def update_status(self, status: InstanceStatus):
        self.status = status

        # Determine color and text
        if not status.reachable:
            color = self.STATUS_COLORS["unreachable"]
            state_text = "Unreachable"
        elif status.state == "executing":
            color = self.STATUS_COLORS["executing"]
            state_text = "Executing"
        elif status.state == "pending":
            color = self.STATUS_COLORS["pending"]
            state_text = "Pending"
        elif status.state == "idle":
            color = self.STATUS_COLORS["idle"]
            state_text = "Idle"
        elif status.state == "stopped":
            color = self.STATUS_COLORS["stopped"]
            state_text = "Bot Stopped"
        else:
            color = self.STATUS_COLORS["unreachable"]
            state_text = status.state.capitalize() if status.state else "Unknown"

        self.status_dot.bgcolor = color
        self.status_text.value = state_text

        # Character info
        if status.reachable and status.character_name != "Unknown":
            self.character_text.value = f"{status.character_name} @ {status.world_name}"
        else:
            self.character_text.value = "Character: Unknown"

        # Runtime
        if status.is_executing and status.runtime_seconds > 0:
            hours, remainder = divmod(status.runtime_seconds, 3600)
            minutes, seconds = divmod(remainder, 60)
            if hours > 0:
                self.runtime_text.value = f"Runtime: {hours}h {minutes}m {seconds}s"
            elif minutes > 0:
                self.runtime_text.value = f"Runtime: {minutes}m {seconds}s"
            else:
                self.runtime_text.value = f"Runtime: {seconds}s"
        else:
            self.runtime_text.value = ""

        # File
        file_text = status.current_file if status.current_file else "None"
        if len(file_text) > 35:
            file_text = "..." + file_text[-32:]
        self.file_text.value = f"File: {file_text}"

        # Button states
        can_run = status.reachable and status.state in ("idle", "stopped")
        can_resume = status.reachable and status.state in ("idle", "stopped") and status.has_incomplete_orders
        can_stop = status.reachable and status.is_executing

        self.run_btn.disabled = not can_run
        self.resume_btn.disabled = not can_resume
        self.stop_btn.disabled = not can_stop

        try:
            self.update()
        except Exception:
            pass


# =============================================================================
# Main Application
# =============================================================================

class WranglerMasterApp:
    """Main application class."""

    def __init__(self, page: ft.Page):
        logger.info("WranglerMasterApp initializing...")
        self.page = page
        self.instances: List[WranglerInstance] = []
        self.panels: Dict[str, InstancePanel] = {}
        self.settings = AppSettings()
        self.default_json_path = ""
        self.polling_active = True
        self.active_timers: Dict[str, dict] = {}

        # File picker (add once to page)
        self.file_picker = ft.FilePicker(on_result=self._on_file_picked)
        self.file_picker_callback = None
        page.overlay.append(self.file_picker)

        self._load_settings()
        self._load_config()
        self._setup_page()
        self._build_ui()
        self._start_polling()
        logger.info("WranglerMasterApp initialized successfully")

    def _setup_page(self):
        self.page.title = "Wrangler Master Control"
        # Flet 0.19.0 uses page.window_* properties
        self.page.window_width = 1000
        self.page.window_height = 750
        self.page.window_min_width = 800
        self.page.window_min_height = 600
        self.page.bgcolor = Colors.BG_PRIMARY
        self.page.padding = 0
        self.page.spacing = 0

    def _build_ui(self):
        # Status bar text
        self.status_text = ft.Text(
            "Ready",
            size=12,
            color=Colors.TEXT_MUTED,
        )

        self.json_path_text = ft.Text(
            "",
            size=12,
            color=Colors.TEXT_MUTED,
        )

        # Panels container
        self.panels_container = ft.Row(
            wrap=True,
            spacing=0,
            run_spacing=0,
        )

        # Build the main layout
        self._rebuild_panels()

        # Background image (if set)
        bg_controls = []
        if self.settings.background_image and os.path.exists(self.settings.background_image):
            bg_controls.append(
                ft.Container(
                    expand=True,
                    content=ft.Image(
                        src=self.settings.background_image,
                        fit=ft.ImageFit.COVER,
                        opacity=self.settings.background_opacity,
                    ),
                )
            )

        # Content layer
        content_layer = ft.Column(
            expand=True,
            spacing=0,
            controls=[
                self._build_toolbar(),
                ft.Container(
                    expand=True,
                    padding=0,
                    content=ft.Column(
                        expand=True,
                        scroll=ft.ScrollMode.AUTO,
                        spacing=0,
                        controls=[self.panels_container],
                    ),
                ),
                self._build_status_bar(),
            ],
        )

        # Use Stack for background image support
        if bg_controls:
            self.main_content = ft.Stack(
                expand=True,
                controls=[
                    bg_controls[0],
                    content_layer,
                ],
            )
        else:
            self.main_content = content_layer

        self.page.add(self.main_content)

    def _build_toolbar(self) -> ft.Container:
        """Builds the toolbar."""
        # Button style helper
        def btn_style(bg_color, text_color=Colors.TEXT_PRIMARY):
            return ft.ButtonStyle(
                shape=ft.RoundedRectangleBorder(radius=0),
                bgcolor=bg_color,
                color=text_color,
            )

        return ft.Container(
            bgcolor=Colors.BG_DARKEST,
            padding=ft.padding.symmetric(horizontal=15, vertical=10),
            content=ft.Row(
                alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
                controls=[
                    ft.Text(
                        "Wrangler Master Control",
                        size=20,
                        weight=ft.FontWeight.BOLD,
                        color=Colors.TEXT_PRIMARY,
                    ),
                    ft.Row(
                        spacing=5,
                        controls=[
                            ft.ElevatedButton(
                                "Settings",
                                style=btn_style(Colors.BG_LIGHTER),
                                on_click=self._show_settings,
                            ),
                            ft.ElevatedButton(
                                "Refresh",
                                style=btn_style(Colors.BLURPLE),
                                on_click=lambda _: self._refresh_all(),
                            ),
                            ft.ElevatedButton(
                                "+ Add",
                                style=btn_style(Colors.BLURPLE),
                                on_click=self._show_add_dialog,
                            ),
                            ft.ElevatedButton(
                                "Stop All",
                                style=btn_style(Colors.BG_LIGHTER),
                                on_click=lambda _: self._stop_all(),
                            ),
                            ft.ElevatedButton(
                                "Resume All",
                                style=btn_style(Colors.BLURPLE),
                                on_click=lambda _: self._resume_all(),
                            ),
                            ft.ElevatedButton(
                                "Start All",
                                style=btn_style(Colors.GREEN, Colors.TEXT_DARK),
                                on_click=lambda _: self._start_all(),
                            ),
                        ],
                    ),
                ],
            ),
        )

    def _build_status_bar(self) -> ft.Container:
        """Builds the status bar."""
        return ft.Container(
            bgcolor=Colors.BG_DARKEST,
            padding=ft.padding.symmetric(horizontal=15, vertical=8),
            content=ft.Row(
                alignment=ft.MainAxisAlignment.SPACE_BETWEEN,
                controls=[
                    self.status_text,
                    self.json_path_text,
                ],
            ),
        )

    def _rebuild_panels(self):
        """Rebuilds all instance panels."""
        self.panels_container.controls.clear()
        self.panels.clear()

        if not self.instances:
            self.panels_container.controls.append(
                ft.Container(
                    padding=50,
                    content=ft.Text(
                        "No instances configured.\nClick '+ Add' to add a Wrangler instance.",
                        size=14,
                        color=Colors.TEXT_MUTED,
                        text_align=ft.TextAlign.CENTER,
                    ),
                )
            )
        else:
            for instance in self.instances:
                if not instance.enabled:
                    continue

                panel = InstancePanel(
                    instance,
                    on_run=self._on_run,
                    on_stop=self._on_stop,
                    on_resume=self._on_resume,
                    on_remove=self._on_remove,
                    on_settings_changed=self._save_config,
                )

                # Wrap in container for sizing
                panel_container = ft.Container(
                    width=320,
                    margin=0,
                    padding=1,
                    content=panel,
                )

                self.panels_container.controls.append(panel_container)
                key = f"{instance.host}:{instance.port}"
                self.panels[key] = panel

    def _set_status(self, message: str):
        self.status_text.value = message
        self.page.update()

    # =========================================================================
    # Instance Actions
    # =========================================================================

    def _on_run(self, instance: WranglerInstance):
        if not self.default_json_path:
            self._pick_json_file(lambda path: self._do_run(instance, path))
        else:
            self._do_run(instance, self.default_json_path)

    def _do_run(self, instance: WranglerInstance, json_path: str):
        if not json_path:
            return
        self.default_json_path = json_path
        self._save_config()
        self._set_status(f"Starting {instance.name}...")

        def run():
            success, message = WranglerClient.run_order(instance, json_path)
            status_msg = f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            self._set_status(status_msg)
            time.sleep(1)
            self._update_instance_status(instance)

        threading.Thread(target=run, daemon=True).start()

    def _on_stop(self, instance: WranglerInstance):
        self._set_status(f"Stopping {instance.name}...")

        def stop():
            success, message = WranglerClient.stop_gently(instance)
            status_msg = f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            self._set_status(status_msg)
            time.sleep(2)
            self._update_instance_status(instance)

        threading.Thread(target=stop, daemon=True).start()

    def _on_resume(self, instance: WranglerInstance):
        self._set_status(f"Resuming {instance.name}...")

        def resume():
            success, message = WranglerClient.resume_orders(instance)
            status_msg = f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            self._set_status(status_msg)
            time.sleep(1)
            self._update_instance_status(instance)

        threading.Thread(target=resume, daemon=True).start()

    def _on_remove(self, instance: WranglerInstance):
        logger.debug(f"_on_remove called for {instance.name}")

        def do_remove(e):
            logger.debug(f"do_remove called with {e.control.text}")
            if e.control.text == "Yes":
                self.instances.remove(instance)
                self._rebuild_panels()
                self._save_config()
                self._set_status(f"Removed: {instance.name}")
            dlg.open = False
            self.page.update()

        dlg = ft.AlertDialog(
            modal=True,
            title=ft.Text("Remove Instance"),
            content=ft.Text(f"Remove '{instance.name}' from the list?"),
            actions=[
                ft.TextButton("Yes", on_click=do_remove),
                ft.TextButton("No", on_click=do_remove),
            ],
        )
        # Flet 0.19.0 style dialog
        self.page.dialog = dlg
        dlg.open = True
        self.page.update()

    # =========================================================================
    # Bulk Actions
    # =========================================================================

    def _start_all(self):
        logger.debug("_start_all called")
        if not self.default_json_path:
            self._pick_json_file(lambda path: self._do_start_all(path))
        else:
            self._do_start_all(self.default_json_path)

    def _do_start_all(self, json_path: str):
        if not json_path:
            return
        self.default_json_path = json_path
        self._save_config()
        self._set_status("Starting all instances...")

        def start():
            successes = 0
            failures = 0
            for instance in self.instances:
                if instance.enabled:
                    success, _ = WranglerClient.run_order(instance, json_path)
                    if success:
                        successes += 1
                    else:
                        failures += 1
            self._set_status(f"Started {successes} instances, {failures} failed")
            time.sleep(2)
            self._refresh_all()

        threading.Thread(target=start, daemon=True).start()

    def _stop_all(self):
        logger.debug("_stop_all called")
        self._set_status("Stopping all instances...")

        def stop():
            successes = 0
            failures = 0
            for instance in self.instances:
                if instance.enabled:
                    success, _ = WranglerClient.stop_gently(instance)
                    if success:
                        successes += 1
                    else:
                        failures += 1
            self._set_status(f"Stopped {successes} instances, {failures} failed")
            time.sleep(2)
            self._refresh_all()

        threading.Thread(target=stop, daemon=True).start()

    def _resume_all(self):
        logger.debug("_resume_all called")
        self._set_status("Resuming all instances...")

        def resume():
            successes = 0
            failures = 0
            skipped = 0
            for instance in self.instances:
                if not instance.enabled:
                    continue
                status = WranglerClient.get_status(instance)
                if not status.reachable or status.is_executing:
                    skipped += 1
                    continue
                if not status.has_incomplete_orders:
                    skipped += 1
                    continue
                success, _ = WranglerClient.resume_orders(instance)
                if success:
                    successes += 1
                else:
                    failures += 1
            self._set_status(f"Resumed {successes}, {failures} failed, {skipped} skipped")
            time.sleep(2)
            self._refresh_all()

        threading.Thread(target=resume, daemon=True).start()

    # =========================================================================
    # Polling & Status Updates
    # =========================================================================

    def _start_polling(self):
        def poll():
            while self.polling_active:
                self._refresh_all()
                time.sleep(POLL_INTERVAL_SECONDS)

        threading.Thread(target=poll, daemon=True).start()

    def _refresh_all(self):
        logger.debug("_refresh_all called")
        for instance in self.instances:
            if instance.enabled:
                self._update_instance_status(instance)

    def _update_instance_status(self, instance: WranglerInstance):
        status = WranglerClient.get_status(instance)
        key = f"{instance.host}:{instance.port}"
        if key in self.panels:
            try:
                self.panels[key].update_status(status)
            except Exception as e:
                logger.error(f"Failed to update panel: {e}")

    # =========================================================================
    # Dialogs
    # =========================================================================

    def _show_add_dialog(self, e):
        logger.debug("_show_add_dialog called!")

        name_field = ft.TextField(
            label="Name",
            value="Account 1",
            border_radius=0,
        )
        host_field = ft.TextField(
            label="Host",
            value="localhost",
            border_radius=0,
        )
        port_field = ft.TextField(
            label="Port",
            value="7800",
            border_radius=0,
        )

        def close_dlg(e):
            logger.debug("close_dlg called")
            dlg.open = False
            self.page.update()

        def add_instance(e):
            logger.debug("add_instance called")
            name = name_field.value.strip()
            host = host_field.value.strip()
            try:
                port = int(port_field.value.strip())
            except ValueError:
                logger.error("Invalid port value")
                return

            # Close dialog first
            dlg.open = False
            self.page.update()

            # Then add instance and rebuild
            instance = WranglerInstance(name=name, host=host, port=port)
            self.instances.append(instance)
            self._rebuild_panels()
            self._save_config()
            self._set_status(f"Added: {name}")
            self.page.update()
            logger.debug(f"Instance added: {name}")

        dlg = ft.AlertDialog(
            modal=True,
            title=ft.Text("Add Wrangler Instance"),
            content=ft.Column(
                tight=True,
                controls=[name_field, host_field, port_field],
            ),
            actions=[
                ft.TextButton("Cancel", on_click=close_dlg),
                ft.ElevatedButton(
                    "Add",
                    style=ft.ButtonStyle(
                        shape=ft.RoundedRectangleBorder(radius=0),
                        bgcolor=Colors.BLURPLE,
                        color=Colors.TEXT_PRIMARY,
                    ),
                    on_click=add_instance,
                ),
            ],
        )
        # Flet 0.19.0 style dialog
        self.page.dialog = dlg
        dlg.open = True
        self.page.update()
        logger.debug("Dialog should be visible now")

    def _show_settings(self, e):
        logger.debug("_show_settings called!")

        bg_field = ft.TextField(
            label="Background Image Path",
            value=self.settings.background_image,
            border_radius=0,
        )

        opacity_slider = ft.Slider(
            min=0,
            max=1,
            value=self.settings.background_opacity,
            divisions=20,
            label="{value}",
        )

        opacity_text = ft.Text(f"Opacity: {int(self.settings.background_opacity * 100)}%")

        def on_opacity_change(e):
            opacity_text.value = f"Opacity: {int(e.control.value * 100)}%"
            self.page.update()

        opacity_slider.on_change = on_opacity_change

        def browse_bg(e):
            logger.debug("browse_bg called")
            # Store the text field ref for the callback
            self._bg_field_ref = bg_field
            self.file_picker_callback = self._on_bg_file_picked
            self.file_picker.pick_files(
                allowed_extensions=["png", "jpg", "jpeg", "gif", "bmp"],
                dialog_title="Select Background Image",
            )

        def close_dlg(e):
            logger.debug("close_dlg called")
            dlg.open = False
            self.page.update()

        def save_settings(e):
            logger.debug("save_settings called")
            self.settings.background_image = bg_field.value
            self.settings.background_opacity = opacity_slider.value
            self._save_settings()
            self._rebuild_ui()
            dlg.open = False
            self.page.update()
            self._set_status("Settings saved")

        dlg = ft.AlertDialog(
            modal=True,
            title=ft.Text("Settings"),
            content=ft.Column(
                tight=True,
                width=400,
                controls=[
                    ft.Row([
                        ft.Container(expand=True, content=bg_field),
                        ft.ElevatedButton(
                            "Browse",
                            style=ft.ButtonStyle(
                                shape=ft.RoundedRectangleBorder(radius=0),
                                bgcolor=Colors.BG_LIGHTER,
                                color=Colors.TEXT_PRIMARY,
                            ),
                            on_click=browse_bg,
                        ),
                    ]),
                    ft.Container(height=10),
                    opacity_text,
                    opacity_slider,
                ],
            ),
            actions=[
                ft.TextButton("Cancel", on_click=close_dlg),
                ft.ElevatedButton(
                    "Save",
                    style=ft.ButtonStyle(
                        shape=ft.RoundedRectangleBorder(radius=0),
                        bgcolor=Colors.BLURPLE,
                        color=Colors.TEXT_PRIMARY,
                    ),
                    on_click=save_settings,
                ),
            ],
        )
        # Flet 0.19.0 style dialog
        self.page.dialog = dlg
        dlg.open = True
        self.page.update()
        logger.debug("Settings dialog should be visible now")

    def _on_bg_file_picked(self, e):
        """Handle background file pick result."""
        if e.files and hasattr(self, '_bg_field_ref'):
            self._bg_field_ref.value = e.files[0].path
            self.page.update()

    def _on_file_picked(self, e):
        """General file picker result handler."""
        if self.file_picker_callback:
            self.file_picker_callback(e)
            self.file_picker_callback = None

    def _pick_json_file(self, callback):
        def on_json_picked(e):
            if e.files:
                path = e.files[0].path
                self.json_path_text.value = f"JSON: {os.path.basename(path)}"
                self.page.update()
                callback(path)

        self.file_picker_callback = on_json_picked
        self.file_picker.pick_files(
            allowed_extensions=["json"],
            dialog_title="Select JSON File",
        )

    def _rebuild_ui(self):
        """Rebuilds the entire UI (for settings changes)."""
        logger.debug("_rebuild_ui called")
        self.page.controls.clear()
        self._build_ui()
        self.page.update()

    # =========================================================================
    # Config & Settings
    # =========================================================================

    def _save_config(self):
        config = {
            "instances": [asdict(i) for i in self.instances],
            "default_json_path": self.default_json_path,
        }
        try:
            with open(CONFIG_DIR / CONFIG_FILENAME, "w") as f:
                json.dump(config, f, indent=2)
        except Exception as e:
            print(f"Failed to save config: {e}")

    def _load_config(self):
        config_path = CONFIG_DIR / CONFIG_FILENAME
        if not config_path.exists():
            return
        try:
            with open(config_path, "r") as f:
                config = json.load(f)
            self.instances = [WranglerInstance(**d) for d in config.get("instances", [])]
            self.default_json_path = config.get("default_json_path", "")
        except Exception as e:
            print(f"Failed to load config: {e}")

    def _save_settings(self):
        settings = {
            "background_image": self.settings.background_image,
            "background_opacity": self.settings.background_opacity,
            "font_size": self.settings.font_size,
        }
        try:
            with open(CONFIG_DIR / SETTINGS_FILENAME, "w") as f:
                json.dump(settings, f, indent=2)
        except Exception as e:
            print(f"Failed to save settings: {e}")

    def _load_settings(self):
        settings_path = CONFIG_DIR / SETTINGS_FILENAME
        if not settings_path.exists():
            return
        try:
            with open(settings_path, "r") as f:
                data = json.load(f)
            self.settings.background_image = data.get("background_image", "")
            self.settings.background_opacity = data.get("background_opacity", 0.3)
            self.settings.font_size = data.get("font_size", 14)
        except Exception as e:
            print(f"Failed to load settings: {e}")


# =============================================================================
# Entry Point
# =============================================================================

def main(page: ft.Page):
    logger.info("main() called, creating WranglerMasterApp")
    app = WranglerMasterApp(page)
    logger.info("App created and running")


if __name__ == "__main__":
    logger.info("Starting Flet application...")
    ft.app(target=main)
