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
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("TalkTo")]
    public class TalkToTag : ProfileBehavior
    {
        public TalkToTag()
        {
            // Defaults

        }

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


                if (IsQuestComplete)
                    return true;
                if (IsStepComplete)
                    return true;


                if (!BypassTargetChange && DoneTalking)
                {
                    return true;
                }

                return false;
            }
        }

        #endregion

        private bool DoneTalking;

        private string QuestGiver;
        protected override void OnStart()
        {
            QuestGiver = DataManager.GetLocalizedNPCName(NpcId);

            CutsceneDetection = new Decorator(r => (Talk.DialogOpen || QuestLogManager.InCutscene) && !dialogwasopen, new Action(r => { dialogwasopen = true; TalkTargetObjectId = Core.Target.ObjectId; return RunStatus.Failure; }));

            TreeHooks.Instance.InsertHook("TreeStart",0,CutsceneDetection);
        }

        protected override void OnResetCachedDone()
        {
            DoneTalking = false;
            dialogwasopen = false;
            TalkTargetObjectId = 0;
        }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }


        [XmlAttribute("InteractDistance")]
        [DefaultValue(5f)]
        public float InteractDistance { get; set; }

        [XmlAttribute("BypassTargetChange")]
        [DefaultValue(false)]
        public bool BypassTargetChange { get; set; }

        [XmlAttribute("SSO")]
        [XmlAttribute("SelectStringOverride")]
        [DefaultValue(0)]
        public int SelectStringOverride { get; set; }

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }


        public override string StatusText { get { return "Talking to " + QuestGiver; } }


        public GameObject NPC
        {
            get
            {
                var npc = GameObjectManager.GetObjectsByNPCId((uint)NpcId).FirstOrDefault(r => r.IsVisible && r.IsTargetable);
                return npc;
            }
        }

        private bool dialogwasopen;
        private uint TalkTargetObjectId;



        private Composite CutsceneDetection;

        protected override void OnDone()
        {
            TreeHooks.Instance.RemoveHook("TreeStart", CutsceneDetection);
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


                new Decorator(r => !QuestLogManager.InCutscene && SelectYesno.IsOpen, new Action(r => { SelectYesno.ClickYes(); return RunStatus.Success; })),
                new Decorator(r => SelectString.IsOpen, new Action(r => { SelectString.ClickSlot((uint)SelectStringOverride); return RunStatus.Success; })),
                new Decorator(r => SelectIconString.IsOpen, new Action(r => { SelectIconString.ClickLineEquals(QuestName); return RunStatus.Success; })),
                new Decorator(r => Talk.DialogOpen, new Action(r => {dialogwasopen = true; TalkTargetObjectId = Core.Target.ObjectId; Talk.Next(); return RunStatus.Success; })),


                //new Decorator(r => !Talk.DialogOpen && dialogwasopen && !SelectIconString.IsOpen, new Action(r => { DoneTalking = true; return RunStatus.Success; })),

                new Decorator(r => dialogwasopen && (!Core.Player.HasTarget || Core.Target?.ObjectId != TalkTargetObjectId), new Action(r => { DoneTalking = true; return RunStatus.Success; })),

                //CommonBehaviors.MoveAndStop(ret => XYZ, ret => InteractDistance, true, ret => $"[{GetType().Name}] Moving to {XYZ} so we can talk to {QuestGiver}"),
                new ActionRunCoroutine(r => moveToNpc()),
                // If we're in interact range, and the NPC/Placeable isn't here... wait 30s.
                new Decorator(ret => NPC == null, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {QuestGiver} to spawn"), new WaitContinue(5, ret => NPC != null, new Action(ret => RunStatus.Failure)))),
                new Action(ret => NPC.Interact()));
                
        }
    }
}
