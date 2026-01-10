/*
 * TheWranglerBotBase.cs - Main BotBase Class
 * ==========================================
 *
 * PURPOSE:
 * This is the main entry point for TheWrangler botbase. It integrates with
 * RebornBuddy's bot framework, providing the required interface for the bot
 * to be recognized and run.
 *
 * CRITICAL - BEHAVIOR TREE STRUCTURE:
 * Use a SINGLE ActionRunCoroutine that handles all states internally.
 * Do NOT use multiple Decorators with conditions that change during execution!
 *
 * BAD PATTERN (causes coroutine orphaning):
 *   new PrioritySelector(
 *       new Decorator(ctx => HasPendingOrder, new ActionRunCoroutine(...)),
 *       new Decorator(ctx => IsExecuting, new ActionRunCoroutine(...))
 *   );
 * When HasPendingOrder becomes false, the tree switches to the second Decorator,
 * potentially orphaning the running coroutine.
 *
 * GOOD PATTERN (used here):
 *   return new ActionRunCoroutine(ctx => MainLoopAsync());
 * Single coroutine checks state internally and awaits Lisbeth directly.
 *
 * NOTES FOR CLAUDE:
 * - Use single ActionRunCoroutine with internal state checks
 * - Await Lisbeth's task directly (it's coroutine-compatible)
 * - Return false from coroutine to re-evaluate next tick
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using TreeSharp;
using TheWrangler.Leveling;

namespace TheWrangler
{
    /// <summary>
    /// Main BotBase class for TheWrangler.
    /// This integrates with RebornBuddy's bot framework.
    /// </summary>
    public class TheWranglerBotBase : BotBase
    {
        #region Constants

        private static readonly Color LogColor = Color.FromRgb(46, 204, 113); // Green

        #endregion

        #region Fields

        private Composite _root;
        private WranglerController _controller;
        private Thread _guiThread;
        private WranglerForm _form;
        private RemoteServer _remoteServer;

        // Pending async debug command (for /test5 go home)
        private Action<string> _pendingGoHomeCallback;

        #endregion

        #region BotBase Properties

        /// <summary>
        /// Display name shown in RebornBuddy's bot selection.
        /// </summary>
        public override string Name => "TheWrangler";

        /// <summary>
        /// Flags indicating what the bot needs to pulse.
        /// All = everything gets updated each tick.
        /// </summary>
        public override PulseFlags PulseFlags => PulseFlags.All;

        /// <summary>
        /// Indicates this bot can run independently (not just combat).
        /// </summary>
        public override bool IsAutonomous => true;

        /// <summary>
        /// Show the settings button in RebornBuddy UI.
        /// </summary>
        public override bool WantButton => true;

        /// <summary>
        /// This bot doesn't require an Order Bot profile.
        /// </summary>
        public override bool RequiresProfile => false;

        /// <summary>
        /// The behavior tree that runs when the bot is active.
        /// </summary>
        public override Composite Root => _root ?? (_root = CreateRoot());

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes TheWrangler BotBase.
        /// Starts the remote server immediately so it's available even before bot starts.
        /// </summary>
        public TheWranglerBotBase()
        {
            _controller = new WranglerController();

            // Start remote server immediately if enabled
            // This allows remote control even when bot isn't running
            StartRemoteServer();

            Log("TheWrangler BotBase initialized.");
        }

        #endregion

        #region BotBase Methods

        /// <summary>
        /// Called when the user clicks the Settings button.
        /// Opens the main UI form.
        /// </summary>
        public override void OnButtonPress()
        {
            ToggleUI();
        }

        /// <summary>
        /// Called when the bot starts running.
        /// Initializes the Lisbeth API connection.
        /// </summary>
        public override void Start()
        {
            Log("TheWrangler started.");
            _controller.Initialize();
        }

        /// <summary>
        /// Called when the bot stops running.
        /// Reset controller state and notify UI.
        /// </summary>
        public override void Stop()
        {
            Log("TheWrangler stopped.");
            _controller.OnBotStopped();
        }

        #endregion

        #region Behavior Tree

        /// <summary>
        /// Creates the main behavior tree.
        ///
        /// IMPORTANT: Use a single ActionRunCoroutine to avoid issues with
        /// Decorator conditions changing mid-execution. If we use Decorators
        /// that check HasPendingOrder, the condition changes when we start
        /// executing, which can orphan the running coroutine.
        ///
        /// This single-coroutine approach handles all states internally.
        /// </summary>
        private Composite CreateRoot()
        {
            return new ActionRunCoroutine(ctx => MainLoopAsync());
        }

        /// <summary>
        /// Main coroutine loop that handles all states.
        /// Checks state each tick and acts accordingly.
        /// </summary>
        private async Task<bool> MainLoopAsync()
        {
            // Process any pending debug commands (runs on bot thread for memory access)
            while (_controller.TryGetDebugCommand(out var debugCmd))
            {
                ExecuteDebugCommand(debugCmd);
            }

            // Check if there's a pending go home action (async debug command)
            if (_pendingGoHomeCallback != null)
            {
                await ExecuteGoHomeAsync();
            }
            // Check if there's a pending order to execute
            else if (_controller.HasPendingOrder)
            {
                await ExecuteOrderAsync();
            }
            // Check if leveling mode is pending start
            else if (_controller.LevelingController.IsPendingStart)
            {
                Log("Starting leveling mode execution...");
                await _controller.LevelingController.ExecuteLevelingAsync();
            }
            else
            {
                // Idle - yield to the coroutine system
                await Coroutine.Yield();
            }

            // Return false to allow tree to re-evaluate (calls us again next tick)
            return false;
        }

        /// <summary>
        /// Executes the pending "go home" action.
        /// Creates helper on-demand to ensure Lisbeth is fully initialized.
        /// </summary>
        private async Task ExecuteGoHomeAsync()
        {
            var callback = _pendingGoHomeCallback;
            _pendingGoHomeCallback = null; // Clear before execution

            try
            {
                Log("Executing Go Home navigation...");

                // Create helper on-demand so Lisbeth's travel API is available
                var homeHelper = new LisbethHomeHelper();
                var result = await homeHelper.GoHomeAsync();

                if (result.Success)
                {
                    callback?.Invoke($"Success: {result.Message}");
                }
                else
                {
                    callback?.Invoke($"Failed: {result.Message}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error in Go Home: {ex.Message}");
                callback?.Invoke($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes the pending order by awaiting Lisbeth directly.
        /// The await handles all coroutine integration automatically.
        /// Includes retry logic for CoroutineStoppedException which can occur
        /// when resuming after a forced stop (Lisbeth's internal state is corrupted).
        /// </summary>
        private async Task ExecuteOrderAsync()
        {
            Log("Executing pending order...");

            // Get order data (clears pending, sets executing)
            var (json, ignoreHome, isResume) = _controller.GetPendingOrderData();

            // For resume orders, use RequestRestart instead of ExecuteOrders
            // RequestRestart doesn't re-expand suborders, which prevents duplication
            if (isResume)
            {
                await ExecuteResumeOrderAsync(json);
                return;
            }

            // Retry once for CoroutineStoppedException - this can happen when
            // Lisbeth's internal state is corrupted from a previous forced stop
            const int maxRetries = 1;
            int attempt = 0;

            while (attempt <= maxRetries)
            {
                try
                {
                    // Await Lisbeth directly - this is the key!
                    // Lisbeth's ExecuteOrders is coroutine-compatible.
                    if (attempt > 0)
                    {
                        Log($"Retry attempt {attempt}...");
                        // Small delay before retry to let Lisbeth clean up
                        await Coroutine.Sleep(500);
                    }

                    bool result = await _controller.LisbethApi.ExecuteOrdersAsync(json, ignoreHome);

                    _controller.OnOrderExecutionComplete(result);
                    return; // Success - exit the retry loop
                }
                catch (Exception ex) when (ex.GetType().Name == "CoroutineStoppedException" && attempt < maxRetries)
                {
                    // This can happen when Lisbeth's internal state is corrupted
                    // from a previous forced stop. Retry once.
                    Log($"CoroutineStoppedException on attempt {attempt + 1}, will retry...");
                    attempt++;
                }
                catch (Exception ex)
                {
                    // Log full exception details for debugging
                    Log($"Error executing order: {ex.Message}");
                    Log($"Exception type: {ex.GetType().FullName}");
                    Log($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Log($"Inner exception: {ex.InnerException.Message}");
                        Log($"Inner stack trace: {ex.InnerException.StackTrace}");
                    }
                    Logging.WriteException(ex);
                    _controller.OnOrderExecutionError(ex.Message);
                    return; // Exit on non-retryable error
                }
            }
        }

        /// <summary>
        /// Executes a resume order using RequestRestart.
        /// RequestRestart is fire-and-forget, so we poll for completion.
        /// </summary>
        private async Task ExecuteResumeOrderAsync(string json)
        {
            Log("Resuming orders using RequestRestart...");

            try
            {
                // RequestRestart doesn't re-expand suborders - it uses them as-is
                _controller.LisbethApi.RequestRestart(json);

                // Poll for completion - check if there are still active orders
                // We'll poll every 2 seconds for up to 8 hours (max reasonable runtime)
                const int pollIntervalMs = 2000;
                const int maxPolls = 14400; // 8 hours

                for (int i = 0; i < maxPolls; i++)
                {
                    await Coroutine.Sleep(pollIntervalMs);

                    // Check if there are still active orders
                    var activeOrders = _controller.LisbethApi.GetActiveOrders();
                    if (string.IsNullOrWhiteSpace(activeOrders) || activeOrders == "{}" || activeOrders == "[]")
                    {
                        // No more active orders - check if incomplete
                        var incompleteOrders = _controller.LisbethApi.GetIncompleteOrders();
                        bool success = string.IsNullOrWhiteSpace(incompleteOrders) || incompleteOrders == "{}";

                        Log(success ? "Resume completed successfully!" : "Resume completed with incomplete orders.");
                        _controller.OnOrderExecutionComplete(success);
                        return;
                    }
                }

                // Timeout after max polls
                Log("Resume timed out after maximum duration.");
                _controller.OnOrderExecutionComplete(false);
            }
            catch (Exception ex)
            {
                Log($"Error in resume: {ex.Message}");
                Logging.WriteException(ex);
                _controller.OnOrderExecutionError(ex.Message);
            }
        }

        /// <summary>
        /// Executes a debug command on the bot thread.
        /// This allows full access to GameObjectManager and other memory-based APIs.
        /// </summary>
        private void ExecuteDebugCommand(DebugCommand cmd)
        {
            try
            {
                // Special handling for async commands
                if (cmd.Command.ToLower() == "/test5")
                {
                    // Queue go home for async execution in MainLoopAsync
                    _pendingGoHomeCallback = cmd.ResultCallback;
                    return;
                }

                var result = cmd.Command.ToLower() switch
                {
                    "/test4" => ExecuteDebugTest4_ListNpcs(),
                    "/unlock" => ExecuteDebugUnlock_CheckStatus(),
                    _ => $"Unknown bot-thread command: {cmd.Command}"
                };

                cmd.ResultCallback?.Invoke(result);
            }
            catch (Exception ex)
            {
                cmd.ResultCallback?.Invoke($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Test 4: List nearby NPCs (runs on bot thread for memory access).
        /// </summary>
        private string ExecuteDebugTest4_ListNpcs()
        {
            var results = new System.Text.StringBuilder();

            if (Core.Me == null)
            {
                return "Character not loaded.";
            }

            results.AppendLine($"Character: {Core.Me.Name}, Location: {Core.Me.Location}");

            // Use GameObjects property (NOT GetObjectsOfType which returns empty)
            var npcs = ff14bot.Managers.GameObjectManager.GameObjects
                .Where(o => o.IsVisible && o.Type == ff14bot.Enums.GameObjectType.EventNpc)
                .OrderBy(o => o.Distance())
                .Take(10)
                .ToList();

            if (npcs.Count == 0)
            {
                // Check for any visible objects
                var allVisible = ff14bot.Managers.GameObjectManager.GameObjects
                    .Where(o => o.IsVisible)
                    .ToList();

                if (allVisible.Count == 0)
                {
                    results.AppendLine("No visible game objects found.");
                }
                else
                {
                    var types = allVisible.GroupBy(o => o.Type).Select(g => $"{g.Key}: {g.Count()}");
                    results.AppendLine($"No NPCs found. Visible objects: {string.Join(", ", types)}");
                }
            }
            else
            {
                results.AppendLine($"Found {npcs.Count} NPC(s):");
                foreach (var npc in npcs)
                {
                    results.AppendLine($"  {npc.Name} (ID: {npc.NpcId}, Type: {npc.Type}) - Distance: {npc.Distance():F1}");
                }
            }

            return results.ToString();
        }

        /// <summary>
        /// Check DoH/DoL class unlock status (runs on bot thread for memory access).
        /// Uses both Core.Me.Levels and QuestLogManager.IsQuestCompleted.
        /// </summary>
        private string ExecuteDebugUnlock_CheckStatus()
        {
            var results = new System.Text.StringBuilder();

            if (Core.Me == null)
            {
                return "Character not loaded.";
            }

            results.AppendLine($"Character: {Core.Me.Name}");
            results.AppendLine();
            results.AppendLine("=== DoH (Crafters) ===");

            // Check all DoH/DoL classes
            foreach (var job in Leveling.ClassUnlockData.AllDohDolClasses)
            {
                // Check if class is unlocked via level
                var level = Core.Me.Levels[job];
                var isUnlockedByLevel = level > 0;

                // Get unlock quest info
                Leveling.ClassUnlockData.UnlockInfo.TryGetValue(job, out var unlockInfo);
                var isUnlockedByQuest = unlockInfo != null &&
                    ff14bot.Managers.QuestLogManager.IsQuestCompleted(unlockInfo.UnlockQuestId);

                var status = isUnlockedByLevel ? $"Lv{level}" : (isUnlockedByQuest ? "Quest Done (no level?)" : "LOCKED");
                var questStatus = unlockInfo != null ?
                    $"Quest {unlockInfo.UnlockQuestId}: {(ff14bot.Managers.QuestLogManager.IsQuestCompleted(unlockInfo.UnlockQuestId) ? "Complete" : "Incomplete")}" : "N/A";

                // Insert separator between DoH and DoL
                if (job == ff14bot.Enums.ClassJobType.Miner && results.ToString().Contains("Culinarian"))
                {
                    results.AppendLine();
                    results.AppendLine("=== DoL (Gatherers) ===");
                }

                results.AppendLine($"{job}: {status} | {questStatus}");
            }

            return results.ToString();
        }

        #endregion

        #region UI Management

        /// <summary>
        /// Toggles the UI form open/closed.
        /// Creates a new STA thread for WinForms if needed.
        /// </summary>
        private void ToggleUI()
        {
            // If form is open, close it
            if (_guiThread != null && _guiThread.IsAlive && _form != null && _form.Visible)
            {
                CloseForm();
                return;
            }

            // Create new thread for the form
            _guiThread = new Thread(() =>
            {
                try
                {
                    _form = new WranglerForm(_controller);
                    Application.Run(_form);
                }
                catch (Exception ex)
                {
                    Log($"Error in UI thread: {ex.Message}");
                    Logging.WriteException(ex);
                }
            })
            {
                IsBackground = true,
                Name = "TheWrangler UI Thread"
            };

            _guiThread.SetApartmentState(ApartmentState.STA);
            _guiThread.Start();

            Log("UI opened.");
        }

        /// <summary>
        /// Closes the form safely from any thread.
        /// Uses Invoke to ensure form is closed on its own thread.
        /// </summary>
        private void CloseForm()
        {
            if (_form != null && _form.Visible && !_form.IsDisposed)
            {
                try
                {
                    _form.Invoke((MethodInvoker)delegate
                    {
                        _form.Close();
                    });
                }
                catch (Exception ex)
                {
                    Log($"Error closing form: {ex.Message}");
                }
            }
        }

        #endregion

        #region Static Methods for UI

        /// <summary>
        /// Starts the bot. Can be called from UI.
        /// </summary>
        public static void StartBot()
        {
            if (!TreeRoot.IsRunning)
            {
                TreeRoot.Start();
            }
        }

        /// <summary>
        /// Stops the bot. Can be called from UI.
        /// </summary>
        public static void StopBot()
        {
            if (TreeRoot.IsRunning)
            {
                TreeRoot.Stop();
            }
        }

        /// <summary>
        /// Returns true if the bot is currently running.
        /// </summary>
        public static bool IsBotRunning => TreeRoot.IsRunning;

        /// <summary>
        /// Current instance of the BotBase (for static access).
        /// </summary>
        public static TheWranglerBotBase Instance { get; private set; }

        #endregion

        #region Remote Server

        /// <summary>
        /// Starts the remote server if enabled in settings.
        /// </summary>
        private void StartRemoteServer()
        {
            Instance = this; // Store instance for static access

            var settings = WranglerSettings.Instance;
            if (settings.RemoteServerEnabled)
            {
                try
                {
                    _remoteServer = new RemoteServer(_controller, settings.RemoteServerPort);
                    _remoteServer.Start();
                }
                catch (Exception ex)
                {
                    Log($"Failed to start remote server: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Stops the remote server if running.
        /// </summary>
        private void StopRemoteServer()
        {
            if (_remoteServer != null)
            {
                _remoteServer.Stop();
                _remoteServer.Dispose();
                _remoteServer = null;
            }
        }

        /// <summary>
        /// Restarts the remote server with current settings.
        /// Called from UI when port is changed.
        /// </summary>
        public void RestartRemoteServer()
        {
            StopRemoteServer();
            StartRemoteServer();
        }

        /// <summary>
        /// Returns true if the remote server is currently running.
        /// </summary>
        public bool IsRemoteServerRunning => _remoteServer?.IsRunning ?? false;

        /// <summary>
        /// Gets the current remote server port.
        /// </summary>
        public int RemoteServerPort => _remoteServer?.Port ?? 0;

        #endregion

        #region Logging

        /// <summary>
        /// Logs a message to RebornBuddy with TheWrangler prefix.
        /// </summary>
        private static void Log(string message)
        {
            Logging.Write(LogColor, $"[TheWrangler] {message}");
        }

        #endregion
    }
}
