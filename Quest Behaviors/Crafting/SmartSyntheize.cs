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

    [XmlElement("SmartSyntheize")]
    public class SmartSyntheize : ProfileBehavior
    {
        public override bool IsDone => _IsDone;

        protected override void OnResetCachedDone()
        {
            _IsDone = false;
        }

        private bool _IsDone;




        [XmlAttribute("RecipeId")]
        [DefaultValue(0)]
        public int RecipeId { get; set; }

        [XmlAttribute("ItemId")]
        [DefaultValue(0)]
        public int ItemId { get; set; }

        [XmlAttribute("Count")]
        [DefaultValue(1)]
        public int Count { get; set; }

        [XmlAttribute("TargetQualityPercent")]
        [DefaultValue(100)]
        public int TargetQualityPercent { get; set; }


        protected override void OnStart()
        {
        }

        CraftingMacroManager.CraftingMacro macro;
        public async Task<bool> StartCrafting()
        {

            if (RecipeId == 0)
            {
                RecipeId = (int)(await CraftingMacroManager.SearchRecipesByItemId((uint)ItemId));
                if (RecipeId == 0)
                {
                    LogError("Could not find a recipe that makes item {0}", ItemId);
                    return false;
                }
            }

            if (CraftingManager.CurrentRecipeId != RecipeId)
            {
                if (!await CraftingManager.SetRecipe((uint)RecipeId))
                {
                    LogError("We don't know the required recipe.");
                    return false;
                }
            }



            macro = await CraftingMacroManager.GenerateMacro((uint)RecipeId,targetQualityPercent:TargetQualityPercent);

            if (!macro.Success)
            {
                LogError("Couldn't generate a macro for {0}",RecipeId);
                return false;
            }

            if (Core.Player.IsMounted)
                await CommonTasks.StopAndDismount();

            await Coroutine.Sleep(500);

            RecipeData contents = CraftingManager.CurrentRecipe;


            for (int i = 0; i < Count; i++)
            {

                if (!macro.IsValid)
                {
                    Log("Macro no longer valid, regenerating...");
                    macro = await CraftingMacroManager.GenerateMacro((uint)RecipeId);
                    if (!macro.Success)
                    {
                        LogError("Couldn't generate a macro for {0}", RecipeId);
                        return false;
                    }

                }


                //Start the crafting
                if (CraftingLog.IsOpen)
                {
                    await Coroutine.Sleep(1000);
                    if (CraftingManager.CanCraft)
                    {
                        var itemData = DataManager.GetItem(contents.ItemId);
                        Log("Crafting {0} ({1}) via {2}", itemData.CurrentLocaleName, contents.ItemId, RecipeId);

                        //Crafting macro assumes we use HQ where we can
                        await CraftingManager.SetAllQuality(true);

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


                if (await Coroutine.Wait(5000, () => CraftingManager.IsCrafting))
                {
                    if (await Coroutine.Wait(5000, () => !CraftingManager.AnimationLocked))
                    {
                        if (await Coroutine.Wait(5000, () => CraftingManager.Progress >= 0))
                        {
                            if (!await CraftingMacroManager.MacroConsumer(macro))
                            {
                                LogError("Something went wrong crafting...");
                                return false;
                            }

                        }
                    }
                }



                await Coroutine.Wait(5000, () => CraftingLog.IsOpen || WKSRecipeNotebook.IsOpen);
                await Coroutine.Sleep(200);

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
