/*
 * LevelingSequence.cs - DoH/DoL Leveling Execution Logic
 * =======================================================
 *
 * Contains the main leveling loop logic, converted from XML profiles to C#.
 * Uses LevelingData.cs for configuration and LlamaLibrary for execution.
 *
 * Main sequence:
 * 1. Unlock all classes (ClassUnlockData)
 * 2. Level gatherers to 21
 * 3. Level crafters 1-21 with class quests
 * 4. Level 21-41 via Ishgard Diadem
 * 5. Continue to 100
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using LlamaLibrary.Helpers;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Executes the DoH/DoL leveling sequence.
    /// </summary>
    public class LevelingSequence
    {
        private readonly LevelingController _controller;
        private readonly LisbethApi _lisbethApi;

        public LevelingSequence(LevelingController controller)
        {
            _controller = controller;
            _lisbethApi = new LisbethApi();
            _lisbethApi.Initialize();
        }

        /// <summary>
        /// Runs the complete leveling sequence.
        /// </summary>
        public async Task<bool> RunAsync(CancellationToken token)
        {
            try
            {
                // Step 1: Unlock all DoH/DoL classes
                _controller.SetDirective("Unlocking Classes", "Checking for locked classes...");
                if (!await UnlockAllClassesAsync(token))
                {
                    _controller.Log("Failed to unlock all classes.");
                    return false;
                }

                // Step 2: Level gatherers to 21 first (needed for crafting mats)
                _controller.SetDirective("Leveling Gatherers", "Leveling MIN/BTN to 21...");
                if (!await LevelGatherersTo21Async(token))
                {
                    _controller.Log("Failed to level gatherers to 21.");
                    return false;
                }

                // Step 3: Level all crafters 1-21 with class quests
                _controller.SetDirective("Leveling Crafters", "Leveling crafters to 21...");
                if (!await LevelCraftersTo21Async(token))
                {
                    _controller.Log("Failed to level crafters to 21.");
                    return false;
                }

                // Step 4: Continue leveling to 100 (Ishgard, etc.)
                _controller.SetDirective("Leveling to 100", "Continuing leveling...");
                if (!await LevelTo100Async(token))
                {
                    _controller.Log("Failed to complete leveling to 100.");
                    return false;
                }

                _controller.Log("Leveling sequence complete!");
                return true;
            }
            catch (OperationCanceledException)
            {
                _controller.Log("Leveling cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                _controller.Log($"Leveling error: {ex.Message}");
                return false;
            }
        }

        #region Step 1: Unlock Classes

        private async Task<bool> UnlockAllClassesAsync(CancellationToken token)
        {
            var lockedClasses = ClassUnlockData.AllDohDolClasses
                .Where(job => Core.Me.Levels[job] == 0)
                .ToList();

            if (lockedClasses.Count == 0)
            {
                _controller.Log("All DoH/DoL classes are already unlocked.");
                return true;
            }

            _controller.Log($"Found {lockedClasses.Count} locked class(es): {string.Join(", ", lockedClasses)}");

            foreach (var job in lockedClasses)
            {
                if (token.IsCancellationRequested) return false;

                if (!await UnlockClassAsync(job, token))
                {
                    _controller.Log($"Failed to unlock {job}.");
                    return false;
                }

                _controller.RefreshClassLevels();
            }

            return true;
        }

        private async Task<bool> UnlockClassAsync(ClassJobType job, CancellationToken token)
        {
            if (!ClassUnlockData.UnlockInfo.TryGetValue(job, out var info))
            {
                _controller.Log($"No unlock info for {job}.");
                return false;
            }

            _controller.SetDirective($"Unlocking {job}", "Navigating to guild...");
            _controller.Log($"Unlocking {job}...");

            // Navigate to pickup NPC location
            _controller.Log($"Navigating to pickup NPC in zone {info.ZoneId}...");
            if (!await Navigation.GetTo(info.ZoneId, info.PickupLocation))
            {
                _controller.Log($"Failed to navigate to {job} guild.");
                return false;
            }

            // Find and interact with NPC
            var pickupNpc = GameObjectManager.GetObjectByNPCId(info.PickupNpcId);
            if (pickupNpc == null)
            {
                _controller.Log($"Pickup NPC {info.PickupNpcId} not found.");
                return false;
            }

            // Move into interact range if needed
            if (!pickupNpc.IsWithinInteractRange)
            {
                await Navigation.OffMeshMoveInteract(pickupNpc);
            }

            // Interact with NPC to start quest
            _controller.Log($"Interacting with pickup NPC...");
            pickupNpc.Interact();
            await Coroutine.Yield(); // Important: yield after interact to maintain context
            await Coroutine.Sleep(1000); // Wait for dialog to open

            // Handle any dialog that appears (prereq quest, guild intro, etc.)
            await HandleQuestDialogAsync(token);
            await Coroutine.Sleep(500);

            // Check if we need to pick up the unlock quest
            if (!QuestLogManager.IsQuestCompleted(info.UnlockQuestId) && !QuestLogManager.HasQuest((int)info.UnlockQuestId))
            {
                // Interact again to pick up the actual unlock quest
                pickupNpc = GameObjectManager.GetObjectByNPCId(info.PickupNpcId);
                if (pickupNpc != null)
                {
                    if (!pickupNpc.IsWithinInteractRange)
                    {
                        await Navigation.OffMeshMoveInteract(pickupNpc);
                    }
                    pickupNpc.Interact();
                    await Coroutine.Yield();
                    await Coroutine.Sleep(1000);
                    await HandleQuestDialogAsync(token);
                }
            }

            // Turn in quest if we have it
            if (QuestLogManager.HasQuest((int)info.UnlockQuestId))
            {
                _controller.Log($"Turning in unlock quest to NPC {info.TurnInNpcId}...");

                // Navigate to turn-in NPC
                if (!await Navigation.GetTo(info.ZoneId, info.TurnInLocation))
                {
                    _controller.Log($"Failed to navigate to turn-in NPC.");
                    return false;
                }

                var turnInNpc = GameObjectManager.GetObjectByNPCId(info.TurnInNpcId);
                if (turnInNpc == null)
                {
                    // Retry finding NPC
                    await Coroutine.Sleep(1000);
                    turnInNpc = GameObjectManager.GetObjectByNPCId(info.TurnInNpcId);
                }

                if (turnInNpc != null)
                {
                    if (!turnInNpc.IsWithinInteractRange)
                    {
                        await Navigation.OffMeshMoveInteract(turnInNpc);
                    }
                    turnInNpc.Interact();
                    await Coroutine.Yield();
                    await Coroutine.Sleep(1000);
                    await HandleQuestDialogAsync(token);
                }
                else
                {
                    _controller.Log($"Turn-in NPC not found.");
                    return false;
                }
            }

            // Verify unlock
            await Coroutine.Sleep(1000);
            var isUnlocked = Core.Me.Levels[job] > 0 || QuestLogManager.IsQuestCompleted(info.UnlockQuestId);
            _controller.Log($"{job} unlock status: {(isUnlocked ? "Success" : "Failed")}");
            return isUnlocked;
        }

        #endregion

        #region Step 2: Level Gatherers to 21

        private async Task<bool> LevelGatherersTo21Async(CancellationToken token)
        {
            // Level Miner to 21
            if (Core.Me.Levels[ClassJobType.Miner] < 21)
            {
                if (!await LevelClassTo(ClassJobType.Miner, 21, token))
                    return false;
            }

            // Level Botanist to 21
            if (Core.Me.Levels[ClassJobType.Botanist] < 21)
            {
                if (!await LevelClassTo(ClassJobType.Botanist, 21, token))
                    return false;
            }

            return true;
        }

        #endregion

        #region Step 3: Level Crafters to 21

        private async Task<bool> LevelCraftersTo21Async(CancellationToken token)
        {
            foreach (var job in LevelingData.CraftingClasses)
            {
                if (token.IsCancellationRequested) return false;

                if (Core.Me.Levels[job] < 21)
                {
                    if (!await LevelClassTo(job, 21, token))
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region Step 4: Level to 100

        private async Task<bool> LevelTo100Async(CancellationToken token)
        {
            // TODO: Implement higher level leveling (Ishgard Diadem, etc.)
            // For now, just log that we've reached this point
            _controller.Log("Level 21+ leveling not yet implemented.");
            _controller.Log("All classes are at level 21 or higher - manual continuation needed.");
            return true;
        }

        #endregion

        #region Core Leveling Logic

        /// <summary>
        /// Levels a class to the target level.
        /// </summary>
        private async Task<bool> LevelClassTo(ClassJobType job, int targetLevel, CancellationToken token)
        {
            _controller.SetDirective($"Leveling {job}", $"Target: Level {targetLevel}");
            _controller.Log($"Leveling {job} to {targetLevel}...");

            // Change to the class
            if (!await ChangeClassAsync(job, token))
            {
                _controller.Log($"Failed to change to {job}.");
                return false;
            }

            while (Core.Me.Levels[job] < targetLevel)
            {
                if (token.IsCancellationRequested) return false;

                var currentLevel = Core.Me.Levels[job];
                _controller.SetDirective($"Leveling {job}", $"Level {currentLevel}/{targetLevel}");

                // Check for class quests first
                var quest = LevelingData.GetNextQuest(job, currentLevel,
                    QuestLogManager.IsQuestCompleted,
                    id => QuestLogManager.HasQuest((int)id));

                if (quest != null)
                {
                    _controller.Log($"Doing class quest {quest.QuestId} at level {quest.RequiredLevel}...");
                    if (!await DoClassQuestAsync(job, quest, token))
                    {
                        _controller.Log($"Failed class quest, continuing with grind...");
                    }
                }

                // Grind using Lisbeth
                var grindOrder = LevelingData.GetGrindOrder(job, currentLevel);
                if (grindOrder != null)
                {
                    _controller.Log($"Grinding {grindOrder.Amount}x item {grindOrder.ItemId}...");
                    if (!await ExecuteLisbethOrderAsync(grindOrder.ToJson(), token))
                    {
                        _controller.Log($"Lisbeth order failed.");
                        return false;
                    }
                }
                else
                {
                    _controller.Log($"No grind order found for {job} at level {currentLevel}.");
                    break;
                }

                // Check if we leveled
                if (Core.Me.Levels[job] == currentLevel)
                {
                    _controller.Log($"No progress made, may need more orders or higher level content.");
                    break;
                }
            }

            var finalLevel = Core.Me.Levels[job];
            _controller.Log($"{job} is now level {finalLevel}.");
            return finalLevel >= targetLevel;
        }

        /// <summary>
        /// Completes a class quest.
        /// </summary>
        private async Task<bool> DoClassQuestAsync(ClassJobType job, ClassQuest quest, CancellationToken token)
        {
            // Craft required items if needed
            if (quest.TurnInItemId > 0 && quest.TurnInItemCount > 0)
            {
                var order = new LisbethOrder
                {
                    ItemId = quest.TurnInItemId,
                    Amount = quest.TurnInItemCount,
                    Type = LevelingData.GetLisbethTypeName(job),
                    Collectable = false,
                    Hq = false
                };

                _controller.Log($"Crafting {quest.TurnInItemCount}x item {quest.TurnInItemId} for quest...");
                if (!await ExecuteLisbethOrderAsync(order.ToJson(), token))
                {
                    return false;
                }
            }

            // Pick up quest
            if (!QuestLogManager.HasQuest((int)quest.QuestId) && !QuestLogManager.IsQuestCompleted(quest.QuestId))
            {
                if (!await Navigation.GetTo(quest.ZoneId, quest.NpcLocation))
                {
                    _controller.Log($"Failed to navigate to quest NPC.");
                    return false;
                }

                var npc = GameObjectManager.GetObjectByNPCId(quest.NpcId);
                if (npc != null)
                {
                    if (!npc.IsWithinInteractRange)
                        await Navigation.OffMeshMoveInteract(npc);
                    npc.Interact();
                    await Coroutine.Yield();
                    await Coroutine.Sleep(1000);
                    await HandleQuestDialogAsync(token);
                }
            }

            // Turn in quest
            if (QuestLogManager.HasQuest((int)quest.QuestId))
            {
                if (!await Navigation.GetTo(quest.ZoneId, quest.NpcLocation))
                {
                    _controller.Log($"Failed to navigate to quest turn-in NPC.");
                    return false;
                }

                var npc = GameObjectManager.GetObjectByNPCId(quest.NpcId);
                if (npc != null)
                {
                    if (!npc.IsWithinInteractRange)
                        await Navigation.OffMeshMoveInteract(npc);
                    npc.Interact();
                    await Coroutine.Yield();
                    await Coroutine.Sleep(1000);
                    await HandleQuestDialogAsync(token);
                }
            }

            return QuestLogManager.IsQuestCompleted(quest.QuestId);
        }

        #endregion

        #region Helper Methods

        private async Task<bool> ChangeClassAsync(ClassJobType job, CancellationToken token)
        {
            if (Core.Me.CurrentJob == job)
                return true;

            _controller.Log($"Changing to {job}...");

            // Use chat command to change class
            ChatManager.SendChat($"/gearset change {LevelingData.GetLisbethTypeName(job)}");
            await Coroutine.Wait(5000, () => Core.Me.CurrentJob == job);

            return Core.Me.CurrentJob == job;
        }

        private async Task<bool> ExecuteLisbethOrderAsync(string json, CancellationToken token)
        {
            try
            {
                var result = await _lisbethApi.ExecuteOrdersAsync(json, false);
                return result;
            }
            catch (Exception ex)
            {
                _controller.Log($"Lisbeth error: {ex.Message}");
                return false;
            }
        }

        private async Task HandleQuestDialogAsync(CancellationToken token)
        {
            var timeout = DateTime.Now.AddSeconds(30);

            while (DateTime.Now < timeout)
            {
                if (token.IsCancellationRequested) return;

                if (Talk.DialogOpen)
                {
                    Talk.Next();
                    await Coroutine.Wait(200, () => !Talk.DialogOpen);
                    continue;
                }

                if (SelectString.IsOpen)
                {
                    SelectString.ClickSlot(0);
                    await Coroutine.Wait(500, () => !SelectString.IsOpen);
                    continue;
                }

                if (SelectYesno.IsOpen)
                {
                    SelectYesno.ClickYes();
                    await Coroutine.Wait(500, () => !SelectYesno.IsOpen);
                    continue;
                }

                if (JournalAccept.IsOpen)
                {
                    JournalAccept.Accept();
                    await Coroutine.Wait(1000, () => !JournalAccept.IsOpen);
                    return;
                }

                if (JournalResult.IsOpen)
                {
                    if (JournalResult.ButtonClickable)
                    {
                        JournalResult.Complete();
                        await Coroutine.Wait(1000, () => !JournalResult.IsOpen);
                    }
                    return;
                }

                // No dialog open, we're done
                if (!Talk.DialogOpen && !SelectString.IsOpen && !SelectYesno.IsOpen &&
                    !JournalAccept.IsOpen && !JournalResult.IsOpen)
                {
                    break;
                }

                await Coroutine.Yield();
            }
        }

        #endregion
    }
}
