/*
 * PickupQuest.cs - Quest pickup interaction
 * ==========================================
 *
 * Handles picking up a quest from an NPC by navigating to them,
 * interacting, handling dialogs, and accepting the quest.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot.Managers;
using ff14bot.RemoteWindows;

namespace TheWrangler.Leveling.QuestInteractions
{
    /// <summary>
    /// Picks up a quest from an NPC.
    /// </summary>
    public class PickupQuest : QuestInteractionBase
    {
        public PickupQuest(uint npcId, uint questId, ushort zoneId, Vector3 location, int timeoutSeconds = 60)
            : base(npcId, questId, zoneId, location, timeoutSeconds)
        {
        }

        public override async Task<bool> ExecuteAsync(CancellationToken token)
        {
            Log($"Picking up quest {QuestId} from NPC {NpcId}");

            // Already have the quest?
            if (QuestLogManager.HasQuest((int)QuestId))
            {
                Log("Already have quest");
                return true;
            }

            // Navigate to NPC
            var npc = await NavigateToNpcAsync();
            if (npc == null)
                return false;

            // Interaction loop
            var timeout = DateTime.Now.AddSeconds(TimeoutSeconds);
            var interacted = false;

            while (DateTime.Now < timeout && !token.IsCancellationRequested)
            {
                // Success check
                if (QuestLogManager.HasQuest((int)QuestId))
                {
                    Log("Quest accepted!");
                    return true;
                }

                // Handle dialogs
                if (await HandleCommonDialogsAsync())
                    continue;

                if (JournalAccept.IsOpen)
                {
                    JournalAccept.Accept();
                    await Coroutine.Sleep(500);
                    continue;
                }

                if (SelectIconString.IsOpen)
                {
                    var questName = DataManager.GetLocalizedQuestName((int)QuestId);
                    SelectIconString.ClickLineEquals(questName);
                    await Coroutine.Sleep(200);
                    continue;
                }

                // Interact if no dialogs open
                if (!interacted)
                {
                    await InteractWithNpcAsync(npc);
                    interacted = true;
                    continue;
                }

                // Re-interact if dialogs closed without accepting
                if (!Talk.DialogOpen && !JournalAccept.IsOpen && !SelectString.IsOpen && !SelectYesno.IsOpen)
                {
                    interacted = false;
                }

                await Coroutine.Yield();
            }

            var result = QuestLogManager.HasQuest((int)QuestId);
            Log($"Finished, hasQuest={result}");
            return result;
        }
    }
}
