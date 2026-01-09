/*
 * QuestInteractionBase.cs - Base class for quest NPC interactions
 * ================================================================
 *
 * Provides common functionality for quest-related NPC interactions:
 * navigation, dialog handling, and timeout management.
 *
 * Why not use ProfileBehaviors directly?
 * --------------------------------------
 * RebornBuddy's ProfileBehaviors (PickupQuestTag, TalkToTag) use ActionRunCoroutine
 * internally for movement. ActionRunCoroutine wraps async methods and relies on
 * RebornBuddy's coroutine scheduler to pump the internal Task.
 *
 * When we manually tick these composites from a BotBase's async context, the inner
 * coroutines don't advance because:
 * 1. Our await Coroutine.Yield() tells RebornBuddy to resume OUR coroutine
 * 2. The inner ActionRunCoroutine's Task has its own yield points
 * 3. RebornBuddy only knows about our coroutine, not the nested one
 *
 * Solution: Use LlamaLibrary's async navigation helpers directly, which are designed
 * to be called from async contexts and work correctly with RebornBuddy's scheduler.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using LlamaLibrary.Helpers;

namespace TheWrangler.Leveling.QuestInteractions
{
    /// <summary>
    /// Base class for quest NPC interactions with common navigation and dialog handling.
    /// </summary>
    public abstract class QuestInteractionBase
    {
        protected uint NpcId { get; }
        protected uint QuestId { get; }
        protected ushort ZoneId { get; }
        protected Vector3 Location { get; }
        protected int TimeoutSeconds { get; }

        protected QuestInteractionBase(uint npcId, uint questId, ushort zoneId, Vector3 location, int timeoutSeconds = 60)
        {
            NpcId = npcId;
            QuestId = questId;
            ZoneId = zoneId;
            Location = location;
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>
        /// Execute the interaction.
        /// </summary>
        public abstract Task<bool> ExecuteAsync(CancellationToken token);

        /// <summary>
        /// Navigate to the NPC and get within interact range.
        /// </summary>
        protected async Task<GameObject> NavigateToNpcAsync()
        {
            if (!await Navigation.GetTo(ZoneId, Location))
            {
                Log("Failed to navigate to NPC location");
                return null;
            }

            var npc = GameObjectManager.GetObjectByNPCId(NpcId);
            if (npc == null)
            {
                Log("NPC not found after navigation");
                return null;
            }

            if (!npc.IsWithinInteractRange)
            {
                await Navigation.OffMeshMoveInteract(npc);
                npc = GameObjectManager.GetObjectByNPCId(NpcId);
            }

            if (npc == null || !npc.IsWithinInteractRange)
            {
                Log("Cannot reach NPC");
                return null;
            }

            return npc;
        }

        /// <summary>
        /// Handle common dialog windows. Returns true if a dialog was handled.
        /// </summary>
        protected async Task<bool> HandleCommonDialogsAsync()
        {
            if (Talk.DialogOpen)
            {
                Talk.Next();
                await Coroutine.Sleep(200);
                return true;
            }

            if (SelectYesno.IsOpen)
            {
                SelectYesno.ClickYes();
                await Coroutine.Sleep(200);
                return true;
            }

            if (SelectString.IsOpen)
            {
                SelectString.ClickSlot(0);
                await Coroutine.Sleep(200);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Interact with the NPC.
        /// </summary>
        protected async Task InteractWithNpcAsync(GameObject npc)
        {
            npc.Target();
            npc.Interact();
            await Coroutine.Sleep(1000);
        }

        protected void Log(string message)
        {
            Logging.Write($"[{GetType().Name}] {message}");
        }
    }
}
