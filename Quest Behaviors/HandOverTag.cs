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
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("HandOver")]
    public class HandOverTag : ProfileBehavior
    {
        public HandOverTag()
        {
            // Defaults

        }

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






        protected bool IsDoneOverride;
        private GameObject _cachedObject;
        private bool _missionBoardAccepted;


        #region Overrides of ProfileBehavior

        public override bool IsDone
        {
            get
            {
                if (IsQuestComplete)
                    return true;
                if (IsStepComplete)
                    return true;

                if (DoneTalking)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        private bool DoneTalking;

        private string QuestGiver;
        private string ItemNames;
        protected override void OnStart()
        {
            QuestGiver = DataManager.GetLocalizedNPCName(NpcId);
            usedSlots = new HashSet<BagSlot>();
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

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < ItemIds.Length; i++)
            {
                var item = DataManager.GetItem((uint)ItemIds[i], RequiresHq[i]);

                if (i == ItemIds.Length - 1)
                {
                    sb.Append($"{item.CurrentLocaleName}");
                }
                else
                {
                    sb.Append($"{item.CurrentLocaleName},");
                }


            }
            ItemNames = sb.ToString();

        }

        protected override void OnResetCachedDone()
        {
            DoneTalking = false;
            dialogwasopen = false;
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
        private HashSet<BagSlot> usedSlots; 
        private bool dialogwasopen;


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


        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                ctx => NPC,



                
                new Decorator(r => Talk.DialogOpen, new Action(r => { dialogwasopen = true; Talk.Next(); return RunStatus.Failure; })),
                new Decorator(r => Request.IsOpen, new ActionRunCoroutine(r => AttemptHandover())),
                new Decorator(r => SelectString.IsOpen, new Action(r => { SelectString.ClickSlot((uint)SelectStringOverride); return RunStatus.Success; })),

                new Decorator(r => dialogwasopen && !Core.Player.HasTarget, new Action(r => { DoneTalking = true; return RunStatus.Success; })),

                //new Decorator(r => !Talk.DialogOpen && dialogwasopen && !SelectIconString.IsOpen, new Action(r => { DoneTalking = true; return RunStatus.Success; })),
                new Decorator(r => SelectIconString.IsOpen, new Action(r => { SelectIconString.ClickLineEquals(QuestName); return RunStatus.Success; })),
                // If we're in interact range, and the NPC/Placeable isn't here... wait 30s.
                //CommonBehaviors.MoveAndStop(ret => XYZ, ret => InteractDistance, true, ret => $"[{GetType().Name}] Moving to {XYZ} so we can give {ItemNames} to {QuestGiver} for {QuestName}"),
                new ActionRunCoroutine(r => moveToNpc()),

                // If we're in interact range, and the NPC/Placeable isn't here... wait 30s.
                new Decorator(ret => NPC == null, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {QuestGiver} to spawn"), new WaitContinue(5, ret => NPC != null, new Action(ret => RunStatus.Failure)))),
                new Action(ret => NPC.Interact())
                
                
                );
        }
    }
}
