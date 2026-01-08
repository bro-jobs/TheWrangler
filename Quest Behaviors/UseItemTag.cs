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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("UseItem")]
    public class UseItemTag : HuntBehavior
    {
        public UseItemTag()
        {
        }




        [XmlAttribute("ItemId")]
        public uint ItemId { get; set; }




        /// <summary>
        /// Gets the status text.
        /// </summary>
        /// <remarks>Created 2012-02-08</remarks>
        public override string StatusText
        {
            get
            {
                if (Target != null)
                {
                    return $"UseItem at object {Target} for quest {QuestName}";
                }
                return $"Looking for target for quest: {QuestName}";
            }
        }



        #region Overrides of ProfileBehavior



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
                    CommonBehaviors.MoveAndStop(r => ((GameObject)r).Location, r => UseDistance, false, r => StatusText),
                    CreateEmoteObject()

                    ));
            }
        }

        public BagSlot Item
        {
            get
            {
                return InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == ItemId);
            }
        }

        public override Composite CustomCombatLogic
        {
            get
            {
                return new PrioritySelector(r => Poi.Current.BattleCharacter, CreateEmoteObject());
                //return new ActionRunCoroutine(r => UseItem(Poi.Current.BattleCharacter));
            }
        }

        public async Task<bool> UseItem(GameObject who)
        {

            await CommonTasks.StopAndDismount();

            if (Item == null || who == null || ShortCircut(who))
                return false;


            Log("Using {0} on {1}", Item, who);

            if (who.IsTargetable)
                who.Target();


            if (Item.Item.IsGroundTargeting)
            {
                Item.UseItem(who.Location);
            }
            else
            {
                Item.UseItem(who);
            }



            if (await Coroutine.Wait(5000, () => Core.Player.IsCasting))
            {
                if (await Coroutine.Wait(15000, () => !Core.Player.IsCasting))
                {
                    if (await Coroutine.Wait(5000, () => ShortCircut(who)))
                    {
                        LogVerbose("Stopping post-item use early, short circut was triggered.");
                    }
                }
            }
            else
            {
                LogVerbose("We are not using the item for some reason!");
                return true;
            }

            if (WaitTime > 0)
            {
                Log($"Waiting {WaitTime}ms...");
                await Coroutine.Sleep(WaitTime);
            }

            if (BlacklistAfter)
                Blacklist.Add(who, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter");

            return false;
        }


        private RunStatus StopCasting
        {
            get
            {
                if (Core.Player.IsCasting)
                {
                    ActionManager.StopCasting();
                }

                return RunStatus.Failure;
            }
        }

        private Composite CreateEmoteObject()
        {
            return
                new ActionRunCoroutine(r => UseItem(((GameObject)r)));
            new Sequence(
                // Stop movement.
                new Action(ret => Navigator.PlayerMover.MoveStop()),
                new DecoratorContinue(r => Core.Player.IsMounted, new Action(r => ActionManager.Dismount())),
                new WaitContinue(5, ret => !MovementManager.IsMoving, new Action(ret => RunStatus.Success)),
                new Sleep(1000),



                new Action(ret => ((GameObject)ret).Target()),

                //new DecoratorContinue(ret => ShortCircut((ret as GameObject)), new Action(ret => RunStatus.Failure)),
                new DecoratorContinue(r => Item != null && !Item.Item.IsGroundTargeting && r != null, new Action(ret => Item.UseItem(((GameObject)ret)))),
                new DecoratorContinue(r => Item != null && Item.Item.IsGroundTargeting && r != null, new Action(ret => Item.UseItem(((GameObject)ret).Location))),

                new Wait(5, ret => Core.Me.IsCasting || ShortCircut((ret as GameObject)), new Action(ret => RunStatus.Success)),
                new DecoratorContinue(r => ShortCircut((r as GameObject)), new Action(r => StopCasting)),
                new DecoratorContinue(r => !Core.Player.IsCasting, new FailLogger(r => "We are not interacting for some reason!")),
                new WaitContinue(15, ret => !Core.Me.IsCasting, new Action(ret => RunStatus.Success)),


                new Sleep(WaitTime),
                new DecoratorContinue(r => BlacklistAfter, new Action(r => Blacklist.Add(r as GameObject, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter")))

            //new WaitContinue(TimeSpan.FromMilliseconds(WaitTime), ret => Core.Me.IsCasting, new Action(ret => RunStatus.Success)),
            //new Action(ret => Blacklist.Add(((GameObject)ret).ObjectId, TimeSpan.FromSeconds(BlacklistDuration)))
            //,new Action(ret => Navigator.Clear())
            );
        }

    }
}
