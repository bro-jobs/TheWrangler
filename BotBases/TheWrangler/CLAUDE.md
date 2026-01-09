# Claude Development Notes for TheWrangler

This file contains development notes and testing information for AI assistants working on TheWrangler.

## RebornConsole Testing

RebornConsole is a tool for running C# snippets in the context of the running bot. It has important limitations:

### Limitations
- **No async/await** - The console doesn't support async methods
- **No Coroutine class** - `Buddy.Coroutines.Coroutine` is not available
- **Synchronous only** - All code must be synchronous

### Testing Approach
Run tests in separate steps, manually waiting between them:

```csharp
// Step 1: Execute action
var targetJob = ClassJobType.Carpenter;
var gearSets = GearsetManager.GearSets.Where(gs => gs.InUse && gs.Class == targetJob).ToList();
Log($"Found {gearSets.Count} gearset(s)");
if (gearSets.Count > 0) gearSets.First().Activate();
```

Then wait 2 seconds manually, then run:

```csharp
// Step 2: Check result
Log($"SelectYesno.IsOpen: {SelectYesno.IsOpen}");
Log($"Current job: {Core.Me.CurrentJob}");
```

If dialog appeared:
```csharp
// Step 3: Handle dialog
SelectYesno.ClickYes();
```

### Common Test Snippets

**List nearby NPCs:**
```csharp
var npcs = GameObjectManager.GameObjects
    .Where(o => o.IsVisible && o.IsTargetable && o.Type == GameObjectType.EventNpc)
    .Take(5);
foreach (var npc in npcs) {
    Log($"NPC: {npc.Name} (ID: {npc.NpcId}) - Distance: {npc.Distance()}");
}
```

**Check teleport availability:**
```csharp
Log($"Can teleport: {WorldManager.CanTeleport()}");
Log($"Current zone: {WorldManager.ZoneId}");
Log($"Available locations: {WorldManager.AvailableLocations.Length}");
```

**Teleport to location:**
```csharp
// Limsa Lominsa = 8, Gridania = 2, Ul'dah = 9
WorldManager.TeleportById(8);
```

**Check current class/gearsets:**
```csharp
Log($"Current job: {Core.Me.CurrentJob}");
Log($"Class level: {Core.Me.ClassLevel}");
var gearsets = GearsetManager.GearSets.Where(gs => gs.InUse);
foreach (var gs in gearsets.Take(10)) {
    Log($"Gearset {gs.Index}: {gs.Class} (iLvl {gs.ItemLevel})");
}
```

**Check dialog states:**
```csharp
Log($"Talk.DialogOpen: {Talk.DialogOpen}");
Log($"SelectYesno.IsOpen: {SelectYesno.IsOpen}");
Log($"SelectString.IsOpen: {SelectString.IsOpen}");
Log($"SelectIconString.IsOpen: {SelectIconString.IsOpen}");
```

**Navigation test (start movement):**
```csharp
// All in one script - variables must stay in same scope
Log($"Current location: {Core.Me.Location}");
var target = new Vector3(Core.Me.Location.X + 10, Core.Me.Location.Y, Core.Me.Location.Z);
Log($"Target: {target}");
Navigator.PlayerMover.MoveTowards(target);
Log("Moving towards target...");
```

**Navigation test (stop movement):**
```csharp
Navigator.PlayerMover.MoveStop();
Navigator.Stop();
Log($"Stopped. Final location: {Core.Me.Location}");
```

## Key ff14bot APIs

### Navigation
- `WorldManager.TeleportById(aetheryteId)` - Teleport to aetheryte
- `WorldManager.CanTeleport()` - Check if teleport is available
- `WorldManager.ZoneId` - Current zone ID
- `WorldManager.AvailableLocations` - Unlocked aetherytes
- `NavGraph.GetPathAsync(zoneId, location)` - Get navigation path
- `Navigator.PlayerMover.MoveTowards(location)` - Move toward location
- `Navigator.Stop()` - Stop movement

### Class/Gearset Management
- `Core.Me.CurrentJob` - Current ClassJobType
- `Core.Me.ClassLevel` - Current class level
- `Core.Me.Levels[ClassJobType.X]` - Level of specific class
- `GearsetManager.GearSets` - All gearsets
- `gearSet.Activate()` - Switch to gearset
- `gearSet.InUse` - Whether gearset is configured

### NPC Interaction
- `GameObjectManager.GetObjectsByNPCId(npcId)` - Find NPCs by ID
- `npc.Interact()` - Interact with NPC
- `npc.IsWithinInteractRange` - Check if in range
- `DataManager.GetLocalizedNPCName(npcId)` - Get NPC name

### Dialog Windows
- `Talk.DialogOpen` / `Talk.Next()` - Dialog text
- `SelectYesno.IsOpen` / `SelectYesno.ClickYes()` - Yes/No prompts
- `SelectString.IsOpen` / `SelectString.ClickSlot(n)` - Selection lists
- `SelectIconString.IsOpen` / `SelectIconString.ClickSlot(n)` - Icon lists
- `JournalAccept.IsOpen` / `JournalAccept.Accept()` - Quest accept
- `JournalResult.IsOpen` / `JournalResult.Complete()` - Quest turn-in
- `Request.IsOpen` - Item hand-over window

### Inventory
- `InventoryManager.FilledSlots` - All filled inventory slots
- `InventoryManager.FilledInventoryAndArmory` - Inventory + armory
- `InventoryManager.EquippedItems` - Currently equipped items
- `item.Move(targetSlot)` - Move/equip item

### Quests
- `QuestLogManager.HasQuest(questId)` - Check if quest is active
- `QuestLogManager.IsQuestCompleted(questId)` - Check if completed
- `QuestLogManager.GetQuestById(questId)` - Get quest details

## Leveling Mode Architecture

The Leveling Mode uses these key classes:

- **LevelingController** (`Leveling/LevelingController.cs`) - Orchestrates leveling process
- **ProfileExecutor** (`Leveling/ProfileExecutor.cs`) - Contains behavior handlers

### Public Methods in ProfileExecutor
These can be called directly for C#-based leveling logic:

```csharp
// Navigation
await NavigateToLocationAsync(zoneId, location, token);
await MoveToLocationAsync(location, token, tolerance);

// Class switching
await ChangeClassAsync("Carpenter", force: false, token);

// NPC interaction
await TalkToNpcAsync(npcId, xyzHint, selectSlot, token);
await PickupQuestAsync(questId, npcId, xyzHint, token);
await TurnInQuestAsync(questId, npcId, xyzHint, rewardSlot, token);

// Gear management
await AutoEquipBestGearAsync(token);
```
