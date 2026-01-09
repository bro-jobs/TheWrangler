/*
 * TagExecutor.cs - ProfileBehavior Execution Utility
 * ===================================================
 *
 * Provides a clean way to execute RebornBuddy ProfileBehaviors from coroutine context.
 * Uses reflection to access protected CreateBehavior() and to load LlamaUtilities types
 * at runtime (avoiding compile-time dependency issues).
 *
 * Usage:
 *   await TagExecutor.ExecuteAsync(tag, successCondition, token);
 */

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot.NeoProfiles;
using ff14bot.NeoProfiles.Tags;
using TreeSharp;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Executes ProfileBehavior tags in coroutine context.
    /// </summary>
    public static class TagExecutor
    {
        private const int DefaultTimeoutSeconds = 120;

        // Cache reflection info
        private static readonly MethodInfo CreateBehaviorMethod = typeof(ProfileBehavior)
            .GetMethod("CreateBehavior", BindingFlags.Instance | BindingFlags.NonPublic);

        // LlamaUtilities types loaded at runtime
        private static Type _llTalkToType;
        private static Type _llPickUpQuestType;
        private static Type _llTurnInTagType;

        static TagExecutor()
        {
            // Try to load LlamaUtilities types at runtime
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    if (asm.FullName.Contains("LlamaUtilities"))
                    {
                        _llTalkToType = asm.GetType("LlamaUtilities.OrderbotTags.LLTalkTo");
                        _llPickUpQuestType = asm.GetType("LlamaUtilities.OrderbotTags.LLPickUpQuest");
                        _llTurnInTagType = asm.GetType("LlamaUtilities.OrderbotTags.LLTurnInTag");
                        break;
                    }
                }
            }
            catch
            {
                // Fallback to base types if LlamaUtilities not available
            }
        }

        /// <summary>
        /// Creates a TalkTo tag (LLTalkTo if available, otherwise TalkToTag).
        /// </summary>
        public static ProfileBehavior CreateTalkToTag(uint npcId, uint questId, Vector3 location)
        {
            if (_llTalkToType != null)
            {
                var tag = Activator.CreateInstance(_llTalkToType);
                SetTagProperties(tag, npcId, questId, location);
                return (ProfileBehavior)tag;
            }

            return new TalkToTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        /// <summary>
        /// Creates a PickupQuest tag (LLPickUpQuest if available, otherwise PickupQuestTag).
        /// </summary>
        public static ProfileBehavior CreatePickupQuestTag(uint npcId, uint questId, Vector3 location)
        {
            if (_llPickUpQuestType != null)
            {
                var tag = Activator.CreateInstance(_llPickUpQuestType);
                SetTagProperties(tag, npcId, questId, location);
                return (ProfileBehavior)tag;
            }

            return new PickupQuestTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        /// <summary>
        /// Creates a TurnIn tag (LLTurnInTag if available, otherwise TurnInQuestTag).
        /// </summary>
        public static ProfileBehavior CreateTurnInTag(uint npcId, uint questId, Vector3 location)
        {
            if (_llTurnInTagType != null)
            {
                var tag = Activator.CreateInstance(_llTurnInTagType);
                SetTagProperties(tag, npcId, questId, location);
                return (ProfileBehavior)tag;
            }

            return new TurnInQuestTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        private static void SetTagProperties(object tag, uint npcId, uint questId, Vector3 location)
        {
            var type = tag.GetType();
            type.GetProperty("NpcId")?.SetValue(tag, (int)npcId);
            type.GetProperty("QuestId")?.SetValue(tag, (int)questId);
            type.GetProperty("XYZ")?.SetValue(tag, location);
        }

        /// <summary>
        /// Executes a ProfileBehavior by ticking its composite until done.
        /// </summary>
        public static async Task<bool> ExecuteAsync(
            ProfileBehavior behavior,
            CancellationToken token,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            return await ExecuteAsync(behavior, () => behavior.IsDone, token, timeoutSeconds);
        }

        /// <summary>
        /// Executes a ProfileBehavior with a custom success condition.
        /// </summary>
        public static async Task<bool> ExecuteAsync(
            ProfileBehavior behavior,
            Func<bool> successCondition,
            CancellationToken token,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));

            behavior.Start();

            // Use reflection to call protected CreateBehavior method
            var composite = CreateBehaviorMethod?.Invoke(behavior, null) as Composite;
            if (composite == null)
            {
                return false;
            }

            var context = new object();
            var timeout = DateTime.Now.AddSeconds(timeoutSeconds);

            try
            {
                composite.Start(context);
                await Coroutine.Yield();

                while (!behavior.IsDone && DateTime.Now < timeout)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }

                    var status = composite.Tick(context);
                    if (status != RunStatus.Running)
                    {
                        break;
                    }

                    await Coroutine.Yield();
                }

                return successCondition();
            }
            finally
            {
                composite.Stop(context);
                behavior.Done();
            }
        }
    }
}
