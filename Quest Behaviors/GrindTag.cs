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
using System.Windows.Automation;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;

using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("Grind")]
    public class GrindTag : ProfileBehavior
    {
        [XmlAttribute("grindRef")]
        public string GrindRef { get; set; }


        [XmlAttribute("While")]
        [XmlAttribute("while")]
        public string WhileCondition { get; set; }





        public override string StatusText { get { return string.Format("Grinding {0}{1}", GrindRef, (!string.IsNullOrWhiteSpace(WhileCondition) ? " while " + WhileCondition : null)); } }

        #region Overrides of ProfileBehavior


        protected Func<bool> Condition;

        public override bool IsDone
        {
            get
            {
                if (GetCondition() != null)
                {
                    return !GetCondition()();
                }

                return false;
                //!string.IsNullOrWhiteSpace(WhileCondition) && !Condition(); 
            }
        }

        private Func<bool> GetCondition()
        {
            try
            {
                if (Condition == null)
                {
                    if (!String.IsNullOrWhiteSpace(WhileCondition))
                    {
                        Condition = ScriptManager.GetCondition(WhileCondition);
                    }
                }
                return Condition;
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
                TreeRoot.Stop("Unable to compile condition for GrindTag!");
                throw;
            }
        }

        protected override void OnStart()
        {
            HotspotManager.Clear();

            var grindArea = NeoProfileManager.CurrentProfile.GrindAreas.FirstOrDefault(ga => ga.Name == GrindRef);
            if (grindArea == null)
            {
                LogError("Could not find a grind area with the name {0}", GrindRef);
                return;
            }

            NeoProfileManager.CurrentGrindArea = grindArea;
        }

        protected override void OnDone()
        {
            NeoProfileManager.CurrentGrindArea = null;
        }

        protected override Composite CreateBehavior()
        {
            return new HookExecutor("HotspotPoi");
        }

        #endregion

    }
}
