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
using ff14bot.Managers;

namespace ff14bot.NeoProfiles.Tags
{

    [XmlElement("RegisterDailies")]
    public class RegisterDailiesTag : ProfileBehavior
    {
        public override bool IsDone
        {
            get { return _isdone; }
        }



        [XmlAttribute("QuestIds")]
        public int[] QuestIds { get; set; }


        private bool _isdone = false;
        protected override void OnStart()
        {
            QuestLogManager.RegisterDailies(QuestIds);
            _isdone = true;
        }

        
    }
}
