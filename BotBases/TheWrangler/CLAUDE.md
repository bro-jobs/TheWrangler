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
// Note: Navigator.PlayerMover is null in RebornConsole - use MovementManager
Log($"Current location: {Core.Me.Location}");
var target = new Vector3(Core.Me.Location.X + 10, Core.Me.Location.Y, Core.Me.Location.Z);
Log($"Target: {target}");
Core.Me.Face(target);
MovementManager.MoveForwardStart();
Log("Moving forward...");
```

**Navigation test (stop movement):**
```csharp
MovementManager.MoveForwardStop();
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
- `GameObjectManager.GameObjects` - **USE THIS** to enumerate all objects
- `GameObjectManager.GetObjectsByNPCId(npcId)` - Find NPCs by ID
- `npc.Interact()` - Interact with NPC
- `npc.IsWithinInteractRange` - Check if in range
- `DataManager.GetLocalizedNPCName(npcId)` - Get NPC name

**IMPORTANT:** Use `GameObjectManager.GameObjects` NOT `GetObjectsOfType<T>()`.
`GetObjectsOfType` returns empty results while `GameObjects` works correctly.

**Note:** `GameObjectManager.GetObjectsByNPCId(npcId)` works correctly (tested).

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

## DoH/DoL Class Unlock Quest Data

### API Testing Status
- `Core.Me.Levels[ClassJobType.X]` - **TESTED** ✓ (level > 0 = unlocked)
- `QuestLogManager.IsQuestCompleted(questId)` - **TESTED** ✓ (verified with /unlock command)

### Unlock Quest IDs and NPC Locations

Each class has a "prereq" quest (talk to NPC) and an "unlock" quest (pickup and turn in).
The unlock quest completion is what actually grants the class.

| Class | Zone | Prereq Quest | Unlock Quest | Pickup NPC | TurnIn NPC |
|-------|------|--------------|--------------|------------|------------|
| Fisher | Limsa (129) | 66670 | 66643 | 1000859 | 1000857 |
| Culinarian | Limsa (128) | 65727 | 65807 | 1000946 | 1000947 |
| Armorer | Limsa (128) | 65722 | 65809 | 1000998 | 1001000 |
| Blacksmith | Limsa (128) | 65721 | 65827 | 1000995 | 1000997 |
| Carpenter | Gridania (132) | 65720 | 65674 | 1000148 | 1000153 |
| Leatherworker | Gridania (133) | 65724 | 65641 | 1000352 | 1000691 |
| Botanist | Gridania (133) | 65729 | 65539 | 1000294 | 1000815 |
| Goldsmith | Ul'dah (131) | 65723 | 66144 | 1002280 | 1004093 |
| Weaver | Ul'dah (131) | 65725 | 66070 | 1002283 | 1003818 |
| Alchemist | Ul'dah (131) | 65726 | 66111 | 1002281 | 1002299 |
| Miner | Ul'dah (131) | 65728 | 66133 | 1002282 | 1002298 |

### NPC Locations (XYZ)

```
Fisher:       Limsa (129) - Pickup: -166.4, 4.5, 152.0 | TurnIn: -166.2, 4.5, 165.5
Culinarian:   Limsa (128) - Pickup: -61.2, 42.3, -161.9 | TurnIn: -54.5, 44.2, -149.4
Armorer:      Limsa (128) - Pickup: -50.0, 42.8, 190.4 | TurnIn: -32.8, 41.5, 207.6
Blacksmith:   Limsa (128) - Pickup: -50.2, 42.8, 192.6 | TurnIn: -32.2, 44.7, 184.6
Carpenter:    Gridania (132) - Pickup: -17.7, -3.3, 45.8 | TurnIn: -45.9, -1.3, 57.1
Leatherworker: Gridania (133) - Pickup: 63.3, 8, -145.0 | TurnIn: 71.1, 8, -165.4
Botanist:     Gridania (133) - Pickup: -237.9, 8, -145.3 | TurnIn: -233.4, 6.2, -168.7
Goldsmith:    Ul'dah (131) - Pickup: -34.2, 13.6, 98.9 | TurnIn: -25.5, 12.2, 109.8
Weaver:       Ul'dah (131) - Pickup: 136.8, 7.6, 97.8 | TurnIn: 156.7, 7.8, 100.3
Alchemist:    Ul'dah (131) - Pickup: -114.7, 41.6, 120.8 | TurnIn: -98.7, 40.2, 122.4
Miner:        Ul'dah (131) - Pickup: 1.4, 7.6, 153.7 | TurnIn: -16.5, 6.2, 157.8
```

### Checking Class Unlock Status

A class is unlocked if either:
1. `Core.Me.Levels[ClassJobType.X] > 0` - The class has a level (already unlocked)
2. `QuestLogManager.IsQuestCompleted(unlockQuestId)` - The unlock quest is completed

### RebornConsole Test: Quest Completion API

Use this to test `QuestLogManager.IsQuestCompleted`:

```csharp
// Test quest completion API with Carpenter unlock quest (65674)
Log($"Carpenter unlock quest (65674) completed: {QuestLogManager.IsQuestCompleted(65674)}");
Log($"Fisher unlock quest (66643) completed: {QuestLogManager.IsQuestCompleted(66643)}");
Log($"Miner unlock quest (66133) completed: {QuestLogManager.IsQuestCompleted(66133)}");
Log($"Botanist unlock quest (65539) completed: {QuestLogManager.IsQuestCompleted(65539)}");
```

Or use the `/unlock` debug command in TheWrangler's Debug Mode tab (requires bot to be running).
