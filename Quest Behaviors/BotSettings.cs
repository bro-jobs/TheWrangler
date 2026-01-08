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
using Clio.XmlEngine;
using ff14bot.BotBases;
using ff14bot.Settings;
using TreeSharp;
using Action = System.Action;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("BotSettings")]
    public class BotSettingsTag : ProfileBehavior
    {
        public override bool IsDone
        {
            get
            {
                return _isdone;
            }
        }

        private bool _isdone;


        [DefaultValue(-1)]
        [XmlAttribute("AutoEquip")]
        public int AutoEquip { get; set; }



        /// <summary>
        /// Set this to 1 to not skip cutscenes, then back to 0 when they are skippable again
        /// </summary>
        [DefaultValue(-1)]
        [XmlAttribute("BlockSkippingCutscenes")]
        public int BlockSkippingCutscenes { get; set; }



        protected override void OnResetCachedDone()
        {
            _isdone = false;

        }

        public async Task<bool> DoSettings()
        {

            if (AutoEquip != -1)
            {
                if (AutoEquip > 0)
                {
                    CharacterSettings.Instance.AutoEquip = true;
                }
                else
                {
                    CharacterSettings.Instance.AutoEquip = false;
                }

            }

            if (BlockSkippingCutscenes != -1)
            {
                if (BlockSkippingCutscenes > 0)
                {
                    OrderBot.BlockSkippingCutscenes = true;
                }
                else
                {
                    OrderBot.BlockSkippingCutscenes = false;
                }

            }


            _isdone = true;
            return false;
        }


        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r => DoSettings());
        }

    }
}
