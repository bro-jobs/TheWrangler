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
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using TreeSharp;
using Action = TreeSharp.Action;
using ff14bot.NeoProfiles;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("MoveToSafe")]
    public class MoveToSafeTag : ProfileBehavior
    {
        private bool _done;

        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }


        [DefaultValue(5f)]
        [XmlAttribute("Radius")]
        public float Radius { get; set; }

        [DefaultValue(3f)]
        [XmlAttribute("Distance")]
        public float Distance { get; set; }


        private ushort _startmap;
        protected override void OnStart()
        {
            _startmap = WorldManager.ZoneId;
        }

        public override bool IsDone => _done;


        private Vector3 _modifiedLocation = Vector3.Zero;
        private Vector3 TheLocation
        {
            get
            {
                if (_modifiedLocation != Vector3.Zero)
                {
                    return _modifiedLocation;
                }

                return XYZ;
            }
        }


        private async Task<bool> UpdateLocation()
        {
            _modifiedLocation = await XYZ.FanOutRandomAsync(Radius);
            return true;
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,
                new Decorator(r => WorldManager.ZoneId != _startmap, new Action(r => _done = true)),
                new Decorator(r=> Core.Player.Location.Distance2D(XYZ) < 25 && _modifiedLocation == Vector3.Zero, new ActionRunCoroutine(r=> UpdateLocation())),
                CommonBehaviors.MoveAndStop(ret => TheLocation, Distance, stopInRange: true, destinationName: Name),
                new Action(r => _done = true)
                );
        }

        /// <summary>
        /// This gets called when a while loop starts over so reset anything that is used inside the IsDone check
        /// </summary>
        protected override void OnResetCachedDone()
        {
            _modifiedLocation = Vector3.Zero;
            _done = false;
        }

        protected override void OnDone()
        {

            // Force a stop!
            Navigator.PlayerMover.MoveStop();
        }
    }

}