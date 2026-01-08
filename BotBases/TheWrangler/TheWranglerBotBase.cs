/*
 * TheWranglerBotBase.cs - Main BotBase Class
 * ==========================================
 *
 * PURPOSE:
 * This is the main entry point for TheWrangler botbase. It integrates with
 * RebornBuddy's bot framework, providing the required interface for the bot
 * to be recognized and run.
 *
 * IMPORTANT - COROUTINE YIELDING:
 * Lisbeth needs coroutine yields to execute. We must yield while waiting
 * for Lisbeth's task to complete, otherwise Lisbeth won't get CPU time.
 *
 * NOTES FOR CLAUDE:
 * - Must yield (Coroutine.Yield()) while waiting for external tasks
 * - Use TreeRoot.Start()/Stop() to control bot from UI
 * - Reset controller state on Stop()
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
        /// FLOW:
        /// 1. If pending order -> Start it and wait (yielding)
        /// 2. If task running -> Keep yielding until done
        /// 3. Otherwise -> Idle
        /// </summary>
        private Composite CreateRoot()
        {
            return new PrioritySelector(
                // Handle pending or running orders
                new Decorator(
                    ctx => _controller.HasPendingOrder || _controller.IsTaskRunning,
                    new ActionRunCoroutine(ctx => HandleOrderExecutionAsync())
                ),
                // Idle - yield to the system
                new ActionRunCoroutine(ctx => IdleAsync())
            );
        }

        /// <summary>
        /// Handles order execution with proper coroutine yielding.
        /// Lisbeth needs yields to get execution time.
        /// </summary>
        private async Task<bool> HandleOrderExecutionAsync()
        {
            // Start pending order if we have one
            if (_controller.HasPendingOrder)
            {
                Log("Starting pending order...");
                _controller.StartPendingOrder();
            }

            // Yield while task is running - this gives Lisbeth CPU time
            while (_controller.IsTaskRunning)
            {
                // Check if task completed
                if (_controller.CheckTaskStatus())
                {
                    // Still running, yield to let Lisbeth work
                    await Coroutine.Yield();
                }
                else
                {
                    // Task completed
                    break;
                }
            }

            return false; // Allow tree to continue
        }

        /// <summary>
        /// Idle coroutine - yields to the coroutine system.
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
