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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;

using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("Dismount")]
    public class DismountTag : ProfileBehavior
    {

        #region Overrides of ProfileBehavior


        private bool _isdone;

        public override bool IsDone
        {
            get
            {
                return _isdone;
            }
        }

        public override bool HighPriority
        {
            get { return true; }
        }

        protected override void OnStart()
        {
            

        }

        protected override void OnDone()
        {

        }

        protected override void OnResetCachedDone()
        {
            _isdone = false;
        }


        private async Task<bool> Dismount()
        {

            await CommonTasks.StopAndDismount();
            _isdone = true;
            return true;
        }
        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r => Dismount());
        }

        #endregion

    }
}
