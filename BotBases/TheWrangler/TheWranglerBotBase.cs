/*
 * TheWranglerBotBase.cs - Main BotBase Class
 * ==========================================
 *
 * PURPOSE:
 * This is the main entry point for TheWrangler botbase. It integrates with
 * RebornBuddy's bot framework, providing the required interface for the bot
 * to be recognized and run.
 *
 * IMPORTANT - TASK POLLING PATTERN:
 * RebornBuddy's coroutine system can't await external tasks.
 * Instead, we use a polling pattern:
 * 1. Start task (fire and forget)
 * 2. Each tick, check if task is completed
 * 3. When done, process result
 *
 * This approach works because we're not awaiting - we just check status.
 *
 * NOTES FOR CLAUDE:
 * - DON'T await external tasks - they break the coroutine system
 * - Use polling pattern: start task, check IsCompleted each tick
 * - Use Coroutine.Yield() for idle, NOT Task.Delay()
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Buddy.Coroutines;
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
        /// Uses polling pattern for task management.
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
        /// Cleanup any resources.
        /// </summary>
        public override void Stop()
        {
            Log("TheWrangler stopped.");
        }

        #endregion

        #region Behavior Tree

        /// <summary>
        /// Creates the main behavior tree.
        ///
        /// POLLING PATTERN:
        /// 1. If pending order exists -> Start it (sync, no await)
        /// 2. If task is running -> Check status (sync, no await)
        /// 3. Otherwise -> Yield and wait
        /// </summary>
        private Composite CreateRoot()
        {
            return new PrioritySelector(
                // Start pending order if available
                new Decorator(
                    ctx => _controller.HasPendingOrder,
                    new TreeSharp.Action(ctx =>
                    {
                        Log("Starting pending order...");
                        _controller.StartPendingOrder();
                        return RunStatus.Success;
                    })
                ),
                // Check running task status
                new Decorator(
                    ctx => _controller.IsTaskRunning,
                    new TreeSharp.Action(ctx =>
                    {
                        // Check if task completed this tick
                        _controller.CheckTaskStatus();
                        return RunStatus.Success;
                    })
                ),
                // Idle - yield to the system
                new ActionRunCoroutine(ctx => IdleAsync())
            );
        }

        /// <summary>
        /// Idle coroutine - yields to the coroutine system.
        /// IMPORTANT: Use Coroutine.Yield(), NOT Task.Delay()!
        /// </summary>
        private async Task<bool> IdleAsync()
        {
            await Coroutine.Yield();
            return false;
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
