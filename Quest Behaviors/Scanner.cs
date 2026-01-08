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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;

using TreeSharp;

namespace ff14bot.NeoProfiles.Tags
{
    [XmlElement("Scan")]
    public class ScannerTag : ProfileBehavior
    {

        #region Overrides of ProfileBehavior


        [XmlAttribute("BruseIds")]
        [XmlAttribute("BruseIds")]
        public int[] BruseIds { get; set; }

        [XmlAttribute("NpcIds")]
        [XmlAttribute("NpcId")]
        public int[] NpcIds { get; set; }

        [XmlAttribute("Groups")]
        public int[][] Groups { get; set; }

        [XmlAttribute("Type")]
        public string Type { get; set; }

        [XmlAttribute("Radius")]
        public int Radius { get; set; }

        [XmlAttribute("QuestParam")]
        public string QuestParam { get; set; }

        [XmlAttribute("ItemId")]
        public string ItemId { get; set; }


        public override bool IsDone
        {
            get
            {
                if (Found.Count == 0)
                    return false;

                return (Searching.Count == Found.Count);
                //!string.IsNullOrWhiteSpace(WhileCondition) && !Condition(); 
            }
        }

        public override bool HighPriority
        {
            get { return true; }
        }

        public struct datas
        {
            public string hotspot;
            public Vector3 vector3;
        }

        private HashSet<int> Searching;
        private readonly HashSet<uint> Found = new HashSet<uint>();
        private readonly Dictionary<int, datas> vector = new Dictionary<int, datas>(); 
        protected override void OnStart()
        {
            Searching = new HashSet<int>(NpcIds);

            if (Type == "UseObject")
            {

                 tag =
                    string.Format(
                        "<UseObject NpcIds=\"{0}\" QuestId=\"{1}\" StepId=\"{2}\">", string.Join(",", NpcIds), QuestId, StepId);

                prefix = string.Format("<If Condition=\"GetQuestById({0}).{1} == {{0}}\">", QuestId, QuestParam);
                afix = "</UseObject></If>";
            }
            else if (Type == "UseItem")
            {
                 tag =
    string.Format(
        "<UseItem NpcIds=\"{0}\" ItemId=\"{1}\" QuestId=\"{2}\" StepId=\"{3}\">", string.Join(",", NpcIds),ItemId, QuestId, StepId);

                prefix = string.Format("<If Condition=\"GetQuestById({0}).{1} == {{0}}\">\r\n", QuestId, QuestParam);
                afix = "</UseItem></If>";
            }
            else if (Type == "SingleSpotUseObject")
            {
                

                prefix = string.Format("<If Condition=\"GetQuestById({0}).{1} == {{0}}\">\r\n", QuestId, QuestParam);
                afix = "</If>";
            }

            else if (Type == "UseBruseUse")
            {
                prefix = string.Format("<If Condition=\"GetQuestById({0}).{1} == {{0}}\">\r\n", QuestId, QuestParam);
                afix = "</If>";
            }

            if (Groups != null)
            {
                int highest = int.MinValue;
                int lowest = int.MaxValue;

                foreach (var group in Groups)
                {
                    foreach (var item in group)
                    {
                        if (item > highest)
                            highest = item;

                        if (item < lowest)
                            lowest = item;
                    }
                }

                if (lowest != 0)
                {
                    int counter = 0;
                    while (lowest <= highest)
                    {
                        foreach (var group in Groups)
                        {
                            for (int i = 0; i < group.Length; i++)
                            {
                                if (group[i] == lowest)
                                {
                                    group[i] = counter;
                                }
                            }
                        }
                        lowest++;
                        counter++;
                    }
                }    
            }

            

        }

        private string prefix, afix,tag;
        protected override void OnDone()
        {
            if (!string.IsNullOrEmpty(Type))
            {

                if (Type == "SingleSpotUseObject")
                {
                    int i = 1;
                    foreach (var group in Groups)
                    {

                        

                        Log(prefix, i);
                        
                        
                        foreach (var item in group)
                        {
                            var loc = vector[item].vector3;
                            tag = string.Format("<UseObject NpcId=\"{0}\" QuestId=\"{1}\" StepId=\"{2}\" XYZ=\"{3},{4},{5}\" Radius=\"{6}\" />", NpcIds[item], QuestId, StepId, loc.X, loc.Y, loc.Z, Radius);
                            Log(tag);
                        }
                        
                        Log(afix);
                        i++;
                    }
                }
                else if (Type == "UseBruseUse")
                {
                    int i = 1;
                    foreach (var group in Groups)
                    {



                        Log(prefix, i);


                        foreach (var item in group)
                        {
                            var loc = vector[item].vector3;
                            tag = string.Format("<UseObject NpcId=\"{0}\" UseTimes=\"1\" XYZ=\"{3},{4},{5}\" Radius=\"{6}\" />", NpcIds[item], QuestId, StepId, loc.X, loc.Y, loc.Z, Radius);
                            Log(tag);
                            tag = string.Format("<UseItem NpcIds=\"{0}\" ItemId=\"{1}\" UseHealthPercent=\"2\" UseTimes=\"1\" XYZ=\"{2},{3},{4}\" Radius=\"{5}\" />", string.Join(",",BruseIds), ItemId, loc.X, loc.Y, loc.Z, Radius);
                            Log(tag);

                        }

                        Log(afix);
                        i++;
                    }
                }
                else
                {
                    int i = 1;
                    foreach (var group in Groups)
                    {

                        Log(prefix, i);
                        Log(tag);
                        Log("<HotSpots>");
                        foreach (var item in group)
                        {
                            Log(vector[item].hotspot);
                        }
                        Log("</HotSpots>");
                        Log(afix);
                        i++;
                    }
                }

                
            }
            else
            {


                
            }

        }

        private async Task<bool> scan()
        {
            foreach (var obj in GameObjectManager.GameObjects)
            {

                var id = obj.NpcId;
                if (Searching.Contains((int) id) && !Found.Contains(id))
                {

                    var str = string.Format("<HotSpot XYZ=\"{0},{1},{2}\" Radius=\"75\" />",obj.Location.X,obj.Location.Y,obj.Location.Z);
                    Logging.Write(@"{0} {1}",obj.Name,id);
                    Logging.Write(str);
                    Found.Add(id);

                    vector[NpcIds.IndexOf((int)id)] = new datas()
                    {
                        hotspot = str,
                        vector3 = obj.Location
                    };

                }

            }


            return true;
        }
        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(r=>scan());
        }

        #endregion

    }
}
