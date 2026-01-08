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
        /// </summary>
        public TheWranglerBotBase()
        {
            _controller = new WranglerController();
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
            // Check if there's a pending order to execute
            if (_controller.HasPendingOrder)
            {
                await ExecuteOrderAsync();
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
        /// Executes the pending order by awaiting Lisbeth directly.
        /// The await handles all coroutine integration automatically.
        /// </summary>
        private async Task ExecuteOrderAsync()
        {
            Log("Executing pending order...");

            // Get order data (clears pending, sets executing)
            var (json, ignoreHome) = _controller.GetPendingOrderData();

            try
            {
                // Await Lisbeth directly - this is the key!
                // Lisbeth's ExecuteOrders is coroutine-compatible.
                bool result = await _controller.LisbethApi.ExecuteOrdersAsync(json, ignoreHome);

                _controller.OnOrderExecutionComplete(result);
            }
            catch (Exception ex)
            {
                Log($"Error executing order: {ex.Message}");
                _controller.OnOrderExecutionError(ex.Message);
            }
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
