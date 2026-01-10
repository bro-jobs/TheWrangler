#!/usr/bin/env python3
"""
Wrangler Master Control Program
===============================

A modern GUI application to control multiple TheWrangler instances across a local network.

Features:
- Beautiful CustomTkinter UI with theme support
- Display Wrangler instances as status panels
- Start/Stop individual instances
- Master Start All / Stop All controls
- Auto-refresh status every 10 seconds
- Save/Load instance configuration
- Custom background image support
- Theme customization (blue, dark-blue, green, wrangler)

Usage:
    python wrangler_master.py

Requirements:
    - Python 3.6+
    - customtkinter (pip install customtkinter)
    - requests library (pip install requests)
    - Pillow (pip install Pillow)
"""

import json
import os
import sys
import threading
import time
from dataclasses import dataclass, field, asdict
from datetime import datetime, timedelta
from pathlib import Path
from typing import Optional, List, Dict, Callable

try:
    import requests
except ImportError:
    print("ERROR: 'requests' library is required.")
    print("Install it with: pip install requests")
    sys.exit(1)

try:
    import customtkinter as ctk
    from PIL import Image, ImageTk
except ImportError:
    print("ERROR: 'customtkinter' and 'Pillow' libraries are required.")
    print("Install them with: pip install customtkinter Pillow")
    sys.exit(1)

from tkinter import messagebox, filedialog


# =============================================================================
# Configuration
# =============================================================================

CONFIG_FILE = "wrangler_config.json"
POLL_INTERVAL_MS = 10000  # 10 seconds
REQUEST_TIMEOUT = 5  # seconds

# Get the directory where this script is located
SCRIPT_DIR = Path(__file__).parent.resolve()
THEMES_DIR = SCRIPT_DIR / "themes"
BACKGROUNDS_DIR = SCRIPT_DIR / "backgrounds"

# Available themes
AVAILABLE_THEMES = ["blue", "dark-blue", "green"]

# Check for custom wrangler theme
WRANGLER_THEME_PATH = THEMES_DIR / "wrangler_theme.json"
if WRANGLER_THEME_PATH.exists():
    AVAILABLE_THEMES.append("wrangler")


@dataclass
class AdvancedRunConfig:
    """Configuration for advanced run modes."""
    mode: str = "none"  # "none", "timer", or "schedule"
    timer_hours: int = 0
    timer_minutes: int = 30
    schedule_start_hour: int = 8
    schedule_start_minute: int = 0
    schedule_end_hour: int = 22
    schedule_end_minute: int = 0
    use_resume: bool = False  # If True, resume orders; if False, run new orders


@dataclass
class WranglerInstance:
    """Represents a Wrangler instance configuration."""
    name: str
    host: str
    port: int
    enabled: bool = True
    go_home_after_session: bool = False  # Go to Lisbeth home after timer/schedule ends
    advanced_config: Optional[dict] = None  # Persisted AdvancedRunConfig as dict

    @property
    def base_url(self) -> str:
        return f"http://{self.host}:{self.port}"

    def get_advanced_config(self) -> AdvancedRunConfig:
        """Returns the advanced config, creating default if none exists."""
        if self.advanced_config is None:
            return AdvancedRunConfig()
        return AdvancedRunConfig(**self.advanced_config)

    def set_advanced_config(self, config: AdvancedRunConfig):
        """Saves the advanced config."""
        self.advanced_config = {
            "mode": config.mode,
            "timer_hours": config.timer_hours,
            "timer_minutes": config.timer_minutes,
            "schedule_start_hour": config.schedule_start_hour,
            "schedule_start_minute": config.schedule_start_minute,
            "schedule_end_hour": config.schedule_end_hour,
            "schedule_end_minute": config.schedule_end_minute,
            "use_resume": config.use_resume
        }


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
    appearance_mode: str = "dark"  # "light", "dark", "system"
    color_theme: str = "wrangler"  # "blue", "dark-blue", "green", "wrangler"
    background_image: str = ""  # Path to custom background image


# =============================================================================
# API Client
# =============================================================================

class WranglerClient:
    """HTTP client for communicating with Wrangler instances."""

    @staticmethod
    def get_status(instance: WranglerInstance) -> InstanceStatus:
        """Fetches the current status from a Wrangler instance."""
        status = InstanceStatus()

        try:
            response = requests.get(
                f"{instance.base_url}/status",
                timeout=REQUEST_TIMEOUT
            )

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
                status.reachable = False

        except requests.exceptions.ConnectionError:
            status.error = "Connection refused"
            status.reachable = False
        except requests.exceptions.Timeout:
            status.error = "Timeout"
            status.reachable = False
        except Exception as e:
            status.error = str(e)
            status.reachable = False

        return status

    @staticmethod
    def health_check(instance: WranglerInstance) -> bool:
        """Quick health check to see if instance is reachable."""
        try:
            response = requests.get(
                f"{instance.base_url}/health",
                timeout=REQUEST_TIMEOUT
            )
            return response.status_code == 200 and response.text.strip() == "ok"
        except:
            return False

    @staticmethod
    def run_order(instance: WranglerInstance, json_path: Optional[str] = None,
                  json_content: Optional[str] = None) -> tuple[bool, str]:
        """Sends a run command to a Wrangler instance."""
        try:
            if json_path:
                payload = {"jsonPath": json_path}
            elif json_content:
                payload = {"json": json_content}
            else:
                return False, "Must provide jsonPath or json content"

            response = requests.post(
                f"{instance.base_url}/run",
                json=payload,
                timeout=REQUEST_TIMEOUT
            )

            data = response.json()
            success = data.get("success", False)
            message = data.get("message", data.get("error", "Unknown response"))
            return success, message

        except requests.exceptions.ConnectionError:
            return False, "Connection refused"
        except requests.exceptions.Timeout:
            return False, "Request timeout"
        except Exception as e:
            return False, str(e)

    @staticmethod
    def stop_gently(instance: WranglerInstance) -> tuple[bool, str]:
        """Sends a stop gently command to a Wrangler instance."""
        try:
            response = requests.post(
                f"{instance.base_url}/stop",
                timeout=REQUEST_TIMEOUT
            )

            data = response.json()
            success = data.get("success", False)
            message = data.get("message", data.get("error", "Unknown response"))
            return success, message

        except requests.exceptions.ConnectionError:
            return False, "Connection refused"
        except requests.exceptions.Timeout:
            return False, "Request timeout"
        except Exception as e:
            return False, str(e)

    @staticmethod
    def resume_orders(instance: WranglerInstance) -> tuple[bool, str]:
        """Sends a resume command to resume incomplete orders."""
        try:
            response = requests.post(
                f"{instance.base_url}/resume",
                timeout=REQUEST_TIMEOUT
            )

            data = response.json()
            success = data.get("success", False)
            message = data.get("message", data.get("error", "Unknown response"))
            return success, message

        except requests.exceptions.ConnectionError:
            return False, "Connection refused"
        except requests.exceptions.Timeout:
            return False, "Request timeout"
        except Exception as e:
            return False, str(e)

    @staticmethod
    def go_home(instance: WranglerInstance) -> tuple[bool, str]:
        """Sends a go home command to navigate to Lisbeth's configured home location."""
        try:
            response = requests.post(
                f"{instance.base_url}/gohome",
                timeout=REQUEST_TIMEOUT
            )

            data = response.json()
            success = data.get("success", False)
            message = data.get("message", data.get("error", "Unknown response"))
            return success, message

        except requests.exceptions.ConnectionError:
            return False, "Connection refused"
        except requests.exceptions.Timeout:
            return False, "Request timeout"
        except Exception as e:
            return False, str(e)


