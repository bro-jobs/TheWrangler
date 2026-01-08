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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    /// <summary>
    /// This tag is for use in areas where other players may be present in high numbers such as raids
    /// It will shift the provided hotspot to another place that should be navigable
    /// the requirement to use this is that the player is *VERY* close to the grindspot before the tag is started
    /// if the player is not close then the shift will fail
    ///
    /// Tl;DR; -> Always make sure to use a moveto before this tag otherwise it might do weird stuff.
    /// </summary>
    [XmlElement("GrindSafe")]
    public class GrindSafeTag : ProfileBehavior
    {
        [XmlAttribute("grindRef")]
        public string GrindRef { get; set; }


        [XmlAttribute("While")]
        [XmlAttribute("while")]
        public string WhileCondition { get; set; }

        /// <summary>
        /// The amount the hotspot should be adjusted
        /// </summary>
        [DefaultValue(5f)]
        [XmlAttribute("Distance")]
        
        public float Distance { get; set; }


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

            //override the default logic with our custom one
            TreeHooks.Instance.InsertHook("HotspotPoi",0, behavior);

            NeoProfileManager.CurrentGrindArea = grindArea;
        }

        protected override void OnDone()
        {
            TreeHooks.Instance.RemoveHook("HotspotPoi", behavior);
            NeoProfileManager.CurrentGrindArea = null;
        }


        


        private Vector3 _cachedPosition;
        private HotSpot _lastHotSpot;

        private Vector3 Location
        {
            get
            {
                return _cachedPosition;
            }
        }

        private async Task<bool> UpdateLocation()
        {
            //FanOutRandom requires the user to be near the location due to its use of raycasts
            var currentHotspot = HotspotManager.CurrentHotspot;
            _cachedPosition = await currentHotspot.ToVector3().FanOutRandomAsync(Distance);
            _lastHotSpot = currentHotspot;
            return true;
        }


        private Composite _cached;
        private Composite behavior
        {
            get
            {
                if (_cached == null)
                    _cached = new PrioritySelector(
                        new Decorator(r => _lastHotSpot != HotspotManager.CurrentHotspot, new ActionRunCoroutine(r => UpdateLocation())),
                        new Decorator(r => !Core.Player.IsMounted && Core.Player.InCombat, new HookExecutor("SetCombatPoi")),
                    CommonBehaviors.MoveAndStop(ret => Location, 2f, true, destinationName: "Hotspot"),
                    new ActionAlwaysSucceed()//Don't let the other stuff run, it'll cause it to compete to where it was going to go, sorry to other hooks!
                    );

                return _cached;
            }
        }

        protected override Composite CreateBehavior()
        {
            return new HookExecutor("HotspotPoi");
        }

        #endregion

    }
}
