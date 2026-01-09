# TheWrangler

A modular BotBase for RebornBuddy that wraps Lisbeth functionality, providing a clean UI for running crafting orders and remote control capabilities for multi-boxing setups.

## Features

### Order Mode
- **Simple UI** - Select a Lisbeth JSON file and run with one click
- **Stop Gently** - Gracefully stop Lisbeth after the current action completes
- **Remote Control** - Built-in HTTP server for controlling multiple instances over a network
- **Master Control Program** - Python GUI application to manage up to 12+ Wrangler instances simultaneously
- **Persistent Settings** - Remembers your last JSON file, window position, and preferences

### Leveling Mode (NEW)
- **Automated DoH/DoL Leveling** - Level all crafters and gatherers to 100 automatically
- **Custom Profile Interpreter** - Bypasses OrderBot for greater control and flexibility
- **Real-time Status Display** - Shows current directive, class levels, and progress
- **Missing Items Checker** - Warns about items that must be manually obtained
- **Direct Lisbeth Integration** - Executes crafting orders directly without profile system overhead

## Installation

### TheWrangler BotBase

1. Download or clone this repository
2. Copy the `BotBases/TheWrangler` folder to your RebornBuddy's `BotBases` directory:
   ```
   RebornBuddy/
   └── BotBases/
       └── TheWrangler/
           ├── TheWranglerBotBase.cs
           ├── WranglerController.cs
           ├── LisbethApi.cs
           ├── RemoteServer.cs
           ├── WranglerForm.cs
           ├── WranglerForm.Designer.cs
           └── WranglerSettings.cs
   ```
3. Launch RebornBuddy
4. Select "TheWrangler" from the bot dropdown
5. Click the Settings button to open the UI

### Wrangler Master (Multi-Instance Control)

The Wrangler Master is a standalone application for controlling multiple Wrangler instances across your network.

#### Option 1: Run from Source
```bash
cd WranglerMaster
pip install requests
python wrangler_master.py
```

#### Option 2: Build Executable
```bash
cd WranglerMaster
build.bat
```
The executable will be created in `WranglerMaster/dist/WranglerMaster.exe`

**Note:** The executable requests administrator privileges (UAC) on launch for network access.

## Usage

### Order Mode (Basic Usage)

1. Open TheWrangler UI (Settings button in RebornBuddy)
2. Click **Browse** to select a Lisbeth order JSON file
3. Click **Run** to execute the orders
4. Use **Stop Gently** to gracefully stop after the current action

### Leveling Mode

Leveling Mode automatically levels all DoH (Disciples of Hand) and DoL (Disciples of Land) classes to 100.

1. Open TheWrangler UI and select the **Leveling Mode** tab
2. Review the **Class Levels** display showing current progress
3. Click **Start Leveling** to begin
4. Monitor the **Current Directive** to see what's being executed
5. Check **Required Items** for things you need to obtain manually
6. Use **Stop** to halt leveling when needed

**Note:** Leveling Mode uses the DoH-DoL-Profiles submodule which must be initialized:
```bash
git submodule update --init --recursive
```

### Remote Control

TheWrangler includes a built-in HTTP server for remote control:

- **Port Configuration**: Set the port in the UI (default: 7800)
- **Server Status**: Green indicator shows when server is running

#### API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check (returns "ok") |
| `/status` | GET | Current status as JSON |
| `/run` | POST | Start execution with JSON body |
| `/stop` | POST | Trigger gentle stop |

#### Example: Start an Order
```bash
curl -X POST http://localhost:7800/run \
  -H "Content-Type: application/json" \
  -d '{"jsonPath": "C:/path/to/orders.json"}'
```

#### Example: Check Status
```bash
curl http://localhost:7800/status
```

### Multi-Instance Setup

1. Run RebornBuddy with TheWrangler on each machine/instance
2. Note each instance's IP address and port
3. Launch Wrangler Master
4. Click **+ Add Instance** for each Wrangler
5. Set a default JSON file (File → Set Default JSON Path)
6. Use **Start All** / **Stop All Gently** for batch control

## Configuration

Settings are saved to:
```
%APPDATA%/RebornBuddy/Settings/TheWrangler/WranglerSettings.json
```

| Setting | Default | Description |
|---------|---------|-------------|
| `LastJsonPath` | - | Last selected JSON file |
| `IgnoreHome` | false | Stay at crafting location after completion |
| `RemoteServerEnabled` | true | Enable HTTP remote control server |
| `RemoteServerPort` | 7800 | Port for remote control server |

## Requirements

- RebornBuddy (with valid license)
- Lisbeth plugin (installed and configured)
- .NET Framework 4.6.1+
- For Wrangler Master: Python 3.6+ with `requests` library

## Troubleshooting

### Remote server not starting
- Check if the port is already in use: `netstat -ano | findstr :7800`
- Try a different port in the UI
- Check Windows Firewall settings

### Cannot connect from another machine
- Add firewall rule for the port:
  ```powershell
  New-NetFirewallRule -DisplayName "TheWrangler" -Direction Inbound -Protocol TCP -LocalPort 7800 -Action Allow
  ```
- Verify the IP address with `ipconfig`

### Lisbeth errors during execution
- Ensure Lisbeth is properly installed and configured
- Check that Lisbeth can run orders normally before using TheWrangler

---

## Roadmap

The following features are planned for future development:

### Leveling Mode Enhancements
- **Lisbeth Error Recovery** - Automatic retry on "Max Sessions" and other errors
- **Progress Persistence** - Save and resume leveling progress across sessions
- **Dynamic Item Calculation** - Calculate exact quantities needed based on current levels
- **Gear Upgrade Automation** - Automatic gear upgrades at level breakpoints (21, 41, 53, 63, 70, 80, 90)
- **Diadem Integration** - Full support for Diadem leveling paths
- **Class Quest Automation** - Complete class quests automatically when available

See [LEVELING_MODE_TODO.md](LEVELING_MODE_TODO.md) for detailed implementation notes.

### Company Chest Integration
- Automatic deposit/withdrawal of materials from Free Company chest
- Smart inventory management between retainers and company storage
- Configurable rules for which items to store where

### Scheduled Operations
- Set active hours (e.g., 10:00 AM to 10:00 PM)
- Automatic start/stop based on schedule
- Day-of-week scheduling support
- Pause during specified time windows

### Crafting Analytics
- Track crafting history with timestamps
- Record time required to complete each order
- Statistics dashboard showing:
  - Items crafted per session
  - Average completion time per item type
  - Success/failure rates
  - Historical trends

### Profit Optimization
- Integration with Universalis API for market data
- Real-time price fetching across data centers
- Demand analysis based on sale velocity
- Profit expectation calculator:
  - Material cost estimation
  - Expected sale price
  - Profit margin calculation
  - Recommended items to craft based on ROI
- Market alerts for price changes

---

## License

This project is provided as-is for personal use with RebornBuddy.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
