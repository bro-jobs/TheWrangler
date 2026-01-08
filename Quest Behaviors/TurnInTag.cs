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
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("TurnIn")]
    public class TurnInTag : ProfileBehavior
    {
        public TurnInTag()
        {
            // Defaults

        }

        [XmlAttribute("RewardSlot")]
        [DefaultValue(-1)]
        public int RewardSlot { get; set; }

        [XmlAttribute("ItemIds")]
        [XmlAttribute("ItemId")]
        public int[] ItemIds { get; set; }

        [XmlAttribute("RequiresHq")]
        public bool[] RequiresHq { get; set; }

        [XmlAttribute("SSO")]
        [XmlAttribute("SelectStringOverride")]
        [DefaultValue(0)]
        public int SelectStringOverride { get; set; }

        [XmlAttribute("UseHQifNoNQ")]
        [DefaultValue(true)]
        public bool UseHQifNoNQ { get; set; }


        #region Overrides of ProfileBehavior

        public override bool IsDone
        {
            get
            {
                if (IsQuestComplete)
                    return true;
                if (IsStepComplete)
                    return true;
                return false;
            }
        }

        #endregion

        private bool DoneTalking;

        private string QuestGiver;

        struct score
        {

            public score(Reward reward)
            {
                Reward = reward;
                Value = ItemWeightsManager.GetItemWeight(reward);
            }

            public Reward Reward;
            public float Value;
        }
        QuestResult questdata;
        protected override void OnStart()
        {
            usedSlots = new HashSet<BagSlot>();
            QuestGiver = DataManager.GetLocalizedNPCName(NpcId);
            if (RewardSlot == -1)
            {
                if (QuestId > 65535)
                {
                    DataManager.QuestCache.TryGetValue((uint)QuestId, out questdata);
                }
                else
                {
                    DataManager.QuestCache.TryGetValue((ushort)QuestId, out questdata);
                }

                if (questdata != null && questdata.Rewards.Any())
                {
                    var values = questdata.Rewards.Select(r => new score(r)).OrderByDescending(r => r.Value).ToArray();

                    //If everything is valued the same cause its items that are not equipment most likely
                    if (values.Select(r => r.Value).Distinct().Count() == 1)
                    {
                        values = values.OrderByDescending(r => r.Reward.Worth).ToArray();
                    }
                    RewardSlot = questdata.Rewards.IndexOf(values[0].Reward);
                    hasrewards = true;
                    //AsmManager.JournalResult_SelectItem(window, );

                }
            }
            else
            {
                RewardSlot = RewardSlot;
                hasrewards = true;
            }



            if (RequiresHq == null)
            {
                if (ItemIds != null)
                {
                    RequiresHq = new bool[ItemIds.Length];
                }     
            }
            else
            {
                if (RequiresHq.Length != ItemIds.Length)
                {
                    LogError("RequiresHq must have the same number of items as ItemIds");
                }
            }


            Log("Turning in quest {0}({1}) from {2} at {3}", QuestName, QuestId, QuestGiver, Position);
        }

        private bool hasrewards = false;
        private bool selectedReward = false;
        


        protected override void OnResetCachedDone()
        {
            DoneTalking = false;
            dialogwasopen = false;
            _interacted = false;
            selectedReward = false;
        }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }

        [XmlAttribute("InteractDistance")]
        [DefaultValue(5f)]
        public float InteractDistance { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ
        {
            get { return Position; }
            set { Position = value; }
        }
        public Vector3 Position = Vector3.Zero;


        public override string StatusText { get { return "Talking to " + QuestGiver; } }


        public GameObject NPC
        {
            get
            {
                return GameObjectManager.GetObjectByNPCId((uint)NpcId);
            }
        }

        private bool dialogwasopen;

        private HashSet<BagSlot> usedSlots;



        private async Task<bool> AttemptHandover()
        {
            try
            {
                return await CommonTasks.HandOverRequestedItems(UseHQifNoNQ);
            }
            catch (InvalidOperationException e)
            {
                LogError("{0}", e.Message);
            }

            return false;
        }


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



        private bool _interacted = false;
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                ctx => NPC,


                new Decorator(r => !_interacted && JournalResult.IsOpen, new Action(r => { _interacted = true; return RunStatus.Failure; })),
                new Decorator(r => SelectString.IsOpen, new Action(r => { SelectString.ClickSlot((uint)SelectStringOverride); return RunStatus.Success; })),
                new Decorator(r => !QuestLogManager.InCutscene && SelectYesno.IsOpen, new Action(r => { SelectYesno.ClickYes(); return RunStatus.Success; })),
                new Decorator(r => dialogwasopen && !Talk.ConvoLock, new Action(r => { DoneTalking = true; return RunStatus.Failure; })),
                new Decorator(r => Talk.DialogOpen, new Action(r => { dialogwasopen = true; Talk.Next(); return RunStatus.Failure; })),
                new Decorator(r => Request.IsOpen, new ActionRunCoroutine(r => AttemptHandover())),
                new Decorator(r => JournalResult.IsOpen && JournalResult.ButtonClickable && (selectedReward || !hasrewards), new Action(r => JournalResult.Complete())),
                new Decorator(r => JournalResult.IsOpen && hasrewards, new Action(r =>
                {
                    selectedReward = true;
                    JournalResult.SelectSlot(RewardSlot);
                })),
                
                //new Decorator(r => !Talk.DialogOpen && dialogwasopen && !SelectIconString.IsOpen, new Action(r => { DoneTalking = true; return RunStatus.Success; })),
                new Decorator(r => SelectIconString.IsOpen, new Action(r => { SelectIconString.ClickLineEquals(QuestName); return RunStatus.Success; })),



                new Decorator(r => QuestLogManager.InCutscene, new ActionAlwaysSucceed()),


                //CommonBehaviors.MoveAndStop(ret => XYZ, ret => InteractDistance, true, ret => $"[{GetType().Name}] Moving to {XYZ} so we can turnin {QuestName} to {QuestGiver}"),
                new ActionRunCoroutine(r => moveToNpc()),

                // If we're in interact range, and the NPC/Placeable isn't here... wait 30s.
                new Decorator(ret => NPC == null, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {QuestGiver} to spawn"), new WaitContinue(5, ret => NPC != null, new Action(ret => RunStatus.Failure)))),
                new Decorator(ret => !Talk.ConvoLock && !SelectIconString.IsOpen && !_interacted, new Action(r => NPC.Interact()))



                  );
        }
    }
}
