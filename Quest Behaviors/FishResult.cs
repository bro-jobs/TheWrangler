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


using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ff14bot.Objects;


namespace ff14bot.NeoProfiles.Tags.Fish
{
    


    [Serializable]
    [Flags]
    public enum KeeperAction : byte
    {
        DontKeep = 0x00,

        KeepNq = 0x01,

        KeepHq = 0x02,

        // MoochFlag = 0x04,
        KeepAll = 0x03, // KeepNq | KeepHq

        Mooch = 0x06, // KeepHq | MoochFlag

        MoochKeepNq = 0x07 // KeepNq | KeepHq | MoochFlag
    }

    [XmlElement("Bait")]
    public class Bait
    {
        internal Item BaitItem;

        private Func<bool> conditionFunc;

        [DefaultValue("True")]
        [XmlAttribute("Condition")]
        public string Condition { get; set; }

        [XmlAttribute("Id")]
        public uint Id { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        public static Bait FindMatch([NotNull] IList<Bait> baits)
        {
            var match = baits.FirstOrDefault(b => b.IsMatch()) ?? baits[0];

            return match;
        }

        public bool IsMatch()
        {
            if (conditionFunc == null)
            {
                conditionFunc = ScriptManager.GetCondition(Condition);
            }

            if (BaitItem == null)
            {
                if (Id > 0)
                {
                    BaitItem = DataManager.ItemCache[Id];
                }
                else if (!string.IsNullOrWhiteSpace(Name))
                {
                    BaitItem =
                        DataManager.ItemCache.Values.Find(
                            i =>
                                string.Equals(i.EnglishName, Name, StringComparison.InvariantCultureIgnoreCase)
                                || string.Equals(i.CurrentLocaleName, Name, StringComparison.InvariantCultureIgnoreCase));
                }
            }

            if (BaitItem == null || BaitItem.ItemCount() == 0)
            {
                return false;
            }

            if (Core.Player.ClassLevel < BaitItem.RequiredLevel)
            {
                return false;
            }

            return conditionFunc();
        }

        public override string ToString()
        {
            return this.DynamicString();
        }
    }

    public interface IFishSpot
    {
        float Heading { get; set; }

        Vector3 Location { get; set; }

        bool Sit { get; set; }

        Task<bool> MoveFromLocation(FishTag tag);

        Task<bool> MoveToLocation(FishTag tag);
    }

    [XmlElement("FishSpot")]
    public class FishSpot : IFishSpot
    {
        public FishSpot()
        {
            Location = Vector3.Zero;
            Heading = 0f;
        }

        public FishSpot(string xyz, float heading)
        {
            Location = new Vector3(xyz);
            Heading = heading;
        }

        public FishSpot(Vector3 xyz, float heading)
        {
            Location = xyz;
            Heading = heading;
        }

        [DefaultValue(true)]
        [XmlAttribute("UseMesh")]
        public bool UseMesh { get; set; }

        public override string ToString()
        {
            return this.DynamicString();
        }

        #region IFishSpot Members

        [XmlAttribute("Heading")]
        public float Heading { get; set; }

        [XmlAttribute("XYZ")]
        [XmlAttribute("Location")]
        public Vector3 Location { get; set; }

        [XmlAttribute("Sit")]
        public bool Sit { get; set; }

        public virtual async Task<bool> MoveFromLocation(FishTag tag)
        {
            return await Task.FromResult(true);
        }

        public virtual async Task<bool> MoveToLocation(FishTag tag)
        {
            return await Task.FromResult(true);
        }

        #endregion IFishSpot Members
    }

    /*public class StealthApproachFishSpot : FishSpot
    {
        [DefaultValue(true)]
        [XmlAttribute("ReturnToStealthLocation")]
        public bool ReturnToStealthLocation { get; set; }

        [XmlAttribute("StealthLocation")]
        public Vector3 StealthLocation { get; set; }

        [XmlAttribute("UnstealthAfter")]
        public bool UnstealthAfter { get; set; }

        public override async Task<bool> MoveFromLocation(ExFishTag tag)
        {
            tag.StatusText = "Moving from " + this;

            var result = true;
            if (ReturnToStealthLocation)
            {
                result &= await StealthLocation.MoveToNoMount(UseMesh, tag.Radius, "Stealth Location", tag.MovementStopCallback);
            }

            if (UnstealthAfter && Core.Player.HasAura((int)AbilityAura.Sneak))
            {
                result &= tag.DoAbility(Ability.Sneak); // TODO: move into abilities map?
            }


            return result;
        }

        public override async Task<bool> MoveToLocation(ExFishTag tag)
        {
            tag.StatusText = "Moving to " + this;

            if (StealthLocation == Vector3.Zero)
            {
                return false;
            }

            var result =
                await
                    StealthLocation.MoveTo(
                        UseMesh,
                        radius: tag.Radius,
                        name: "Stealth Location",
                        stopCallback: tag.MovementStopCallback,
                        dismountAtDestination: true);

            if (result)
            {
                await Coroutine.Yield();
                if (!Core.Player.HasAura((int)AbilityAura.Sneak))
                {
                    tag.DoAbility(Ability.Sneak);
                }

                result = await Location.MoveToNoMount(UseMesh, tag.Radius, tag.Name, tag.MovementStopCallback);
            }

            return result;
        }

        public override string ToString()
        {
            return this.DynamicString("UnstealthAfter");
        }
    }*/

