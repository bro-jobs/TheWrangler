//
// LICENSE:
// This work is licensed under the
//     Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// also known as CC-BY-NC-SA.  To view a copy of this license, visit
//      http://creativecommons.org/licenses/by-nc-sa/3.0/
// or send a letter to
//      Creative Commons // 171 Second Street, Suite 300 // San Francisco, California, 94105, USA.
//

//Work originally done by exmatt, relicensed with permission.

using Buddy.Coroutines;
using Clio.Common;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.BotBases;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.NeoProfiles.Tags.Fish;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using ff14bot.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TreeSharp;
using Action = TreeSharp.Action;
// ReSharper disable LocalizableElement

namespace ff14bot.NeoProfiles.Tags
{



    [XmlElement("ExFish")]
    [XmlElement("Fish")]
    public class FishTag : ProfileBehavior
    {
        public enum Ability
        {
            None = 0,
            Bait = 288,
            Cast = 289,
            Hook = 296,
            Quit = 299,
            CastLight = 2135,
            Sneak = 305,
            Release = 300,
            Mooch = 297,
            Snagging = 4100,
            CollectorsGlove = 4101,
            Patience = 4102,
            PowerfulHookset = 4103,
            Chum = 4104,
            PrecisionHookset = 4179,
            FishEyes = 4105,
            Patience2 = 4106,
            SharkEye = 7904,
            Gig = 7632,
            GigHead = 7634,
            Mooch2 = 268,
            VeteranTrade = 7906,
            CalmWaters = 7908,
            SharkEye2 = 7905,
            Truth = 7911,
            DoubleHook = 269,
            Salvage = 7910,
            BountifulCatch = 7907,
            NaturesBounty = 7909,
            SurfaceSlap = 4595,
            IdenticalGig = 4591,
            IdenticalCast = 4596
        }




        public sealed override bool IsDone => _isDone;
        private bool _isDone;


        internal SpellData CordialSpellData;


  
        protected override Composite CreateBehavior()
        {
            _fishlimit = GetFishLimit();

            return new PrioritySelector(
                new ActionRunCoroutine(ctx=> HandleCatchWindow()),
                StateTransitionAlwaysSucceed,
                Conditional,
                Blacklist,
                MoveToFishSpot,
                GoFish(
                    StopMovingComposite,
                    DismountComposite,
                    CheckStealthComposite,
                    CheckWeatherComposite,
                    // Waits up to 10 hours, might want to rethink this one.
                    new ActionRunCoroutine(ctx => HandleBait()),
                    InitFishSpotComposite,
                    new ActionRunCoroutine(ctx => HandleCollectable()),
                    ReleaseComposite,
                    IdenticalCastComposite,
                    MoochComposite,
                    FishCountLimitComposite,
                    InventoryFullComposite,
                    SitComposite,
                    CollectorsGloveComposite,
                    SnaggingComposite,
                    new ActionRunCoroutine(ctx => HandleCordial()),
                    PatienceComposite,
                    FishEyesComposite,
                    ChumComposite,
                    CastComposite,
                    HookComposite));
        }

        protected virtual void DoCleanup()
        {
            try
            {
                GamelogManager.MessageRecevied -= ReceiveMessage;
            }
            catch (Exception ex)
            {
                Logging.Write(Colors.OrangeRed, ex.Message);
            }

            CharacterSettings.Instance.UseMount = _initialMountSetting;
        }

        protected override void OnResetCachedDone()
        {
            _mooch = 0;
            _sitRoll = 1.0;
            _spotinit = false;
            _fishcount = 0;
            _amissfish = 0;
            _isSitting = false;
            _isFishIdentified = false;
            _catchWindowProcessed = false;
            _fishlimit = GetFishLimit();
            _checkRelease = false;
            _checkIdenticalCast = false;

            // Temp fix, only set it to true if it was initially true. Need to find out why this value is false here when it shouldn't be.
            if (_initialMountSetting)
            {
                CharacterSettings.Instance.UseMount = _initialMountSetting;
            }
        }

        protected Composite GoFish(params Composite[] children)
        {
            return
                new PrioritySelector(
                    new Decorator(
                        ret => Vector3.Distance(Core.Me.Location, FishSpots.CurrentOrDefault.Location) < 2,
                        new PrioritySelector(children)));
        }


