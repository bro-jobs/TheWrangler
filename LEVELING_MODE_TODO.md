# Leveling Mode - Implementation Notes and TODO

This document tracks the implementation status of Leveling Mode and provides context for future development.

## Overview

Leveling Mode is a custom profile interpreter that executes the DoH-DoL-Profiles directly via TheWrangler, bypassing RebornBuddy's OrderBot profile system. This provides:

1. **Greater Control** - Dynamic item quantity calculation, better error handling
2. **Better UI** - Real-time status display showing exactly what's happening
3. **Error Recovery** - Ability to retry failed operations (e.g., Lisbeth Max Sessions)
4. **Direct Lisbeth Integration** - Call Lisbeth API directly without profile overhead

## Architecture

```
WranglerForm (UI)
    |
    v
LevelingController (Orchestration)
    |
    v
ProfileExecutor (XML Parsing & Execution)
    |
    +---> LisbethApi (Crafting/Gathering)
    +---> ff14bot APIs (Navigation, Quests, etc.)
```

### Key Files

- `BotBases/TheWrangler/WranglerForm.cs` - UI with tabbed interface
- `BotBases/TheWrangler/Leveling/LevelingController.cs` - Main controller
- `BotBases/TheWrangler/Leveling/ProfileExecutor.cs` - XML parser and executor
- `Profiles/DoH-DoL-Profiles/DoH-DoL Leveling/Start.xml` - Main profile

## Implementation Status

### Completed

- [x] Tabbed UI with Order Mode and Leveling Mode
- [x] Class levels display (CRP, BSM, ARM, GSM, LTW, WVR, ALC, CUL, MIN, BTN, FSH)
- [x] Current directive display with detail
- [x] Missing items checker (parses GrindMats.txt)
- [x] Profile XML parser with entity preprocessing
- [x] Condition evaluator (If/While with complex expressions)
- [x] Basic behavior handlers (stubs for most tags)

### Behavior Handlers - Implementation Status

| Tag | Status | Notes |
|-----|--------|-------|
| `If` | Implemented | Full condition evaluation |
| `While` | Implemented | With iteration safety limit |
| `Lisbeth` | Partial | Calls LisbethApi, needs error handling |
| `GetTo` | Stub | TODO: Implement navigation |
| `TeleportTo` | Stub | TODO: Implement teleportation |
| `ChangeClass` | Stub | TODO: Implement class switching |
| `WaitTimer` | Implemented | Working |
| `LLoadProfile` | Implemented | Loads and executes sub-profiles |
| `LLTalkTo` | Stub | TODO: Implement NPC interaction |
| `LLSmallTalk` | Implemented | Simple wait |
| `LLPickupQuest` | Stub | TODO: Implement quest pickup |
| `LLTurnIn` | Stub | TODO: Implement quest turn-in |
| `LogMessage` | Implemented | Working |
| `RunCode` | Stub | CodeChunks cannot be directly executed |
| `AutoInventoryEquip` | Stub | TODO: Implement auto-equip |

### Condition Functions - Implementation Status

| Function | Status | Notes |
|----------|--------|-------|
| `IsQuestCompleted(id)` | Implemented | Uses QuestLogManager |
| `HasQuest(id)` | Implemented | Uses QuestLogManager |
| `HasItem(id)` | Implemented | Uses InventoryManager |
| `HqHasAtLeast(id, count)` | Implemented | Checks HQ items |
| `NqHasAtLeast(id, count)` | Implemented | Checks NQ items |
| `IsQuestAcceptQualified(id)` | Partial | Basic check only |
| `GetQuestStep(id)` | Implemented | Uses QuestLogManager |
| `Core.Me.Levels[ClassJobType.X]` | Implemented | Level comparisons |
| `Core.Player.ClassLevel` | Implemented | Current class level |
| `ClassName == ClassJobType.X` | Implemented | Class comparison |

## Known Issues

### 1. CodeChunks Don't Execute
The profile contains C# code in `<CodeChunk>` elements that cannot be directly executed at runtime. This includes:

- `SetLisbethJson*` - Dynamically builds Lisbeth JSON orders
- `RestartLisbeth` - Attempts to restart Lisbeth on errors

**Workaround:** Implement equivalent functionality directly in ProfileExecutor.

