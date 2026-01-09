# Leveling Mode - Implementation Notes and TODO

This document tracks the implementation status of Leveling Mode and provides context for future development.

## Overview

Leveling Mode automates DoH/DoL class leveling using pure C# code. No XML profile parsing is needed - all leveling logic is defined directly in C# classes.

Key features:
1. **Pure C# Implementation** - All leveling data and logic in C#
2. **Better UI** - Real-time status display showing exactly what's happening
3. **Error Recovery** - Ability to retry failed operations
4. **Direct Lisbeth Integration** - Call Lisbeth API directly

## Architecture

```
WranglerForm (UI)
    |
    v
LevelingController (Orchestration)
    |
    v
LevelingSequence (Execution)
    |
    +---> LevelingData (Grind items, quests by level)
    +---> ClassUnlockData (Class unlock quest info)
    +---> LlamaLibrary (Navigation, NPC interaction)
    +---> LisbethApi (Crafting/Gathering)
```

### Key Files

- `BotBases/TheWrangler/WranglerForm.cs` - UI with tabbed interface
- `BotBases/TheWrangler/Leveling/LevelingController.cs` - Main controller, coordinates UI and sequence
- `BotBases/TheWrangler/Leveling/LevelingSequence.cs` - Main execution loop
- `BotBases/TheWrangler/Leveling/LevelingData.cs` - Grind items and class quests data
- `BotBases/TheWrangler/Leveling/ClassUnlockData.cs` - Class unlock quest data

## Implementation Status

### Completed

- [x] Tabbed UI with Order Mode and Leveling Mode
- [x] Class levels display (CRP, BSM, ARM, GSM, LTW, WVR, ALC, CUL, MIN, BTN, FSH)
- [x] Current directive display with detail
- [x] Pure C# leveling architecture (no XML parsing)
- [x] Class unlock sequence for all DoH/DoL classes
- [x] LevelingData structure for grind items and class quests
- [x] LevelingSequence for execution

### Leveling Sequence Steps

| Step | Status | Notes |
|------|--------|-------|
| 1. Unlock Classes | Implemented | Uses ClassUnlockData, LlamaLibrary navigation |
| 2. Level Gatherers to 21 | Implemented | MIN/BTN using LevelClassTo |
| 3. Level Crafters to 21 | Implemented | All DoH classes via Lisbeth |
| 4. Level to 100 | TODO | Ishgard Diadem, higher level content |

### Data Population Status

| Class | GrindItems | ClassQuests |
|-------|------------|-------------|
| Carpenter | Sample data | Sample data |
| Blacksmith | TODO | TODO |
| Armorer | TODO | TODO |
| Goldsmith | TODO | TODO |
| Leatherworker | TODO | TODO |
| Weaver | TODO | TODO |
| Alchemist | TODO | TODO |
| Culinarian | TODO | TODO |
| Miner | TODO | TODO |
| Botanist | TODO | TODO |

## Known Issues

### 1. Max Sessions Error
When Lisbeth hits "Max Sessions reached", need error handling and retry.

**TODO:** Implement proper Lisbeth restart in LevelingSequence:
```csharp
if (lisbethError.Contains("Max Sessions"))
{
    await LisbethApi.Stop();
    await Task.Delay(5000);
    await LisbethApi.Start();
    // Retry the order
}
```

### 2. Gearset Switching
Class switching uses chat command which may not work if gearset isn't named correctly.

**TODO:** Implement gearset lookup by ClassJobType and activate directly.

## Required Items (Manual Obtain)

These items cannot be obtained by Lisbeth/RebornBuddy and must be acquired manually:

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
- Gaja Hide, Sea Swallow Skin, Almasty Fur
- Silver Lobo Hide, Hammerhead Crocodile Skin
- Br'aax Hide, Gomphotherium Skin, Rroneek Fleece
- And many more...

## Gear Breakpoints

The leveling system should handle gear upgrades at these levels:
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
1. **1-21**: Class quests + basic grinding via Lisbeth
2. **21-40**: Collectables (GC turn-ins available at 20+)
3. **41-63**: Collectables + Ishgard Restoration
4. **63-70**: Diadem gathering + Ishgard crafting
5. **70-80**: Collectables + Custom Deliveries
6. **80-90**: Studium deliveries + Collectables
7. **90-100**: Wachumeqimeqi deliveries + Leves

### Gatherers (DoL)
1. **1-21**: Class quests + normal gathering via Lisbeth
2. **21-50**: Levequests + timed nodes
3. **50-60**: Collectables
4. **60-70**: Diadem
5. **70-80**: Collectables + Custom Deliveries
6. **80-100**: Collectables + Wachumeqimeqi

## Future Enhancements

### Priority 1 (Essential)
- [ ] Populate LevelingData.GrindItems for all classes
- [ ] Populate LevelingData.ClassQuests for all classes
- [ ] Implement LevelTo100Async (Ishgard Diadem content)
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

### Adding New Grind Items

Add entries to `LevelingData.GrindItems` dictionary:
```csharp
{ ClassJobType.Carpenter, new List<(int minLevel, uint itemId, int amount)>
    {
        (1, 1000, 10),   // Level 1+: Item 1000 x10
        (5, 1001, 15),   // Level 5+: Item 1001 x15
        (10, 1002, 20),  // Level 10+: Item 1002 x20
        // ...
    }
}
```

### Adding New Class Quests

Add entries to `LevelingData.ClassQuests` dictionary:
```csharp
{ ClassJobType.Carpenter, new List<ClassQuest>
    {
        new ClassQuest { QuestId = 65675, RequiredLevel = 5, NpcId = 1000153, ... },
        new ClassQuest { QuestId = 65676, RequiredLevel = 10, NpcId = 1000153, ... },
        // ...
    }
}
```

### Testing

Testing requires in-game execution:
- Use Debug Mode tab in TheWrangler UI
- `/unlock` command to check class status
- `/test4` command to list nearby NPCs
- Manual breakpoint testing with leveling

## References

- LlamaLibrary: Navigation, NPC interaction helpers
- Lisbeth documentation (in-game help)
- ff14bot API documentation
- ClassUnlockData.cs for unlock quest IDs and NPC locations
