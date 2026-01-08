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
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.RemoteWindows;
using TreeSharp;
using Action = TreeSharp.Action;
namespace ff14bot.NeoProfiles.Tags
{
    public class HuntBehavior : ProfileBehavior
    {
        protected HuntBehavior()
        {
            Hotspots = new IndexedList<HotSpot>();
        }

        public sealed override bool IsDone
        {
            get
            {

                if (UseTimes > 0)
                {

                    //Pulse the object here
                    var trgt = Target;


                    if (NumberOfTimesCompleted >= UseTimes)
                        return true;

					//Release mode so we can debug it easier, its a no-op
                    GC.KeepAlive(trgt);
                }


                if (IsQuestComplete)
                    return true;

                if (IsStepComplete)
                    return true;

                if (Conditional != null)
                {
                    var cond = !Conditional();
                    return cond;
                }



                return false;
            }
        }



        #region Condition
        [XmlAttribute("Condition")]
        public string Condition { get; set; }
        public Func<bool> Conditional { get; set; }


        public void SetupConditional()
        {
            try
            {
                if (Conditional == null && !string.IsNullOrEmpty(Condition))
                {
                    Conditional = ScriptManager.GetCondition(Condition);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
                // Stop on syntax errors.
                TreeRoot.Stop();
                throw;
            }

        }
        #endregion

        private Vector3 _position;
        //Stuff we are intrested in....
        [XmlAttribute("XYZ")]
        public Vector3 XYZ
        {
            get { return _position; }
            set { _position = value; }
        }


        [XmlAttribute("WaitTime")]
        public int WaitTime { get; set; }

        [DefaultValue(0.0f)]
        [XmlAttribute("UseHealthPercent")]
        public float UseHealthPercent { get; set; }


        [XmlAttribute("LacksAuraId")]
        public int LacksAuraId { get; set; }

        [DefaultValue(180)]
        [XmlAttribute("BlacklistDuration")]
        public int BlacklistDuration { get; set; }

        [XmlAttribute("UseDistance")]
        [DefaultValue(3.24f)]
        public float UseDistance { get; set; }


        [XmlAttribute("UseTimes")]
        [DefaultValue(0)]
        public int UseTimes { get; set; }

        [XmlAttribute("SearchRadius")]
        [XmlAttribute("Radius")]
        [DefaultValue(50f)]
        public float Radius { get; set; }

        [XmlAttribute("NpcIds")]
        [XmlAttribute("NpcId")]
        public int[] NpcIds { get; set; }

        [XmlElement("HotSpots")]
        public IndexedList<HotSpot> Hotspots { get; set; }


        [XmlAttribute("IgnoreLOS")]
        public bool IgnoreLOS { get; set; }

        //[DefaultValue(true)]
        [XmlAttribute("BlacklistAfter")]
        public bool BlacklistAfter { get; set; }


        [XmlAttribute("inCombat")]
        [XmlAttribute("InCombat")]
        [DefaultValue(false)]
        public new bool InCombat { get; set; }

        [XmlAttribute("InCombatOnly")]
        [DefaultValue(false)]
        public bool InCombatOnly { get; set; }

        protected virtual void OnStartHunt()
        {

        }

        protected virtual void OnDoneHunt()
        {

        }

        //sealed override to make sure this one gets called
        protected sealed override void OnResetCachedDone()
        {
            NumberOfTimesCompleted = 0;
        }


        private Composite _combatlogic;
        protected sealed override void OnStart()
        {
            SetupConditional();

            if (Hotspots != null)
            {


                if (Hotspots.Count == 0)
                {
                    if (XYZ == Vector3.Zero)
                    {
                        LogError("No hotspots and no XYZ provided, this is an invalid combination for this behavior");
                        return;
                    }

                    Hotspots.Add(new HotSpot(XYZ,Radius));
                }

                Hotspots.IsCyclic = true;
                Hotspots.Index = 0;

            }


			//I'm really not a fan of how this "combat" logic works, but it was done in such a rush and changing it now could really break things

            if (InCombatOnly)
            {
                NeoProfileManager.CurrentGrindArea = new GrindArea()
                {
                    Hotspots = Hotspots.ToList(),
                    TargetMobs = NpcIds.Select(r => new TargetMob() { Id = r }).ToList()
                };
                InCombat = true;
            }
                

            if (InCombat)
            {
                _combatlogic = CombatLogic;
                LogVerbose("Injecting hunt behavior into combat area.");
                TreeHooks.Instance.InsertHook("PreCombatLogic", 0, _combatlogic);
            }




            OnStartHunt();
        }
        protected sealed override void OnDone()
        {

            if (_combatlogic != null)
                TreeHooks.Instance.RemoveHook("PreCombatLogic", _combatlogic);

            NumberOfTimesCompleted = 0;
            NeoProfileManager.CurrentGrindArea = null;

            OnDoneHunt();
        }


        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <remarks>Created 2012-02-08</remarks>
        public HotSpot Position
        {
            get
            {
                return Hotspots.CurrentOrDefault;
            }
        }

        public virtual Composite CustomLogic
        {
            get { return null; }
        }

        public virtual Composite CustomCombatLogic
        {
            get { return null; }
        }


        private int NumberOfTimesCompleted = 0;
        private GameObject _target;
        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <remarks>Created 2012-02-08</remarks>
        public GameObject Target
        {
            get
            {
                if (_target != null)
                {
                    if (!_target.IsValid || !_target.IsTargetable || !_target.IsVisible || Blacklist.Contains(_target))
                    {
                        NumberOfTimesCompleted++;
                    }
                    else
                    {
                        return _target;
                    }
                }
                _target = GetObject();
                if (_target != null)
                {
                    Log("Target set to {0}", _target);
                }
                return _target;
            }
        }


        private Composite CombatLogic
        {
            get
            {
                return new Decorator(r => NpcIds.Contains((int)Poi.Current.Unit.NpcId),
                    new PrioritySelector(
                        new Decorator(r => LacksAuraId > 0 && BlacklistAfter && Poi.Current.BattleCharacter.HasAura((uint)LacksAuraId), new Action(
                            r =>
                            {
                                Blacklist.Add(Poi.Current.BattleCharacter.ObjectId, BlacklistFlags.SpecialHunt, TimeSpan.FromSeconds(BlacklistDuration), "BlacklistAfter");
                                Poi.Clear("BlacklistAfter");
                            })),
                        new Decorator(r => Poi.Current.Unit.CurrentHealthPercent < UseHealthPercent && (LacksAuraId == 0 || !Poi.Current.BattleCharacter.HasAura((uint)LacksAuraId)), 
                            
                            new PrioritySelector(
                                CommonBehaviors.MoveAndStop(r => Poi.Current.Unit.Location, r => UseDistance, false, "[HuntBehavior] Moving into range"),
                                CustomCombatLogic
                            
                            ))

                            ));
            }
        }

        private string HotspotString
        {
            get { return $"Moving to hotspot at {Position}"; }
        }
        protected sealed override Composite CreateBehavior()
        {
            if (InCombatOnly)
            {
                return new HookExecutor("HotspotPoi");
            }

            return // Context as the object. Kthxbai!
                new PrioritySelector(ctx => Target, // Always kill stuff near the POI kthx.
                    CreateKillFirst(),
                    CustomLogic,
                    new Decorator(
                        ret => Hotspots.Count != 0 && Navigator.InPosition(Position, Core.Player.Location, 5f),
                        new Action(ret => Hotspots.Next())),
                    CommonBehaviors.MoveAndStop(ret => Position, 3f, true, HotspotString),
                    new Action(ret => RunStatus.Success));
        }

        protected bool ShortCircut(GameObject obj)
        {
            if (!obj.IsValid || !obj.IsTargetable || !obj.IsVisible)
                return true;

            if (Core.Player.InCombat && !InCombat)
                return true;

            if (Talk.DialogOpen)
                return true;

            return false;
        }


        #region Clear Area
        private Composite CreateKillFirst()
        {
            return new PrioritySelector(
                new Decorator(
                    ret => Core.Me.InCombat && CombatTargeting.Instance.FirstUnit != null,
                    new Sequence(
                        new Action(ret => Poi.Current = new Poi(CombatTargeting.Instance.FirstUnit, PoiType.Kill)),
                        new Action(ret => Navigator.Clear()))),
                new PrioritySelector(context => EnemiesNearObject,
                    new Decorator(
                        ret => (ret as IEnumerable<BattleCharacter>).Count() != 0,
                        new Action(ret => Poi.Current = new Poi((ret as IEnumerable<BattleCharacter>).First(), PoiType.Kill)))
                    ));
        }

        private IEnumerable<BattleCharacter> EnemiesNearObject
        {
            get
            {
                // Should probably add an AggroRadius to this just in case. For now, its fine I guess.
                return GameObjectManager.GetObjectsOfType<BattleCharacter>().Where(n => n.Location.DistanceSqr(Position) < 1.5 && n.Distance() < 5 && !n.IsDead && n.CanAttack).OrderBy(n => n.Location.DistanceSqr(Position));
            }
        }
        #endregion

        #region Object Searching

        private BlacklistFlags UseObjectFlag = (BlacklistFlags)0x200000;
        protected virtual GameObject GetObject()
        {
            var possible = GameObjectManager.GetObjectsOfType<GameObject>(true, false).Where(obj => obj.IsVisible && obj.IsTargetable && !Blacklist.Contains(obj.ObjectId) && NpcIds.Contains((int)obj.NpcId)).OrderBy(obj => obj.DistanceSqr(Core.Player.Location));

            float closest = float.MaxValue;
            foreach (var obj in possible)
            {
                if (LacksAuraId > 0)
                {
                    var c = obj as Character;
                    if (c != null && c.HasAura((uint)LacksAuraId))
                        continue;
                }

                if (obj.DistanceSqr() < 1)
                    return obj;

                HotSpot target = null;
                foreach (var hotspot in Hotspots)
                {
                    //Log("Distance to {0}:{1}", hotspot.Position, hotspot.Position.Distance(Core.Player.Location));
                    if (hotspot.WithinHotSpot2D(obj.Location))
                    {
                        var dist = hotspot.Position.DistanceSqr(obj.Location);
                        if (dist < closest)
                        {
                            closest = dist;
                            target = hotspot;
                        }
                    }
                }


                if (target != null)
                {
                    while (Hotspots.Current != target)
                    {
                        Hotspots.Next();
                    }
                    return obj;
                }

               //if (Hotspots.Any(r=> r.WithinHotSpot2D(obj.Location)))
               //    return obj;
            }


            return null;
        }
        #endregion


    }
}
