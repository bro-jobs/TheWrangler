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
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("UseObject")]
    public class UseObjectTag : HuntBehavior
    {



        /// <summary>
        /// Gets the status text.
        /// </summary>
        /// <remarks>Created 2012-02-08</remarks>
        public override string StatusText { get { return $"Using object {ObjectName} for quest {QuestName}"; } }



        #region Overrides of ProfileBehavior

        



        
        private string ObjectName;
        protected override void OnStartHunt()
        {
            Log("Started");

        }

        protected override void OnDoneHunt()
        {
            Log("Done");
        }

        #endregion

        public override Composite CustomLogic
        {
            get
            {
                
                
                return new Decorator(r => (r as GameObject) != null, new PrioritySelector(
                    CommonBehaviors.MoveAndStop(r => ((GameObject)r).Location, r => UseDistance, false, r=> StatusText),
                    CreateUseObject()

                    ));
            }
        }


        private Composite CreateUseObject()
        {
            return
                new Sequence(

                    new Action(ret => Navigator.PlayerMover.MoveStop()),
                    new WaitContinue(5, ret => !MovementManager.IsMoving, new Action(ret => RunStatus.Success)),


                    new Action(ret => (ret as GameObject).Interact()),


                    new Wait(5, ret => Core.Me.IsCasting || ShortCircut((ret as GameObject)), new Action(ret => RunStatus.Success)),
                    new DecoratorContinue(r=> ShortCircut((r as GameObject)), new ActionAlwaysFail()), 
                    new DecoratorContinue(r => !Core.Player.IsCasting, new FailLogger(r => "We are not interacting for some reason!")),
                    new WaitContinue(15, ret => !Core.Me.IsCasting, new Action(ret => RunStatus.Success)),


                    new Sleep(WaitTime),
                    new DecoratorContinue(r=> BlacklistAfter, new Action(r=>Blacklist.Add(r as GameObject, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter")))
                    //new Wait(TimeSpan.FromMilliseconds(WaitTime), ret => Core.Me.IsCasting, new Action(ret => RunStatus.Success))
                    //new Action(ret => Blacklist.Add(((GameObject)ret).ObjectId, UseObjectFlag, TimeSpan.FromSeconds(BlacklistDuration)))
                //,new Action(ret => Navigator.Clear())
                );
        }

    }
}
