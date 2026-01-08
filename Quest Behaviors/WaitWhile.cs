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
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Helpers;
using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("WaitWhile")]
    internal class WaitWhileTag : ProfileBehavior
    {
        #region Overrides of ProfileBehavior

        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        public Func<bool> Conditional { get; set; }

        /// <summary> Gets a value indicating whether this object is done. </summary>
        /// <value> true if this object is done, false if not. </value>
        public override bool IsDone
        {
            get { return !GetConditionExec(); }
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                new Sleep(100)
                );
        }

        public bool GetConditionExec()
        {
            try
            {
                if (Conditional == null)
                    Conditional = ScriptManager.GetCondition(Condition);

                return Conditional();
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
                // Stop on syntax errors.
                TreeRoot.Stop(reason:"Error in condition.");
                throw;
            }
        }

        #endregion

    }
}
