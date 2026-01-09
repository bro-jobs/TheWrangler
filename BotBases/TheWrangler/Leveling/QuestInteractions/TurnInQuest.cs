/*
 * TurnInQuest.cs - Quest turn-in interaction
 * ===========================================
 *
 * Handles turning in a quest to an NPC by navigating to them,
 * interacting, handling dialogs, and completing the quest.
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
    /// Turns in a quest to an NPC.
    /// </summary>
    public class TurnInQuest : QuestInteractionBase
    {
        public TurnInQuest(uint npcId, uint questId, ushort zoneId, Vector3 location, int timeoutSeconds = 120)
            : base(npcId, questId, zoneId, location, timeoutSeconds)
        {
        }

        public override async Task<bool> ExecuteAsync(CancellationToken token)
        {
            Log($"Turning in quest {QuestId} to NPC {NpcId}");

            // Quest already completed?
            if (QuestLogManager.IsQuestCompleted(QuestId))
            {
                Log("Quest already completed");
                return true;
            }

            // Don't have the quest?
            if (!QuestLogManager.HasQuest((int)QuestId))
            {
                Log("Don't have the quest to turn in");
                return false;
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
                if (QuestLogManager.IsQuestCompleted(QuestId))
                {
                    Log("Quest completed!");
                    return true;
                }

                // Handle dialogs
                if (await HandleCommonDialogsAsync())
                    continue;

                if (JournalResult.IsOpen)
                {
                    if (JournalResult.ButtonClickable)
                    {
                        JournalResult.Complete();
                        await Coroutine.Sleep(500);
                    }
                    continue;
                }

                if (SelectIconString.IsOpen)
                {
                    var questName = DataManager.GetLocalizedQuestName(QuestId);
                    SelectIconString.ClickLineEquals(questName);
                    await Coroutine.Sleep(200);
                    continue;
                }

                // Handle item handover if required
                if (Request.IsOpen)
                {
                    // Let the game's auto-handover handle this, or implement item selection
                    await Coroutine.Sleep(500);
                    continue;
                }

                // Interact if no dialogs open
                if (!interacted)
                {
                    await InteractWithNpcAsync(npc);
                    interacted = true;
                    continue;
                }

                // Re-interact if dialogs closed without completing
                if (!Talk.DialogOpen && !JournalResult.IsOpen && !SelectString.IsOpen && !SelectYesno.IsOpen && !Request.IsOpen)
                {
                    interacted = false;
                }

                await Coroutine.Yield();
            }

            var result = QuestLogManager.IsQuestCompleted(QuestId);
            Log($"Finished, isComplete={result}");
            return result;
        }
    }
}
