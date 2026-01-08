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
using System.Windows.Media;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace ff14bot.NeoProfiles
{
    [XmlElement("TeleportTo")]

    public class TeleportTo : ProfileBehavior
    {
        private bool _done;
        [DefaultValue(0)]
        [XmlAttribute("ZoneId")]
        public int ZoneId { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("AetheryteId")]
        public int AetheryteId { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("Force")]
        public bool Force { get; set; }

        private uint aeID,zoId;
        protected override void OnStart()
        {
            if (AetheryteId > 0)
            {
                aeID = (uint) AetheryteId;
                zoId = WorldManager.GetZoneForAetheryteId((uint)AetheryteId);
                if (zoId == 0)
                {
                    Log(Colors.Orange, @"Couldnt find zone id for AetheryteId:{0}", AetheryteId);
                }
            }
            else
            {
                aeID = CheckAetheryteIds();
                zoId = (uint)ZoneId;
            }

            var locs = WorldManager.AvailableLocations;
            if (!locs.Select(r => r.AetheryteId).Contains(aeID))
            {
                if (!DataManager.AetheryteCache.ContainsKey(aeID))
                {
                    Log(Colors.Orange, "Unknown Aetheryte specified AetheryteId:{0}", aeID);
                }
                else
                {
                    Log(Colors.Orange, "Player missing AetheryteId:{0} Zone:{1}", aeID, DataManager.AetheryteCache[aeID]);
                }

                
                TreeRoot.Stop("Missing AetheryteId"); 
            }
        }

        private uint CheckAetheryteIds()
        {
            Tuple<uint, Vector3>[] ids = WorldManager.AetheryteIdsForZone((uint)ZoneId);
            var count = ids.Count();
            if (count == 0 || !WorldManager.CanTeleport())
                return 0;

            if (count == 1)
                return ids[0].Item1;

            if (count > 1)
                throw new Exception("Zone has more then one Aetheryte, please use 'AetheryteId' instead.");

            return 0;
        }

        protected override void OnResetCachedDone()
        {
            _done = false;

        }


        public GameObject Aetheryte
        {
            get { return GameObjectManager.GetObjectByNPCId<Aetheryte>(aeID); }
        }

        public override bool IsDone { get { return _done; } }

        protected override Composite CreateBehavior()
        {
            return new PrioritySelector(
                
                CommonBehaviors.HandleLoading,
                //new Decorator(r=>Core.Player.IsMounted,new Action(r=>ActionManager.Dismount())),
                //new Decorator(r=> WorldManager.ZoneId == zoId, new Action(r=>_done = true)),
                new Decorator(r => (Force && Aetheryte != null && Aetheryte.Distance2D() < 30) || (!Force && WorldManager.ZoneId == zoId), new Action(r => _done = true)),
                new Decorator(r=> !Core.Player.IsCasting,CommonBehaviors.CreateTeleportBehavior(r=>aeID,r=>zoId))
                
                );
        }

        protected override void OnDone()
        {
        }
    }
}
