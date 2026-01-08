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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clio.XmlEngine;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("WaitTimer")]

    public class WaitTimerTag : ProfileBehavior
    {
        #region Overrides of ProfileBehavior

        private bool _done;
        public override bool IsDone
        {
            get
            {
                if (_done)
                    return true;
                //if (IsDone)
                //    return true;

                return false;
            }
        }

        #endregion

        [XmlAttribute("WaitTime")]
        public int WaitTime { get; set; }


        protected override void OnResetCachedDone()
        {

            _done = false;

        }

        protected override Composite CreateBehavior()
        {
            return new Sequence(
                new Sleep(WaitTime * 1000),
                new Action(ret => _done = true));
        }
    }
}
