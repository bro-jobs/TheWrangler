/*
 * TagExecutor.cs - ProfileBehavior Execution Utility
 * ===================================================
 *
 * Provides ways to execute RebornBuddy ProfileBehaviors from coroutine context.
 *
 * Key insight: Some ProfileBehaviors (like LLTurnInTag) use synchronous composites
 * (CommonBehaviors.MoveAndStop) which work with manual ticking. Others (like
 * PickupQuestTag, TalkToTag) use ActionRunCoroutine internally which doesn't work
 * with manual ticking because the coroutine scheduler isn't pumped.
 *
 * This utility provides:
 * 1. ExecuteAsync() - For ProfileBehaviors with synchronous composites (LLTurnInTag)
 * 2. PickupQuestAsync() - Uses LlamaLibrary helpers for quest pickup
 * 3. TalkToNpcAsync() - Uses LlamaLibrary helpers for NPC dialog
 * 4. TurnInQuestAsync() - Wraps LLTurnInTag which works with ExecuteAsync
 *
 * Usage:
 *   await TagExecutor.PickupQuestAsync(npcId, questId, zoneId, location, token);
 *   await TagExecutor.TurnInQuestAsync(npcId, questId, zoneId, location, token);
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.NeoProfiles.Tags;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using LlamaLibrary.Helpers;
using TreeSharp;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Executes ProfileBehavior tags in coroutine context.
    /// </summary>
    public static class TagExecutor
    {
        private const int DefaultTimeoutSeconds = 120;

        // Cache reflection info
        private static readonly MethodInfo CreateBehaviorMethod = typeof(ProfileBehavior)
            .GetMethod("CreateBehavior", BindingFlags.Instance | BindingFlags.NonPublic);

        // LlamaUtilities types loaded at runtime
        private static Type _llTalkToType;
        private static Type _llPickUpQuestType;
        private static Type _llTurnInTagType;
        private static bool _typesLoaded;

        private static void EnsureTypesLoaded()
        {
            if (_typesLoaded) return;
            _typesLoaded = true;

            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    if (asm.FullName.Contains("LlamaUtilities"))
                    {
                        _llTalkToType = asm.GetType("LlamaUtilities.OrderbotTags.LLTalkTo");
                        _llPickUpQuestType = asm.GetType("LlamaUtilities.OrderbotTags.LLPickUpQuest");
                        _llTurnInTagType = asm.GetType("LlamaUtilities.OrderbotTags.LLTurnInTag");
                        Logging.Write($"[TagExecutor] Loaded LlamaUtilities types: TalkTo={_llTalkToType != null}, PickUp={_llPickUpQuestType != null}, TurnIn={_llTurnInTagType != null}");
                        break;
                    }
                }

                if (_llTalkToType == null)
                {
                    Logging.Write("[TagExecutor] LlamaUtilities types not found, using base RebornBuddy tags");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"[TagExecutor] Error loading LlamaUtilities types: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a TalkTo tag (LLTalkTo if available, otherwise TalkToTag).
        /// </summary>
        public static ProfileBehavior CreateTalkToTag(uint npcId, uint questId, Vector3 location)
        {
            EnsureTypesLoaded();

            if (_llTalkToType != null)
            {
                try
                {
                    var tag = Activator.CreateInstance(_llTalkToType);
                    SetTagProperties(tag, npcId, questId, location);
                    return (ProfileBehavior)tag;
                }
                catch (Exception ex)
                {
                    Logging.Write($"[TagExecutor] Error creating LLTalkTo: {ex.Message}");
                }
            }

            return new TalkToTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        /// <summary>
        /// Creates a PickupQuest tag (LLPickUpQuest if available, otherwise PickupQuestTag).
        /// </summary>
        public static ProfileBehavior CreatePickupQuestTag(uint npcId, uint questId, Vector3 location)
        {
            EnsureTypesLoaded();

            if (_llPickUpQuestType != null)
            {
                try
                {
                    var tag = Activator.CreateInstance(_llPickUpQuestType);
                    SetTagProperties(tag, npcId, questId, location);
                    return (ProfileBehavior)tag;
                }
                catch (Exception ex)
                {
                    Logging.Write($"[TagExecutor] Error creating LLPickUpQuest: {ex.Message}");
                }
            }

            return new PickupQuestTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        /// <summary>
        /// Creates a TurnIn tag (LLTurnInTag if available, otherwise TurnInTag).
        /// </summary>
        public static ProfileBehavior CreateTurnInTag(uint npcId, uint questId, Vector3 location)
        {
            EnsureTypesLoaded();

            if (_llTurnInTagType != null)
            {
                try
                {
                    var tag = Activator.CreateInstance(_llTurnInTagType);
                    SetTagProperties(tag, npcId, questId, location);
                    return (ProfileBehavior)tag;
                }
                catch (Exception ex)
                {
                    Logging.Write($"[TagExecutor] Error creating LLTurnInTag: {ex.Message}");
                }
            }

            return new TurnInTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        private static void SetTagProperties(object tag, uint npcId, uint questId, Vector3 location)
        {
            var type = tag.GetType();
            type.GetProperty("NpcId")?.SetValue(tag, (int)npcId);
            type.GetProperty("QuestId")?.SetValue(tag, (int)questId);
            type.GetProperty("XYZ")?.SetValue(tag, location);

            // Set DialogOption to empty array (DefaultValue attribute doesn't work with Activator.CreateInstance)
            var dialogOptionProp = type.GetProperty("DialogOption");
            if (dialogOptionProp != null && dialogOptionProp.GetValue(tag) == null)
            {
                dialogOptionProp.SetValue(tag, new int[0]);
            }
        }

        /// <summary>
        /// Executes a ProfileBehavior by wrapping its composite in our own coroutine.
        /// For behaviors with ActionRunCoroutine nodes, we extract and await the underlying tasks.
        /// </summary>
        public static async Task<bool> ExecuteAsync(
            ProfileBehavior behavior,
            CancellationToken token,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            return await ExecuteAsync(behavior, () => behavior.IsDone, token, timeoutSeconds);
        }

        /// <summary>
        /// Executes a ProfileBehavior with a custom success condition.
        /// This works by ticking the composite and properly handling ActionRunCoroutine nodes.
        /// </summary>
        public static async Task<bool> ExecuteAsync(
            ProfileBehavior behavior,
            Func<bool> successCondition,
            CancellationToken token,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (behavior == null)
            {
                Logging.Write("[TagExecutor] Error: behavior is null");
                return false;
            }

            if (CreateBehaviorMethod == null)
            {
                Logging.Write("[TagExecutor] Error: CreateBehaviorMethod not found via reflection");
                return false;
            }

            try
            {
                // Start the behavior (calls OnStart)
                behavior.Start();
                Logging.Write($"[TagExecutor] Starting behavior: {behavior.GetType().Name}");

                // Get the composite from the behavior
                var composite = CreateBehaviorMethod.Invoke(behavior, null) as Composite;
                if (composite == null)
                {
                    Logging.Write("[TagExecutor] Error: CreateBehavior returned null");
                    return false;
                }

                var context = new object();
                var timeout = DateTime.Now.AddSeconds(timeoutSeconds);
                var tickCount = 0;
                var lastLogTime = DateTime.Now;

                // Start the composite
                composite.Start(context);

                // Main execution loop
                while (!behavior.IsDone && DateTime.Now < timeout)
                {
                    if (token.IsCancellationRequested)
                    {
                        Logging.Write("[TagExecutor] Cancelled");
                        break;
                    }

                    // Check success condition early
                    if (successCondition())
                    {
                        Logging.Write("[TagExecutor] Success condition met");
                        break;
                    }

                    // Tick the composite
                    var status = composite.Tick(context);
                    tickCount++;

                    // Log progress every 5 seconds
                    if ((DateTime.Now - lastLogTime).TotalSeconds >= 5)
                    {
                        Logging.Write($"[TagExecutor] Running: ticks={tickCount}, status={status}, IsDone={behavior.IsDone}");
                        lastLogTime = DateTime.Now;
                    }

                    // Handle different statuses
                    if (status == RunStatus.Failure)
                    {
                        Logging.Write($"[TagExecutor] Composite returned Failure after {tickCount} ticks");
                        break;
                    }

                    if (status == RunStatus.Success)
                    {
                        // Success on one tick doesn't mean we're done - check IsDone
                        if (behavior.IsDone)
                        {
                            Logging.Write($"[TagExecutor] Behavior complete after {tickCount} ticks");
                            break;
                        }
                        // Reset and continue - the behavior needs more ticks
                        composite.Stop(context);
                        composite.Start(context);
                    }

                    // Yield to RebornBuddy's scheduler
                    await Coroutine.Yield();
                }

                if (DateTime.Now >= timeout)
                {
                    Logging.Write($"[TagExecutor] Timeout after {tickCount} ticks, IsDone={behavior.IsDone}");
                }

                // Clean up
                composite.Stop(context);
                behavior.Done();

                var result = successCondition();
                Logging.Write($"[TagExecutor] Finished after {tickCount} ticks, success={result}");
                return result;
            }
            catch (Exception ex)
            {
                Logging.Write($"[TagExecutor] ExecuteAsync error: {ex.Message}");
                Logging.Write($"[TagExecutor] Stack: {ex.StackTrace}");
                return false;
            }
        }

        #region Async Helper Methods (for tags that use ActionRunCoroutine internally)

        /// <summary>
        /// Picks up a quest using LlamaLibrary async helpers.
        /// Use this instead of ExecuteAsync for PickupQuestTag since it uses ActionRunCoroutine.
        /// </summary>
        public static async Task<bool> PickupQuestAsync(uint npcId, uint questId, ushort zoneId, Vector3 location, CancellationToken token, int timeoutSeconds = 60)
        {
            Logging.Write($"[TagExecutor] PickupQuestAsync: Quest {questId} from NPC {npcId}");

            var timeout = DateTime.Now.AddSeconds(timeoutSeconds);

            // Check if already have the quest
            if (QuestLogManager.HasQuest((int)questId))
            {
                Logging.Write("[TagExecutor] Already have quest");
                return true;
            }

            // Navigate to NPC
            if (!await Navigation.GetTo(zoneId, location))
            {
                Logging.Write("[TagExecutor] Failed to navigate to NPC");
                return false;
            }

            var npc = GameObjectManager.GetObjectByNPCId(npcId);
            if (npc == null)
            {
                Logging.Write("[TagExecutor] NPC not found after navigation");
                return false;
            }

            // Move to NPC if needed
            if (!npc.IsWithinInteractRange)
            {
                await Navigation.OffMeshMoveInteract(npc);
                npc = GameObjectManager.GetObjectByNPCId(npcId);
            }

            if (npc == null || !npc.IsWithinInteractRange)
            {
                Logging.Write("[TagExecutor] Cannot reach NPC");
                return false;
            }

            // Interact and handle dialogs
            var interacted = false;
            while (DateTime.Now < timeout && !token.IsCancellationRequested)
            {
                // Check if quest is accepted
                if (QuestLogManager.HasQuest((int)questId))
                {
                    Logging.Write("[TagExecutor] Quest accepted!");
                    return true;
                }

                // Handle dialog windows
                if (Talk.DialogOpen)
                {
                    Talk.Next();
                    await Coroutine.Sleep(200);
                    continue;
                }

                if (JournalAccept.IsOpen)
                {
                    JournalAccept.Accept();
                    await Coroutine.Sleep(500);
                    continue;
                }

                if (SelectYesno.IsOpen)
                {
                    SelectYesno.ClickYes();
                    await Coroutine.Sleep(200);
                    continue;
                }

                if (SelectString.IsOpen)
                {
                    SelectString.ClickSlot(0);
                    await Coroutine.Sleep(200);
                    continue;
                }

                if (SelectIconString.IsOpen)
                {
                    var questName = DataManager.GetLocalizedQuestName(questId);
                    SelectIconString.ClickLineEquals(questName);
                    await Coroutine.Sleep(200);
                    continue;
                }

                // Interact with NPC if no dialogs open
                if (!interacted)
                {
                    npc.Target();
                    npc.Interact();
                    interacted = true;
                    await Coroutine.Sleep(1000);
                    continue;
                }

                // If dialog closed after interacting, we might need to re-interact
                if (!Talk.DialogOpen && !JournalAccept.IsOpen && !SelectString.IsOpen && !SelectYesno.IsOpen)
                {
                    interacted = false;
                }

                await Coroutine.Yield();
            }

            var hasQuest = QuestLogManager.HasQuest((int)questId);
            Logging.Write($"[TagExecutor] PickupQuestAsync finished, hasQuest={hasQuest}");
            return hasQuest;
        }

        /// <summary>
        /// Talks to an NPC using LlamaLibrary async helpers.
        /// Use this instead of ExecuteAsync for TalkToTag since it uses ActionRunCoroutine.
        /// </summary>
        public static async Task<bool> TalkToNpcAsync(uint npcId, uint questId, ushort zoneId, Vector3 location, CancellationToken token, int timeoutSeconds = 60)
        {
            Logging.Write($"[TagExecutor] TalkToNpcAsync: Quest {questId} with NPC {npcId}");

            var timeout = DateTime.Now.AddSeconds(timeoutSeconds);

            // Check if quest is already completed
            if (QuestLogManager.IsQuestCompleted(questId))
            {
                Logging.Write("[TagExecutor] Quest already completed");
                return true;
            }

            // Navigate to NPC
            if (!await Navigation.GetTo(zoneId, location))
            {
                Logging.Write("[TagExecutor] Failed to navigate to NPC");
                return false;
            }

            var npc = GameObjectManager.GetObjectByNPCId(npcId);
            if (npc == null)
            {
                Logging.Write("[TagExecutor] NPC not found after navigation");
                return false;
            }

            // Move to NPC if needed
            if (!npc.IsWithinInteractRange)
            {
                await Navigation.OffMeshMoveInteract(npc);
                npc = GameObjectManager.GetObjectByNPCId(npcId);
            }

            if (npc == null || !npc.IsWithinInteractRange)
            {
                Logging.Write("[TagExecutor] Cannot reach NPC");
                return false;
            }

            // Interact and handle dialogs
            var interacted = false;
            var dialogSeen = false;
            while (DateTime.Now < timeout && !token.IsCancellationRequested)
            {
                // Check if quest completed
                if (QuestLogManager.IsQuestCompleted(questId))
                {
                    Logging.Write("[TagExecutor] Quest completed!");
                    return true;
                }

                // Handle dialog windows
                if (Talk.DialogOpen)
                {
                    dialogSeen = true;
                    Talk.Next();
                    await Coroutine.Sleep(200);
                    continue;
                }

                if (SelectYesno.IsOpen)
                {
                    SelectYesno.ClickYes();
                    await Coroutine.Sleep(200);
                    continue;
                }

                if (SelectString.IsOpen)
                {
                    SelectString.ClickSlot(0);
                    await Coroutine.Sleep(200);
                    continue;
                }

                // If we've seen dialog and it's now closed, we're probably done
                if (dialogSeen && !Talk.DialogOpen && !SelectYesno.IsOpen && !SelectString.IsOpen)
                {
                    Logging.Write("[TagExecutor] Dialog completed");
                    await Coroutine.Sleep(500);
                    return true;
                }

                // Interact with NPC if no dialogs open
                if (!interacted || (!Talk.DialogOpen && !SelectYesno.IsOpen && !SelectString.IsOpen && !dialogSeen))
                {
                    npc.Target();
                    npc.Interact();
                    interacted = true;
                    await Coroutine.Sleep(1000);
                    continue;
                }

                await Coroutine.Yield();
            }

            var isComplete = QuestLogManager.IsQuestCompleted(questId);
            Logging.Write($"[TagExecutor] TalkToNpcAsync finished, isComplete={isComplete}");
            return isComplete;
        }

        /// <summary>
        /// Turns in a quest using ExecuteAsync with LLTurnInTag.
        /// LLTurnInTag uses CommonBehaviors.MoveAndStop which works with manual ticking.
        /// </summary>
        public static async Task<bool> TurnInQuestAsync(uint npcId, uint questId, ushort zoneId, Vector3 location, CancellationToken token, int timeoutSeconds = 120)
        {
            Logging.Write($"[TagExecutor] TurnInQuestAsync: Quest {questId} to NPC {npcId}");

            // LLTurnInTag uses synchronous movement, so ExecuteAsync works
            var tag = CreateTurnInTag(npcId, questId, location);

            // Set the zone-aware success condition
            return await ExecuteAsync(
                tag,
                () => QuestLogManager.IsQuestCompleted(questId),
                token,
                timeoutSeconds);
        }

        #endregion
    }
}