# =============================================================================
# Instance Panel Widget
# =============================================================================

class InstancePanel(ctk.CTkFrame):
    """A panel widget displaying a single Wrangler instance."""

    # Color schemes for status indicators
    COLORS = {
        "unreachable": "#666666",
        "stopped": "#95a5a6",
        "idle": "#3498db",
        "pending": "#f39c12",
        "executing": "#2ecc71",
    }

    def __init__(self, parent, instance: WranglerInstance,
                 on_run: Callable, on_stop: Callable, on_resume: Callable,
                 on_advanced_run: Callable, on_remove: Callable,
                 on_settings_changed: Callable = None):
        super().__init__(parent, corner_radius=10)

        self.instance = instance
        self.status = InstanceStatus()
        self.on_run = on_run
        self.on_stop = on_stop
        self.on_resume = on_resume
        self.on_advanced_run = on_advanced_run
        self.on_remove = on_remove
        self.on_settings_changed = on_settings_changed

        self._create_widgets()
        self._layout_widgets()

    def _create_widgets(self):
        """Creates all child widgets."""
        # Header with name and address
        self.header_frame = ctk.CTkFrame(self, fg_color="transparent")

        self.name_label = ctk.CTkLabel(
            self.header_frame,
            text=self.instance.name,
            font=ctk.CTkFont(size=14, weight="bold")
        )

        self.address_label = ctk.CTkLabel(
            self.header_frame,
            text=f"{self.instance.host}:{self.instance.port}",
            font=ctk.CTkFont(size=11),
            text_color="gray"
        )

        # Status indicator frame
        self.status_frame = ctk.CTkFrame(self, fg_color="transparent")

        self.status_indicator = ctk.CTkLabel(
            self.status_frame,
            text="\u25CF",  # Filled circle
            font=ctk.CTkFont(size=18),
            text_color=self.COLORS["unreachable"]
        )

        self.status_text = ctk.CTkLabel(
            self.status_frame,
            text="Unknown",
            font=ctk.CTkFont(size=12),
            width=100,
            anchor="w"
        )

        # Character info label
        self.character_label = ctk.CTkLabel(
            self,
            text="Character: Unknown",
            font=ctk.CTkFont(size=11),
            text_color="gray",
            anchor="w"
        )

        # Runtime label
        self.runtime_label = ctk.CTkLabel(
            self,
            text="",
            font=ctk.CTkFont(size=11),
            text_color="#2ecc71",
            anchor="w"
        )

        # Current file label
        self.file_label = ctk.CTkLabel(
            self,
            text="File: None",
            font=ctk.CTkFont(size=11),
            text_color="gray",
            anchor="w"
        )

        # Button frame
        self.button_frame = ctk.CTkFrame(self, fg_color="transparent")

        self.run_btn = ctk.CTkButton(
            self.button_frame,
            text="Run",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#2ecc71",
            hover_color="#27ae60",
            width=70,
            height=28,
            command=self._on_run_click
        )

        self.resume_btn = ctk.CTkButton(
            self.button_frame,
            text="Resume",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#3498db",
            hover_color="#2980b9",
            width=70,
            height=28,
            command=self._on_resume_click
        )

        self.stop_btn = ctk.CTkButton(
            self.button_frame,
            text="Stop",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#e67e22",
            hover_color="#d35400",
            width=70,
            height=28,
            command=self._on_stop_click
        )

        self.advanced_btn = ctk.CTkButton(
            self.button_frame,
            text="Advanced",
            font=ctk.CTkFont(size=11),
            fg_color="#9b59b6",
            hover_color="#8e44ad",
            width=70,
            height=28,
            command=self._on_advanced_click
        )

        self.remove_btn = ctk.CTkButton(
            self.button_frame,
            text="X",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#e74c3c",
            hover_color="#c0392b",
            width=28,
            height=28,
            command=self._on_remove_click
        )

        # Menu button (three dots)
        self.menu_btn = ctk.CTkButton(
            self.button_frame,
            text="\u22EE",  # Vertical ellipsis
            font=ctk.CTkFont(size=14),
            fg_color="#555555",
            hover_color="#666666",
            width=28,
            height=28,
            command=self._show_menu
        )

        # Go home checkbox
        self.go_home_var = ctk.BooleanVar(value=self.instance.go_home_after_session)
        self.go_home_check = ctk.CTkCheckBox(
            self,
            text="Go Home after session",
            font=ctk.CTkFont(size=11),
            variable=self.go_home_var,
            command=self._on_go_home_toggle,
            height=20,
            checkbox_width=18,
            checkbox_height=18
        )

    def _layout_widgets(self):
        """Arranges widgets in the panel."""
        # Header
        self.header_frame.pack(fill="x", padx=12, pady=(12, 6))
        self.name_label.pack(side="left")
        self.address_label.pack(side="right")

        # Status
        self.status_frame.pack(fill="x", padx=12, pady=4)
        self.status_indicator.pack(side="left")
        self.status_text.pack(side="left", padx=6)

        # Character info
        self.character_label.pack(fill="x", padx=12, pady=1)

        # Runtime
        self.runtime_label.pack(fill="x", padx=12, pady=1)

        # File
        self.file_label.pack(fill="x", padx=12, pady=2)

        # Go home checkbox
        self.go_home_check.pack(fill="x", padx=12, pady=4)

        # Buttons
        self.button_frame.pack(fill="x", padx=12, pady=(6, 12))
        self.run_btn.pack(side="left", padx=2)
        self.resume_btn.pack(side="left", padx=2)
        self.stop_btn.pack(side="left", padx=2)
        self.advanced_btn.pack(side="left", padx=2)
        self.menu_btn.pack(side="right", padx=2)
        self.remove_btn.pack(side="right", padx=2)

    def update_status(self, status: InstanceStatus):
        """Updates the panel display with new status."""
        self.status = status

        # Determine color based on state
        if not status.reachable:
            color = self.COLORS["unreachable"]
            state_text = "Unreachable"
        elif status.state == "executing":
            color = self.COLORS["executing"]
            state_text = "Executing"
        elif status.state == "pending":
            color = self.COLORS["pending"]
            state_text = "Pending"
        elif status.state == "idle":
            color = self.COLORS["idle"]
            state_text = "Idle"
        elif status.state == "stopped":
            color = self.COLORS["stopped"]
            state_text = "Bot Stopped"
        else:
            color = self.COLORS["unreachable"]
            state_text = status.state.capitalize() if status.state else "Unknown"

        # Update display
        self.status_indicator.configure(text_color=color)
        self.status_text.configure(text=state_text)

        # Update character label
        if status.reachable and status.character_name != "Unknown":
            char_text = f"{status.character_name} @ {status.world_name}"
            self.character_label.configure(text=char_text, text_color=("gray40", "gray60"))
        else:
            self.character_label.configure(text="Character: Unknown", text_color="gray")

        # Update runtime label
        if status.is_executing and status.runtime_seconds > 0:
            hours, remainder = divmod(status.runtime_seconds, 3600)
            minutes, seconds = divmod(remainder, 60)
            if hours > 0:
                runtime_text = f"Runtime: {hours}h {minutes}m {seconds}s"
            elif minutes > 0:
                runtime_text = f"Runtime: {minutes}m {seconds}s"
            else:
                runtime_text = f"Runtime: {seconds}s"
            self.runtime_label.configure(text=runtime_text)
        else:
            self.runtime_label.configure(text="")

        # Update file label
        file_text = status.current_file if status.current_file else "None"
        if len(file_text) > 30:
            file_text = "..." + file_text[-27:]
        self.file_label.configure(text=f"File: {file_text}")

        # Update button states
        can_run = status.reachable and status.state in ("idle", "stopped")
        can_resume = status.reachable and status.state in ("idle", "stopped") and status.has_incomplete_orders
        can_stop = status.reachable and status.is_executing
        can_advanced = status.reachable and status.state in ("idle", "stopped")

        self.run_btn.configure(state="normal" if can_run else "disabled")
        self.resume_btn.configure(state="normal" if can_resume else "disabled")
        self.stop_btn.configure(state="normal" if can_stop else "disabled")
        self.advanced_btn.configure(state="normal" if can_advanced else "disabled")

    def _on_run_click(self):
        """Handles Run button click."""
        self.on_run(self.instance)

    def _on_resume_click(self):
        """Handles Resume button click."""
        self.on_resume(self.instance)

    def _on_stop_click(self):
        """Handles Stop button click."""
        self.on_stop(self.instance)

    def _on_advanced_click(self):
        """Handles Advanced button click."""
        self.on_advanced_run(self.instance)

    def _on_remove_click(self):
        """Handles Remove button click."""
        self.on_remove(self.instance)

    def _show_menu(self):
        """Shows a simple info dialog for now."""
        messagebox.showinfo(
            "Instance Info",
            f"Name: {self.instance.name}\n"
            f"Host: {self.instance.host}\n"
            f"Port: {self.instance.port}\n"
            f"Go Home: {'Yes' if self.instance.go_home_after_session else 'No'}"
        )

    def _on_go_home_toggle(self):
        """Handles Go Home checkbox toggle."""
        self.instance.go_home_after_session = self.go_home_var.get()
        if self.on_settings_changed:
            self.on_settings_changed()