        protected override void OnDone()
        {
            TreeRoot.OnStop -= _cleanup;
            DoCleanup();
        }

        protected override void OnStart()
        {
            BaitDelay = int.Clamp(BaitDelay, 0, 5000);

            Item baitItem = null;
            if (BaitId > 0)
            {
                baitItem = DataManager.ItemCache[BaitId];
            }
            else if (!string.IsNullOrWhiteSpace(Bait))
            {
                baitItem =
                    DataManager.ItemCache.Values.Find(
                        i =>
                            string.Equals(i.EnglishName, Bait, StringComparison.InvariantCultureIgnoreCase)
                            || string.Equals(i.CurrentLocaleName, Bait, StringComparison.InvariantCultureIgnoreCase));

                if (baitItem == null)
                {
                    _isDone = true;
                    Logging.Write(Colors.OrangeRed, "Cannot find bait: " + Bait);
                    return;
                }
            }

            if (baitItem != null)
            {
                Baits ??= new List<Bait>();

                Baits.Insert(0, new Bait { Id = baitItem.Id, Name = baitItem.EnglishName, BaitItem = baitItem, Condition = "True" });
            }

            if (baitItem != null && baitItem.Affinity != 19)
            {
                _isDone = true;
                Logging.Write(Colors.OrangeRed, "The item {0} is not usable as bait.", baitItem.EnglishName);
                return;
            }

            Keepers ??= [];

            if (Collect && Collectables == null)
            {
                Collectables = [new Collectable { Name = string.Empty, Value = (int)CollectabilityValue }];
            }

            GamelogManager.MessageRecevied += ReceiveMessage;
            FishSpots.IsCyclic = true;
            _isSitting = false;
            _initialMountSetting = CharacterSettings.Instance.UseMount;
            ShuffleFishSpots();

            _sitRoll = SitRng.NextDouble();

            if (CanDoAbility(Ability.Quit))
            {
                DoAbility(Ability.Quit);
            }

            CordialSpellData = DataManager.GetItem((uint)CordialType.Cordial).BackingAction;

            _cleanup = _ =>
            {
                DoCleanup();
                TreeRoot.OnStop -= _cleanup;
            };

            TreeRoot.OnStop += _cleanup;
        }

        internal bool CanUseCordial(ushort withinSeconds = 5)
        {
            return CordialSpellData.Cooldown.TotalSeconds < withinSeconds && !HasChum && !HasPatience && !HasFishEyes
                   && ((CordialType == CordialType.WateredCordial && Cordial.HasWateredCordials())
                   || (CordialType == CordialType.Cordial && (Cordial.HasWateredCordials() || Cordial.HasCordials()))
                   || ((CordialType == CordialType.HiCordial || CordialType == CordialType.Auto) && Cordial.HasAnyCordials()));
        }

        private async Task<bool> HandleBait()
        {
            if (!IsBaitSpecified || IsCorrectBaitSelected)
            {
                // we don't need to worry about bait. Either not specified, or we already have the correct bait selected.
                return false;
            }

            if (FishingManager.State != FishingState.None && FishingManager.State != FishingState.PoleReady)
            {
                // we are not in the proper state to modify our bait. continue.
                return false;
            }

            if (!HasSpecifiedBait)
            {
                Logging.Write(Colors.OrangeRed, "You do not have the required bait: " + Bait);
                return _isDone = true;
            }

            var baitItem = Fish.Bait.FindMatch(Baits).BaitItem;

            if (!await FishingManager.ChangeBait(baitItem.Id))
            {
                Logging.Write(Colors.OrangeRed, "Failed to select bait. Please check your settings.");
                return _isDone = true;
            }

            Logging.Write("Using bait: " + baitItem.EnglishName);

            return true;
        }




