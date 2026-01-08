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
class WranglerInstance:
    """Represents a Wrangler instance configuration."""
    name: str
    host: str
    port: int
    enabled: bool = True

    @property
    def base_url(self) -> str:
        return f"http://{self.host}:{self.port}"


@dataclass
class InstanceStatus:
    """Current status of a Wrangler instance."""
    state: str = "unknown"
    is_executing: bool = False
    has_pending_order: bool = False
    current_file: str = "None"
    api_status: str = "Unknown"
    bot_running: bool = False
    reachable: bool = False
    error: Optional[str] = None


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
                status.current_file = data.get("currentFile", "None")
                status.api_status = data.get("apiStatus", "Unknown")
                status.bot_running = data.get("botRunning", False)
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
                 on_run: Callable, on_stop: Callable, on_remove: Callable):
        super().__init__(parent, padding=10)

        self.instance = instance
        self.status = InstanceStatus()
        self.on_run = on_run
        self.on_stop = on_stop
        self.on_remove = on_remove

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

        # File
        self.file_label.pack(fill=tk.X, padx=10, pady=2)

        # Buttons
        self.button_frame.pack(fill=tk.X, padx=10, pady=(5, 10))
        self.run_btn.pack(side=tk.LEFT, padx=2)
        self.stop_btn.pack(side=tk.LEFT, padx=2)
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

        # Update file label
        file_text = status.current_file if status.current_file else "None"
        if len(file_text) > 30:
            file_text = "..." + file_text[-27:]
        self.file_label.config(text=f"File: {file_text}")

        # Update button states
        can_run = status.reachable and status.state in ("idle", "stopped")
        can_stop = status.reachable and status.is_executing

        self.run_btn.config(state=tk.NORMAL if can_run else tk.DISABLED)
        self.stop_btn.config(state=tk.NORMAL if can_stop else tk.DISABLED)

    def _on_run_click(self):
        """Handles Run button click."""
        self.on_run(self.instance)

    def _on_stop_click(self):
        """Handles Stop button click."""
        self.on_stop(self.instance)

    def _on_remove_click(self):
        """Handles Remove button click."""
        self.on_remove(self.instance)


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
                on_remove=self._on_panel_remove
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

    def _on_panel_remove(self, instance: WranglerInstance):
        """Handles remove button click from a panel."""
        if messagebox.askyesno("Remove Instance",
                               f"Remove '{instance.name}' from the list?"):
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
