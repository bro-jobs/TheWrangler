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
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;
namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("LogMessage")]
    public class LogMessageTag : ProfileBehavior
    {
        public override bool IsDone
        {
            get
            {
                return _isdone;
            }
        }

        private bool _isdone;
        [XmlAttribute("Message")]
        public string Message { get; set; }

        protected override void OnResetCachedDone()
        {
            _isdone = false;

        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(

                new FailLogger(r => Message),
                new Action(r => _isdone = true)
                );
        }


    }
}
