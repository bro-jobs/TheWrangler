/*
 * TagExecutor.cs - ProfileBehavior Execution Utility
 * ===================================================
 *
 * Provides a clean, functional way to execute RebornBuddy ProfileBehaviors
 * (like PickupQuestTag, TurnInQuestTag, LLTalkTo) from coroutine context.
 *
 * Usage:
 *   await TagExecutor.ExecuteAsync(new LLTalkTo { NpcId = 123, XYZ = loc }, token);
 *   await TagExecutor.ExecuteAsync(new PickupQuestTag { NpcId = 123, QuestId = 456 }, token);
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Buddy.Coroutines;
using ff14bot.NeoProfiles;
using TreeSharp;

namespace TheWrangler.Leveling
{
    /// <summary>
    /// Executes ProfileBehavior tags in coroutine context.
    /// </summary>
    public static class TagExecutor
    {
        private const int DefaultTimeoutSeconds = 120;

        /// <summary>
        /// Executes a ProfileBehavior by ticking its composite until done.
        /// </summary>
        /// <param name="behavior">The ProfileBehavior to execute</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="timeoutSeconds">Timeout in seconds (default 120)</param>
        /// <returns>True if behavior completed without timing out or cancellation</returns>
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
        /// <param name="behavior">The ProfileBehavior to execute</param>
        /// <param name="successCondition">Custom condition to check for success</param>
        /// <param name="token">Cancellation token</param>
        /// <param name="timeoutSeconds">Timeout in seconds (default 120)</param>
        /// <returns>True if success condition is met</returns>
        public static async Task<bool> ExecuteAsync(
            ProfileBehavior behavior,
            Func<bool> successCondition,
            CancellationToken token,
            int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));

            behavior.Start();

            var composite = behavior.CreateBehavior();
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
