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
using ff14bot.Helpers;
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
        private static bool _typesLoaded;

        private static void EnsureTypesLoaded()
        {
            if (_typesLoaded) return;
            _typesLoaded = true;

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
                        Logging.Write($"[TagExecutor] Loaded LlamaUtilities types: TalkTo={_llTalkToType != null}, PickUp={_llPickUpQuestType != null}, TurnIn={_llTurnInTagType != null}");
                        break;
                    }
                }

                if (_llTalkToType == null)
                {
                    Logging.Write("[TagExecutor] LlamaUtilities types not found, using base RebornBuddy tags");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"[TagExecutor] Error loading LlamaUtilities types: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a TalkTo tag (LLTalkTo if available, otherwise TalkToTag).
        /// </summary>
        public static ProfileBehavior CreateTalkToTag(uint npcId, uint questId, Vector3 location)
        {
            EnsureTypesLoaded();

            if (_llTalkToType != null)
            {
                try
                {
                    var tag = Activator.CreateInstance(_llTalkToType);
                    SetTagProperties(tag, npcId, questId, location);
                    return (ProfileBehavior)tag;
                }
                catch (Exception ex)
                {
                    Logging.Write($"[TagExecutor] Error creating LLTalkTo: {ex.Message}");
                }
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
            EnsureTypesLoaded();

            if (_llPickUpQuestType != null)
            {
                try
                {
                    var tag = Activator.CreateInstance(_llPickUpQuestType);
                    SetTagProperties(tag, npcId, questId, location);
                    return (ProfileBehavior)tag;
                }
                catch (Exception ex)
                {
                    Logging.Write($"[TagExecutor] Error creating LLPickUpQuest: {ex.Message}");
                }
            }

            return new PickupQuestTag
            {
                NpcId = (int)npcId,
                QuestId = (int)questId,
                XYZ = location
            };
        }

        /// <summary>
        /// Creates a TurnIn tag (LLTurnInTag if available, otherwise TurnInTag).
        /// </summary>
        public static ProfileBehavior CreateTurnInTag(uint npcId, uint questId, Vector3 location)
        {
            EnsureTypesLoaded();

            if (_llTurnInTagType != null)
            {
                try
                {
                    var tag = Activator.CreateInstance(_llTurnInTagType);
                    SetTagProperties(tag, npcId, questId, location);
                    return (ProfileBehavior)tag;
                }
                catch (Exception ex)
                {
                    Logging.Write($"[TagExecutor] Error creating LLTurnInTag: {ex.Message}");
                }
            }

            return new TurnInTag
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
            {
                Logging.Write("[TagExecutor] Error: behavior is null");
                return false;
            }

            if (CreateBehaviorMethod == null)
            {
                Logging.Write("[TagExecutor] Error: CreateBehaviorMethod not found via reflection");
                return false;
            }

            try
            {
                behavior.Start();

                // Use reflection to call protected CreateBehavior method
                var composite = CreateBehaviorMethod.Invoke(behavior, null) as Composite;
                if (composite == null)
                {
                    Logging.Write("[TagExecutor] Error: CreateBehavior returned null");
                    return false;
                }

                var context = new object();
                var timeout = DateTime.Now.AddSeconds(timeoutSeconds);

                composite.Start(context);
                await Coroutine.Yield();

                while (!behavior.IsDone && DateTime.Now < timeout)
                {
                    if (token.IsCancellationRequested)
                    {
                        composite.Stop(context);
                        return false;
                    }

                    var status = composite.Tick(context);
                    if (status != RunStatus.Running)
                    {
                        break;
                    }

                    await Coroutine.Yield();
                }

                composite.Stop(context);
                behavior.Done();

                return successCondition();
            }
            catch (Exception ex)
            {
                Logging.Write($"[TagExecutor] ExecuteAsync error: {ex.Message}");
                Logging.Write($"[TagExecutor] Stack: {ex.StackTrace}");
                return false;
            }
        }
    }
}
