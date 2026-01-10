#!/usr/bin/env python3
"""
Wrangler Master Control Program
===============================

A GUI application to control multiple TheWrangler instances across a local network.

Features:
- Display Wrangler instances as status panels
- Start/Stop individual instances
- Master Start All / Stop All controls
- Auto-refresh status every 10 seconds
- Save/Load instance configuration

Usage:
    python wrangler_master.py

Requirements:
    - Python 3.6+
    - tkinter (usually included with Python)
    - requests library (pip install requests)
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

import tkinter as tk
from tkinter import ttk, messagebox, simpledialog, filedialog


# =============================================================================
# Configuration
# =============================================================================

CONFIG_FILE = "wrangler_config.json"
POLL_INTERVAL_MS = 10000  # 10 seconds
REQUEST_TIMEOUT = 5  # seconds


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

class InstancePanel(ttk.Frame):
    """A panel widget displaying a single Wrangler instance."""

    # Color schemes
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
        super().__init__(parent, padding=10)

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
        # Main container with border
        self.container = tk.Frame(self, bg="#2d2d30", relief=tk.RAISED, bd=2)

        # Header with name and address
        self.header_frame = tk.Frame(self.container, bg="#2d2d30")
        self.name_label = tk.Label(
            self.header_frame,
            text=self.instance.name,
            font=("Segoe UI", 12, "bold"),
            fg="white",
            bg="#2d2d30"
        )
        self.address_label = tk.Label(
            self.header_frame,
            text=f"{self.instance.host}:{self.instance.port}",
            font=("Segoe UI", 9),
            fg="#888888",
            bg="#2d2d30"
        )

        # Status indicator
        self.status_frame = tk.Frame(self.container, bg="#2d2d30")
        self.status_indicator = tk.Label(
            self.status_frame,
            text="\u25CF",  # Filled circle
            font=("Segoe UI", 16),
            fg=self.COLORS["unreachable"],
            bg="#2d2d30"
        )
        self.status_text = tk.Label(
            self.status_frame,
            text="Unknown",
            font=("Segoe UI", 10),
            fg="#cccccc",
            bg="#2d2d30",
            width=12,
            anchor="w"
        )

        # Character info label
        self.character_label = tk.Label(
            self.container,
            text="Character: Unknown",
            font=("Segoe UI", 9),
            fg="#aaaaaa",
            bg="#2d2d30",
            anchor="w"
        )

        # Runtime label
        self.runtime_label = tk.Label(
            self.container,
            text="",
            font=("Segoe UI", 9),
            fg="#2ecc71",
            bg="#2d2d30",
            anchor="w"
        )

        # Current file label
        self.file_label = tk.Label(
            self.container,
            text="File: None",
            font=("Segoe UI", 9),
            fg="#aaaaaa",
            bg="#2d2d30",
            anchor="w"
        )

        # Button frame
        self.button_frame = tk.Frame(self.container, bg="#2d2d30")

        self.run_btn = tk.Button(
            self.button_frame,
            text="Run",
            font=("Segoe UI", 9, "bold"),
            bg="#2ecc71",
            fg="white",
            activebackground="#27ae60",
            activeforeground="white",
            relief=tk.FLAT,
            width=8,
            command=self._on_run_click
        )

        self.resume_btn = tk.Button(
            self.button_frame,
            text="Resume",
            font=("Segoe UI", 9, "bold"),
            bg="#3498db",
            fg="white",
            activebackground="#2980b9",
            activeforeground="white",
            relief=tk.FLAT,
            width=8,
            command=self._on_resume_click
        )

        self.stop_btn = tk.Button(
            self.button_frame,
            text="Stop",
            font=("Segoe UI", 9, "bold"),
            bg="#e67e22",
            fg="white",
            activebackground="#d35400",
            activeforeground="white",
            relief=tk.FLAT,
            width=8,
            command=self._on_stop_click
        )

        self.advanced_btn = tk.Button(
            self.button_frame,
            text="Advanced",
            font=("Segoe UI", 9),
            bg="#9b59b6",
            fg="white",
            activebackground="#8e44ad",
            activeforeground="white",
            relief=tk.FLAT,
            width=8,
            command=self._on_advanced_click
        )

        self.remove_btn = tk.Button(
            self.button_frame,
            text="X",
            font=("Segoe UI", 8, "bold"),
            bg="#e74c3c",
            fg="white",
            activebackground="#c0392b",
            activeforeground="white",
            relief=tk.FLAT,
            width=2,
            command=self._on_remove_click
        )

        # Menu button (three dots)
        self.menu_btn = tk.Button(
            self.button_frame,
            text="\u22EE",  # Vertical ellipsis
            font=("Segoe UI", 10),
            bg="#555555",
            fg="white",
            activebackground="#666666",
            activeforeground="white",
            relief=tk.FLAT,
            width=2,
            command=self._show_menu
        )

        # Create popup menu
        self.popup_menu = tk.Menu(self, tearoff=0, bg="#2d2d30", fg="white",
                                   activebackground="#3d3d40", activeforeground="white")
        self.go_home_var = tk.BooleanVar(value=self.instance.go_home_after_session)
        self.popup_menu.add_checkbutton(
            label="Go Home after session",
            variable=self.go_home_var,
            command=self._on_go_home_toggle
        )

    def _layout_widgets(self):
        """Arranges widgets in the panel."""
        self.container.pack(fill=tk.BOTH, expand=True)

        # Header
        self.header_frame.pack(fill=tk.X, padx=10, pady=(10, 5))
        self.name_label.pack(side=tk.LEFT)
        self.address_label.pack(side=tk.RIGHT)

        # Status
        self.status_frame.pack(fill=tk.X, padx=10, pady=5)
        self.status_indicator.pack(side=tk.LEFT)
        self.status_text.pack(side=tk.LEFT, padx=5)

        # Character info
        self.character_label.pack(fill=tk.X, padx=10, pady=1)

        # Runtime
        self.runtime_label.pack(fill=tk.X, padx=10, pady=1)

        # File
        self.file_label.pack(fill=tk.X, padx=10, pady=2)

        # Buttons
        self.button_frame.pack(fill=tk.X, padx=10, pady=(5, 10))
        self.run_btn.pack(side=tk.LEFT, padx=2)
        self.resume_btn.pack(side=tk.LEFT, padx=2)
        self.stop_btn.pack(side=tk.LEFT, padx=2)
        self.advanced_btn.pack(side=tk.LEFT, padx=2)
        self.menu_btn.pack(side=tk.RIGHT, padx=2)
        self.remove_btn.pack(side=tk.RIGHT, padx=2)

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
        self.status_indicator.config(fg=color)
        self.status_text.config(text=state_text)

        # Update character label
        if status.reachable and status.character_name != "Unknown":
            char_text = f"{status.character_name} @ {status.world_name}"
            self.character_label.config(text=char_text, fg="#cccccc")
        else:
            self.character_label.config(text="Character: Unknown", fg="#666666")

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
            self.runtime_label.config(text=runtime_text, fg="#2ecc71")
        else:
            self.runtime_label.config(text="", fg="#2ecc71")

        # Update file label
        file_text = status.current_file if status.current_file else "None"
        if len(file_text) > 30:
            file_text = "..." + file_text[-27:]
        self.file_label.config(text=f"File: {file_text}")

        # Update button states
        can_run = status.reachable and status.state in ("idle", "stopped")
        can_resume = status.reachable and status.state in ("idle", "stopped") and status.has_incomplete_orders
        can_stop = status.reachable and status.is_executing
        can_advanced = status.reachable and status.state in ("idle", "stopped")

        self.run_btn.config(state=tk.NORMAL if can_run else tk.DISABLED)
        self.resume_btn.config(state=tk.NORMAL if can_resume else tk.DISABLED)
        self.stop_btn.config(state=tk.NORMAL if can_stop else tk.DISABLED)
        self.advanced_btn.config(state=tk.NORMAL if can_advanced else tk.DISABLED)

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
        """Shows the popup menu."""
        # Position menu near the button
        x = self.menu_btn.winfo_rootx()
        y = self.menu_btn.winfo_rooty() + self.menu_btn.winfo_height()
        self.popup_menu.post(x, y)

    def _on_go_home_toggle(self):
        """Handles Go Home checkbox toggle."""
        self.instance.go_home_after_session = self.go_home_var.get()
        if self.on_settings_changed:
            self.on_settings_changed()


# =============================================================================
# Advanced Run Dialog
# =============================================================================


class AdvancedRunDialog(tk.Toplevel):
    """Dialog for configuring advanced run options (none, timer, or schedule mode)."""

    def __init__(self, parent, instance_name: str, has_incomplete_orders: bool = False,
                 existing_config: Optional[AdvancedRunConfig] = None):
        super().__init__(parent)
        self.title(f"Advanced Run - {instance_name}")
        self.geometry("450x450")
        self.resizable(False, False)
        self.configure(bg="#2d2d30")

        self.result: Optional[AdvancedRunConfig] = None
        self.save_only: bool = False  # True if user clicked Save instead of Start
        self.has_incomplete_orders = has_incomplete_orders

        # Load existing config or use defaults
        self.config = existing_config or AdvancedRunConfig()

        # Mode variable - load from existing config
        self.mode_var = tk.StringVar(value=self.config.mode)

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
        # Mode selection
        mode_frame = tk.Frame(self, bg="#2d2d30")
        mode_frame.pack(fill=tk.X, padx=20, pady=(20, 10))

        tk.Label(
            mode_frame, text="Run Mode:", font=("Segoe UI", 10, "bold"),
            fg="white", bg="#2d2d30"
        ).pack(side=tk.LEFT)

        tk.Radiobutton(
            mode_frame, text="None", variable=self.mode_var, value="none",
            font=("Segoe UI", 10), fg="white", bg="#2d2d30", selectcolor="#3d3d40",
            activebackground="#2d2d30", activeforeground="white",
            command=self._on_mode_change
        ).pack(side=tk.LEFT, padx=10)

        tk.Radiobutton(
            mode_frame, text="Timer", variable=self.mode_var, value="timer",
            font=("Segoe UI", 10), fg="white", bg="#2d2d30", selectcolor="#3d3d40",
            activebackground="#2d2d30", activeforeground="white",
            command=self._on_mode_change
        ).pack(side=tk.LEFT, padx=10)

        tk.Radiobutton(
            mode_frame, text="Schedule", variable=self.mode_var, value="schedule",
            font=("Segoe UI", 10), fg="white", bg="#2d2d30", selectcolor="#3d3d40",
            activebackground="#2d2d30", activeforeground="white",
            command=self._on_mode_change
        ).pack(side=tk.LEFT)

        # Resume option (applies to all modes)
        resume_frame = tk.Frame(self, bg="#2d2d30")
        resume_frame.pack(fill=tk.X, padx=20, pady=(0, 10))

        # Always allow the checkbox to be set (as a preference for future runs)
        self.resume_var = tk.BooleanVar(value=self.config.use_resume)
        self.resume_check = tk.Checkbutton(
            resume_frame, text="Resume incomplete orders (instead of starting new)",
            variable=self.resume_var, font=("Segoe UI", 10),
            fg="#cccccc", bg="#2d2d30", selectcolor="#3d3d40",
            activebackground="#2d2d30", activeforeground="white"
        )
        self.resume_check.pack(side=tk.LEFT)

        # None mode description
        self.none_frame = tk.LabelFrame(
            self, text="None Mode (Normal Run)", font=("Segoe UI", 10),
            fg="white", bg="#2d2d30", padx=15, pady=10
        )
        self.none_frame.pack(fill=tk.X, padx=20, pady=10)

        tk.Label(
            self.none_frame,
            text="Runs or resumes orders immediately without any timer\nor schedule. The bot will run until orders complete\nor you stop it manually.",
            font=("Segoe UI", 9), fg="#888888", bg="#2d2d30", justify=tk.LEFT
        ).pack(anchor="w")

        # Timer mode frame
        self.timer_frame = tk.LabelFrame(
            self, text="Timer Mode", font=("Segoe UI", 10),
            fg="white", bg="#2d2d30", padx=15, pady=10
        )
        self.timer_frame.pack(fill=tk.X, padx=20, pady=10)

        tk.Label(
            self.timer_frame, text="Run for:", font=("Segoe UI", 10),
            fg="#cccccc", bg="#2d2d30"
        ).grid(row=0, column=0, sticky="w", pady=5)

        timer_input_frame = tk.Frame(self.timer_frame, bg="#2d2d30")
        timer_input_frame.grid(row=0, column=1, sticky="w", pady=5)

        # Load persisted values
        self.timer_hours_var = tk.StringVar(value=str(self.config.timer_hours))
        self.timer_hours_entry = tk.Entry(
            timer_input_frame, textvariable=self.timer_hours_var,
            font=("Segoe UI", 10), width=4
        )
        self.timer_hours_entry.pack(side=tk.LEFT)
        tk.Label(timer_input_frame, text="hours", fg="#cccccc", bg="#2d2d30").pack(side=tk.LEFT, padx=5)

        self.timer_minutes_var = tk.StringVar(value=str(self.config.timer_minutes))
        self.timer_minutes_entry = tk.Entry(
            timer_input_frame, textvariable=self.timer_minutes_var,
            font=("Segoe UI", 10), width=4
        )
        self.timer_minutes_entry.pack(side=tk.LEFT)
        tk.Label(timer_input_frame, text="minutes", fg="#cccccc", bg="#2d2d30").pack(side=tk.LEFT, padx=5)

        tk.Label(
            self.timer_frame,
            text="After the timer expires, StopGently will be called.",
            font=("Segoe UI", 9), fg="#888888", bg="#2d2d30"
        ).grid(row=1, column=0, columnspan=2, sticky="w", pady=(5, 0))

        # Schedule mode frame
        self.schedule_frame = tk.LabelFrame(
            self, text="Schedule Mode", font=("Segoe UI", 10),
            fg="white", bg="#2d2d30", padx=15, pady=10
        )
        self.schedule_frame.pack(fill=tk.X, padx=20, pady=10)

        # Start time
        tk.Label(
            self.schedule_frame, text="Start at:", font=("Segoe UI", 10),
            fg="#cccccc", bg="#2d2d30"
        ).grid(row=0, column=0, sticky="w", pady=5)

        start_frame = tk.Frame(self.schedule_frame, bg="#2d2d30")
        start_frame.grid(row=0, column=1, sticky="w", pady=5)

        # Load persisted values
        self.start_hour_var = tk.StringVar(value=f"{self.config.schedule_start_hour:02d}")
        self.start_hour_entry = tk.Entry(
            start_frame, textvariable=self.start_hour_var,
            font=("Segoe UI", 10), width=3
        )
        self.start_hour_entry.pack(side=tk.LEFT)
        tk.Label(start_frame, text=":", fg="#cccccc", bg="#2d2d30").pack(side=tk.LEFT)

        self.start_minute_var = tk.StringVar(value=f"{self.config.schedule_start_minute:02d}")
        self.start_minute_entry = tk.Entry(
            start_frame, textvariable=self.start_minute_var,
            font=("Segoe UI", 10), width=3
        )
        self.start_minute_entry.pack(side=tk.LEFT)
        tk.Label(start_frame, text="(local time)", fg="#888888", bg="#2d2d30").pack(side=tk.LEFT, padx=5)

        # End time
        tk.Label(
            self.schedule_frame, text="Stop at:", font=("Segoe UI", 10),
            fg="#cccccc", bg="#2d2d30"
        ).grid(row=1, column=0, sticky="w", pady=5)

        end_frame = tk.Frame(self.schedule_frame, bg="#2d2d30")
        end_frame.grid(row=1, column=1, sticky="w", pady=5)

        self.end_hour_var = tk.StringVar(value=f"{self.config.schedule_end_hour:02d}")
        self.end_hour_entry = tk.Entry(
            end_frame, textvariable=self.end_hour_var,
            font=("Segoe UI", 10), width=3
        )
        self.end_hour_entry.pack(side=tk.LEFT)
        tk.Label(end_frame, text=":", fg="#cccccc", bg="#2d2d30").pack(side=tk.LEFT)

        self.end_minute_var = tk.StringVar(value=f"{self.config.schedule_end_minute:02d}")
        self.end_minute_entry = tk.Entry(
            end_frame, textvariable=self.end_minute_var,
            font=("Segoe UI", 10), width=3
        )
        self.end_minute_entry.pack(side=tk.LEFT)
        tk.Label(end_frame, text="(local time)", fg="#888888", bg="#2d2d30").pack(side=tk.LEFT, padx=5)

        tk.Label(
            self.schedule_frame,
            text="Runs daily: starts at start time, stops at end time.",
            font=("Segoe UI", 9), fg="#888888", bg="#2d2d30"
        ).grid(row=2, column=0, columnspan=2, sticky="w", pady=(5, 0))

        # Buttons
        btn_frame = tk.Frame(self, bg="#2d2d30")
        btn_frame.pack(fill=tk.X, padx=20, pady=20)

        tk.Button(
            btn_frame, text="Cancel", font=("Segoe UI", 10),
            bg="#666666", fg="white", relief=tk.FLAT, width=10,
            command=self.destroy
        ).pack(side=tk.RIGHT, padx=5)

        tk.Button(
            btn_frame, text="Start", font=("Segoe UI", 10, "bold"),
            bg="#2ecc71", fg="white", relief=tk.FLAT, width=10,
            command=self._on_start
        ).pack(side=tk.RIGHT, padx=5)

        tk.Button(
            btn_frame, text="Save", font=("Segoe UI", 10),
            bg="#3498db", fg="white", relief=tk.FLAT, width=10,
            command=self._on_save
        ).pack(side=tk.RIGHT, padx=5)

        # Initialize visibility
        self._on_mode_change()

    def _on_mode_change(self):
        """Updates frame visibility based on selected mode."""
        mode = self.mode_var.get()

        # Update frame styling based on mode
        self.none_frame.config(fg="white" if mode == "none" else "#666666")
        self.timer_frame.config(fg="white" if mode == "timer" else "#666666")
        self.schedule_frame.config(fg="white" if mode == "schedule" else "#666666")

        # Enable/disable timer entries
        for child in self.timer_frame.winfo_children():
            if isinstance(child, tk.Frame):
                for subchild in child.winfo_children():
                    if isinstance(subchild, tk.Entry):
                        subchild.config(state=tk.NORMAL if mode == "timer" else tk.DISABLED)
            elif isinstance(child, tk.Entry):
                child.config(state=tk.NORMAL if mode == "timer" else tk.DISABLED)

        # Enable/disable schedule entries
        for child in self.schedule_frame.winfo_children():
            if isinstance(child, tk.Frame):
                for subchild in child.winfo_children():
                    if isinstance(subchild, tk.Entry):
                        subchild.config(state=tk.NORMAL if mode == "schedule" else tk.DISABLED)
            elif isinstance(child, tk.Entry):
                child.config(state=tk.NORMAL if mode == "schedule" else tk.DISABLED)

    def _on_start(self):
        """Validates and returns result."""
        config = AdvancedRunConfig()
        config.mode = self.mode_var.get()
        # Always save the user's preference for resume - it will be used when incomplete orders exist
        config.use_resume = self.resume_var.get()

        try:
            # Always save timer values even if not in timer mode (for persistence)
            config.timer_hours = int(self.timer_hours_var.get())
            config.timer_minutes = int(self.timer_minutes_var.get())

            # Always save schedule values even if not in schedule mode (for persistence)
            config.schedule_start_hour = int(self.start_hour_var.get())
            config.schedule_start_minute = int(self.start_minute_var.get())
            config.schedule_end_hour = int(self.end_hour_var.get())
            config.schedule_end_minute = int(self.end_minute_var.get())

            # Validate based on current mode
            if config.mode == "timer":
                if config.timer_hours < 0 or config.timer_minutes < 0:
                    raise ValueError("Time cannot be negative")
                if config.timer_hours == 0 and config.timer_minutes == 0:
                    raise ValueError("Timer must be at least 1 minute")
            elif config.mode == "schedule":
                # Validate ranges
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
        """Builds and validates configuration from form values. Returns None on error."""
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
# Main Application
# =============================================================================

class WranglerMasterApp:
    """Main application class."""

    def __init__(self, root: tk.Tk):
        self.root = root
        self.root.title("Wrangler Master Control")
        self.root.geometry("900x700")
        self.root.minsize(600, 400)
        self.root.configure(bg="#1e1e1e")

        # Data
        self.instances: List[WranglerInstance] = []
        self.panels: Dict[str, InstancePanel] = {}  # key = host:port
        self.polling_active = True

        # Default JSON path for run commands
        self.default_json_path = ""

        # Advanced run tracking
        # key = "host:port", value = dict with schedule info
        self.active_timers: Dict[str, dict] = {}  # {key: {"end_time": datetime, "stopped": False}}
        self.active_schedules: Dict[str, dict] = {}  # {key: {"config": AdvancedRunConfig, "last_action": str}}

        # Create UI
        self._create_menu()
        self._create_toolbar()
        self._create_main_area()
        self._create_status_bar()

        # Load config and start polling
        self._load_config()
        self._start_polling()

        # Handle window close
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    def _create_menu(self):
        """Creates the menu bar."""
        menubar = tk.Menu(self.root)
        self.root.config(menu=menubar)

        # File menu
        file_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="File", menu=file_menu)
        file_menu.add_command(label="Add Instance...", command=self._add_instance_dialog)
        file_menu.add_separator()
        file_menu.add_command(label="Set Default JSON Path...", command=self._set_json_path)
        file_menu.add_separator()
        file_menu.add_command(label="Save Config", command=self._save_config)
        file_menu.add_command(label="Load Config...", command=self._load_config_dialog)
        file_menu.add_separator()
        file_menu.add_command(label="Exit", command=self._on_close)

        # Actions menu
        actions_menu = tk.Menu(menubar, tearoff=0)
        menubar.add_cascade(label="Actions", menu=actions_menu)
        actions_menu.add_command(label="Refresh All", command=self._refresh_all)
        actions_menu.add_separator()
        actions_menu.add_command(label="Start All", command=self._start_all)
        actions_menu.add_command(label="Resume All", command=self._resume_all)
        actions_menu.add_command(label="Stop All Gently", command=self._stop_all)

    def _create_toolbar(self):
        """Creates the toolbar with master controls."""
        toolbar = tk.Frame(self.root, bg="#2d2d30", pady=10)
        toolbar.pack(fill=tk.X, padx=10, pady=(10, 0))

        # Title
        title = tk.Label(
            toolbar,
            text="Wrangler Master Control",
            font=("Segoe UI", 16, "bold"),
            fg="white",
            bg="#2d2d30"
        )
        title.pack(side=tk.LEFT, padx=10)

        # Master buttons (right side)
        btn_frame = tk.Frame(toolbar, bg="#2d2d30")
        btn_frame.pack(side=tk.RIGHT, padx=10)

        self.start_all_btn = tk.Button(
            btn_frame,
            text="Start All",
            font=("Segoe UI", 10, "bold"),
            bg="#2ecc71",
            fg="white",
            activebackground="#27ae60",
            activeforeground="white",
            relief=tk.FLAT,
            padx=20,
            pady=5,
            command=self._start_all
        )
        self.start_all_btn.pack(side=tk.LEFT, padx=5)

        self.resume_all_btn = tk.Button(
            btn_frame,
            text="Resume All",
            font=("Segoe UI", 10, "bold"),
            bg="#3498db",
            fg="white",
            activebackground="#2980b9",
            activeforeground="white",
            relief=tk.FLAT,
            padx=20,
            pady=5,
            command=self._resume_all
        )
        self.resume_all_btn.pack(side=tk.LEFT, padx=5)

        self.stop_all_btn = tk.Button(
            btn_frame,
            text="Stop All Gently",
            font=("Segoe UI", 10, "bold"),
            bg="#e67e22",
            fg="white",
            activebackground="#d35400",
            activeforeground="white",
            relief=tk.FLAT,
            padx=20,
            pady=5,
            command=self._stop_all
        )
        self.stop_all_btn.pack(side=tk.LEFT, padx=5)

        self.add_btn = tk.Button(
            btn_frame,
            text="+ Add Instance",
            font=("Segoe UI", 10),
            bg="#3498db",
            fg="white",
            activebackground="#2980b9",
            activeforeground="white",
            relief=tk.FLAT,
            padx=15,
            pady=5,
            command=self._add_instance_dialog
        )
        self.add_btn.pack(side=tk.LEFT, padx=5)

        self.refresh_btn = tk.Button(
            btn_frame,
            text="Refresh",
            font=("Segoe UI", 10),
            bg="#9b59b6",
            fg="white",
            activebackground="#8e44ad",
            activeforeground="white",
            relief=tk.FLAT,
            padx=15,
            pady=5,
            command=self._refresh_all
        )
        self.refresh_btn.pack(side=tk.LEFT, padx=5)

    def _create_main_area(self):
        """Creates the main scrollable area for instance panels."""
        # Container frame
        container = tk.Frame(self.root, bg="#1e1e1e")
        container.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)

        # Canvas with scrollbar
        self.canvas = tk.Canvas(container, bg="#1e1e1e", highlightthickness=0)
        scrollbar = ttk.Scrollbar(container, orient=tk.VERTICAL, command=self.canvas.yview)

        self.panels_frame = tk.Frame(self.canvas, bg="#1e1e1e")

        self.canvas.configure(yscrollcommand=scrollbar.set)

        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        self.canvas_window = self.canvas.create_window((0, 0), window=self.panels_frame, anchor=tk.NW)

        # Bind resize events
        self.panels_frame.bind("<Configure>", self._on_frame_configure)
        self.canvas.bind("<Configure>", self._on_canvas_configure)

        # Enable mousewheel scrolling
        self.canvas.bind_all("<MouseWheel>", self._on_mousewheel)

    def _create_status_bar(self):
        """Creates the status bar at the bottom."""
        self.status_bar = tk.Label(
            self.root,
            text="Ready",
            font=("Segoe UI", 9),
            fg="#888888",
            bg="#2d2d30",
            anchor=tk.W,
            padx=10,
            pady=5
        )
        self.status_bar.pack(fill=tk.X, side=tk.BOTTOM)

    def _on_frame_configure(self, event=None):
        """Updates scroll region when frame size changes."""
        self.canvas.configure(scrollregion=self.canvas.bbox("all"))

    def _on_canvas_configure(self, event):
        """Adjusts frame width when canvas is resized."""
        self.canvas.itemconfig(self.canvas_window, width=event.width)

    def _on_mousewheel(self, event):
        """Handles mousewheel scrolling."""
        self.canvas.yview_scroll(int(-1 * (event.delta / 120)), "units")

    def _refresh_panels(self):
        """Recreates all instance panels."""
        # Clear existing panels
        for widget in self.panels_frame.winfo_children():
            widget.destroy()
        self.panels.clear()

        if not self.instances:
            # Show placeholder
            placeholder = tk.Label(
                self.panels_frame,
                text="No instances configured.\nClick '+ Add Instance' to add a Wrangler instance.",
                font=("Segoe UI", 12),
                fg="#666666",
                bg="#1e1e1e",
                pady=50
            )
            placeholder.pack(fill=tk.BOTH, expand=True)
            return

        # Create panels in a grid layout
        cols = 3
        for i, instance in enumerate(self.instances):
            if not instance.enabled:
                continue

            panel = InstancePanel(
                self.panels_frame,
                instance,
                on_run=self._on_panel_run,
                on_stop=self._on_panel_stop,
                on_resume=self._on_panel_resume,
                on_advanced_run=self._on_panel_advanced_run,
                on_remove=self._on_panel_remove,
                on_settings_changed=self._save_config
            )

            row = i // cols
            col = i % cols
            panel.grid(row=row, column=col, padx=5, pady=5, sticky="nsew")

            key = f"{instance.host}:{instance.port}"
            self.panels[key] = panel

        # Configure grid weights for equal sizing
        for c in range(cols):
            self.panels_frame.columnconfigure(c, weight=1)

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
                # Check timers and schedules every poll cycle
                self._check_timers()
                self._check_schedules()
                time.sleep(POLL_INTERVAL_MS / 1000)

        thread = threading.Thread(target=poll, daemon=True)
        thread.start()

    def _refresh_all_async(self):
        """Fetches status from all instances (called from background thread)."""
        for instance in self.instances:
            if not instance.enabled:
                continue

            status = WranglerClient.get_status(instance)

            # Update UI from main thread
            self.root.after(0, lambda i=instance, s=status: self._update_panel(i, s))

    def _refresh_all(self):
        """Manual refresh triggered by button."""
        self._set_status("Refreshing...")

        def do_refresh():
            self._refresh_all_async()
            self.root.after(0, lambda: self._set_status("Refresh complete"))

        thread = threading.Thread(target=do_refresh, daemon=True)
        thread.start()

    def _add_instance_dialog(self):
        """Shows dialog to add a new instance."""
        dialog = AddInstanceDialog(self.root)
        self.root.wait_window(dialog)

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

    def _on_panel_run(self, instance: WranglerInstance):
        """Handles run button click from a panel."""
        if not self.default_json_path:
            # Ask for JSON path
            path = filedialog.askopenfilename(
                title="Select JSON File to Run",
                filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
            )
            if not path:
                return
        else:
            path = self.default_json_path

        self._set_status(f"Starting {instance.name}...")

        def do_run():
            success, message = WranglerClient.run_order(instance, json_path=path)
            self.root.after(0, lambda: self._set_status(
                f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            ))
            # Refresh status after short delay
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.root.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_run, daemon=True)
        thread.start()

    def _on_panel_stop(self, instance: WranglerInstance):
        """Handles stop button click from a panel."""
        self._set_status(f"Stopping {instance.name}...")

        def do_stop():
            success, message = WranglerClient.stop_gently(instance)
            self.root.after(0, lambda: self._set_status(
                f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            ))
            # Refresh status after short delay
            time.sleep(2)
            status = WranglerClient.get_status(instance)
            self.root.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_stop, daemon=True)
        thread.start()

    def _on_panel_resume(self, instance: WranglerInstance):
        """Handles resume button click from a panel."""
        self._set_status(f"Resuming {instance.name}...")

        def do_resume():
            success, message = WranglerClient.resume_orders(instance)
            self.root.after(0, lambda: self._set_status(
                f"{instance.name}: {message}" if success else f"{instance.name} failed: {message}"
            ))
            # Refresh status after short delay
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.root.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_resume, daemon=True)
        thread.start()

    def _on_panel_advanced_run(self, instance: WranglerInstance):
        """Handles advanced run button click from a panel."""
        # Get current status to check for incomplete orders
        key = f"{instance.host}:{instance.port}"
        panel = self.panels.get(key)
        has_incomplete = panel.status.has_incomplete_orders if panel else False

        # Get existing config from instance
        existing_config = instance.get_advanced_config()

        # Show dialog with existing config
        dialog = AdvancedRunDialog(self.root, instance.name, has_incomplete, existing_config)
        self.root.wait_window(dialog)

        if dialog.result is None:
            return

        config = dialog.result

        # Save the config to the instance for persistence
        instance.set_advanced_config(config)
        self._save_config()

        # If user clicked Save (not Start), just save and return without running
        if dialog.save_only:
            self._set_status(f"{instance.name}: Configuration saved")
            return

        # Check for default JSON path for non-resume runs
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
            # None mode: just run or resume immediately, no timer/schedule
            self._start_none_mode(instance, config)
        elif config.mode == "timer":
            # Timer mode: start now, stop after duration
            self._start_timer_mode(instance, config)
        else:
            # Schedule mode: manage based on current time
            self._start_schedule_mode(instance, config)

    def _start_none_mode(self, instance: WranglerInstance, config: AdvancedRunConfig):
        """Starts none mode - just runs or resumes immediately without timer/schedule."""
        action = "Resuming" if config.use_resume else "Starting"
        self._set_status(f"{instance.name}: {action}...")

        def do_run():
            if config.use_resume:
                success, message = WranglerClient.resume_orders(instance)
                action_past = "Resumed"
            else:
                success, message = WranglerClient.run_order(instance, json_path=self.default_json_path)
                action_past = "Started"

            self.root.after(0, lambda: self._set_status(
                f"{instance.name}: {action_past}" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.root.after(0, lambda: self._update_panel(instance, status))

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
        self._set_status(f"{instance.name}: Timer started ({config.timer_hours}h {config.timer_minutes}m), {action.lower()}...")

        # Start the order now
        def do_run():
            if config.use_resume:
                success, message = WranglerClient.resume_orders(instance)
                action_past = "Resumed"
            else:
                success, message = WranglerClient.run_order(instance, json_path=self.default_json_path)
                action_past = "Started"

            self.root.after(0, lambda: self._set_status(
                f"{instance.name}: {action_past} (timer mode)" if success else f"{instance.name} failed: {message}"
            ))
            time.sleep(1)
            status = WranglerClient.get_status(instance)
            self.root.after(0, lambda: self._update_panel(instance, status))

        thread = threading.Thread(target=do_run, daemon=True)
        thread.start()

    def _start_schedule_mode(self, instance: WranglerInstance, config: AdvancedRunConfig):
        """Starts schedule mode for an instance."""
        key = f"{instance.host}:{instance.port}"

        self.active_schedules[key] = {
            "config": config,
            "instance": instance,
            "last_action": None  # "started" or "stopped"
        }

        mode_desc = "resume" if config.use_resume else "run"
        self._set_status(
            f"{instance.name}: Schedule activated ({mode_desc}) "
            f"({config.schedule_start_hour:02d}:{config.schedule_start_minute:02d} - "
            f"{config.schedule_end_hour:02d}:{config.schedule_end_minute:02d})"
        )

        # Check if we should start immediately based on current time
        self._check_schedule_for_instance(key)

    def _check_timers(self):
        """Checks all active timers and stops instances that have expired."""
        now = datetime.now()
        keys_to_remove = []

        for key, timer_data in self.active_timers.items():
            if timer_data["stopped"]:
                continue

            if now >= timer_data["end_time"]:
                # Timer expired, stop the instance
                instance = timer_data["instance"]
                timer_data["stopped"] = True
                keys_to_remove.append(key)

                def do_stop(inst=instance):
                    success, _ = WranglerClient.stop_gently(inst)
                    self.root.after(0, lambda: self._set_status(
                        f"{inst.name}: Timer expired, stopping..."
                    ))
                    time.sleep(2)
                    status = WranglerClient.get_status(inst)
                    self.root.after(0, lambda: self._update_panel(inst, status))

                    # Go home if enabled
                    if inst.go_home_after_session:
                        time.sleep(3)  # Wait for stop to complete
                        success, msg = WranglerClient.go_home(inst)
                        self.root.after(0, lambda: self._set_status(
                            f"{inst.name}: Going home..." if success else f"{inst.name}: Go home failed: {msg}"
                        ))

                threading.Thread(target=do_stop, daemon=True).start()

        # Clean up expired timers
        for key in keys_to_remove:
            del self.active_timers[key]

    def _check_schedules(self):
        """Checks all active schedules and starts/stops instances as needed."""
        now = datetime.now()
        current_minutes = now.hour * 60 + now.minute

        for key, schedule_data in self.active_schedules.items():
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

        # Determine if we're in the active window
        if start_minutes <= end_minutes:
            # Normal case: e.g., 08:00 - 22:00
            in_window = start_minutes <= current_minutes < end_minutes
        else:
            # Overnight case: e.g., 22:00 - 06:00
            in_window = current_minutes >= start_minutes or current_minutes < end_minutes

        # Get current status
        status = WranglerClient.get_status(instance)

        if in_window:
            # Should be running
            if last_action != "started" and not status.is_executing:
                # Start or resume based on config.use_resume
                schedule_data["last_action"] = "started"

                def do_start(inst=instance, use_resume=config.use_resume):
                    if use_resume:
                        success, message = WranglerClient.resume_orders(inst)
                        action = "Resumed"
                    else:
                        success, message = WranglerClient.run_order(inst, json_path=self.default_json_path)
                        action = "Started"

                    self.root.after(0, lambda: self._set_status(
                        f"{inst.name}: {action} (schedule)" if success else f"{inst.name} failed: {message}"
                    ))
                    time.sleep(1)
                    st = WranglerClient.get_status(inst)
                    self.root.after(0, lambda: self._update_panel(inst, st))

                threading.Thread(target=do_start, daemon=True).start()
        else:
            # Should be stopped
            if last_action != "stopped" and status.is_executing:
                # Stop
                schedule_data["last_action"] = "stopped"

                def do_stop(inst=instance):
                    success, _ = WranglerClient.stop_gently(inst)
                    self.root.after(0, lambda: self._set_status(
                        f"{inst.name}: Stopped (schedule)"
                    ))
                    time.sleep(2)
                    st = WranglerClient.get_status(inst)
                    self.root.after(0, lambda: self._update_panel(inst, st))

                    # Go home if enabled
                    if inst.go_home_after_session:
                        time.sleep(3)  # Wait for stop to complete
                        success, msg = WranglerClient.go_home(inst)
                        self.root.after(0, lambda: self._set_status(
                            f"{inst.name}: Going home..." if success else f"{inst.name}: Go home failed: {msg}"
                        ))

                threading.Thread(target=do_stop, daemon=True).start()

    def _on_panel_remove(self, instance: WranglerInstance):
        """Handles remove button click from a panel."""
        if messagebox.askyesno("Remove Instance",
                               f"Remove '{instance.name}' from the list?"):
            # Clean up any active timers/schedules for this instance
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

            self.root.after(0, lambda: self._set_status(
                f"Started {successes} instances, {failures} failed"
            ))

            # Refresh all after delay
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

            self.root.after(0, lambda: self._set_status(
                f"Stopped {successes} instances, {failures} failed"
            ))

            # Refresh all after delay
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

                # First check if instance has incomplete orders
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

            self.root.after(0, lambda: self._set_status(
                f"Resumed {successes} instances, {failures} failed, {skipped} skipped"
            ))

            # Refresh all after delay
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
            self._refresh_panels()
            self._set_status(f"Loaded {len(self.instances)} instances")

        except Exception as e:
            messagebox.showerror("Error", f"Failed to load config: {e}")

    def _load_config_dialog(self):
        """Shows dialog to load config from a different file."""
        path = filedialog.askopenfilename(
            title="Load Configuration",
            filetypes=[("JSON Files", "*.json"), ("All Files", "*.*")]
        )
        if path:
            global CONFIG_FILE
            CONFIG_FILE = path
            self._load_config()

    def _set_status(self, message: str):
        """Updates the status bar message."""
        self.status_bar.config(text=message)

    def _on_close(self):
        """Handles window close."""
        self.polling_active = False
        self._save_config()
        self.root.destroy()


# =============================================================================
# Add Instance Dialog
# =============================================================================

class AddInstanceDialog(tk.Toplevel):
    """Dialog for adding a new Wrangler instance."""

    def __init__(self, parent):
        super().__init__(parent)
        self.title("Add Wrangler Instance")
        self.geometry("400x200")
        self.resizable(False, False)
        self.configure(bg="#2d2d30")

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
        # Name field
        name_frame = tk.Frame(self, bg="#2d2d30")
        name_frame.pack(fill=tk.X, padx=20, pady=(20, 10))

        tk.Label(
            name_frame, text="Name:", font=("Segoe UI", 10),
            fg="white", bg="#2d2d30", width=8, anchor="e"
        ).pack(side=tk.LEFT)

        self.name_entry = tk.Entry(name_frame, font=("Segoe UI", 10), width=30)
        self.name_entry.pack(side=tk.LEFT, padx=10)
        self.name_entry.insert(0, "Account 1")

        # Host field
        host_frame = tk.Frame(self, bg="#2d2d30")
        host_frame.pack(fill=tk.X, padx=20, pady=10)

        tk.Label(
            host_frame, text="Host:", font=("Segoe UI", 10),
            fg="white", bg="#2d2d30", width=8, anchor="e"
        ).pack(side=tk.LEFT)

        self.host_entry = tk.Entry(host_frame, font=("Segoe UI", 10), width=30)
        self.host_entry.pack(side=tk.LEFT, padx=10)
        self.host_entry.insert(0, "localhost")

        # Port field
        port_frame = tk.Frame(self, bg="#2d2d30")
        port_frame.pack(fill=tk.X, padx=20, pady=10)

        tk.Label(
            port_frame, text="Port:", font=("Segoe UI", 10),
            fg="white", bg="#2d2d30", width=8, anchor="e"
        ).pack(side=tk.LEFT)

        self.port_entry = tk.Entry(port_frame, font=("Segoe UI", 10), width=30)
        self.port_entry.pack(side=tk.LEFT, padx=10)
        self.port_entry.insert(0, "7800")

        # Buttons
        btn_frame = tk.Frame(self, bg="#2d2d30")
        btn_frame.pack(fill=tk.X, padx=20, pady=20)

        tk.Button(
            btn_frame, text="Cancel", font=("Segoe UI", 10),
            bg="#666666", fg="white", relief=tk.FLAT, width=10,
            command=self.destroy
        ).pack(side=tk.RIGHT, padx=5)

        tk.Button(
            btn_frame, text="Add", font=("Segoe UI", 10, "bold"),
            bg="#2ecc71", fg="white", relief=tk.FLAT, width=10,
            command=self._on_add
        ).pack(side=tk.RIGHT, padx=5)

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
# Entry Point
# =============================================================================

def main():
    """Application entry point."""
    root = tk.Tk()

    # Set dark theme for ttk widgets
    style = ttk.Style()
    style.theme_use("clam")
    style.configure("TFrame", background="#1e1e1e")
    style.configure("TScrollbar", background="#2d2d30", troughcolor="#1e1e1e")

    app = WranglerMasterApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
