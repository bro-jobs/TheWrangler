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
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.Navigation;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles.Tags
{



    [XmlElement("ClimbHill")]
    class ClimbHill : ProfileBehavior
    {
        #region XML Attributes
        /// <summary>
        /// Starting position.
        /// </summary>
        [XmlAttribute("Start")]
        public Vector3 StartingPoint { set; get; }

        /// <summary>
        /// Destination position.
        /// </summary>
        [XmlAttribute("End")]
        public Vector3 EndingPoint { set; get; }

        /// <summary>
        /// Movement accuracy; how close to Start and End counts as arriving there.
        /// </summary>
        [XmlAttribute("Distance")]
        [DefaultValue(1.0f)]
        public float Distance { get; set; } = 1f;

        /// <summary>
        /// <see langword="true"/> to continuously jump instead of only when blocked.
        /// Useful when stuck without expected jumps.
        /// </summary>
        [XmlAttribute("SpamJump")]
        [DefaultValue(false)]
        public bool SpamJump { get; set; } = false;

        /// <summary>
        /// <see langword="true"/> to dismount before moving between Start and End.
        /// </summary>
        [XmlAttribute("ForceDismount")]
        [DefaultValue(false)]
        public bool ForceDismount { get; set; } = false;
        #endregion XML Attributes

        private bool _isDone;
        public override bool IsDone => _isDone;

        public ClimbHill() : base() { }

        protected override void OnStart() { }

        protected override void OnDone() { }

        protected override void OnResetCachedDone()
        {
            _isDone = false;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r => ClimbHillTask());
        }

        private async Task<bool> ClimbHillTask()
        {
            if (_isDone)
            {
                await Coroutine.Yield();
                return false;
            }

            // Get to StartingPoint
            while (Core.Player.Distance(StartingPoint) > Distance)
            {
                MovementManager.MoveForwardStart();
                Core.Player.Face(StartingPoint);
                await Coroutine.Yield();
            }

            MovementManager.MoveStop();

            // Dismount if needed
            if (ForceDismount && Core.Player.IsMounted)
            {
                await CommonTasks.StopAndDismount();
            }

            // Get to EndingPoint
            while (Core.Player.Distance(EndingPoint) > Distance)
            {
                MovementManager.MoveForwardStart();
                Core.Player.Face(EndingPoint);

                var scalar = 1.0f;
                var heading = Core.Player.Heading;
                var current = Core.Player.Location;
                var lookAhead = new Vector3(
                    (float)(current.X + Math.Sin(heading) * scalar),
                    current.Y,
                    (float)(current.Z + Math.Cos(heading) * scalar)
                );

                if ((SpamJump || WorldManager.Raycast(current, lookAhead, out _)) && Core.Player.Distance(EndingPoint) > Distance)
                {
                    MovementManager.Jump();
                }

                await Coroutine.Yield();
            }

            MovementManager.MoveStop();

            _isDone = true;
            return false;
        }
    }
}
