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
using ff14bot.BotBases;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("SelectStringOverride")]
    public class SelectStringOverride : ProfileBehavior
    {
        protected bool _IsDone;


        #region Overrides of ProfileBehavior

        public override bool IsDone
        {
            get
            {
                return _IsDone;
            }
        }

        #endregion


        [XmlAttribute("Index")]
        public int[] Index { get; set; }


        protected override void OnStart()
        {
            foreach (var index in Index)
            {
                OrderBot.StringSelectIndex.Enqueue(index);
            }

        }

        protected override void OnResetCachedDone()
        {
            _IsDone = false;
        }


        private async Task<bool> SSO()
        {
            _IsDone = true;
            return true;
        }
        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => SSO());
        }
    }
}
