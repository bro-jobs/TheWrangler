/*
 * ClassUnlockData.cs - DoH/DoL Class Unlock Quest Data
 * =====================================================
 *
 * Contains the quest IDs, NPC IDs, and locations for unlocking
 * all crafting and gathering classes.
 *
 * Data sourced from: Profiles/DoH-DoL-Profiles/DoH-DoL Leveling/Quests/UnlockDoHDoLClasses.xml
 */

using System.Collections.Generic;
using Clio.Utilities;
using ff14bot.Enums;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Contains unlock quest information for a single class.
    /// </summary>
    public class ClassUnlockInfo
    {
        /// <summary>The ClassJobType for this class.</summary>
        public ClassJobType Job { get; set; }

        /// <summary>Zone ID where the guild is located.</summary>
        public ushort ZoneId { get; set; }

        /// <summary>Quest ID for the prerequisite quest (talk to NPC to unlock the guild).</summary>
        public uint PrereqQuestId { get; set; }

        /// <summary>Quest ID for the unlock quest (completing this grants the class).</summary>
        public uint UnlockQuestId { get; set; }

        /// <summary>NPC ID to pick up the unlock quest from.</summary>
        public uint PickupNpcId { get; set; }

        /// <summary>Location of the pickup NPC.</summary>
        public Vector3 PickupLocation { get; set; }

        /// <summary>NPC ID to turn in the unlock quest to.</summary>
        public uint TurnInNpcId { get; set; }

        /// <summary>Location of the turn-in NPC.</summary>
        public Vector3 TurnInLocation { get; set; }
    }

    /// <summary>
    /// Static data for all DoH/DoL class unlock quests.
    /// </summary>
    public static class ClassUnlockData
    {
        /// <summary>
        /// All DoH/DoL classes that can be unlocked.
        /// </summary>
        public static readonly ClassJobType[] AllDohDolClasses = new[]
        {
            ClassJobType.Carpenter,
            ClassJobType.Blacksmith,
            ClassJobType.Armorer,
            ClassJobType.Goldsmith,
            ClassJobType.Leatherworker,
            ClassJobType.Weaver,
            ClassJobType.Alchemist,
            ClassJobType.Culinarian,
            ClassJobType.Miner,
            ClassJobType.Botanist,
            ClassJobType.Fisher
        };

        /// <summary>
        /// Unlock quest data for each class.
        /// </summary>
        public static readonly Dictionary<ClassJobType, ClassUnlockInfo> UnlockInfo = new Dictionary<ClassJobType, ClassUnlockInfo>
        {
            // Gridania Classes
            [ClassJobType.Carpenter] = new ClassUnlockInfo
            {
                Job = ClassJobType.Carpenter,
                ZoneId = 132, // New Gridania
                PrereqQuestId = 65720,
                UnlockQuestId = 65674, // "Way of the Carpenter"
                PickupNpcId = 1000148,
                PickupLocation = new Vector3(-17.67216f, -3.25f, 45.76995f),
                TurnInNpcId = 1000153,
                TurnInLocation = new Vector3(-45.85313f, -1.250001f, 57.11108f)
            },
            [ClassJobType.Leatherworker] = new ClassUnlockInfo
            {
                Job = ClassJobType.Leatherworker,
                ZoneId = 133, // Old Gridania
                PrereqQuestId = 65724,
                UnlockQuestId = 65641, // "Way of the Leatherworker"
                PickupNpcId = 1000352,
                PickupLocation = new Vector3(63.3223f, 8f, -145.0385f),
                TurnInNpcId = 1000691,
                TurnInLocation = new Vector3(71.08187f, 8f, -165.4199f)
            },
            [ClassJobType.Botanist] = new ClassUnlockInfo
            {
                Job = ClassJobType.Botanist,
                ZoneId = 133, // Old Gridania
                PrereqQuestId = 65729,
                UnlockQuestId = 65539, // "Way of the Botanist"
                PickupNpcId = 1000294,
                PickupLocation = new Vector3(-237.854f, 8f, -145.268f),
                TurnInNpcId = 1000815,
                TurnInLocation = new Vector3(-233.4313f, 6.248232f, -168.6552f)
            },

            // Limsa Lominsa Classes
            [ClassJobType.Armorer] = new ClassUnlockInfo
            {
                Job = ClassJobType.Armorer,
                ZoneId = 128, // Limsa Lominsa Upper Decks
                PrereqQuestId = 65722,
                UnlockQuestId = 65809, // "Way of the Armorer"
                PickupNpcId = 1000998,
                PickupLocation = new Vector3(-49.96643f, 42.79987f, 190.44f),
                TurnInNpcId = 1001000,
                TurnInLocation = new Vector3(-32.81106f, 41.49998f, 207.5591f)
            },
            [ClassJobType.Blacksmith] = new ClassUnlockInfo
            {
                Job = ClassJobType.Blacksmith,
                ZoneId = 128, // Limsa Lominsa Upper Decks
                PrereqQuestId = 65721,
                UnlockQuestId = 65827, // "Way of the Blacksmith"
                PickupNpcId = 1000995,
                PickupLocation = new Vector3(-50.23656f, 42.79998f, 192.5967f),
                TurnInNpcId = 1000997,
                TurnInLocation = new Vector3(-32.22669f, 44.6637f, 184.621f)
            },
            [ClassJobType.Culinarian] = new ClassUnlockInfo
            {
                Job = ClassJobType.Culinarian,
                ZoneId = 128, // Limsa Lominsa Upper Decks
                PrereqQuestId = 65727,
                UnlockQuestId = 65807, // "Way of the Culinarian"
                PickupNpcId = 1000946,
                PickupLocation = new Vector3(-61.18506f, 42.29994f, -161.9528f),
                TurnInNpcId = 1000947,
                TurnInLocation = new Vector3(-54.52119f, 44.17484f, -149.4378f)
            },
            [ClassJobType.Fisher] = new ClassUnlockInfo
            {
                Job = ClassJobType.Fisher,
                ZoneId = 129, // Limsa Lominsa Lower Decks
                PrereqQuestId = 66670,
                UnlockQuestId = 66643, // "Way of the Fisher"
                PickupNpcId = 1000859,
                PickupLocation = new Vector3(-166.4096f, 4.54997f, 152.0395f),
                TurnInNpcId = 1000857,
                TurnInLocation = new Vector3(-166.1612f, 4.550006f, 165.4742f)
            },

            // Ul'dah Classes
            [ClassJobType.Goldsmith] = new ClassUnlockInfo
            {
                Job = ClassJobType.Goldsmith,
                ZoneId = 131, // Ul'dah Steps of Thal
                PrereqQuestId = 65723,
                UnlockQuestId = 66144, // "Way of the Goldsmith"
                PickupNpcId = 1002280,
                PickupLocation = new Vector3(-34.21239f, 13.59995f, 98.9277f),
                TurnInNpcId = 1004093,
                TurnInLocation = new Vector3(-25.54107f, 12.2f, 109.7778f)
            },
            [ClassJobType.Weaver] = new ClassUnlockInfo
            {
                Job = ClassJobType.Weaver,
                ZoneId = 131, // Ul'dah Steps of Thal
                PrereqQuestId = 65725,
                UnlockQuestId = 66070, // "Way of the Weaver"
                PickupNpcId = 1002283,
                PickupLocation = new Vector3(136.8372f, 7.591891f, 97.83977f),
                TurnInNpcId = 1003818,
                TurnInLocation = new Vector3(156.6901f, 7.792007f, 100.3423f)
            },
            [ClassJobType.Alchemist] = new ClassUnlockInfo
            {
                Job = ClassJobType.Alchemist,
                ZoneId = 131, // Ul'dah Steps of Thal
                PrereqQuestId = 65726,
                UnlockQuestId = 66111, // "Way of the Alchemist"
                PickupNpcId = 1002281,
                PickupLocation = new Vector3(-114.7261f, 41.59998f, 120.8445f),
                TurnInNpcId = 1002299,
                TurnInLocation = new Vector3(-98.68385f, 40.2f, 122.3508f)
            },
            [ClassJobType.Miner] = new ClassUnlockInfo
            {
                Job = ClassJobType.Miner,
                ZoneId = 131, // Ul'dah Steps of Thal
                PrereqQuestId = 65728,
                UnlockQuestId = 66133, // "Way of the Miner"
                PickupNpcId = 1002282,
                PickupLocation = new Vector3(1.417394f, 7.599999f, 153.6795f),
                TurnInNpcId = 1002298,
                TurnInLocation = new Vector3(-16.52736f, 6.2f, 157.8005f)
            }
        };

        /// <summary>
        /// Checks if a class is a DoH (crafting) class.
        /// </summary>
        public static bool IsCrafter(ClassJobType job)
        {
            return job == ClassJobType.Carpenter ||
                   job == ClassJobType.Blacksmith ||
                   job == ClassJobType.Armorer ||
                   job == ClassJobType.Goldsmith ||
                   job == ClassJobType.Leatherworker ||
                   job == ClassJobType.Weaver ||
                   job == ClassJobType.Alchemist ||
                   job == ClassJobType.Culinarian;
        }

        /// <summary>
        /// Checks if a class is a DoL (gathering) class.
        /// </summary>
        public static bool IsGatherer(ClassJobType job)
        {
            return job == ClassJobType.Miner ||
                   job == ClassJobType.Botanist ||
                   job == ClassJobType.Fisher;
        }
    }
}