        private async Task<bool> HandleCollectable()
        {
            if (Collectables == null)
            {
                //we are not collecting
                return false;
            }

            if (FishingManager.State != FishingState.Waitin)
            {
                // we are not waitin yet!
                return false;
            }

            if (!SelectYesno.IsOpen)
            {
                //Wait a few seconds
                var opened = await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
                if (!opened)
                {
                    Logging.Write("SelectYesno never appeared");
                    return false;
                }
            }

            var required = CollectabilityValue;
            var itemName = string.Empty;
            if (!string.IsNullOrWhiteSpace(Collectables.First().Name))
            {
                var item = SelectYesno.Item;
                if (item == null
                    || !Collectables.Any(c => string.Equals(c.Name, item.EnglishName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var ticks = 0;
                    while ((item == null
                            ||
                            !Collectables.Any(c => string.Equals(c.Name, item.EnglishName, StringComparison.InvariantCultureIgnoreCase)))
                           && ticks++ < 60 && true)//Behaviors.ShouldContinue)
                    {
                        item = SelectYesno.Item;
                        await Coroutine.Yield();
                    }

                    // handle timeout
                    if (ticks > 60)
                    {
                        required = (uint)Collectables.Select(c => c.Value).Max();
                    }
                }

                if (item != null)
                {
                    // handle normal
                    itemName = item.EnglishName;
                    var collectable = Collectables.FirstOrDefault(c => string.Equals(c.Name, item.EnglishName));

                    if (collectable != null)
                    {
                        required = (uint)collectable.Value;
                    }
                }
            }

            // handle

            var value = SelectYesno.CollectabilityValue;

            if (value >= required)
            {
                Logging.Write("Collecting {0} with collectability value {1} (required: {2})", itemName, value, required);
                SelectYesno.Yes();
            }
            else
            {
                Logging.Write("Declining {0} with collectability value {1} (required: {2})", itemName, value, required);
                SelectYesno.No();
            }

            await Coroutine.Wait(3000, () => !SelectYesno.IsOpen && FishingManager.State != FishingState.Waitin);

            return true;
        }

        private async Task<bool> HandleCordial()
        {
            if (CordialType == CordialType.None)
            {
                // Not using cordials, skip method.
                return false;
            }

            if (FishingManager.State >= FishingState.Bite)
            {
                // Need to wait till we are in the correct state
                return false;
            }

            CordialSpellData ??= Cordial.GetSpellData();

            if (CordialSpellData == null)
            {
                CordialType = CordialType.None;
                return false;
            }

            if (!CanUseCordial(8))
            {
                // has a buff or cordial cooldown not ready or we have no cordials.
                return false;
            }

            var localPlayer = Core.Me;
            var missingGp = localPlayer.MaxGP - localPlayer.CurrentGP;

            if (missingGp < 300 && !ForceCordial)
            {
                // Not forcing cordial and less than 300gp missing from max.
                return false;
            }

            await Coroutine.Wait(10000, () => CanDoAbility(Ability.Quit));
            DoAbility(Ability.Quit);
            _isSitting = false;

            await Coroutine.Wait(5000, () => FishingManager.State == FishingState.None);

            if (missingGp >= 380 && (CordialType == CordialType.HiCordial || CordialType == CordialType.Auto))
            {
                if (await UseCordial(CordialType.HiCordial))
                {
                    return true;
                }
            }

            if (missingGp >= 280 && (CordialType == CordialType.Cordial || CordialType == CordialType.Auto))
            {
                if (await UseCordial(CordialType.Cordial))
                {
                    return true;
                }
            }

            if (await UseCordial(CordialType.WateredCordial))
            {
                return true;
            }

            return false;
        }

        private async Task<bool> UseCordial(CordialType cordialType, int maxTimeoutSeconds = 5)
        {
            if (CordialSpellData.Cooldown.TotalSeconds < maxTimeoutSeconds)
            {
                var cordial = InventoryManager.FilledSlots.FirstOrDefault(slot => slot.RawItemId == (uint)cordialType);

                if (cordial != null)
                {
                    StatusText = "Will use cordial when available.";

                    Logging.Write(
                        "Cordial is ready in {0}s, Current GP: {1}",
                        (int)CordialSpellData.Cooldown.TotalSeconds,
                        Core.Me.CurrentGP);

                    if (await Coroutine.Wait(
                        TimeSpan.FromSeconds(maxTimeoutSeconds),
                        () =>
                        {
                            if (Core.Me.IsMounted && CordialSpellData.Cooldown.TotalSeconds < 2)
                            {
                                ActionManager.Dismount();
                                return false;
                            }

                            return cordial.CanUse(Core.Me);
                        }))
                    {
                        await Coroutine.Sleep(500);
                        Logging.Write("Using " + cordialType);
                        cordial.UseItem(Core.Me);
                        await Coroutine.Sleep(1500);
                        return true;
                    }
                }
                else
                {
                    Logging.Write(Colors.Goldenrod, "No cordial of type: " + cordialType);
                }
            }

            return false;
        }

        private async Task<bool> HandleCatchWindow()
        {
            if (Catch.IsOpen)
            {
                if (!_catchWindowProcessed)
                {
                    _catchWindowProcessed = true;

                    FishResult = new FishResult(Catch.CaughtFish,Catch.Large,Catch.FishSize);
                    _isFishIdentified = true;

                    // Log information about the caught fish
                    Logging.Write("Catch window opened: {0} ({1} ilms)", Catch.FishName, Catch.FishSize);
                    
                    // Wait for the window to close
                    await Coroutine.Wait(5000, () => !Catch.IsOpen);
                }
                
            }
            else
            {
                // Clear the flag when window is closed so it can be processed again next time
                if (_catchWindowProcessed)
                {
                    _catchWindowProcessed = false;
                }
                
                
            }

            return false;
        }

        #region Aura Properties

        // Gathering Fortune Up (Fishing)
        protected bool HasPatience => Core.Me.HasAura(850);
        // Snagging
        protected bool HasSnagging => Core.Me.HasAura(761);
        // Collector's Glove
        protected bool HasCollectorsGlove => Core.Me.HasAura(805);
        // Chum
        protected bool HasChum => Core.Me.HasAura(763);
        // Fish Eyes
        protected bool HasFishEyes => Core.Me.HasAura(762);

        #endregion Aura Properties

        #region Fields

        

        protected static readonly Random SitRng = new Random();









        protected static FishResult FishResult = null;
        private Func<bool> _conditionFunc;
        private Func<bool> _moochConditionFunc;
        private bool _initialMountSetting;
        private BotEvent _cleanup;
        private bool _checkRelease;
        private bool _checkIdenticalCast;
        private bool _isSitting;
        private bool _isFishIdentified;
        private bool _catchWindowProcessed;
        private int _mooch;
        private int _fishcount;
        private int _amissfish;
        private double _fishlimit;
        private double _sitRoll = 1.0;
        private bool _spotinit;

        #endregion Fields

        #region Public Properties

        [XmlElement("Baits")]
        public List<Bait> Baits { get; set; }

        [DefaultValue(CordialType.None)]
        [XmlAttribute("CordialType")]
        public CordialType CordialType { get; set; }

        [XmlAttribute("ForceCordial")]
        public bool ForceCordial { get; set; }

        [XmlElement("Keepers")]
        public List<Keeper> Keepers { get; set; }

        [XmlElement("Collectables")]
        public List<Collectable> Collectables { get; set; }

        [XmlElement("FishSpots")]
        public IndexedList<FishSpot> FishSpots { get; set; }

        [DefaultValue(0)]
        [XmlAttribute("Mooch")]
        public int MoochLevel { get; set; }

        [DefaultValue("True")]
        [XmlAttribute("MoochCondition")]
        public string MoochCondition { get; set; }

        [DefaultValue(20)]
        [XmlAttribute("MinFish")]
        public int MinimumFishPerSpot { get; set; }

        [DefaultValue(30)]
        [XmlAttribute("MaxFish")]
        public int MaximumFishPerSpot { get; set; }

        [XmlAttribute("Bait")]
        public string Bait { get; set; }

        [XmlAttribute("BaitId")]
        public uint BaitId { get; set; }

        [DefaultValue(200)]
        [XmlAttribute("BaitDelay")]
        public int BaitDelay { get; set; }

        [XmlAttribute("Chum")]
        public bool Chum { get; set; }

        [DefaultValue(30)]
        [XmlAttribute("LastFishTimeout")]
        public int LastFishTimeout { get; set; }

        [DefaultValue("True")]
        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        [XmlAttribute("Weather")]
        public string Weather { get; set; }

        [DefaultValue(2.0f)]
        [XmlAttribute("Radius")]
        public float Radius { get; set; }

        [XmlAttribute("ShuffleFishSpots")]
        public bool Shuffle { get; set; }

        [DefaultValue(true)]
        [XmlAttribute("EnableKeeper")]
        public bool EnableKeeper { get; set; }

        [DefaultValue(false)]
        [XmlAttribute("KeepNone")]
        public bool KeepNone { get; set; }

        [XmlAttribute("SitRate")]
        public float SitRate { get; set; }

        [XmlAttribute("Sit")]
        public bool Sit { get; set; }

        [XmlAttribute("Stealth")]
        public bool Stealth { get; set; }

        [XmlAttribute("Collect")]
        public bool Collect { get; set; }

        [XmlAttribute("CollectabilityValue")]
        public uint CollectabilityValue { get; set; }

        [DefaultValue(Ability.None)]
        [XmlAttribute("Patience")]
        internal Ability Patience { get; set; }

        [DefaultValue(600)]
        [XmlAttribute("MinimumGPPatience")]
        public int MinimumGPPatience { get; set; }

        [XmlAttribute("FishEyes")]
        public bool FishEyes { get; set; }

        [XmlAttribute("Snagging")]
        public bool Snagging { get; set; }

        [XmlAttribute("IdenticalCast")]
        public bool IdenticalCast { get; set; }

        [XmlElement("PatienceTugs")]
        public List<PatienceTug> PatienceTugs { get; set; }

        #endregion Public Properties

        #region Private Properties

        private bool HasSpecifiedBait => Fish.Bait.FindMatch(Baits).BaitItem.ItemCount() > 0;
        private bool IsBaitSpecified => Baits != null && Baits.Count > 0;
        private bool IsCorrectBaitSelected => Fish.Bait.FindMatch(Baits).BaitItem.Id == FishingManager.SelectedBaitItemId;

        #endregion Private Properties

        #region Fishing Composites

        protected Composite DismountComposite
        {
            get { return new Decorator(ret => Core.Me.IsMounted, CommonBehaviors.Dismount()); }
        }

        protected Composite FishCountLimitComposite
        {
            get
            {
                return
                    new Decorator(
                         ret =>
                            _fishcount >= _fishlimit && !HasPatience && CanDoAbility(Ability.Quit)
                            && FishingManager.State == FishingState.PoleReady && !SelectYesno.IsOpen,
                        new Sequence(
                            new Sleep(2, 3),
                            new Action(r => { DoAbility(Ability.Quit); }),
                            new Sleep(2, 3),
                            new Action(r => { ChangeFishSpot(); })));
            }
        }

        protected Composite SitComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                            !_isSitting && (Sit || FishSpots.CurrentOrDefault.Sit || _sitRoll < SitRate)
                            && FishingManager.State == FishingState.NormalFishing,
                        // this is when you have already cast and are waiting for a bite.
                        new Sequence(
                            new Sleep(1, 1),
                            new Action(
                                r =>
                                {
                                    _isSitting = true;
                                    
                                    Log("Sitting at fish spot: " + FishSpots.CurrentOrDefault);
                                    ChatManager.SendChat("/sit");
                                })));
            }
        }

