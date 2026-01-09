/*
 * ClassUnlocker.cs - DoH/DoL Class Unlock Sequence
 * =================================================
 *
 * Handles unlocking all DoH/DoL classes by:
 * 1. Completing prerequisite quest (talk to guild NPC)
 * 2. Picking up the unlock quest
 * 3. Turning in the unlock quest
 * 4. Changing to the new class and equipping gear
 *
 * Based on the original XML profile pattern from kagepande.
 */

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using LlamaLibrary.Helpers;
using TheWrangler.Leveling.QuestInteractions;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Handles unlocking DoH/DoL classes.
    /// </summary>
    public class ClassUnlocker
    {
        private readonly LevelingController _controller;

        public ClassUnlocker(LevelingController controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// Unlocks all locked DoH/DoL classes.
        /// </summary>
        public async Task<bool> UnlockAllClassesAsync(CancellationToken token)
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

        /// <summary>
        /// Unlocks a single class following the XML profile pattern:
        /// 1. Complete prereq quest with LLTalkTo + LLSmallTalk
        /// 2. Pickup unlock quest
        /// 3. Turn in unlock quest + LLSmallTalk
        /// 4. Wait 2s, ChangeClass, AutoInventoryEquip, Wait 5s
        /// </summary>
        private async Task<bool> UnlockClassAsync(ClassJobType job, CancellationToken token)
        {
            if (!ClassUnlockData.UnlockInfo.TryGetValue(job, out var info))
            {
                _controller.Log($"No unlock info for {job}.");
                return false;
            }

            _controller.SetDirective($"Unlocking {job}", "Starting unlock sequence...");
            _controller.Log($"Unlocking {job}...");

            // Step 1: Complete prereq quest (talk to guild NPC)
            if (!QuestLogManager.IsQuestCompleted(info.PrereqQuestId))
            {
                _controller.Log($"Completing prereq quest {info.PrereqQuestId}...");

                var talkTo = new TalkToNpc(info.PickupNpcId, info.PrereqQuestId, info.ZoneId, info.PickupLocation);
                if (!await talkTo.ExecuteAsync(token))
                {
                    _controller.Log("Failed to complete prereq quest.");
                    return false;
                }

                // Handle any remaining dialogs (LLSmallTalk WaitTime="1500")
                await GeneralFunctions.SmallTalk(1500);
            }

            // Step 2: Pickup the unlock quest
            if (!QuestLogManager.IsQuestCompleted(info.UnlockQuestId) && !QuestLogManager.HasQuest((int)info.UnlockQuestId))
            {
                _controller.Log($"Picking up unlock quest {info.UnlockQuestId}...");

                var pickup = new PickupQuest(info.PickupNpcId, info.UnlockQuestId, info.ZoneId, info.PickupLocation);
                if (!await pickup.ExecuteAsync(token))
                {
                    _controller.Log("Failed to pickup unlock quest.");
                    return false;
                }
            }

            // Step 3: Turn in the unlock quest
            if (QuestLogManager.HasQuest((int)info.UnlockQuestId))
            {
                _controller.Log($"Turning in unlock quest {info.UnlockQuestId}...");

                var turnIn = new TurnInQuest(info.TurnInNpcId, info.UnlockQuestId, info.ZoneId, info.TurnInLocation);
                if (!await turnIn.ExecuteAsync(token))
                {
                    _controller.Log("Failed to turn in unlock quest.");
                    return false;
                }

                // Handle any remaining dialogs (LLSmallTalk WaitTime="1500")
                await GeneralFunctions.SmallTalk(1500);
            }

            // Step 4: Wait, change class, equip, wait
            if (QuestLogManager.IsQuestCompleted(info.UnlockQuestId) && Core.Me.CurrentJob != job)
            {
                // WaitTimer WaitTime="2"
                await Coroutine.Sleep(2000);

                // ChangeClass
                _controller.Log($"Changing to {job}...");
                await ChangeClassAsync(job);

                // AutoInventoryEquip
                await GeneralFunctions.InventoryEquipBest(updateGearSet: true, useRecommendEquip: true);

                // WaitTimer WaitTime="5"
                await Coroutine.Sleep(5000);
            }

            var isUnlocked = Core.Me.Levels[job] > 0 || QuestLogManager.IsQuestCompleted(info.UnlockQuestId);
            _controller.Log($"{job} unlock status: {(isUnlocked ? "Success" : "Failed")}");
            return isUnlocked;
        }

        /// <summary>
        /// Changes to the specified job using gearset chat command.
        /// Handles the YesNo confirmation dialog that may appear.
        /// </summary>
        private async Task<bool> ChangeClassAsync(ClassJobType job)
        {
            if (Core.Me.CurrentJob == job)
                return true;

            ChatManager.SendChat($"/gearset change {LevelingData.GetLisbethTypeName(job)}");

            // Handle the confirmation dialog that may appear
            await Coroutine.Wait(3000, () => SelectYesno.IsOpen || Core.Me.CurrentJob == job);
            if (SelectYesno.IsOpen)
            {
                SelectYesno.Yes();
                await Coroutine.Wait(5000, () => Core.Me.CurrentJob == job);
            }

            return Core.Me.CurrentJob == job;
        }
    }
}
