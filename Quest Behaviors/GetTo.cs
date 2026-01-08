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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using QuickGraph;
using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Observers;
using QuickGraph.Algorithms.ShortestPath;
using SQLite;
using TreeSharp;
using Action = TreeSharp.Action;
namespace ff14bot.NeoProfiles.Tags
{

    [XmlElement("GetTo")]
    public class GetTo : ProfileBehavior
    {
        [XmlAttribute("XYZ")]
        public Vector3 XYZ { get; set; }

        [XmlAttribute("ZoneId")]
        public int ZoneId { get; set; }

        [DefaultValue(5000)]
        [XmlAttribute("Wait")]
        public int Wait { get; set; }


        private bool _generatedNodes = false;
        private bool _done;
        private bool _waiting;
        private bool _waited;
        public override bool IsDone => _done;

        public override bool HighPriority => true;

        protected override void OnResetCachedDone()
        {
            _done = false;
            _generatedNodes = false;
            FinalizedPath = null;
            _waiting = false;
            _waited = false;
            _abortCache = false;
        }

        public Queue<NavGraph.INode> FinalizedPath;
        protected override void OnStart()
        {
        }

        protected override void OnDone()
        {
        }

        private async Task<bool> Destination()
        {
            if (!Core.Player.InCombat)
            {
                _done = true;
                return true;
            }

            if (_waited)
                return false;

            Log("Reached destination, waiting for {0}ms or until combat drops.", Wait);

            _waiting = true;
            if (await Coroutine.Wait(Wait, () => !Core.Player.InCombat || AbortWaiting))
            {
                Log(!AbortWaiting ? "Combat exited, completing early." : "Unit in melee, aborting wait");
            }
            _waited = true;
            _waiting = false;
            //_done = true;
            return true;
        }

        private bool _abortCache;
        private bool AbortWaiting
        {
            get
            {

                if (_abortCache)
                    return true;

                var player = Core.Player;
                var playerLocation = player.Location;
                var playerReach = player.CombatReach;
                foreach (var attacker in GameObjectManager.Attackers)
                {
                    //Log($"{attacker} d2:{attacker.Distance2D(playerLocation)} combinedR:{attacker.CombatReach + playerReach} {attacker.Distance2D(playerLocation) < attacker.CombatReach + playerReach}");
                    if ((attacker.Distance2D(playerLocation) - (attacker.CombatReach + playerReach)) <= 5)
                    {
                        _abortCache = true;
                        return true;
                    }

                }

                return false;
            }
        }
        private async Task<bool> GenerateNodes()
        {
            var path = await NavGraph.GetPathAsync((uint)ZoneId, XYZ);
            if (path == null)
            {
                LogError($"Couldn't get a path to {XYZ} on {ZoneId}, Stopping.");
                return true;
            }
            _generatedNodes = true;
            FinalizedPath = path;
            return true;
        }

        private bool shouldBlock
        {
            get
            {

                if (_waiting)
                    return true;

                var path = FinalizedPath;
                if (path != null && path.Count > 0)
                {
                    //only ignore combat if the node we are working on is a movement node, that way we will fight something if we get in combat while teleporting
                    if (path.Peek() is NavGraph.PointNode || path.Peek() is NavGraph.ZoneTransitionNode)
                        return true;
                }

                return false;
            }
        }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                CommonBehaviors.HandleLoading,

                new Decorator(ret => FinalizedPath?.Count == 0, new ActionRunCoroutine(r => Destination())),
                new Decorator(r => !_generatedNodes, new ActionRunCoroutine(r => GenerateNodes())),
                new Decorator(ret => FinalizedPath?.Count > 0, NavGraph.NavGraphConsumer(r => FinalizedPath)),
                new Decorator(ret => shouldBlock, new ActionAlwaysSucceed())



                );
        }


    }
}