# =============================================================================
# Advanced Run Dialog
# =============================================================================

class AdvancedRunDialog(ctk.CTkToplevel):
    """Dialog for configuring advanced run options (none, timer, or schedule mode)."""

    def __init__(self, parent, instance_name: str, has_incomplete_orders: bool = False,
                 existing_config: Optional[AdvancedRunConfig] = None):
        super().__init__(parent)
        self.title(f"Advanced Run - {instance_name}")
        self.geometry("480x520")
        self.minsize(480, 520)
        self.resizable(True, True)

        self.result: Optional[AdvancedRunConfig] = None
        self.save_only: bool = False
        self.has_incomplete_orders = has_incomplete_orders

        # Load existing config or use defaults
        self.config = existing_config or AdvancedRunConfig()

        # Mode variable
        self.mode_var = ctk.StringVar(value=self.config.mode)

        self._create_widgets()

        # Make dialog modal
        self.transient(parent)
        self.grab_set()

        # Center on parent
        self.update_idletasks()
        x = parent.winfo_x() + (parent.winfo_width() - self.winfo_width()) // 2
        y = parent.winfo_y() + (parent.winfo_height() - self.winfo_height()) // 2
        self.geometry(f"+{x}+{y}")

    def _create_widgets(self):
        """Creates dialog widgets."""
        # Main scrollable frame
        main_frame = ctk.CTkScrollableFrame(self)
        main_frame.pack(fill="both", expand=True, padx=20, pady=20)

        # Mode selection
        mode_label = ctk.CTkLabel(
            main_frame,
            text="Run Mode:",
            font=ctk.CTkFont(size=13, weight="bold")
        )
        mode_label.pack(anchor="w", pady=(0, 10))

        mode_frame = ctk.CTkFrame(main_frame, fg_color="transparent")
        mode_frame.pack(fill="x", pady=(0, 15))

        self.none_radio = ctk.CTkRadioButton(
            mode_frame,
            text="None",
            variable=self.mode_var,
            value="none",
            command=self._on_mode_change
        )
        self.none_radio.pack(side="left", padx=10)

        self.timer_radio = ctk.CTkRadioButton(
            mode_frame,
            text="Timer",
            variable=self.mode_var,
            value="timer",
            command=self._on_mode_change
        )
        self.timer_radio.pack(side="left", padx=10)

        self.schedule_radio = ctk.CTkRadioButton(
            mode_frame,
            text="Schedule",
            variable=self.mode_var,
            value="schedule",
            command=self._on_mode_change
        )
        self.schedule_radio.pack(side="left", padx=10)

        # Resume option
        self.resume_var = ctk.BooleanVar(value=self.config.use_resume)
        self.resume_check = ctk.CTkCheckBox(
            main_frame,
            text="Resume incomplete orders (instead of starting new)",
            variable=self.resume_var
        )
        self.resume_check.pack(anchor="w", pady=(0, 20))

        # None mode frame
        self.none_frame = ctk.CTkFrame(main_frame)
        self.none_frame.pack(fill="x", pady=10)

        ctk.CTkLabel(
            self.none_frame,
            text="None Mode (Normal Run)",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(anchor="w", padx=15, pady=(15, 5))

        ctk.CTkLabel(
            self.none_frame,
            text="Runs or resumes orders immediately without any timer\nor schedule. The bot will run until orders complete\nor you stop it manually.",
            font=ctk.CTkFont(size=11),
            text_color="gray",
            justify="left"
        ).pack(anchor="w", padx=15, pady=(0, 15))

        # Timer mode frame
        self.timer_frame = ctk.CTkFrame(main_frame)
        self.timer_frame.pack(fill="x", pady=10)

        ctk.CTkLabel(
            self.timer_frame,
            text="Timer Mode",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(anchor="w", padx=15, pady=(15, 10))

        timer_input_frame = ctk.CTkFrame(self.timer_frame, fg_color="transparent")
        timer_input_frame.pack(fill="x", padx=15, pady=5)

        ctk.CTkLabel(timer_input_frame, text="Run for:").pack(side="left")

        self.timer_hours_var = ctk.StringVar(value=str(self.config.timer_hours))
        self.timer_hours_entry = ctk.CTkEntry(
            timer_input_frame,
            textvariable=self.timer_hours_var,
            width=50
        )
        self.timer_hours_entry.pack(side="left", padx=5)
        ctk.CTkLabel(timer_input_frame, text="hours").pack(side="left")

        self.timer_minutes_var = ctk.StringVar(value=str(self.config.timer_minutes))
        self.timer_minutes_entry = ctk.CTkEntry(
            timer_input_frame,
            textvariable=self.timer_minutes_var,
            width=50
        )
        self.timer_minutes_entry.pack(side="left", padx=5)
        ctk.CTkLabel(timer_input_frame, text="minutes").pack(side="left")

        ctk.CTkLabel(
            self.timer_frame,
            text="After the timer expires, StopGently will be called.",
            font=ctk.CTkFont(size=11),
            text_color="gray"
        ).pack(anchor="w", padx=15, pady=(5, 15))

        # Schedule mode frame
        self.schedule_frame = ctk.CTkFrame(main_frame)
        self.schedule_frame.pack(fill="x", pady=10)

        ctk.CTkLabel(
            self.schedule_frame,
            text="Schedule Mode",
            font=ctk.CTkFont(size=12, weight="bold")
        ).pack(anchor="w", padx=15, pady=(15, 10))

        # Start time
        start_frame = ctk.CTkFrame(self.schedule_frame, fg_color="transparent")
        start_frame.pack(fill="x", padx=15, pady=5)

        ctk.CTkLabel(start_frame, text="Start at:", width=60).pack(side="left")

        self.start_hour_var = ctk.StringVar(value=f"{self.config.schedule_start_hour:02d}")
        self.start_hour_entry = ctk.CTkEntry(
            start_frame,
            textvariable=self.start_hour_var,
            width=40
        )
        self.start_hour_entry.pack(side="left", padx=2)
        ctk.CTkLabel(start_frame, text=":").pack(side="left")

        self.start_minute_var = ctk.StringVar(value=f"{self.config.schedule_start_minute:02d}")
        self.start_minute_entry = ctk.CTkEntry(
            start_frame,
            textvariable=self.start_minute_var,
            width=40
        )
        self.start_minute_entry.pack(side="left", padx=2)
        ctk.CTkLabel(start_frame, text="(local time)", text_color="gray").pack(side="left", padx=10)

        # End time
        end_frame = ctk.CTkFrame(self.schedule_frame, fg_color="transparent")
        end_frame.pack(fill="x", padx=15, pady=5)

        ctk.CTkLabel(end_frame, text="Stop at:", width=60).pack(side="left")

        self.end_hour_var = ctk.StringVar(value=f"{self.config.schedule_end_hour:02d}")
        self.end_hour_entry = ctk.CTkEntry(
            end_frame,
            textvariable=self.end_hour_var,
            width=40
        )
        self.end_hour_entry.pack(side="left", padx=2)
        ctk.CTkLabel(end_frame, text=":").pack(side="left")

        self.end_minute_var = ctk.StringVar(value=f"{self.config.schedule_end_minute:02d}")
        self.end_minute_entry = ctk.CTkEntry(
            end_frame,
            textvariable=self.end_minute_var,
            width=40
        )
        self.end_minute_entry.pack(side="left", padx=2)
        ctk.CTkLabel(end_frame, text="(local time)", text_color="gray").pack(side="left", padx=10)

        ctk.CTkLabel(
            self.schedule_frame,
            text="Runs daily: starts at start time, stops at end time.",
            font=ctk.CTkFont(size=11),
            text_color="gray"
        ).pack(anchor="w", padx=15, pady=(5, 15))

        # Button frame
        btn_frame = ctk.CTkFrame(self, fg_color="transparent")
        btn_frame.pack(fill="x", padx=20, pady=20)

        ctk.CTkButton(
            btn_frame,
            text="Cancel",
            fg_color="gray",
            hover_color="gray30",
            width=100,
            command=self.destroy
        ).pack(side="right", padx=5)

        ctk.CTkButton(
            btn_frame,
            text="Start",
            fg_color="#2ecc71",
            hover_color="#27ae60",
            width=100,
            command=self._on_start
        ).pack(side="right", padx=5)

        ctk.CTkButton(
            btn_frame,
            text="Save",
            fg_color="#3498db",
            hover_color="#2980b9",
            width=100,
            command=self._on_save
        ).pack(side="right", padx=5)

        # Initialize visibility
        self._on_mode_change()

    def _on_mode_change(self):
        """Updates frame opacity based on selected mode."""
        mode = self.mode_var.get()

        # Visual feedback for active mode (using border color)
        for frame, frame_mode in [(self.none_frame, "none"),
                                   (self.timer_frame, "timer"),
                                   (self.schedule_frame, "schedule")]:
            if mode == frame_mode:
                frame.configure(border_width=2, border_color="#9b59b6")
            else:
                frame.configure(border_width=0)

    def _on_start(self):
        """Validates and returns result."""
        config = self._build_and_validate_config()
        if config is None:
            return

        self.result = config
        self.destroy()

    def _on_save(self):
        """Validates and saves config without starting a run."""
        config = self._build_and_validate_config()
        if config is None:
            return

        self.save_only = True
        self.result = config
        self.destroy()

    def _build_and_validate_config(self) -> Optional[AdvancedRunConfig]:
        """Builds and validates configuration from form values."""
        config = AdvancedRunConfig()
        config.mode = self.mode_var.get()
        config.use_resume = self.resume_var.get()

        try:
            config.timer_hours = int(self.timer_hours_var.get())
            config.timer_minutes = int(self.timer_minutes_var.get())
            config.schedule_start_hour = int(self.start_hour_var.get())
            config.schedule_start_minute = int(self.start_minute_var.get())
            config.schedule_end_hour = int(self.end_hour_var.get())
            config.schedule_end_minute = int(self.end_minute_var.get())

            if config.mode == "timer":
                if config.timer_hours < 0 or config.timer_minutes < 0:
                    raise ValueError("Time cannot be negative")
                if config.timer_hours == 0 and config.timer_minutes == 0:
                    raise ValueError("Timer must be at least 1 minute")
            elif config.mode == "schedule":
                if not (0 <= config.schedule_start_hour <= 23):
                    raise ValueError("Start hour must be 0-23")
                if not (0 <= config.schedule_start_minute <= 59):
                    raise ValueError("Start minute must be 0-59")
                if not (0 <= config.schedule_end_hour <= 23):
                    raise ValueError("End hour must be 0-23")
                if not (0 <= config.schedule_end_minute <= 59):
                    raise ValueError("End minute must be 0-59")

        except ValueError as e:
            messagebox.showerror("Invalid Input", str(e))
            return None

        return config


# =============================================================================
# Add Instance Dialog
# =============================================================================

class AddInstanceDialog(ctk.CTkToplevel):
    """Dialog for adding a new Wrangler instance."""

    def __init__(self, parent):
        super().__init__(parent)
        self.title("Add Wrangler Instance")
        self.geometry("420x280")
        self.minsize(420, 280)
        self.resizable(True, True)

        self.result = None

        self._create_widgets()

        # Make dialog modal
        self.transient(parent)
        self.grab_set()

        # Center on parent
        self.update_idletasks()
        x = parent.winfo_x() + (parent.winfo_width() - self.winfo_width()) // 2
        y = parent.winfo_y() + (parent.winfo_height() - self.winfo_height()) // 2
        self.geometry(f"+{x}+{y}")

    def _create_widgets(self):
        """Creates dialog widgets."""
        # Main frame
        main_frame = ctk.CTkFrame(self, fg_color="transparent")
        main_frame.pack(fill="both", expand=True, padx=30, pady=30)

        # Name field
        ctk.CTkLabel(main_frame, text="Name:", font=ctk.CTkFont(size=12)).pack(anchor="w", pady=(0, 5))
        self.name_entry = ctk.CTkEntry(main_frame, width=360, placeholder_text="Account 1")
        self.name_entry.pack(fill="x", pady=(0, 15))
        self.name_entry.insert(0, "Account 1")

        # Host field
        ctk.CTkLabel(main_frame, text="Host:", font=ctk.CTkFont(size=12)).pack(anchor="w", pady=(0, 5))
        self.host_entry = ctk.CTkEntry(main_frame, width=360, placeholder_text="localhost")
        self.host_entry.pack(fill="x", pady=(0, 15))
        self.host_entry.insert(0, "localhost")

        # Port field
        ctk.CTkLabel(main_frame, text="Port:", font=ctk.CTkFont(size=12)).pack(anchor="w", pady=(0, 5))
        self.port_entry = ctk.CTkEntry(main_frame, width=360, placeholder_text="7800")
        self.port_entry.pack(fill="x", pady=(0, 20))
        self.port_entry.insert(0, "7800")

        # Button frame
        btn_frame = ctk.CTkFrame(main_frame, fg_color="transparent")
        btn_frame.pack(fill="x")

        ctk.CTkButton(
            btn_frame,
            text="Cancel",
            fg_color="gray",
            hover_color="gray30",
            width=100,
            command=self.destroy
        ).pack(side="right", padx=5)

        ctk.CTkButton(
            btn_frame,
            text="Add",
            fg_color="#2ecc71",
            hover_color="#27ae60",
            width=100,
            command=self._on_add
        ).pack(side="right", padx=5)

    def _on_add(self):
        """Validates and returns result."""
        name = self.name_entry.get().strip()
        host = self.host_entry.get().strip()
        port_str = self.port_entry.get().strip()

        if not name:
            messagebox.showerror("Error", "Name is required")
            return

        if not host:
            messagebox.showerror("Error", "Host is required")
            return

        try:
            port = int(port_str)
            if port < 1 or port > 65535:
                raise ValueError()
        except ValueError:
            messagebox.showerror("Error", "Port must be a number between 1 and 65535")
            return

        self.result = (name, host, port)
        self.destroy()


# =============================================================================
# Settings Dialog
# =============================================================================

class SettingsDialog(ctk.CTkToplevel):
    """Dialog for application settings."""

    def __init__(self, parent, settings: AppSettings, on_apply: Callable):
        super().__init__(parent)
        self.title("Settings")
        self.geometry("450x400")
        self.minsize(450, 400)
        self.resizable(True, True)

        self.settings = settings
        self.on_apply = on_apply

        self._create_widgets()

        # Make dialog modal
        self.transient(parent)
        self.grab_set()

        # Center on parent
        self.update_idletasks()
        x = parent.winfo_x() + (parent.winfo_width() - self.winfo_width()) // 2
        y = parent.winfo_y() + (parent.winfo_height() - self.winfo_height()) // 2
        self.geometry(f"+{x}+{y}")

    def _create_widgets(self):
        """Creates dialog widgets."""
        # Main frame
        main_frame = ctk.CTkFrame(self, fg_color="transparent")
        main_frame.pack(fill="both", expand=True, padx=30, pady=30)

        # Appearance section
        ctk.CTkLabel(
            main_frame,
            text="Appearance",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(anchor="w", pady=(0, 15))

        # Appearance mode
        mode_frame = ctk.CTkFrame(main_frame, fg_color="transparent")
        mode_frame.pack(fill="x", pady=10)

        ctk.CTkLabel(mode_frame, text="Mode:", width=120).pack(side="left")
        self.appearance_var = ctk.StringVar(value=self.settings.appearance_mode)
        self.appearance_menu = ctk.CTkOptionMenu(
            mode_frame,
            values=["light", "dark", "system"],
            variable=self.appearance_var,
            width=200
        )
        self.appearance_menu.pack(side="left")

        # Color theme
        theme_frame = ctk.CTkFrame(main_frame, fg_color="transparent")
        theme_frame.pack(fill="x", pady=10)

        ctk.CTkLabel(theme_frame, text="Color Theme:", width=120).pack(side="left")
        self.theme_var = ctk.StringVar(value=self.settings.color_theme)
        self.theme_menu = ctk.CTkOptionMenu(
            theme_frame,
            values=AVAILABLE_THEMES,
            variable=self.theme_var,
            width=200
        )
        self.theme_menu.pack(side="left")

        # Background section
        ctk.CTkLabel(
            main_frame,
            text="Background",
            font=ctk.CTkFont(size=16, weight="bold")
        ).pack(anchor="w", pady=(30, 15))

        # Background image
        bg_frame = ctk.CTkFrame(main_frame, fg_color="transparent")
        bg_frame.pack(fill="x", pady=10)

        ctk.CTkLabel(bg_frame, text="Image:", width=120).pack(side="left")

        self.bg_path_var = ctk.StringVar(value=self.settings.background_image)
        self.bg_entry = ctk.CTkEntry(
            bg_frame,
            textvariable=self.bg_path_var,
            width=200,
            placeholder_text="No image selected"
        )
        self.bg_entry.pack(side="left", padx=(0, 10))

        ctk.CTkButton(
            bg_frame,
            text="Browse",
            width=70,
            command=self._browse_background
        ).pack(side="left")

        # Clear background button
        clear_frame = ctk.CTkFrame(main_frame, fg_color="transparent")
        clear_frame.pack(fill="x", pady=5)

        ctk.CTkButton(
            clear_frame,
            text="Clear Background",
            fg_color="gray",
            hover_color="gray30",
            width=150,
            command=self._clear_background
        ).pack(side="left", padx=(120, 0))

        # Note about theme changes
        note_label = ctk.CTkLabel(
            main_frame,
            text="Note: Theme changes require restart to take full effect.",
            font=ctk.CTkFont(size=11),
            text_color="gray"
        )
        note_label.pack(anchor="w", pady=(30, 0))

        # Button frame
        btn_frame = ctk.CTkFrame(self, fg_color="transparent")
        btn_frame.pack(fill="x", padx=30, pady=20)

        ctk.CTkButton(
            btn_frame,
            text="Cancel",
            fg_color="gray",
            hover_color="gray30",
            width=100,
            command=self.destroy
        ).pack(side="right", padx=5)

        ctk.CTkButton(
            btn_frame,
            text="Apply",
            fg_color="#2ecc71",
            hover_color="#27ae60",
            width=100,
            command=self._on_apply
        ).pack(side="right", padx=5)

    def _browse_background(self):
        """Opens file dialog to select background image."""
        path = filedialog.askopenfilename(
            title="Select Background Image",
            filetypes=[
                ("Image Files", "*.png *.jpg *.jpeg *.gif *.bmp"),
                ("All Files", "*.*")
            ]
        )
        if path:
            self.bg_path_var.set(path)

    def _clear_background(self):
        """Clears the background image selection."""
        self.bg_path_var.set("")

    def _on_apply(self):
        """Applies settings and closes dialog."""
        self.settings.appearance_mode = self.appearance_var.get()
        self.settings.color_theme = self.theme_var.get()
        self.settings.background_image = self.bg_path_var.get()
        self.on_apply(self.settings)
        self.destroy()


# =============================================================================
# Main Application
# =============================================================================

class WranglerMasterApp(ctk.CTk):
    """Main application class."""

    def __init__(self):
        super().__init__()

        # Load settings first
        self.app_settings = AppSettings()
        self._load_app_settings()

        # Apply theme
        self._apply_theme()

        self.title("Wrangler Master Control")
        self.geometry("950x750")
        self.minsize(700, 500)

        # Data
        self.instances: List[WranglerInstance] = []
        self.panels: Dict[str, InstancePanel] = {}
        self.polling_active = True

        # Default JSON path for run commands
        self.default_json_path = ""

        # Advanced run tracking
        self.active_timers: Dict[str, dict] = {}
        self.active_schedules: Dict[str, dict] = {}

        # Background image
        self.bg_image = None
        self.bg_label = None

        # Create UI
        self._create_ui()

        # Load config and start polling
        self._load_config()
        self._start_polling()

        # Handle window close
        self.protocol("WM_DELETE_WINDOW", self._on_close)

    def _apply_theme(self):
        """Applies the current theme settings."""
        ctk.set_appearance_mode(self.app_settings.appearance_mode)

        if self.app_settings.color_theme == "wrangler" and WRANGLER_THEME_PATH.exists():
            ctk.set_default_color_theme(str(WRANGLER_THEME_PATH))
        elif self.app_settings.color_theme in ["blue", "dark-blue", "green"]:
            ctk.set_default_color_theme(self.app_settings.color_theme)

    def _create_ui(self):
        """Creates the main UI."""
        # Background image (if set)
        self._setup_background()

        # Main container
        self.main_container = ctk.CTkFrame(self, fg_color="transparent")
        self.main_container.pack(fill="both", expand=True)

        self._create_toolbar()
        self._create_main_area()
        self._create_status_bar()

    def _setup_background(self):
        """Sets up background image if configured."""
        if self.app_settings.background_image and os.path.exists(self.app_settings.background_image):
            try:
                img = Image.open(self.app_settings.background_image)
                self.bg_image = ctk.CTkImage(
                    light_image=img,
                    dark_image=img,
                    size=(self.winfo_screenwidth(), self.winfo_screenheight())
                )
                self.bg_label = ctk.CTkLabel(self, image=self.bg_image, text="")
                self.bg_label.place(x=0, y=0, relwidth=1, relheight=1)

                # Bind resize event to update background
                self.bind("<Configure>", self._on_resize)
            except Exception as e:
                print(f"Failed to load background image: {e}")

    def _on_resize(self, event=None):
        """Updates background image size on window resize."""
        if self.bg_image and self.app_settings.background_image:
            try:
                img = Image.open(self.app_settings.background_image)
                self.bg_image = ctk.CTkImage(
                    light_image=img,
                    dark_image=img,
                    size=(self.winfo_width(), self.winfo_height())
                )
                if self.bg_label:
                    self.bg_label.configure(image=self.bg_image)
            except:
                pass

    def _create_toolbar(self):
        """Creates the toolbar with master controls."""
        toolbar = ctk.CTkFrame(self.main_container, corner_radius=0)
        toolbar.pack(fill="x", padx=15, pady=(15, 0))

        # Left side - Title
        title_frame = ctk.CTkFrame(toolbar, fg_color="transparent")
        title_frame.pack(side="left", padx=15, pady=15)

        title = ctk.CTkLabel(
            title_frame,
            text="Wrangler Master Control",
            font=ctk.CTkFont(size=20, weight="bold")
        )
        title.pack(side="left")

        # Right side - Buttons
        btn_frame = ctk.CTkFrame(toolbar, fg_color="transparent")
        btn_frame.pack(side="right", padx=15, pady=10)

        self.settings_btn = ctk.CTkButton(
            btn_frame,
            text="Settings",
            font=ctk.CTkFont(size=12),
            fg_color="gray",
            hover_color="gray30",
            width=90,
            height=32,
            command=self._show_settings
        )
        self.settings_btn.pack(side="left", padx=5)

        self.refresh_btn = ctk.CTkButton(
            btn_frame,
            text="Refresh",
            font=ctk.CTkFont(size=12),
            fg_color="#9b59b6",
            hover_color="#8e44ad",
            width=90,
            height=32,
            command=self._refresh_all
        )
        self.refresh_btn.pack(side="left", padx=5)

        self.add_btn = ctk.CTkButton(
            btn_frame,
            text="+ Add",
            font=ctk.CTkFont(size=12),
            fg_color="#3498db",
            hover_color="#2980b9",
            width=90,
            height=32,
            command=self._add_instance_dialog
        )
        self.add_btn.pack(side="left", padx=5)

        self.stop_all_btn = ctk.CTkButton(
            btn_frame,
            text="Stop All",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#e67e22",
            hover_color="#d35400",
            width=90,
            height=32,
            command=self._stop_all
        )
        self.stop_all_btn.pack(side="left", padx=5)

        self.resume_all_btn = ctk.CTkButton(
            btn_frame,
            text="Resume All",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#3498db",
            hover_color="#2980b9",
            width=100,
            height=32,
            command=self._resume_all
        )
        self.resume_all_btn.pack(side="left", padx=5)

        self.start_all_btn = ctk.CTkButton(
            btn_frame,
            text="Start All",
            font=ctk.CTkFont(size=12, weight="bold"),
            fg_color="#2ecc71",
            hover_color="#27ae60",
            width=90,
            height=32,
            command=self._start_all
        )
        self.start_all_btn.pack(side="left", padx=5)

    def _create_main_area(self):
        """Creates the main scrollable area for instance panels."""
        self.panels_scroll = ctk.CTkScrollableFrame(
            self.main_container,
            corner_radius=10
        )
        self.panels_scroll.pack(fill="both", expand=True, padx=15, pady=15)

    def _create_status_bar(self):
        """Creates the status bar at the bottom."""
        status_frame = ctk.CTkFrame(self.main_container, corner_radius=0, height=35)
        status_frame.pack(fill="x", side="bottom")
        status_frame.pack_propagate(False)

        self.status_label = ctk.CTkLabel(
            status_frame,
            text="Ready",
            font=ctk.CTkFont(size=11),
            text_color="gray"
        )
        self.status_label.pack(side="left", padx=15, pady=8)

        # JSON path indicator
        self.json_label = ctk.CTkLabel(
            status_frame,
            text="",
            font=ctk.CTkFont(size=11),
            text_color="gray"
        )
        self.json_label.pack(side="right", padx=15, pady=8)

    def _refresh_panels(self):
        """Recreates all instance panels."""
        # Clear existing panels
        for widget in self.panels_scroll.winfo_children():
            widget.destroy()
        self.panels.clear()

        if not self.instances:
            placeholder = ctk.CTkLabel(
                self.panels_scroll,
                text="No instances configured.\nClick '+ Add' to add a Wrangler instance.",
                font=ctk.CTkFont(size=14),
                text_color="gray"
            )
            placeholder.pack(pady=50)
            return

        # Create panels in a grid layout
        cols = 3
        row_frame = None

        for i, instance in enumerate(self.instances):
            if not instance.enabled:
                continue

            if i % cols == 0:
                row_frame = ctk.CTkFrame(self.panels_scroll, fg_color="transparent")
                row_frame.pack(fill="x", pady=5)

            panel = InstancePanel(
                row_frame,
                instance,
                on_run=self._on_panel_run,
                on_stop=self._on_panel_stop,
                on_resume=self._on_panel_resume,
                on_advanced_run=self._on_panel_advanced_run,
                on_remove=self._on_panel_remove,
                on_settings_changed=self._save_config
            )
            panel.pack(side="left", padx=5, pady=5, fill="both", expand=True)

            key = f"{instance.host}:{instance.port}"
            self.panels[key] = panel

    def _update_panel(self, instance: WranglerInstance, status: InstanceStatus):
        """Updates a single panel with new status."""
        key = f"{instance.host}:{instance.port}"
        if key in self.panels:
            self.panels[key].update_status(status)

    def _start_polling(self):
        """Starts the background polling loop."""
        def poll():
            while self.polling_active:
                self._refresh_all_async()
                self._check_timers()
                self._check_schedules()
                time.sleep(POLL_INTERVAL_MS / 1000)

        thread = threading.Thread(target=poll, daemon=True)
        thread.start()

    def _refresh_all_async(self):
        """Fetches status from all instances."""
        for instance in self.instances:
            if not instance.enabled:
                continue

            status = WranglerClient.get_status(instance)
            self.after(0, lambda i=instance, s=status: self._update_panel(i, s))

    def _refresh_all(self):
        """Manual refresh triggered by button."""
        self._set_status("Refreshing...")

        def do_refresh():
            self._refresh_all_async()
            self.after(0, lambda: self._set_status("Refresh complete"))

        thread = threading.Thread(target=do_refresh, daemon=True)
        thread.start()

    def _show_settings(self):
        """Shows the settings dialog."""
        dialog = SettingsDialog(self, self.app_settings, self._on_settings_apply)
        self.wait_window(dialog)

    def _on_settings_apply(self, settings: AppSettings):
        """Handles settings apply."""
        self.app_settings = settings
        self._save_app_settings()

        # Apply appearance mode immediately
        ctk.set_appearance_mode(settings.appearance_mode)

        # Update background
        if self.bg_label:
            self.bg_label.destroy()
            self.bg_label = None
        self._setup_background()

        self._set_status("Settings applied. Restart for full theme changes.")

    def _add_instance_dialog(self):
        """Shows dialog to add a new instance."""
        dialog = AddInstanceDialog(self)
        self.wait_window(dialog)

        if dialog.result:
            name, host, port = dialog.result
            instance = WranglerInstance(name=name, host=host, port=port)
            self.instances.append(instance)
            self._refresh_panels()
            self._save_config()
            self._set_status(f"Added instance: {name}")

    def _set_json_path(self):
        """Sets the default JSON path for run commands."""
        path = filedialog.askopenfilename(
            title="Select Default JSON File",
            filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
        )
        if path:
            self.default_json_path = path
            self._save_config()
            self._set_status(f"Default JSON: {os.path.basename(path)}")
            self.json_label.configure(text=f"JSON: {os.path.basename(path)}")

    def _on_panel_run(self, instance: WranglerInstance):
        """Handles run button click from a panel."""
        if not self.default_json_path:
            path = filedialog.askopenfilename(
                title="Select JSON File to Run",
                filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
            )
            if not path:
                return
            self.default_json_path = path
            self._save_config()
            self.json_label.configure(text=f"JSON: {os.path.basename(path)}")
        else:
            path = self.default_json_path

        self._set_status(f"Starting {instance.name}...")

        def do_run():
            success, message = WranglerClient.run_order(instance, json_path=path)
            self.after(0, lambda: self._set_status(
                f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_run, daemon=True)
        thread.start()

    def _on_panel_stop(self, instance: WranglerInstance):
        """Handles stop button click from a panel."""
        self._set_status(f"Stopping {instance.name}...")

        def do_stop():
            success, message = WranglerClient.stop_gently(instance)
            self.after(0, lambda: self._set_status(
                f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(2)
            status = WranglerClient.get_status(instance)
            self.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_stop, daemon=True)
        thread.start()

    def _on_panel_resume(self, instance: WranglerInstance):
        """Handles resume button click from a panel."""
        self._set_status(f"Resuming {instance.name}...")

        def do_resume():
            success, message = WranglerClient.resume_orders(instance)
            self.after(0, lambda: self._set_status(
                f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_resume, daemon=True)
        thread.start()

    def _on_panel_advanced_run(self, instance: WranglerInstance):
        """Handles advanced run button click from a panel."""
        key = f"{instance.host}:{instance.port}"
        panel = self.panels.get(key)
        has_incomplete = panel.status.has_incomplete_orders if panel else False

        existing_config = instance.get_advanced_config()

        dialog = AdvancedRunDialog(self, instance.name, has_incomplete, existing_config)
        self.wait_window(dialog)

        if dialog.result is None:
            return

        config = dialog.result

        instance.set_advanced_config(config)
        self._save_config()

        if dialog.save_only:
            self._set_status(f"{instance.name}: Configuration saved")
            return

        if not config.use_resume and not self.default_json_path:
            path = filedialog.askopenfilename(
                title="Select JSON File to Run",
                filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
            )
            if not path:
                return
            self.default_json_path = path
            self._save_config()

        if config.mode == "none":
            self._start_none_mode(instance, config)
        elif config.mode == "timer":
            self._start_timer_mode(instance, config)
        else:
            self._start_schedule_mode(instance, config)

    def _start_none_mode(self, instance: WranglerInstance, config: AdvancedRunConfig):
        """Starts none mode - runs or resumes immediately."""
        action = "Resuming" if config.use_resume else "Starting"
        self._set_status(f"{instance.name}: {action}...")

        def do_run():
            if config.use_resume:
                success, message = WranglerClient.resume_orders(instance)
                action_past = "Resumed"
            else:
                success, message = WranglerClient.run_order(instance, json_path=self.default_json_path)
                action_past = "Started"

            self.after(0, lambda: self._set_status(
                f"{instance.name}: {action_past}" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_run, daemon=True)
        thread.start()

    def _start_timer_mode(self, instance: WranglerInstance, config: AdvancedRunConfig):
        """Starts timer mode for an instance."""
        key = f"{instance.host}:{instance.port}"
        duration_seconds = config.timer_hours * 3600 + config.timer_minutes * 60
        end_time = datetime.now() + timedelta(seconds=duration_seconds)

        self.active_timers[key] = {
            "end_time": end_time,
            "stopped": False,
            "instance": instance
        }

        action = "Resuming" if config.use_resume else "Starting"
        self._set_status(f"{instance.name}: Timer started ({config.timer_hours}h {config.timer_minutes}m)")

        def do_run():
            if config.use_resume:
                success, message = WranglerClient.resume_orders(instance)
            else:
                success, message = WranglerClient.run_order(instance, json_path=self.default_json_path)

            self.after(0, lambda: self._set_status(
                f"{instance.name}: {action} (timer mode)" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_run, daemon=True)
        thread.start()

    def _start_schedule_mode(self, instance: WranglerInstance, config: AdvancedRunConfig):
        """Starts schedule mode for an instance."""
        key = f"{instance.host}:{instance.port}"

        self.active_schedules[key] = {
            "config": config,
            "instance": instance,
            "last_action": None
        }

        mode_desc = "resume" if config.use_resume else "run"
        self._set_status(
            f"{instance.name}: Schedule activated ({mode_desc}) "
            f"({config.schedule_start_hour:02d}:{config.schedule_start_minute:02d} - "
            f"{config.schedule_end_hour:02d}:{config.schedule_end_minute:02d})"
        )

        self._check_schedule_for_instance(key)

    def _check_timers(self):
        """Checks all active timers and stops instances that have expired."""
        now = datetime.now()
        keys_to_remove = []

        for key, timer_data in self.active_timers.items():
            if timer_data["stopped"]:
                continue

            if now >= timer_data["end_time"]:
                instance = timer_data["instance"]
                timer_data["stopped"] = True
                keys_to_remove.append(key)

                def do_stop(inst=instance):
                    WranglerClient.stop_gently(inst)
                    self.after(0, lambda: self._set_status(f"{inst.name}: Timer expired, stopping..."))
                    time.sleep(2)
                    status = WranglerClient.get_status(inst)
                    self.after(0, lambda: self._update_panel(inst, status))

                    if inst.go_home_after_session:
                        time.sleep(3)
                        success, msg = WranglerClient.go_home(inst)
                        self.after(0, lambda: self._set_status(
                            f"{inst.name}: Going home..." if success else f"{inst.name}: Go home failed"
                        ))

                threading.Thread(target=do_stop, daemon=True).start()

        for key in keys_to_remove:
            del self.active_timers[key]

    def _check_schedules(self):
        """Checks all active schedules."""
        for key in self.active_schedules:
            self._check_schedule_for_instance(key)

    def _check_schedule_for_instance(self, key: str):
        """Checks and manages schedule for a single instance."""
        if key not in self.active_schedules:
            return

        schedule_data = self.active_schedules[key]
        config = schedule_data["config"]
        instance = schedule_data["instance"]
        last_action = schedule_data["last_action"]

        now = datetime.now()
        current_minutes = now.hour * 60 + now.minute
        start_minutes = config.schedule_start_hour * 60 + config.schedule_start_minute
        end_minutes = config.schedule_end_hour * 60 + config.schedule_end_minute

        if start_minutes <= end_minutes:
            in_window = start_minutes <= current_minutes < end_minutes
        else:
            in_window = current_minutes >= start_minutes or current_minutes < end_minutes

        status = WranglerClient.get_status(instance)

        if in_window:
            if last_action != "started" and not status.is_executing:
                schedule_data["last_action"] = "started"

                def do_start(inst=instance, use_resume=config.use_resume):
                    if use_resume:
                        success, message = WranglerClient.resume_orders(inst)
                        action = "Resumed"
                    else:
                        success, message = WranglerClient.run_order(inst, json_path=self.default_json_path)
                        action = "Started"

                    self.after(0, lambda: self._set_status(
                        f"{inst.name}: {action} (schedule)" if success else f"{inst.name} failed: {message}"
                    ))
                    time.sleep(1)
                    st = WranglerClient.get_status(inst)
                    self.after(0, lambda: self._update_panel(inst, st))

                threading.Thread(target=do_start, daemon=True).start()
        else:
            if last_action != "stopped" and status.is_executing:
                schedule_data["last_action"] = "stopped"

                def do_stop(inst=instance):
                    WranglerClient.stop_gently(inst)
                    self.after(0, lambda: self._set_status(f"{inst.name}: Stopped (schedule)"))
                    time.sleep(2)
                    st = WranglerClient.get_status(inst)
                    self.after(0, lambda: self._update_panel(inst, st))

                    if inst.go_home_after_session:
                        time.sleep(3)
                        success, msg = WranglerClient.go_home(inst)
                        self.after(0, lambda: self._set_status(
                            f"{inst.name}: Going home..." if success else f"{inst.name}: Go home failed"
                        ))

                threading.Thread(target=do_stop, daemon=True).start()

    def _on_panel_remove(self, instance: WranglerInstance):
        """Handles remove button click from a panel."""
        if messagebox.askyesno("Remove Instance", f"Remove '{instance.name}' from the list?"):
            key = f"{instance.host}:{instance.port}"
            if key in self.active_timers:
                del self.active_timers[key]
            if key in self.active_schedules:
                del self.active_schedules[key]

            self.instances.remove(instance)
            self._refresh_panels()
            self._save_config()
            self._set_status(f"Removed: {instance.name}")

    def _start_all(self):
        """Starts all instances."""
        if not self.default_json_path:
            path = filedialog.askopenfilename(
                title="Select JSON File to Run on All",
                filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
            )
            if not path:
                return
            self.default_json_path = path
            self._save_config()
        else:
            path = self.default_json_path

        self._set_status("Starting all instances...")

        def do_start_all():
            successes = 0
            failures = 0

            for instance in self.instances:
                if not instance.enabled:
                    continue

                success, _ = WranglerClient.run_order(instance, json_path=path)
                if success:
                    successes += 1
                else:
                    failures += 1

            self.after(0, lambda: self._set_status(f"Started {successes} instances, {failures} failed"))

            time.sleep(2)
            self._refresh_all_async()

        thread = threading.Thread(target=do_start_all, daemon=True)
        thread.start()

    def _stop_all(self):
        """Stops all instances gently."""
        self._set_status("Stopping all instances...")

        def do_stop_all():
            successes = 0
            failures = 0

            for instance in self.instances:
                if not instance.enabled:
                    continue

                success, _ = WranglerClient.stop_gently(instance)
                if success:
                    successes += 1
                else:
                    failures += 1

            self.after(0, lambda: self._set_status(f"Stopped {successes} instances, {failures} failed"))

            time.sleep(2)
            self._refresh_all_async()

        thread = threading.Thread(target=do_stop_all, daemon=True)
        thread.start()

    def _resume_all(self):
        """Resumes all instances with incomplete orders."""
        self._set_status("Resuming all instances...")

        def do_resume_all():
            successes = 0
            failures = 0
            skipped = 0

            for instance in self.instances:
                if not instance.enabled:
                    continue

                status = WranglerClient.get_status(instance)
                if not status.reachable:
                    failures += 1
                    continue

                if not status.has_incomplete_orders:
                    skipped += 1
                    continue

                if status.is_executing:
                    skipped += 1
                    continue

                success, _ = WranglerClient.resume_orders(instance)
                if success:
                    successes += 1
                else:
                    failures += 1

            self.after(0, lambda: self._set_status(
                f"Resumed {successes} instances, {failures} failed, {skipped} skipped"
            ))

            time.sleep(2)
            self._refresh_all_async()

        thread = threading.Thread(target=do_resume_all, daemon=True)
        thread.start()

    def _save_config(self):
        """Saves current configuration to file."""
        config = {
            "instances": [asdict(i) for i in self.instances],
            "default_json_path": self.default_json_path
        }

        try:
            with open(CONFIG_FILE, "w") as f:
                json.dump(config, f, indent=2)
        except Exception as e:
            messagebox.showerror("Error", f"Failed to save config: {e}")

    def _load_config(self):
        """Loads configuration from file."""
        if not os.path.exists(CONFIG_FILE):
            self._refresh_panels()
            return

        try:
            with open(CONFIG_FILE, "r") as f:
                config = json.load(f)

            self.instances = [
                WranglerInstance(**data)
                for data in config.get("instances", [])
            ]
            self.default_json_path = config.get("default_json_path", "")

            if self.default_json_path:
                self.json_label.configure(text=f"JSON: {os.path.basename(self.default_json_path)}")

            self._refresh_panels()
            self._set_status(f"Loaded {len(self.instances)} instances")

        except Exception as e:
            messagebox.showerror("Error", f"Failed to load config: {e}")

    def _save_app_settings(self):
        """Saves application settings."""
        settings_file = SCRIPT_DIR / "app_settings.json"
        settings = {
            "appearance_mode": self.app_settings.appearance_mode,
            "color_theme": self.app_settings.color_theme,
            "background_image": self.app_settings.background_image
        }

        try:
            with open(settings_file, "w") as f:
                json.dump(settings, f, indent=2)
        except Exception as e:
            print(f"Failed to save app settings: {e}")

    def _load_app_settings(self):
        """Loads application settings."""
        settings_file = SCRIPT_DIR / "app_settings.json"

        if not settings_file.exists():
            return

        try:
            with open(settings_file, "r") as f:
                settings = json.load(f)

            self.app_settings.appearance_mode = settings.get("appearance_mode", "dark")
            self.app_settings.color_theme = settings.get("color_theme", "wrangler")
            self.app_settings.background_image = settings.get("background_image", "")

        except Exception as e:
            print(f"Failed to load app settings: {e}")

    def _set_status(self, message: str):
        """Updates the status bar message."""
        self.status_label.configure(text=message)

    def _on_close(self):
        """Handles window close."""
        self.polling_active = False
        self._save_config()
        self._save_app_settings()
        self.destroy()


# =============================================================================
# Entry Point
# =============================================================================

def main():
    """Application entry point."""
    # Create themes directory if it doesn't exist
    THEMES_DIR.mkdir(exist_ok=True)

    app = WranglerMasterApp()
    app.mainloop()


if __name__ == "__main__":
    main()