### 2. Max Sessions Error
When Lisbeth hits "Max Sessions reached", the profile's CodeChunk restart logic doesn't work.

**TODO:** Implement proper Lisbeth restart in LevelingController:
```csharp
// Pseudo-code
if (lisbethError.Contains("Max Sessions"))
{
    await LisbethApi.Stop();
    await Task.Delay(5000);
    await LisbethApi.Start();
    // Retry the order
}
```

### 3. Entity Variables Not Dynamic
Profile uses `&crp;`, `&bsm;`, etc. to enable/disable class leveling. Currently these are pre-processed as static values.

**TODO:** Consider adding a configuration UI for these toggles.

## Required Items (Manual Obtain)

These items from GrindMats.txt cannot be obtained by Lisbeth/RebornBuddy and must be acquired manually:

### Early Levels (1-40)
- Aldgoat Skin x30
- Acidic Secretions x23

### Mid Levels (41-70)
- Peiste Skin x40
- Fleece x76
- Wyvern Skin x51
- Gyuki Hide x72
- Tiger Skin x174
- Bear Fat x58

### High Levels (80-100)
See GrindMats.txt for complete list including:
- Gaja Hide, Sea Swallow Skin, Almasty Fur
- Silver Lobo Hide, Hammerhead Crocodile Skin
- Br'aax Hide, Gomphotherium Skin, Rroneek Fleece
- And many more...

## Gear Breakpoints

The profile handles gear upgrades at these levels:
- Level 21: GEAR21 set
- Level 41: GEAR41 set
- Level 53: GEAR53 set
- Level 63: GEAR63 set
- Level 70: GEAR70 set (Scrip gear)
- Level 80: GEAR80 set
- Level 90: GEAR90 set

**TODO:** Implement automatic gear crafting/purchasing at these breakpoints.

## Leveling Paths

### Crafters (DoH)
1. **1-21**: Class quests + basic grinding
2. **21-40**: Collectables (GC turn-ins available at 20+)
3. **41-63**: Collectables + Ishgard Restoration
4. **63-70**: Diadem gathering + Ishgard crafting
5. **70-80**: Collectables + Custom Deliveries
6. **80-90**: Studium deliveries + Collectables
7. **90-100**: Wachumeqimeqi deliveries + Leves

### Gatherers (DoL)
1. **1-21**: Class quests + normal gathering
2. **21-50**: Levequests + timed nodes
3. **50-60**: Collectables
4. **60-70**: Diadem
5. **70-80**: Collectables + Custom Deliveries
6. **80-100**: Collectables + Wachumeqimeqi

## Future Enhancements

### Priority 1 (Essential)
- [ ] Implement actual navigation (GetTo, TeleportTo)
- [ ] Implement class switching (ChangeClass)
- [ ] Implement NPC interaction (LLTalkTo, LLPickupQuest, LLTurnIn)
- [ ] Add Lisbeth error recovery with retry logic
- [ ] Implement auto-equip for gear upgrades

### Priority 2 (Important)
- [ ] Progress persistence (save/resume)
- [ ] Dynamic item quantity calculation based on levels
- [ ] Better UI feedback during long operations
- [ ] Pause/resume functionality

### Priority 3 (Nice to Have)
- [ ] Configuration UI for class toggles
- [ ] Statistics tracking (crafts completed, time spent)
- [ ] Market board integration for material purchasing
- [ ] Multi-character support

## Development Notes

### Adding New Behavior Handlers

1. Add case in `ProfileExecutor.ExecuteElementAsync()`
2. Create `Execute[TagName]Async()` method
3. Implement actual ff14bot API calls
4. Test with isolated profile snippets

### Adding New Condition Functions

1. Add regex match in `EvaluateConditionFunction()`
2. Implement helper method for the condition
3. Handle edge cases (not in game, null checks)

### Testing

Due to dependency on ff14bot and game state, testing is limited to:
- Profile parsing validation
- Condition expression parsing
- Integration testing in-game

## References

- DoH-DoL-Profiles Wiki: https://github.com/bro-jobs/DoH-DoL-Profiles/wiki
- Lisbeth documentation (in-game help)
- ff14bot API documentation
- Quest Behaviors in `Quest Behaviors/` folder for reference implementations
