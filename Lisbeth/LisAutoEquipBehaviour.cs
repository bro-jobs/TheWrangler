using System;
using System.Linq;
using System.Threading.Tasks;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("LisAutoEquip")]
    public class LisAutoEquipBehaviour : ProfileBehavior
    {
        private bool _isDone;
        private Func<Task> _equipOptimalGear;

        public override bool IsDone => _isDone;

        public async Task EquipOptimalGear()
        {
            await _equipOptimalGear();
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

            await EquipOptimalGear();
            _isDone = true;

            return true;
        }

        private void FindLisbeth()
        {
            var lisbeth = GetLisbethBotObject();
            if (lisbeth == null) { return; }

            var orderMethod = lisbeth.GetType().GetMethod("ExecuteOrders");
            if (orderMethod == null) { return; }

            var apiObject = lisbeth.GetType().GetProperty("Api")?.GetValue(lisbeth);

            if (apiObject != null)
            {
                _equipOptimalGear = (Func<Task>) Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "EquipOptimalGear");
            }

            Logging.Write("Lisbeth found.");
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
    }
}
