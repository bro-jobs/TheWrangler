# TheWrangler BotBase

A modular botbase for running Lisbeth crafting/gathering orders via JSON files.

## Quick Start

1. Copy the `TheWrangler` folder to your `BotBases` directory
2. Launch RebornBuddy and select "TheWrangler" from the bot dropdown
3. Click the Settings button to open the UI
4. Browse for a Lisbeth order JSON file
5. Click "Run" to execute the orders

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        RebornBuddy                              │
│                             │                                   │
│                             ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              TheWranglerBotBase.cs                       │   │
│  │  • Entry point for RebornBuddy integration               │   │
│  │  • Manages UI thread lifecycle                           │   │
│  │  • Implements BotBase interface                          │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│                           │                                     │
│            ┌──────────────┴──────────────┐                      │
│            │                             │                      │
│            ▼                             ▼                      │
│  ┌─────────────────────┐     ┌─────────────────────────┐        │
│  │  WranglerForm.cs    │     │  WranglerController.cs  │        │
│  │  • WinForms UI      │◄───►│  • Business logic       │        │
│  │  • File selection   │     │  • Coordinates API      │        │
│  │  • Run button       │     │  • Event publishing     │        │
│  └─────────────────────┘     └───────────┬─────────────┘        │
│                                          │                      │
│                                          ▼                      │
│                              ┌─────────────────────────┐        │
│                              │    LisbethApi.cs        │        │
│                              │  • Reflection-based     │        │
│                              │  • Lisbeth discovery    │        │
│                              │  • API method binding   │        │
│                              └───────────┬─────────────┘        │
│                                          │                      │
│                                          ▼                      │
│                              ┌─────────────────────────┐        │
│                              │    Lisbeth BotBase      │        │
│                              │  (External Dependency)  │        │
│                              └─────────────────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

---

## File Descriptions

### Core Files

| File | Purpose |
|------|---------|
| `TheWranglerBotBase.cs` | Main entry point. Implements RebornBuddy's `BotBase` interface. |
| `WranglerController.cs` | Business logic layer. Coordinates between UI and Lisbeth API. |
| `LisbethApi.cs` | Reflection-based wrapper for Lisbeth's internal API. |
| `WranglerSettings.cs` | Persistent settings using JsonSettings. |
| `WranglerForm.cs` | WinForms UI code-behind. |
| `WranglerForm.Designer.cs` | WinForms UI layout definition. |

### Support Files

| File | Purpose |
|------|---------|
| `TheWrangler.csproj` | .NET 4.8 project file |
| `Properties/AssemblyInfo.cs` | Assembly metadata |
| `README.md` | This documentation |

---

## Notes for Future Development (Claude)

### Key Concepts to Remember

#### 1. Reflection Pattern for Lisbeth API
Lisbeth is loaded dynamically at runtime, so we can't have compile-time references to it.
Instead, we use reflection to:
1. Find the "Lisbeth" bot in `BotManager.Bots`
2. Extract the `Lisbeth` property from the bot loader
3. Get the `Api` property from Lisbeth
4. Bind methods using `Delegate.CreateDelegate`

```csharp
// Example pattern
var loader = BotManager.Bots.FirstOrDefault(c => c.Name == "Lisbeth");
var lisbeth = loader.GetType().GetProperty("Lisbeth").GetValue(loader);
var api = lisbeth.GetType().GetProperty("Api").GetValue(lisbeth);
var method = (Func<string, Task<bool>>)Delegate.CreateDelegate(
    typeof(Func<string, Task<bool>>), api, "ExecuteOrders");
```

#### 2. WinForms Threading
WinForms requires an STA (Single-Threaded Apartment) thread. Always:
- Create forms on a new thread with `SetApartmentState(ApartmentState.STA)`
- Use `Invoke()` or `BeginInvoke()` when updating UI from other threads
- Use `Application.Run(form)` to start the message pump

#### 3. BotBase Interface
Required for RebornBuddy integration:
- `Name`: Display name in dropdown
- `Root`: Composite behavior tree (runs each tick when bot is active)
- `Start()` / `Stop()`: Lifecycle methods
- `OnButtonPress()`: Called when Settings button clicked
- `PulseFlags`: What to update each tick
- `IsAutonomous`: True for non-combat bots
- `RequiresProfile`: False if bot doesn't need OrderBot profiles

