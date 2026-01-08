//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//
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
    [XmlElement("UseTransport")]
    public class UseTransport : ProfileBehavior
    {
        public UseTransport()
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

        private string npcName;
        protected override void OnStart()
        {
            npcName = DataManager.GetLocalizedNPCName(NpcId);
            if (QuestId > 0)
            {
                Log("Going to {0} to use transport for {1}", npcName, QuestName);
            }
            else
            {
                Log("Going to {0} to use transport option {1}", npcName, DialogOption);
            }
        }

        protected override void OnResetCachedDone()
        {
            DoneTalking = false;
            dialogwasopen = false;
        }

        [XmlAttribute("NpcId")]
        public int NpcId { get; set; }


        [DefaultValue(-1)]
        [XmlAttribute("DialogOption")]
        public int DialogOption { get; set; }

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


        public override string StatusText { get { return "Talking to " + npcName; } }


        public GameObject NPC
        {
            get
            {
                //If a position is specified find the object closest to it
                if (Position != Vector3.Zero)
                {
                    //Kinda inefficent since this will get done over and over but i dont think it will be an issue
                    var objs = GameObjectManager.GetObjectsByNPCId((uint) NpcId).OrderBy(r => r.DistanceSqr(Position));
                    return objs.FirstOrDefault();
                }
                else
                {
                    return GameObjectManager.GetObjectByNPCId((uint)NpcId);
                }
            }
        }

        private async Task<bool> moveToNpc()
        {
            var movetoParam = new MoveToParameters(XYZ, npcName) { DistanceTolerance = 7f };

            var npcObject = GameObjectManager.GetObjectByNPCId((uint)NpcId);
            if (npcObject != null && npcObject.IsTargetable && npcObject.IsVisible)
            {
                movetoParam.Location = npcObject.Location;
                return await CommonTasks.MoveAndStop(movetoParam, () => npcObject.IsWithinInteractRange, $"[{GetType().Name}] Moving to {XYZ} so we can talk to {npcName}");
            }

            return await CommonTasks.MoveAndStop(movetoParam, 7f, true, $"[{GetType().Name}] Moving to {XYZ} so we can talk to {npcName}");
        }

        private bool dialogwasopen;
        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                ctx => NPC,


                CommonBehaviors.HandleLoading,
                
                new Decorator(r => SelectIconString.IsOpen, new Action(r =>
                {

                    if (DialogOption > -1)
                    {
                        SelectIconString.ClickSlot((uint)DialogOption);
                    }
                    else
                    {
                        SelectIconString.ClickLineEquals(QuestName); 
                    }
                    
                    
                    return RunStatus.Success;
                })),

                                new Decorator(r => SelectString.IsOpen, new Action(r =>
                                {

                                    if (DialogOption > -1)
                                    {
                                        SelectString.ClickSlot((uint)DialogOption);
                                    }
                                    else
                                    {
                                        SelectString.ClickLineEquals(QuestName);
                                    }


                                    return RunStatus.Success;
                                })),


                new Decorator(r => SelectYesno.IsOpen, new Action(r => { dialogwasopen = true;  SelectYesno.ClickYes(); return RunStatus.Success; })),
                new Decorator(r => Talk.DialogOpen, new Action(r => { Talk.Next(); return RunStatus.Success; })),

                new Decorator(r => dialogwasopen && !Core.Player.HasTarget, new Action(r => { DoneTalking = true; return RunStatus.Success; })),

                //CommonBehaviors.MoveAndStop(ret => XYZ, ret => InteractDistance, true, ret => $"[{GetType().Name}] Moving to {XYZ} so we can use {npcName}'s transport {(DialogOption > -1 ? DialogOption.ToString() : QuestName)} "),
                new ActionRunCoroutine(r => moveToNpc()),
                // If we're in interact range, and the NPC/Placeable isn't here... wait 30s.
                new Decorator(ret => NPC == null, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {npcName} to spawn"), new WaitContinue(5, ret => NPC != null, new Action(ret => RunStatus.Failure)))),
                new Decorator(ret => !NPC.IsTargetable, new Sequence(new SucceedLogger(r => $"Waiting at {Core.Player.Location} for {npcName} to become targetable"), new WaitContinue(5, ret => !NPC.IsTargetable, new Action(ret => RunStatus.Failure)))),

                new Decorator(ret => NPC != null && NPC.IsTargetable, new Action(ret => NPC.Interact()))
                
                );
        }
    }
}