        protected Composite StopMovingComposite
        {
            get { return new Decorator(ret => MovementManager.IsMoving, CommonBehaviors.MoveStop()); }
        }

        protected Composite InitFishSpotComposite
        {
            get
            {
                return new Decorator(
                    ret => !_spotinit,
                    new Action(
                        r =>
                        {
                            FaceFishSpot();
                            Log("Initializing fish spot, target: " + _fishlimit + " fish(s) to catch.");
                            _spotinit = true;
                        }));
            }
        }

        protected Composite CheckWeatherComposite
        {
            get
            {
                return new Decorator(
                    ret => Weather != null && Weather != WorldManager.CurrentWeather,
                    new Sequence(
                        new Action(r => { Logging.Write(Colors.Orange, "Waiting for correct weather..."); }),
                        new Wait(36000, ret => Weather == WorldManager.CurrentWeather, new ActionAlwaysSucceed())));
            }
        }

        protected Composite CollectorsGloveComposite
        {
            get
            {
                return new Decorator(
                    ret => CanDoAbility(Ability.CollectorsGlove) && Collectables != null ^ HasCollectorsGlove,
                    new Sequence(
                        new Action(
                            r =>
                            {
                                Log("Using Collector's Glove.");
                                DoAbility(Ability.CollectorsGlove);
                            }),
                        new Sleep(2, 3)));
            }
        }

