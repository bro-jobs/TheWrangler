/*
 * LevelingSequence.cs - DoH/DoL Leveling Execution Logic
 * =======================================================
 *
 * Contains the main leveling loop logic, converted from XML profiles to C#.
 * Uses LevelingData.cs for configuration and LlamaLibrary for execution.
 * Uses existing RebornBuddy ProfileBehaviors (PickupQuestTag, TurnInQuestTag)
 * instead of reimplementing quest logic.
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
using Clio.Utilities;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.NeoProfiles.Tags;
using ff14bot.RemoteWindows;
using LlamaLibrary.Helpers;
using TreeSharp;

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

            _controller.SetDirective($"Unlocking {job}", "Starting unlock sequence...");
            _controller.Log($"Unlocking {job}...");

            // Step 1: Complete prereq quest if not done (e.g., 65720 for Carpenter)
            // This is just talking to the guild NPC to "discover" the guild
            if (!QuestLogManager.IsQuestCompleted(info.PrereqQuestId))
            {
                _controller.Log($"Completing prereq quest {info.PrereqQuestId}...");

                if (!await Navigation.GetTo(info.ZoneId, info.PickupLocation))
                {
                    _controller.Log($"Failed to navigate to guild NPC.");
                    return false;
                }

                // Talk to NPC to complete prereq quest
                if (!await TalkToNpcForQuestAsync(info.PickupNpcId, info.PrereqQuestId, token))
                {
                    _controller.Log($"Failed to complete prereq quest.");
                    return false;
                }

                await Coroutine.Sleep(1500);
            }

            // Step 2: Pickup the unlock quest if not already have it or completed
            if (!QuestLogManager.IsQuestCompleted(info.UnlockQuestId) && !QuestLogManager.HasQuest((int)info.UnlockQuestId))
            {
                _controller.Log($"Picking up unlock quest {info.UnlockQuestId}...");

                if (!await Navigation.GetTo(info.ZoneId, info.PickupLocation))
                {
                    _controller.Log($"Failed to navigate to quest NPC.");
                    return false;
                }

                if (!await PickupQuestAsync(info.PickupNpcId, info.UnlockQuestId, token))
                {
                    _controller.Log($"Failed to pickup unlock quest.");
                    return false;
                }
            }

            // Step 3: Turn in the unlock quest
            if (QuestLogManager.HasQuest((int)info.UnlockQuestId))
            {
                _controller.Log($"Turning in unlock quest {info.UnlockQuestId}...");

                if (!await Navigation.GetTo(info.ZoneId, info.TurnInLocation))
                {
                    _controller.Log($"Failed to navigate to turn-in NPC.");
                    return false;
                }

                if (!await TurnInQuestAsync(info.TurnInNpcId, info.UnlockQuestId, token))
                {
                    _controller.Log($"Failed to turn in unlock quest.");
                    return false;
                }

                await Coroutine.Sleep(1500);
            }

            // Step 4: Change to the new class and equip gear
            if (QuestLogManager.IsQuestCompleted(info.UnlockQuestId) && Core.Me.CurrentJob != job)
            {
                _controller.Log($"Changing to {job}...");
                await ChangeClassAsync(job, token);
                await Coroutine.Sleep(2000);
            }

            // Verify unlock
            var isUnlocked = Core.Me.Levels[job] > 0 || QuestLogManager.IsQuestCompleted(info.UnlockQuestId);
            _controller.Log($"{job} unlock status: {(isUnlocked ? "Success" : "Failed")}");
            return isUnlocked;
        }

        /// <summary>
        /// Talks to an NPC to complete a simple talk-to quest.
        /// </summary>
        private async Task<bool> TalkToNpcForQuestAsync(uint npcId, uint questId, CancellationToken token)
        {
            var npc = GameObjectManager.GetObjectByNPCId(npcId);
            if (npc == null)
            {
                _controller.Log($"NPC {npcId} not found.");
                return false;
            }

            if (!npc.IsWithinInteractRange)
            {
                await Navigation.OffMeshMoveInteract(npc);
            }

            npc.Interact();
            await Coroutine.Yield();

            // Handle dialog until quest is completed or timeout
            var timeout = DateTime.Now.AddSeconds(30);
            while (DateTime.Now < timeout && !QuestLogManager.IsQuestCompleted(questId))
            {
                if (token.IsCancellationRequested) return false;
                await HandleDialogAsync();
                await Coroutine.Sleep(200);
            }

            return QuestLogManager.IsQuestCompleted(questId);
        }

        /// <summary>
        /// Picks up a quest from an NPC using the existing PickupQuestTag behavior.
        /// </summary>
        private async Task<bool> PickupQuestAsync(uint npcId, uint questId, CancellationToken token)
        {
            _controller.Log($"Using PickupQuestTag for quest {questId} from NPC {npcId}");

            // Get NPC location
            var npc = GameObjectManager.GetObjectByNPCId(npcId);
            var location = npc?.Location ?? Vector3.Zero;

            // Create and configure the PickupQuestTag
            var tag = new PickupQuestTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };

            // Execute the behavior
            return await ExecuteProfileBehaviorAsync(tag, () => QuestLogManager.HasQuest((int)questId), token);
        }

        /// <summary>
        /// Turns in a quest to an NPC using the existing TurnInQuestTag behavior.
        /// </summary>
        private async Task<bool> TurnInQuestAsync(uint npcId, uint questId, CancellationToken token)
        {
            _controller.Log($"Using TurnInQuestTag for quest {questId} to NPC {npcId}");

            // Get NPC location
            var npc = GameObjectManager.GetObjectByNPCId(npcId);
            var location = npc?.Location ?? Vector3.Zero;

            // Create and configure the TurnInQuestTag
            var tag = new TurnInQuestTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };

            // Execute the behavior
            return await ExecuteProfileBehaviorAsync(tag, () => QuestLogManager.IsQuestCompleted(questId), token);
        }

        /// <summary>
        /// Executes a ProfileBehavior by ticking its composite until done.
        /// This allows us to use existing quest behaviors (PickupQuestTag, TurnInQuestTag, etc.)
        /// from within a coroutine context.
        /// </summary>
        private async Task<bool> ExecuteProfileBehaviorAsync(ProfileBehavior behavior, Func<bool> successCondition, CancellationToken token)
        {
            try
            {
                // Initialize the behavior
                behavior.Start();

                // Get the composite from the behavior
                var composite = behavior.CreateBehavior();
                if (composite == null)
                {
                    _controller.Log("ProfileBehavior returned null composite");
                    return false;
                }

                var context = new object();
                var timeout = DateTime.Now.AddSeconds(60);

                // Tick the composite until done
                composite.Start(context);
                await Coroutine.Yield();

                while (!behavior.IsDone && DateTime.Now < timeout)
                {
                    if (token.IsCancellationRequested)
                    {
                        composite.Stop(context);
                        return false;
                    }

                    var status = composite.Tick(context);
                    if (status != RunStatus.Running)
                    {
                        break;
                    }

                    await Coroutine.Yield();
                }

                composite.Stop(context);
                behavior.Done();

                return successCondition();
            }
            catch (Exception ex)
            {
                _controller.Log($"ProfileBehavior execution error: {ex.Message}");
                return false;
            }
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

                if (!await PickupQuestAsync(quest.NpcId, quest.QuestId, token))
                {
                    _controller.Log($"Failed to pickup class quest.");
                    return false;
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

                if (!await TurnInQuestAsync(quest.NpcId, quest.QuestId, token))
                {
                    _controller.Log($"Failed to turn in class quest.");
                    return false;
                }
            }

            return QuestLogManager.IsQuestCompleted(quest.QuestId);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Handles common dialog windows (Talk, SelectYesno, JournalAccept, etc.)
        /// </summary>
        private async Task HandleDialogAsync()
        {
            if (Talk.DialogOpen)
            {
                Talk.Next();
                await Coroutine.Yield();
            }

            if (SelectYesno.IsOpen)
            {
                SelectYesno.ClickYes();
                await Coroutine.Yield();
            }

            if (JournalAccept.IsOpen)
            {
                JournalAccept.Accept();
                await Coroutine.Yield();
            }

            if (SelectString.IsOpen)
            {
                SelectString.ClickSlot(0);
                await Coroutine.Yield();
            }

            if (JournalResult.IsOpen)
            {
                JournalResult.Complete();
                await Coroutine.Yield();
            }
        }

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

        #endregion
    }
}