#### 4. Settings Persistence
Use `JsonSettings` base class for automatic JSON serialization:
```csharp
public class WranglerSettings : JsonSettings
{
    public static WranglerSettings Instance { get; } = new WranglerSettings();

    private WranglerSettings() : base(GetSettingsFilePath("Global", "TheWrangler.json")) { }

    [Setting]
    public string SomeSetting { get; set; }
}
```

---

## Extending TheWrangler

### Adding New Lisbeth API Methods

1. Open `LisbethApi.cs`
2. Add a private delegate field:
   ```csharp
   private Func<YourReturnType> _yourMethod;
   ```
3. Bind it in `BindApiMethods()`:
   ```csharp
   _yourMethod = CreateDelegate<Func<YourReturnType>>(apiObject, "MethodName");
   ```
4. Add a public wrapper:
   ```csharp
   public YourReturnType YourMethod() => _yourMethod?.Invoke();
   ```

### Adding New Settings

1. Open `WranglerSettings.cs`
2. Add a property with the `[Setting]` attribute:
   ```csharp
   [Setting]
   [DefaultValue("default")]
   public string NewSetting { get; set; } = "default";
   ```

### Adding New UI Elements

1. Open `WranglerForm.Designer.cs`
2. Add control declaration at bottom:
   ```csharp
   private System.Windows.Forms.Button btnNewButton;
   ```
3. Initialize in `InitializeComponent()`:
   ```csharp
   this.btnNewButton = new System.Windows.Forms.Button();
   this.btnNewButton.Location = new System.Drawing.Point(x, y);
   // ... other properties
   this.pnlMain.Controls.Add(this.btnNewButton);
   ```
4. Add event handler in `WranglerForm.cs`:
   ```csharp
   private void btnNewButton_Click(object sender, EventArgs e) { }
   ```

---

## Troubleshooting

### "Lisbeth not found"
- Ensure Lisbeth is installed in your BotBases folder
- Ensure Lisbeth appears in the bot dropdown (it must load successfully)

### UI doesn't open
- Check RebornBuddy log for exceptions
- Ensure .NET 4.8 is installed
- Try recompiling the project

### Orders don't run
- Verify JSON file format is correct
- Check if Lisbeth can run the orders directly
- Look for errors in the log output

---

## Available Lisbeth API Methods (Reference)

These methods are available on Lisbeth's API and can be bound in `LisbethApi.cs`:

| Method | Signature | Description |
|--------|-----------|-------------|
| `ExecuteOrders` | `Task<bool> ExecuteOrders(string json, bool ignoreHome)` | Main order execution |
| `GetActiveOrders` | `string GetActiveOrders()` | Get current orders as JSON |
| `GetIncompleteOrders` | `string GetIncompleteOrders()` | Get incomplete orders as JSON |
| `StopGently` | `Task StopGently()` | Gracefully stop current operation |
| `OpenWindow` | `void OpenWindow()` | Open Lisbeth's UI window |
| `EquipOptimalGear` | `Task EquipOptimalGear()` | Equip best gear for current class |
| `ExtractMateria` | `Task ExtractMateria()` | Extract spiritbound materia |
| `SelfRepair` | `Task SelfRepair()` | Repair own gear |
| `TravelTo` | `Task<bool> TravelTo(uint zone, uint subzone, Vector3 pos)` | Navigate to location |
| `Craft` | `Task Craft(bool quickSynth)` | Execute crafting |

---

## Dependencies

- .NET Framework 4.8
- RebornBuddy (ff14bot.exe)
- Lisbeth BotBase (must be installed separately)
- TreeSharp (bundled with RebornBuddy)
- Newtonsoft.Json (bundled with RebornBuddy)

---

## Version History

- **1.0.0** - Initial release
  - Basic JSON file selection
  - Run button to execute Lisbeth orders
  - Modern dark theme UI
  - Settings persistence

---

## Future Enhancement Ideas

- [ ] Auto-run mode (execute orders when bot starts)
- [ ] Order queue (run multiple JSON files in sequence)
- [ ] Order preview (show what will be crafted)
- [ ] Recent files list
- [ ] Drag-and-drop JSON files
- [ ] Order editor (create/modify JSON in UI)
- [ ] Schedule system (run orders at specific times)
- [ ] Integration with MBBuy and CompanyChest
