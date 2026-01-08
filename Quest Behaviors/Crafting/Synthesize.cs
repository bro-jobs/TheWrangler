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
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.AClasses;
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

    [XmlElement("Synthesize")]
    public class Synthesize : ProfileBehavior
    {
        public override bool IsDone
        {
            get
            {
                return _IsDone;
            }
        }

        protected override void OnResetCachedDone()
        {
            _IsDone = false;
        }

        private bool _IsDone;
        [XmlAttribute("MinimumCp")]
        public int MinimumCp { get; set; }

        [XmlAttribute("RecipeId")]
        public uint RecipeId { get; set; }

        [XmlAttribute("UseCR")]
        public bool UseCR { get; set; }

        [XmlAttribute("RequiredSkills")]
        public int[] RequiredSkills { get; set; }



        /// <summary>
        /// List of how many high quality materials to use for each ingredient in the recipe. 
        /// Example HQMats="0,3,2" This will use all normal items for the first ingredient, 
        /// 3 high quality items for the second, and 2 high quality items for the third. Default: all zero. 
        /// Special numbers: If you set an index to -1 then it will prefer high quality mats, and then use normal mats once you run out of high quality. 
        /// A value of -2 will use normal quality until you run out then use high quality.
        /// </summary>
        [XmlAttribute("HQMats")]
        public int[] HQMats { get; set; }

        protected override void OnStart()
        {
            if (MinimumCp > Core.Player.MaxCP)
            {
                LogError("MinimumCp is greater then player max cp.");
                return;
            }

            if (RequiredSkills != null)
            {
                foreach (var skill in RequiredSkills)
                {
                    if (!ActionManager.CurrentActions.ContainsKey((uint)skill))
                    {
                        var data = DataManager.GetSpellData((uint) skill);
                        if (data != null)
                        {
                            LogError("Missing skill id {0} named {1} from class {2}",skill,data.LocalizedName,data.Job);
                        }
                        else
                        {
                            LogError("Invalid skill supplied {0} we don't have any information on this skill",skill);
                        }
                    }
                }
            }

            if (UseCR && (RoutineManager.Current == null || RoutineManager.Current.CombatBehavior == null))
            {
                LogError("UseCR is true and no combat routine is assigned or combatbehavior is null");
                return;
            }

            /*if (LastGuid != id)
            {
                LastGuid = id;
                if (LastRecipe != RecipeId)
                {
                    //Reset the window so that high quality and normal quality counts get reset
                    if (CraftingLog.IsOpen)
                    {

                    }
                    LastRecipe = RecipeId;
                }

            }*/
        }


        public async Task<bool> StartCrafting()
        {
            if (CraftingManager.CurrentRecipeId != RecipeId)
            {
                if (!await CraftingManager.SetRecipe(RecipeId))
                {
                    LogError("We don't know the required recipe.");
                    return false;
                }
            }

            await Coroutine.Sleep(500);

            RecipeData contents = CraftingManager.CurrentRecipe;

            await CraftingManager.SetQuality(HQMats);

            //Start the crafting
            if (CraftingLog.IsOpen)
            {
                await Coroutine.Sleep(1000);
                if (CraftingManager.CanCraft)
                {
                    var itemData = DataManager.GetItem(contents.ItemId);
                    Log("Crafting {0} ({1}) via {2}", itemData.CurrentLocaleName, contents.ItemId, RecipeId);
                    CraftingLog.Synthesize();
                }
                else
                {
                    LogError("Cannot craft, perhaps we are out of materials?");
                    return false;
                }

            }
            else if (WKSRecipeNotebook.IsOpen)
            {
                await Coroutine.Sleep(1000);
                if (CraftingManager.CanCraft)
                {
                    var itemData = DataManager.GetItem(contents.ItemId);
                    Log("Crafting {0} ({1}) via {2}", itemData.CurrentLocaleName, contents.ItemId, RecipeId);
                    WKSRecipeNotebook.Synthesize();
                }
                else
                {
                    LogError("Cannot craft, perhaps we are out of materials?");
                    return false;
                }
            }


            await Coroutine.Yield();
            await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => CraftingManager.IsCrafting);
            await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => !CraftingManager.AnimationLocked);

            //-1 while we aren't ready
            await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => CraftingManager.Progress >= 0);


            if (UseCR)
            {
                var behavior = RoutineManager.Current.CombatBehavior;
                while (CraftingManager.IsCrafting)
                {
                    //run combat routine while we are crafting
                    await CommonTasks.ExecuteCoroutine(behavior);
                    await Coroutine.Yield();
                }
            }

            _IsDone = true;

            return true;
        }

        protected override Composite CreateBehavior()
        {
            return new ActionRunCoroutine(ctx => StartCrafting());
        }
    }
}