        protected Composite SnaggingComposite
        {
            get
            {
                return new Decorator(
                    ret => CanDoAbility(Ability.Snagging) && Snagging ^ HasSnagging,
                    new Sequence(
                        new Action(
                            r =>
                            {
                                Log("Using Snagging.");
                                DoAbility(Ability.Snagging);
                            }),
                        new Sleep(2, 3)));
            }
        }

        protected Composite MoochComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                            CanDoAbility(Ability.Mooch) && MoochLevel != 0 && _mooch < MoochLevel && MoochConditionCheck()
                            && (!EnableKeeper
                                || Keepers.Count == 0
                                || Keepers.All(k => !string.Equals(k.Name, FishResult.Item.EnglishName, StringComparison.InvariantCultureIgnoreCase) && !string.Equals(k.Name, FishResult.Item.CurrentLocaleName, StringComparison.InvariantCultureIgnoreCase))
                                || Keepers.Any(
                                    k =>
                                        (string.Equals(k.Name, FishResult.Item.EnglishName, StringComparison.InvariantCultureIgnoreCase) || string.Equals(k.Name, FishResult.Item.CurrentLocaleName, StringComparison.InvariantCultureIgnoreCase))
                                        && FishResult.ShouldMooch(k))),
                        new Sequence(
                            new Action(
                                r =>
                                {
                                    _checkRelease = true;
                                    FishingManager.Mooch();
                                    _mooch++;
                                    if (MoochLevel > 1)
                                    {
                                        Log("Mooching ({0}/{1}).", _mooch, MoochLevel);
                                    }
                                    else
                                    {
                                        Log("Using Mooch.");
                                    }
                                }),
                            new Sleep(2, 2)));
            }
        }

        protected Composite ChumComposite
        {
            get
            {
                return new Decorator(
                    ret => Chum && !HasChum && CanDoAbility(Ability.Chum),
                    new Sequence(new Action(r => DoAbility(Ability.Chum)), new Sleep(1, 2)));
            }
        }

        protected Composite PatienceComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                            Patience > Ability.None
                            && (FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady) && !HasPatience
                            && CanDoAbility(Patience) &&
                            (Core.Me.CurrentGP >= MinimumGPPatience || Core.Me.CurrentGPPercent > 99.0f),
                        new Sequence(
                            new Action(
                                r =>
                                {
                                    DoAbility(Patience);
                                    Log("Using Patience.");
                                }),
                            new Sleep(1, 2)));
            }
        }

        protected Composite FishEyesComposite
        {
            get
            {
                return new Decorator(
                    ret => FishEyes && !HasFishEyes && CanDoAbility(Ability.FishEyes),
                    new Sequence(new Action(r => DoAbility(Ability.FishEyes)), new Sleep(1, 2)));
            }
        }

        protected Composite IdenticalCastComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                            IdenticalCast && _checkIdenticalCast && FishingManager.State == FishingState.PoleReady && CanDoAbility(Ability.IdenticalCast)
                            && (Keepers.Count != 0 || KeepNone),
                        new Sequence(
                            new Wait(
                                2,
                                ret => _isFishIdentified,
                                new Action(
                                    r =>
                                    {
                                        // If its a keeper AND (we aren't mooching OR we can't mooch) AND Keeper is enabled, then use Identical Cast
                                        if (Keepers.Any(FishResult.IsKeeper) && (MoochLevel == 0 || !CanDoAbility(Ability.Mooch)) && EnableKeeper)
                                        {
                                            DoAbility(Ability.IdenticalCast);
                                            Log("Casting Identical Cast for {0}.", FishResult.Item.CurrentLocaleName);
                                        }

                                        _checkIdenticalCast = false;
                                    })),
                            new Wait(2, ret => !CanDoAbility(Ability.Release), new ActionAlwaysSucceed())));
            }
        }

        protected Composite ReleaseComposite
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                            _checkRelease && FishingManager.State == FishingState.PoleReady && CanDoAbility(Ability.Release)
                            && (Keepers.Count != 0 || KeepNone),
                        new Sequence(
                            new Wait(
                                2,
                                ret => _isFishIdentified,
                                new Action(
                                    r =>
                                    {
                                        // If its not a keeper AND (we aren't mooching OR we can't mooch) AND Keeper is enabled, then release
                                        if (!Keepers.Any(FishResult.IsKeeper) && (MoochLevel == 0 || !CanDoAbility(Ability.Mooch)) && EnableKeeper)
                                        {
                                            DoAbility(Ability.Release);
                                            Log("Releasing {0}.", FishResult.Item.CurrentLocaleName);
                                        }

                                        _checkRelease = false;
                                    })),
                            new Wait(2, ret => !CanDoAbility(Ability.Release), new ActionAlwaysSucceed())));
            }
        }

        protected Composite CastComposite
        {
            get
            {
                return
                    new Decorator(
                        ret => FishingManager.State == FishingState.None || FishingManager.State == FishingState.PoleReady,
                        new Action(r => Cast()));
            }
        }

        protected Composite InventoryFullComposite
        {
            get
            {
                return new Decorator(
                    // TODO: Log reason for quit.
                    ret => InventoryManager.FilledSlots.Count(c => c.BagId != InventoryBagId.KeyItems) >= 140, IsDoneAction);
            }
        }

        protected Composite HookComposite
        {
            get
            {
                return new Decorator(
                    ret => FishingManager.CanHook && FishingManager.State == FishingState.Bite,
                    new Action(
                        r =>
                        {
                            var tugType = FishingManager.TugType;
                            var patienceTug = new PatienceTug { MoochLevel = _mooch, TugType = tugType };
                            var hookset = tugType == TugType.Light ? Ability.PrecisionHookset : Ability.PowerfulHookset;
                            if (HasPatience && CanDoAbility(hookset) && (PatienceTugs == null || PatienceTugs.Contains(patienceTug)))
                            {
                                DoAbility(hookset);
                                Log("Tug type: {0}, using: {1}", tugType, hookset);
                            }
                            else
                            {
                                FishingManager.Hook();
                            }

                            _amissfish = 0;
                            if (_mooch == 0)
                            {
                                _fishcount++;
                            }

                            Log("Caught fish {0} of {1}.", _fishcount, _fishlimit);
                        }));
            }
        }

        protected Composite CheckStealthComposite
        {
            get
            {
                return new Decorator(
                    ret => Stealth && !Core.Me.HasAura(47),
                    new Sequence(
                        new Action(
                            r =>
                            {
                                CharacterSettings.Instance.UseMount = false;
                                DoAbility(Ability.Sneak);
                            }),
                        new Sleep(2, 3)));
            }
        }

        #endregion Fishing Composites

        #region Composites

        protected Composite Conditional
        {
            get { return new Decorator(ret => FishingManager.State < FishingState.Bite && !ConditionCheck(), IsDoneAction); }
        }

        protected Composite Blacklist
        {
            get
            {
                return new Decorator(
                    ret => _amissfish > Math.Min(FishSpots.Count, 4),
                    new Sequence(
                        new Action(
                            r =>
                            {
                                Logging.Write(Colors.OrangeRed, "Missed fish - possible bugged fish spot.");
                                Logging.Write(Colors.OrangeRed, "Blacklisting this fish spot...");
                            }),
                        IsDoneAction));
            }
        }

        protected Composite StateTransitionAlwaysSucceed
        {
            get
            {
                return
                    new Decorator(
                        ret =>
                            FishingManager.State == FishingState.Reelin || FishingManager.State == FishingState.Quit
                            || FishingManager.State == FishingState.PullPoleIn,
                        new ActionAlwaysSucceed());
            }
        }

        protected Composite MoveToFishSpot
        {
            get
            {
                return new Decorator(
                    ret => Vector3.Distance(Core.Me.Location, FishSpots.CurrentOrDefault.Location) > 1,
                    new Sequence(
                        new Action(r =>
                        {
                            if (!MovementManager.IsFlying && !MovementManager.IsDiving)
                            {
                                Navigator.MoveTo(new MoveToParameters(FishSpots.CurrentOrDefault.Location));
                            }
                            else
                            {
                                Flightor.MoveTo(new FlyToParameters(FishSpots.CurrentOrDefault.Location));
                            }
                        })));
            }
        }

        protected Composite IsDoneAction
        {
            get
            {
                return
                    new Sequence(
                        new WaitContinue(
                            LastFishTimeout,
                            ret => FishingManager.State < FishingState.Bite,
                            new Sequence(
                                new PrioritySelector(
                                    new ActionRunCoroutine(ctx => HandleCollectable()),
                                    ReleaseComposite,
                                    new ActionAlwaysSucceed()),
                                new Sleep(2, 3),
                                new Action(r => DoAbility(Ability.Quit)),
                                new Sleep(2, 3),
                                new Action(r => { _isDone = true; }))));
            }
        }

        #endregion Composites

        #region Ability Checks and Actions

        internal bool CanDoAbility(Ability ability)
        {
            return ActionManager.CanCast((uint)ability, Core.Me);
        }

        internal bool DoAbility(Ability ability)
        {
            return ActionManager.DoAction((uint)ability, Core.Me);
        }

        #endregion Ability Checks and Actions

        #region Methods

        protected virtual bool ConditionCheck()
        {
            if (_conditionFunc == null)
            {
                _conditionFunc = ScriptManager.GetCondition(Condition);
            }

            return _conditionFunc();
        }

        protected virtual bool MoochConditionCheck()
        {
            if (_moochConditionFunc == null)
            {
                _moochConditionFunc = ScriptManager.GetCondition(MoochCondition);
            }

            return _moochConditionFunc();
        }

        protected virtual void Cast()
        {
            _isFishIdentified = false;
            _checkRelease = true;
            _checkIdenticalCast = true;
            FishingManager.Cast();
            ResetMooch();
        }

        protected virtual void FaceFishSpot()
        {
            var i = MathEx.Random(0, 25);
            i = i / 100;

            var i2 = MathEx.Random(0, 100);

            if (i2 > 50)
            {
                Core.Me.SetFacing(FishSpots.Current.Heading - (float)i);
            }
            else
            {
                Core.Me.SetFacing(FishSpots.Current.Heading + (float)i);
            }
        }

        protected virtual void ChangeFishSpot()
        {
            FishSpots.Next();
            Log("Changing fish spot.");
            _fishcount = 0;
            Log("Fish counter reset for spot.");
            _fishlimit = GetFishLimit();
            _sitRoll = SitRng.NextDouble();
            _spotinit = false;
            _isSitting = false;
        }

        protected virtual int GetFishLimit()
        {
            return Convert.ToInt32(MathEx.Random(MinimumFishPerSpot, MaximumFishPerSpot));
        }

        protected void ShuffleFishSpots()
        {
            if (Shuffle && FishSpots.Index == 0)
            {
                ShuffleX(FishSpots);
                Log("Shuffling FishSpots.");
            }
        }
        public static IList<T> ShuffleX<T>(IList<T> list)
        {
            if (list.Count <= 1) 
                return list;

            var n = list.Count;
            while (n > 1)
            {
                var k = RandomNumberGenerator.GetInt32(n);
                n--;
                (list[k], list[n]) = (list[n], list[k]);
            }

            return list;
        }

        protected void ResetMooch()
        {
            if (_mooch != 0)
            {
                _mooch = 0;
                Log("Mooch counter reset.");
            }
        }


        protected void ReceiveMessage(object sender, ChatEventArgs e)
        {

            if (e.ChatLogEntry.MessageType != (MessageType)2115)
                return;


            if (e.ChatLogEntry.Contents.Equals("You sense no fish here...", StringComparison.InvariantCultureIgnoreCase))
            {
                Logging.Write("No sense of fish, trying a different spot...");

                if (CanDoAbility(Ability.Quit))
                {
                    DoAbility(Ability.Quit);
                }

                ChangeFishSpot();
            }

            else if (e.ChatLogEntry.Contents == "You hooked something, but it got away.")
            {
                Logging.Write("Missed fish at spot, counting up amiss count.");
                _amissfish++;

                if (CanDoAbility(Ability.Quit))
                {
                    DoAbility(Ability.Quit);
                }

                ChangeFishSpot();
            }
        }

        #endregion Methods
    }
}