    public class IndirectApproachFishSpot : FishSpot { }


    [XmlElement("Collectable")]
    public  class Collectable
    {

        [XmlAttribute("PlusPlus")]
        public int PlusPlus { get; set; }

        [XmlAttribute("Value")]
        public int Value { get; set; }

        #region INamedItem Members

        [XmlAttribute("Id")]
        public uint Id { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("LocalName")]
        public string LocalName { get; set; }

        #endregion INamedItem Members

        public override string ToString()
        {
            return this.DynamicString();
        }
    }


    [XmlElement("Keeper")]
    public class Keeper
    {
        [DefaultValue(KeeperAction.KeepAll)]
        [XmlAttribute("Action")]
        public KeeperAction Action { get; set; }

        [XmlAttribute("Name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return this.DynamicString();
        }
    }

    [XmlElement("PatienceTug")]
    public class PatienceTug : IEquatable<PatienceTug>
    {
        [XmlAttribute("MoochLevel")]
        public int MoochLevel { get; set; }

        [DefaultValue(TugType.Medium)]
        [XmlAttribute("TugType")]
        public TugType TugType { get; set; }

        #region IEquatable<PatienceTug> Members

        public bool Equals(PatienceTug other)
        {
            return MoochLevel == other.MoochLevel && TugType == other.TugType;
        }

        #endregion IEquatable<PatienceTug> Members

        public override string ToString()
        {
            return this.DynamicString();
        }
    }

    public class FishResult(Item item, bool hq, float size)
    {
        //public string FishName =>

        public bool IsHighQuality { get; set; } = hq;

        //public string Name { get; set; }

        public Item Item { get; set; } = item;

        public float Size { get; set; } = size;

        public bool IsKeeper(Keeper keeper)
        {
            if (!string.Equals(keeper.Name, item.EnglishName , StringComparison.InvariantCultureIgnoreCase) && !string.Equals(keeper.Name, item.CurrentLocaleName, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if ((!keeper.Action.HasFlag(KeeperAction.KeepHq) && IsHighQuality))
            {
                return false;
            }

            return keeper.Action.HasFlag(KeeperAction.KeepNq) || IsHighQuality;
        }

        public bool ShouldMooch(Keeper keeper) => keeper.Action.HasFlag((KeeperAction)0x04);
    }

    public enum CordialType : ushort
    {
        None,

        Cordial = 6141,

        HiCordial = 12669,

        WateredCordial = 16911,

        Auto = ushort.MaxValue
    }
    public static class Cordial
    {
        public static SpellData GetSpellData()
        {
            var cordialSpellData = DataManager.GetItem((uint)CordialType.Cordial).BackingAction;

            if (cordialSpellData == null)
            {
                var item =
                    InventoryManager.FilledSlots.FirstOrDefault(
                        bs => bs.RawItemId == (uint)CordialType.WateredCordial || bs.RawItemId == (uint)CordialType.Cordial || bs.RawItemId == (uint)CordialType.HiCordial);

                if (item != null)
                {
                    cordialSpellData = item.Item.BackingAction;
                }
            }

            if (cordialSpellData == null)
            {
                Logging.Write("Cordinal_NullSpellData");
            }

            return cordialSpellData;
        }

        public static bool HasAnyCordials()
        {
            return HasWateredCordials() || HasCordials() || HasHiCordials();
        }

        public static bool HasWateredCordials()
        {
            return DataManager.GetItem((uint)CordialType.WateredCordial).ItemCount() > 0;
        }

        public static bool HasCordials()
        {
            return DataManager.GetItem((uint)CordialType.Cordial).ItemCount() > 0;
        }

        public static bool HasHiCordials()
        {
            return DataManager.GetItem((uint)CordialType.HiCordial).ItemCount() > 0;
        }
    }

}