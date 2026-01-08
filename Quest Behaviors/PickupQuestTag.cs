//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;
using Clio.XmlEngine;
using ff14bot.Pathing;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("PickupDailyQuest")]
    [XmlElement("PickUpDailyQuest")]
    [XmlElement("PickUpQuest")]
    [XmlElement("PickupQuest")]
    public class PickupQuestTag : ProfileBehavior
    {


        protected bool IsDoneOverride;
        private GameObject _cachedObject;
        private bool _missionBoardAccepted;
        

        #region Overrides of ProfileBehavior

        public override bool IsDone
        {
            get
            {
                /*if (IsQuestComplete)
                {
                    return true;
                }

                // Make sure we don't already have the quest.
               */
                if (!HasQuest)
                {
                    return false;
                }
                return true;
            }
        }

        #endregion

        private bool HasQuest
        {
            get { return ConditionParser.HasQuest(QuestId); }
        }

        private string QuestGiver;
        protected override void OnStart()
        {

            QuestGiver = DataManager.GetLocalizedNPCName(NpcId);


            NPC = GameObjectManager.GetObjectByNPCId((uint)NpcId);
            if (NPC == null)
            {
                throw new Exception("Couldn't find NPC with NPCId " + NpcId);
            }
            else
            {
                if (XYZ == Vector3.Zero)
                    XYZ = NPC.Location;
            }

            Log("Picking up quest {0}({1}) from {2} at {3}", QuestName, QuestId, QuestGiver, XYZ);
        }


        [XmlAttribute("InteractDistance")]
        [DefaultValue(5f)]
        public float InteractDistance { get; set; }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }


        [XmlAttribute("SelectStringSlot")]
        [DefaultValue(0)]
        public int SelectStringSlot { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }


        public override string StatusText { get { return "Picking up quest " + QuestName + " from " + QuestGiver; } }


        public GameObject NPC;
        private bool _throttled = false;
        private bool _interacted = false;

        protected override void OnResetCachedDone()
        {
            waiting = false;
            _throttled = false;
            _interacted = false;
        }

        private async Task<bool> throttleTask()
        {
            await Coroutine.Sleep(500);
            _throttled = true;
            return true;
        }

        private bool waiting;

        private async Task<bool> moveToNpc()
        {
            var movetoParam = new MoveToParameters(XYZ, QuestGiver) { DistanceTolerance = 7f };

            var npcObject = GameObjectManager.GetObjectByNPCId((uint)NpcId);
            if (npcObject != null && npcObject.IsTargetable && npcObject.IsVisible)
            {
                movetoParam.Location = npcObject.Location;
                return await CommonTasks.MoveAndStop(movetoParam, () => npcObject.IsWithinInteractRange, $"[{GetType().Name}] Moving to {XYZ} so we can talk to {QuestGiver}");
            }

            return await CommonTasks.MoveAndStop(movetoParam, 7f, true, $"[{GetType().Name}] Moving to {XYZ} so we can talk to {QuestGiver}");
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                ctx => NPC,


                new Decorator(r => !QuestLogManager.InCutscene && SelectYesno.IsOpen, new Action(r => { SelectYesno.ClickYes();return RunStatus.Success; })),
                new Decorator(r => Talk.DialogOpen, new Action(r => { Talk.Next(); return RunStatus.Success; })),
                new Decorator(r => SelectYesno.IsOpen, new Action(r => { SelectYesno.ClickYes(); return RunStatus.Success; })),
                new Decorator(r => SelectString.IsOpen, new Action(r => { SelectString.ClickSlot((uint)SelectStringSlot); return RunStatus.Success; })),
                new Decorator(r => SelectIconString.IsOpen, new Action(r => { SelectIconString.ClickLineEquals(QuestName); return RunStatus.Success; })),
                new Decorator(r => JournalAccept.IsOpen, new Action(r => { waiting = true; _interacted = true; JournalAccept.Accept(); return RunStatus.Success; })),

                //CommonBehaviors.MoveAndStop(ret => XYZ, ret => InteractDistance, true, ret => $"[{GetType().Name}] Moving to {XYZ} so we can pickup {QuestName} from {QuestGiver}"),
                new ActionRunCoroutine(r => moveToNpc()),
                // If we're in interact range, and the NPC/Placeable isn't here... wait 30s.
                new Decorator(ret => NPC == null, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {QuestGiver} to spawn"), new WaitContinue(5, ret => NPC != null, new Action(ret => RunStatus.Failure)))),

                //Place a guard here, previously the cutscene would be viewed to the end and thats when everything would change
                //When cutscenes are skipped we can move around while it is accepted, this guard prevents us from interacting with the npc
                //while waiting for the status to update
                new Decorator(r => (waiting && !HasQuest) || HasQuest, new ActionAlwaysSucceed()),
                new Decorator(r => !_throttled, new ActionRunCoroutine(r=>throttleTask())),
                new Decorator(ret => !_interacted, new Action(r => NPC.Interact()))




                        );
        }
    }
}
