using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("Lisbeth")]
    public class LisbethBehaviour : ProfileBehavior
    {
        private Func<string> _getActiveOrders;
        private Func<string> _getCurrentAreaName;
        private Func<HashSet<uint>> _getAllOrderItems;
        private Action<HashSet<uint>> _setTrashExclusionItems;
        private Func<string, Vector3, bool, Task<bool>> _travelToWithArea;
        private Func<uint, uint, Vector3, bool, Task<bool>> _travelTo;
        private Func<Task> _stopGently, _equipOptimalGear, _extractMateria, _selfRepair, _selfRepairWithMenderFallback;
        private Func<Task<bool>> _exitCrafting, _isProductKeyValid;
        private Func<int> _getEmptyInventorySlotCount;
        private Func<string, Task<string>> _getOrderExpansionAsJson;
        private Action<string, Func<Task>> _addHook, _addCompletionHook, _addCraftHook, _addGrindHook, _addGatherHook;
        private Action<string> _removeHook, _removeCompletionHook, _removeCraftHook, _removeGatherHook, _removeGrindHook;
        private Func<List<string>> _getHookList;
        private Func<uint, uint, Vector3, string> _getAreaName;
        private Func<Character, Task> _kill;
        private System.Action _openWindow;
        private Func<string> _getIncompleteOrders;
        private Action<string> _requestRestart;
        private Func<bool, Task> _craft;
        private Func<Task> _optimizeEquipment;

        private object _lisbeth;
        private MethodInfo _orderMethod;
        private bool _isDone;

        [XmlAttribute("IgnoreHome")]
        public bool IgnoreHome { get; set; }

        [XmlAttribute("Path")]
        public string Path { get; set; }

        [XmlAttribute("Json")]
        public string Json { get; set; }

        [XmlAttribute("TrashExclusionItems")]
        public HashSet<uint> TrashExclusionItems { get; set; }

        public override bool IsDone => _isDone;

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
            if (_lisbeth == null || _orderMethod == null)
            {
                Logging.Write("Can't start without Lisbeth.");
            }

            string json = null;
            if (!string.IsNullOrWhiteSpace(Json))
            {
                json = Json;
            }
            else if (!string.IsNullOrWhiteSpace(Path))
            {
                try { json = File.ReadAllText(Path); }
                catch (Exception e) { Logging.Write(e); }
            }
            else
            {
                Logging.Write("Can't start without orders.");
            }

            if (TrashExclusionItems != null)
            {
                _setTrashExclusionItems?.Invoke(TrashExclusionItems);
            }

            var result = await ExecuteOrders(json);

            _isDone = true;
            return result;
        }

        public async Task<bool> ExecuteOrders(string json)
        {
            if (_orderMethod == null) { return false; }

            return await (Task<bool>)_orderMethod.Invoke(_lisbeth, new object[] {json, IgnoreHome});
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

            _orderMethod = orderMethod;
            _lisbeth = lisbeth;

            var apiObject = lisbeth.GetType().GetProperty("Api")?.GetValue(lisbeth);

            if (apiObject != null)
            {
                // 6.51f3
                _getAllOrderItems = (Func<HashSet<uint>>)Delegate.CreateDelegate(typeof(Func<HashSet<uint>>), apiObject, "GetAllOrderItems");
                _setTrashExclusionItems = (Action<HashSet<uint>>)Delegate.CreateDelegate(typeof(Action<HashSet<uint>>), apiObject, "SetTrashExclusions");

                // Previous
                _getActiveOrders = (Func<string>) Delegate.CreateDelegate(typeof(Func<string>), apiObject, "GetActiveOrders");
                _kill = (Func<Character, Task>) Delegate.CreateDelegate(typeof(Func<Character, Task>), apiObject, "Kill");
                _addCraftHook = (Action<string, Func<Task>>) Delegate.CreateDelegate(typeof(Action<string, Func<Task>>), apiObject, "AddCraftCycleHook");
                _removeCraftHook = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), apiObject, "RemoveCraftCycleHook");
                _addCompletionHook = (Action<string, Func<Task>>) Delegate.CreateDelegate(typeof(Action<string, Func<Task>>), apiObject, "AddCompletionHook");
                _removeCompletionHook = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), apiObject, "RemoveCompletioneHook");
                _getAreaName = (Func<uint, uint, Vector3, string>) Delegate.CreateDelegate(typeof(Func<uint, uint, Vector3, string>), apiObject, "GetAreaName");
                _exitCrafting = (Func<Task<bool>>) Delegate.CreateDelegate(typeof(Func<Task<bool>>), apiObject, "ExitCrafting");
                _isProductKeyValid = (Func<Task<bool>>)Delegate.CreateDelegate(typeof(Func<Task<bool>>), apiObject, "IsProductKeyValid");
                _getEmptyInventorySlotCount = (Func<int>) Delegate.CreateDelegate(typeof(Func<int>), apiObject, "GetEmptyInventorySlotCount");
                _getCurrentAreaName = (Func<string>) Delegate.CreateDelegate(typeof(Func<string>), apiObject, "GetCurrentAreaName");
                _stopGently = (Func<Task>) Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "StopGently");
                _addHook = (Action<string, Func<Task>>) Delegate.CreateDelegate(typeof(Action<string, Func<Task>>), apiObject, "AddHook");
                _removeHook = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), apiObject, "RemoveHook");
                _getHookList = (Func<List<string>>) Delegate.CreateDelegate(typeof(Func<List<string>>), apiObject, "GetHookList");

                _equipOptimalGear = (Func<Task>) Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "EquipOptimalGear");
                _extractMateria = (Func<Task>) Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "ExtractMateria");
                _selfRepair = (Func<Task>) Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "SelfRepair");
                _selfRepairWithMenderFallback = (Func<Task>) Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "SelfRepairWithMenderFallback");

                _getOrderExpansionAsJson = (Func<string, Task<string>>) Delegate.CreateDelegate(typeof(Func<string, Task<string>>), apiObject, "GetOrderExpansionAsJson");

                _travelTo = (Func<uint, uint, Vector3, bool, Task<bool>>)Delegate.CreateDelegate(typeof(Func<uint, uint, Vector3, bool, Task<bool>>), apiObject, "TravelTo");
                _travelToWithArea = (Func<string, Vector3, bool, Task<bool>>)Delegate.CreateDelegate(typeof(Func<string, Vector3, bool, Task<bool>>), apiObject, "TravelToWithArea");

                _openWindow = (System.Action) Delegate.CreateDelegate(typeof(System.Action), apiObject, "OpenWindow");
                _requestRestart = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), apiObject, "RequestRestart");
                _getIncompleteOrders = (Func<string>) Delegate.CreateDelegate(typeof(Func<string>), apiObject, "GetIncompleteOrders");

                // 7.01
                _addGatherHook = (Action<string, Func<Task>>)Delegate.CreateDelegate(typeof(Action<string, Func<Task>>), apiObject, "AddGatherCycleHook");
                _removeGatherHook = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), apiObject, "RemoveGatherCycleHook");

                _addGrindHook = (Action<string, Func<Task>>)Delegate.CreateDelegate(typeof(Action<string, Func<Task>>), apiObject, "AddGrindCycleHook");
                _removeGrindHook = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), apiObject, "RemoveGrindCycleHook");

                // 7.21
                _craft = (Func<bool, Task>)Delegate.CreateDelegate(typeof(Func<bool, Task>), apiObject, "Craft");

                // 7.35
                _optimizeEquipment = (Func<Task>)Delegate.CreateDelegate(typeof(Func<Task>), apiObject, "OptimizeEquipment");
            }

            Logging.Write("Lisbeth found.");
        }

        // External API
        public async Task MakeEquipment()
        {
            if (_optimizeEquipment == null) { return; }
            await _optimizeEquipment();
        }

        public string GetActiveOrders()
        {
            return _getActiveOrders?.Invoke();
        }

        public async Task Kill(Character mob)
        {
            await _kill(mob);
        }

        public string GetCurrentAreaName()
        {
            return _getCurrentAreaName?.Invoke();
        }

        public async Task StopGently()
        {
            if (_stopGently == null) { return; }
            await _stopGently();
        }

        public void AddHook(string name, Func<Task> function)
        {
            _addHook?.Invoke(name, function);
        }

        public void RemoveHook(string name)
        {
            _removeHook?.Invoke(name);
        }

        public void AddCompletionHook(string name, Func<Task> function)
        {
            _addCompletionHook?.Invoke(name, function);
        }

        public void RemoveCompletionHook(string name)
        {
            _removeCompletionHook?.Invoke(name);
        }

        public void AddCraftHook(string name, Func<Task> function)
        {
            _addCraftHook?.Invoke(name, function);
        }

        public void RemoveCraftHook(string name)
        {
            _removeCraftHook?.Invoke(name);
        }

        public List<string> GetHookList()
        {
            return _getHookList?.Invoke();
        }

        public async Task EquipOptimalGear()
        {
            await _equipOptimalGear();
        }

        public async Task ExtractMateria()
        {
            await _extractMateria();
        }

        public async Task SelfRepair()
        {
            await _selfRepair();
        }

        public async Task SelfRepairWithMenderFallback()
        {
            await _selfRepairWithMenderFallback();
        }

        public async Task<string> GetOrderExpansionAsJson(string orderJson)
        {
            return await _getOrderExpansionAsJson(orderJson);
        }

        public async Task<bool> ExitCrafting()
        {
            return await _exitCrafting();
        }

        public int GetEmptyInventorySlotCount()
        {
            return _getEmptyInventorySlotCount();
        }

        public string GetAreaName(uint zoneId, uint subzoneId, Vector3 position)
        {
            return _getAreaName(zoneId, subzoneId, position);
        }

        public async Task<bool> TravelToWithArea(string area, Vector3 position, bool skipLanding = false)
        {
            return await _travelToWithArea(area, position, skipLanding);
        }

        public async Task<bool> TravelTo(uint zoneId, uint subzoneId, Vector3 position, bool skipLanding = false)
        {
            return await _travelTo(zoneId, subzoneId, position, skipLanding);
        }

        public void OpenWindow()
        {
            _openWindow();
        }

        public void RequestRestart(string json)
        {
            _requestRestart(json);
        }

        public string GetIncompleteOrders()
        {
            return _getIncompleteOrders();
        }

        public async Task<bool> IsProductKeyValid()
        {
            return await _isProductKeyValid();
        }

        public async Task Craft(bool quickSynth)
        {
            await _craft(quickSynth);
        }
    }
}
