/*
 * LevelingData.cs - DoH/DoL Leveling Configuration Data
 * ======================================================
 *
 * Contains all the data for leveling crafters and gatherers:
 * - Level breakpoints with Lisbeth orders
 * - Class quest definitions
 * - Gear upgrade thresholds
 *
 * This replaces the XML profiles with pure C# data.
 */

using System.Collections.Generic;
using Clio.Utilities;
using ff14bot.Enums;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Defines a Lisbeth crafting/gathering order.
    /// </summary>
    public class LisbethOrder
    {
        public uint ItemId { get; set; }
        public int Amount { get; set; }
        public string Type { get; set; } // "Carpenter", "Miner", etc.
        public bool Collectable { get; set; }
        public bool QuickSynth { get; set; }
        public bool Hq { get; set; }

        /// <summary>
        /// Generates the JSON string for Lisbeth API.
        /// </summary>
        public string ToJson()
        {
            return $"[{{'Item': {ItemId},'Group': 0,'Amount': {Amount},'Collectable': {Collectable.ToString().ToLower()},'QuickSynth': {QuickSynth.ToString().ToLower()},'SuborderQuickSynth': false,'Hq': {Hq.ToString().ToLower()},'Food': 0,'Primary': true,'Type': '{Type}','Enabled': true,'Manual': 0,'Medicine': 0}}]";
        }
    }

    /// <summary>
    /// Defines a class quest with pickup/turnin info.
    /// </summary>
    public class ClassQuest
    {
        public uint QuestId { get; set; }
        public uint PrereqQuestId { get; set; }
        public int RequiredLevel { get; set; }
        public uint NpcId { get; set; }
        public ushort ZoneId { get; set; }
        public Vector3 NpcLocation { get; set; }

        // Items to craft/gather for the quest
        public uint TurnInItemId { get; set; }
        public int TurnInItemCount { get; set; }
        public bool AllowHq { get; set; } = true;
    }

    /// <summary>
    /// Defines a level range with grind orders.
    /// </summary>
    public class LevelRange
    {
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public LisbethOrder GrindOrder { get; set; }
        public ClassQuest Quest { get; set; } // Optional quest at this level
    }

    /// <summary>
    /// Static leveling data for all DoH/DoL classes.
    /// Converted from XML profiles to C# data.
    /// </summary>
    public static class LevelingData
    {
        /// <summary>
        /// Lisbeth order item IDs by class and level.
        /// Format: Class -> (MinLevel, ItemId)
        /// </summary>
        public static readonly Dictionary<ClassJobType, List<(int minLevel, uint itemId, int amount)>> GrindItems =
            new Dictionary<ClassJobType, List<(int, uint, int)>>
        {
            [ClassJobType.Carpenter] = new List<(int, uint, int)>
            {
                (1, 5361, 10),   // Maple Lumber
                (5, 5364, 10),   // Ash Lumber
                (10, 5364, 10),  // Ash Lumber
                (15, 5367, 10),  // Elm Lumber
            },
            [ClassJobType.Blacksmith] = new List<(int, uint, int)>
            {
                (1, 5056, 10),   // Bronze Ingot
                (5, 5091, 10),   // Bronze Rivets
                (10, 5057, 10),  // Iron Ingot
                (15, 5057, 10),  // Iron Ingot
            },
            [ClassJobType.Armorer] = new List<(int, uint, int)>
            {
                (1, 5091, 10),   // Bronze Rivet
                (5, 5091, 10),   // Bronze Rivet
                (10, 5092, 10),  // Iron Rivets
                (15, 5092, 10),  // Iron Rivets
            },
            [ClassJobType.Goldsmith] = new List<(int, uint, int)>
            {
                (1, 5062, 10),   // Copper Ingot
                (5, 5258, 10),   // Ragstone Whetstone
                (10, 5063, 10),  // Brass Ingot
                (15, 5063, 10),  // Brass Ingot
            },
            [ClassJobType.Leatherworker] = new List<(int, uint, int)>
            {
                (1, 5257, 10),   // Leather
                (5, 5276, 10),   // Hard Leather
                (10, 5276, 10),  // Hard Leather
                (15, 5277, 10),  // Aldgoat Leather
            },
            [ClassJobType.Weaver] = new List<(int, uint, int)>
            {
                (1, 5333, 10),   // Hempen Yarn
                (5, 5324, 10),   // Undyed Hempen Cloth
                (10, 5334, 10),  // Cotton Yarn
                (15, 5325, 10),  // Undyed Cotton Cloth
            },
            [ClassJobType.Alchemist] = new List<(int, uint, int)>
            {
                (1, 5487, 10),   // Distilled Water
                (5, 4564, 10),   // Antidote
                (10, 5515, 10),  // Beeswax
                (15, 4856, 10),  // Clove Oil
            },
            [ClassJobType.Culinarian] = new List<(int, uint, int)>
            {
                (1, 4849, 10),   // Maple Syrup
                (5, 4850, 10),   // Honey
                (10, 4853, 10),  // Smooth Butter
                (15, 4863, 10),  // Gelatin
            },
        };

        /// <summary>
        /// Class quests by class. Each class has quests at levels 1, 5, 10, 15, 20, etc.
        /// </summary>
        public static readonly Dictionary<ClassJobType, List<ClassQuest>> ClassQuests =
            new Dictionary<ClassJobType, List<ClassQuest>>
        {
            [ClassJobType.Carpenter] = new List<ClassQuest>
            {
                // Level 1: My First Saw
                new ClassQuest
                {
                    QuestId = 65741, PrereqQuestId = 65674, RequiredLevel = 1,
                    NpcId = 1000153, ZoneId = 132,
                    NpcLocation = new Vector3(-44.87683f, -1.250002f, 56.83984f),
                    TurnInItemId = 5361, TurnInItemCount = 1
                },
                // Level 5: To Be the Wood
                new ClassQuest
                {
                    QuestId = 65675, PrereqQuestId = 65741, RequiredLevel = 5,
                    NpcId = 1000153, ZoneId = 132,
                    NpcLocation = new Vector3(-44.87683f, -1.250002f, 56.83984f),
                    TurnInItemId = 2219, TurnInItemCount = 3
                },
                // Level 10: Supplies for the Sick
                new ClassQuest
                {
                    QuestId = 65676, PrereqQuestId = 65675, RequiredLevel = 10,
                    NpcId = 1000153, ZoneId = 132,
                    NpcLocation = new Vector3(-44.87683f, -1.250002f, 56.83984f),
                    TurnInItemId = 5364, TurnInItemCount = 12
                },
                // Level 15: A Crisis of Confidence
                new ClassQuest
                {
                    QuestId = 65758, PrereqQuestId = 65676, RequiredLevel = 15,
                    NpcId = 1000153, ZoneId = 132,
                    NpcLocation = new Vector3(-44.87683f, -1.250002f, 56.83984f),
                    TurnInItemId = 0, TurnInItemCount = 0 // No item turnin
                },
                // Level 20: Nothing to Hide
                new ClassQuest
                {
                    QuestId = 65677, PrereqQuestId = 65758, RequiredLevel = 20,
                    NpcId = 1000153, ZoneId = 132,
                    NpcLocation = new Vector3(-44.87683f, -1.250002f, 56.83984f),
                    TurnInItemId = 1617, TurnInItemCount = 1 // Iron Lance (Materia Enhanced)
                },
            },
            // TODO: Add other classes' quests as needed
        };

        /// <summary>
        /// Gear upgrade thresholds - when to prompt for new gear.
        /// </summary>
        public static readonly int[] GearUpgradeLevels = { 21, 41, 53, 63, 70, 80, 90, 100 };

        /// <summary>
        /// Gets the appropriate grind item for a class at a given level.
        /// </summary>
        public static LisbethOrder GetGrindOrder(ClassJobType job, int currentLevel)
        {
            if (!GrindItems.TryGetValue(job, out var items))
                return null;

            // Find the highest level item that we can use
            (int minLevel, uint itemId, int amount) bestMatch = (0, 0, 0);
            foreach (var item in items)
            {
                if (item.minLevel <= currentLevel && item.minLevel > bestMatch.minLevel)
                {
                    bestMatch = item;
                }
            }

            if (bestMatch.itemId == 0)
                return null;

            return new LisbethOrder
            {
                ItemId = bestMatch.itemId,
                Amount = bestMatch.amount,
                Type = GetLisbethTypeName(job),
                Collectable = false,
                QuickSynth = false,
                Hq = false
            };
        }

        /// <summary>
        /// Gets the next incomplete class quest for a class.
        /// </summary>
        public static ClassQuest GetNextQuest(ClassJobType job, int currentLevel,
            System.Func<uint, bool> isQuestCompleted, System.Func<uint, bool> hasQuest)
        {
            if (!ClassQuests.TryGetValue(job, out var quests))
                return null;

            foreach (var quest in quests)
            {
                if (quest.RequiredLevel <= currentLevel &&
                    !isQuestCompleted(quest.QuestId) &&
                    !hasQuest(quest.QuestId))
                {
                    // Check prereq is done
                    if (quest.PrereqQuestId == 0 || isQuestCompleted(quest.PrereqQuestId))
                    {
                        return quest;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Converts ClassJobType to Lisbeth type name.
        /// </summary>
        public static string GetLisbethTypeName(ClassJobType job)
        {
            return job switch
            {
                ClassJobType.Carpenter => "Carpenter",
                ClassJobType.Blacksmith => "Blacksmith",
                ClassJobType.Armorer => "Armorer",
                ClassJobType.Goldsmith => "Goldsmith",
                ClassJobType.Leatherworker => "Leatherworker",
                ClassJobType.Weaver => "Weaver",
                ClassJobType.Alchemist => "Alchemist",
                ClassJobType.Culinarian => "Culinarian",
                ClassJobType.Miner => "Miner",
                ClassJobType.Botanist => "Botanist",
                ClassJobType.Fisher => "Fisher",
                _ => job.ToString()
            };
        }

        /// <summary>
        /// All crafting classes in order.
        /// </summary>
        public static readonly ClassJobType[] CraftingClasses =
        {
            ClassJobType.Carpenter,
            ClassJobType.Blacksmith,
            ClassJobType.Armorer,
            ClassJobType.Goldsmith,
            ClassJobType.Leatherworker,
            ClassJobType.Weaver,
            ClassJobType.Alchemist,
            ClassJobType.Culinarian
        };

        /// <summary>
        /// All gathering classes in order.
        /// </summary>
        public static readonly ClassJobType[] GatheringClasses =
        {
            ClassJobType.Miner,
            ClassJobType.Botanist,
            ClassJobType.Fisher
        };
    }
}
