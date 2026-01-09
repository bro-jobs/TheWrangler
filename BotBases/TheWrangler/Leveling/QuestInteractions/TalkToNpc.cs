/*
 * TalkToNpc.cs - NPC dialog interaction
 * ======================================
 *
 * Handles talking to an NPC for quest progression by navigating to them,
 * interacting, and handling dialogs until complete.
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
    /// Talks to an NPC for quest progression.
    /// </summary>
    public class TalkToNpc : QuestInteractionBase
    {
        public TalkToNpc(uint npcId, uint questId, ushort zoneId, Vector3 location, int timeoutSeconds = 60)
            : base(npcId, questId, zoneId, location, timeoutSeconds)
        {
        }

        public override async Task<bool> ExecuteAsync(CancellationToken token)
        {
            Log($"Talking to NPC {NpcId} for quest {QuestId}");

            // Quest already completed?
            if (QuestLogManager.IsQuestCompleted(QuestId))
            {
                Log("Quest already completed");
                return true;
            }

            // Navigate to NPC
            var npc = await NavigateToNpcAsync();
            if (npc == null)
                return false;

            // Interaction loop
            var timeout = DateTime.Now.AddSeconds(TimeoutSeconds);
            var interacted = false;
            var dialogSeen = false;

            while (DateTime.Now < timeout && !token.IsCancellationRequested)
            {
                // Success check
                if (QuestLogManager.IsQuestCompleted(QuestId))
                {
                    Log("Quest completed!");
                    return true;
                }

                // Handle dialogs
                if (Talk.DialogOpen)
                {
                    dialogSeen = true;
                    Talk.Next();
                    await Coroutine.Sleep(200);
                    continue;
                }

                if (await HandleCommonDialogsAsync())
                    continue;

                // Dialog completed - we're done
                if (dialogSeen && !Talk.DialogOpen && !SelectYesno.IsOpen && !SelectString.IsOpen)
                {
                    Log("Dialog completed");
                    await Coroutine.Sleep(500);
                    return true;
                }

                // Interact if no dialogs open
                if (!interacted || (!Talk.DialogOpen && !SelectYesno.IsOpen && !SelectString.IsOpen && !dialogSeen))
                {
                    await InteractWithNpcAsync(npc);
                    interacted = true;
                    continue;
                }

                await Coroutine.Yield();
            }

            var result = QuestLogManager.IsQuestCompleted(QuestId);
            Log($"Finished, isComplete={result}");
            return result;
        }
    }
}
