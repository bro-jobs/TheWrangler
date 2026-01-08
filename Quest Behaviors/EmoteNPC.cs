//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("EmoteNPC")]
    public class EmoteNPCTag : HuntBehavior
    {
        public EmoteNPCTag()
        {
        }


        [XmlAttribute("Emote")]
        public string Emote { get; set; }

        /// <summary>
        /// Gets the status text.
        /// </summary>
        /// <remarks>Created 2012-02-08</remarks>
        public override string StatusText => $"Emoting at object {ObjectName} for quest {QuestName}";

        #region Overrides of ProfileBehavior

        




        public override Composite CustomCombatLogic
        {
            get { return new PrioritySelector(r => Poi.Current.BattleCharacter, CreateEmoteObject()); }
        }

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
                    new Decorator(r => Talk.DialogOpen || MovementManager.MovementLocked, new ActionAlwaysSucceed()),
                    CreateEmoteObject()

                    ));
            }
        }

        private Composite CreateEmoteObject()
        {
            return
                new Sequence(
                // Stop movement.
                    new Action(ret => Navigator.PlayerMover.MoveStop()),
                // Wait 5s for movement to stop.
                    new WaitContinue(5, ret => !MovementManager.IsMoving, new Action(ret => RunStatus.Success)),
                    new DecoratorContinue(r => Core.Player.IsMounted, new Action(r => ActionManager.Dismount())),
                    new Action(ret => (ret as GameObject)?.Target()),
                    new Sleep(1000),

                    // Wait up to 15s while casting.
                    new Action(ret => ChatManager.SendChat("/" + Emote)),
                    new Sleep(WaitTime),
                    new DecoratorContinue(r=> BlacklistAfter, new Action(r=>Blacklist.Add(r as GameObject, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter")))
                    //new Action(ret => Blacklist.Add(((GameObject)ret).ObjectId, UseObjectFlag, TimeSpan.FromSeconds(BlacklistDuration)))
                //,new Action(ret => Navigator.Clear())
                );
        }


    }
}
