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
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
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

namespace ff14bot.NeoProfiles
{
    [XmlElement("CraftAction")]
    public class CraftAction : ProfileBehavior
    {
        protected bool _IsDone;
        #region Overrides of ProfileBehavior

        public override bool IsDone
        {
            get
            {
                if (!CraftingManager.IsCrafting)
                    return true;

                return _IsDone;
            }
        }

        #endregion

        protected override void OnStart()
        {

            if (ActionId == 0 && string.IsNullOrEmpty(Name))
            {
                LogError("Either ActionId or Name must be supplied");
                return;
            }


            if (!string.IsNullOrEmpty(Name))
            {
                if (ActionManager.CurrentActions.TryGetValue(Name, out Action))
                    return;

                LogError("Couldn't locate action with name of {0}",Name);
                return;
            }

            if (!ActionManager.CurrentActions.TryGetValue(ActionId, out Action))
            {
                Action = DataManager.GetSpellData(ActionId);
                if (Action == null)
                {
                    LogError("Couldn't locate action with id of " + ActionId);
                    return;
                }
                else
                {
                    LogError("Action {0} with id {1} is currently not known.",Action.LocalizedName,ActionId);
                    return;
                }
            }
        }




        protected override void OnResetCachedDone()
        {
            _IsDone = false;
        }

        [XmlAttribute("ActionId")]
        public uint ActionId { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [DefaultValue(250)]
        [XmlAttribute("MinDelay")]
        public int MinDelay { get; set; }

        [DefaultValue(500)]
        [XmlAttribute("MaxDelay")]
        public int MaxDelay { get; set; }

        private SpellData Action;
        private static Random rng = new Random();

        public override string StatusText { get { return "Casting " + Action; } }

        private async Task<bool> CastAction()
        {
            await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => !CraftingManager.AnimationLocked);

            if (ActionManager.CanCast(Action,null))
            {
                Log("Casting {0} ({1})",Action.LocalizedName,ActionId);
                ActionManager.DoAction(Action, null);
            }
            else
            {
                LogError("Could not cast {0} ({1}), stopping bot!", Action.LocalizedName, ActionId);
                _IsDone = true;
                return true;
            }

            await Coroutine.Wait(10000, () => CraftingManager.AnimationLocked);
            await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => !CraftingManager.AnimationLocked);

            await Coroutine.Sleep(rng.Next(MinDelay,MaxDelay));

            _IsDone = true;

            return true;
        }

        private bool dialogwasopen;
        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => CastAction());
        }


        public override string ToString()
        {
            return string.Format("Craftaction id:{0} MinDelay:{1} MaxDelay:{2}", ActionId,MinDelay,MaxDelay);
        }
    }
}
