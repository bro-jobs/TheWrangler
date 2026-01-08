using System;
using System.Linq;
using System.Threading.Tasks;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("LisbethTravel")]
    public class LisbethTravelBehaviour : ProfileBehavior
    {
        private object _lisbeth;
        private Func<string, Vector3, Func<bool>, bool, Task<bool>> _travelToWithArea;
        private Func<uint, uint, Vector3, Func<bool>, bool, Task<bool>> _travelTo;
        private Func<uint, Vector3, Func<bool>, bool, Task<bool>> _travelToWithoutSubzone;
        private bool _isDone;

        [XmlAttribute("Area")]
        public string Area { get; set; }

        [XmlAttribute("Position")]
        [XmlAttribute("XYZ")]
        public Vector3 Position { get; set; }

        [XmlAttribute("Zone")]
        [XmlAttribute("ZoneId")]
        public uint Zone { get; set; }

        [XmlAttribute("Subzone")]
        public uint Subzone { get; set; }

        [XmlAttribute("SkipLanding")]
        public bool SkipLanding { get; set; }

        public override bool IsDone => _isDone;

        private bool AlwaysTrue()
        {
            return true;
        }

        protected override void OnStart()
        {
            FindLisbeth();
        }

        protected override void OnResetCachedDone()
        {
            _isDone = false;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => Execute());
        }

        public async Task<bool> Execute()
        {
            if (_isDone) { return true; }

            if (_lisbeth == null)
            {
                Logging.Write("Can't start without Lisbeth.");
            }

            if (Position == Vector3.Zero)
            {
                Logging.Write("You need to specify a position.");
            }

            if (string.IsNullOrWhiteSpace(Area) && Zone == 0)
            {
                Logging.Write("You need to specify either a Lisbeth area or a zone and subzone pair.");
            }

            var result = string.IsNullOrWhiteSpace(Area) 
                ? await TravelTo(Zone, Subzone, Position, null, SkipLanding) 
                : await TravelToWithArea(Area, Position, null, SkipLanding);

            _isDone = true;
            return result;
        }

        public async Task<bool> TravelToWithArea(string area, Vector3 position, Func<bool> condition = null, bool skipLanding = false)
        {
            if (condition == null) { condition = AlwaysTrue; }

            return await _travelToWithArea(area, position, condition, skipLanding);
        }

        public async Task<bool> TravelTo(uint zoneId, uint subzoneId, Vector3 position, Func<bool> condition = null, bool skipLanding = false)
        {
            if (condition == null) { condition = AlwaysTrue; }

            return subzoneId > 0
                ? await _travelTo(zoneId, subzoneId, position, condition, skipLanding)
                : await _travelToWithoutSubzone(zoneId, position, condition, skipLanding);
        }

        private static object GetLisbethBotObject()
        {
            var loader = BotManager.Bots
                .FirstOrDefault(c => c.Name == "Lisbeth");

            if (loader == null) { return null; }

            var lisbethObjectProperty = loader.GetType().GetProperty("Lisbeth");
            var lisbeth = lisbethObjectProperty?.GetValue(loader);

            return lisbeth;
        }

        private void FindLisbeth()
        {
            var lisbeth = GetLisbethBotObject();
            if (lisbeth == null) { return; }

            var orderMethod = lisbeth.GetType().GetMethod("ExecuteOrders");
            if (orderMethod == null) { return; }

            _lisbeth = lisbeth;

            var apiObject = lisbeth.GetType().GetProperty("Api")?.GetValue(lisbeth);

            if (apiObject != null)
            {
                _travelToWithoutSubzone = (Func<uint, Vector3, Func<bool>, bool, Task<bool>>) Delegate.CreateDelegate(typeof(Func<uint, Vector3, Func<bool>, bool, Task<bool>>), apiObject, "TravelToWithoutSubzone");
                _travelTo = (Func<uint, uint, Vector3, Func<bool>, bool, Task<bool>>) Delegate.CreateDelegate(typeof(Func<uint, uint, Vector3, Func<bool>, bool, Task<bool>>), apiObject, "TravelTo");
                _travelToWithArea = (Func<string, Vector3, Func<bool>, bool, Task<bool>>) Delegate.CreateDelegate(typeof(Func<string, Vector3, Func<bool>, bool, Task<bool>>), apiObject, "TravelToWithArea");
            }

            Logging.Write("Lisbeth found.");
        }
    }
}
