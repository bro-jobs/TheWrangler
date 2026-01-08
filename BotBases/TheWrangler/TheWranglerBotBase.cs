/*
 * TheWranglerBotBase.cs - Main BotBase Class
 * ==========================================
 *
 * PURPOSE:
 * This is the main entry point for TheWrangler botbase. It integrates with
 * RebornBuddy's bot framework, providing the required interface for the bot
 * to be recognized and run.
 *
 * BOT BASE INTERFACE:
 * - Name: Display name in bot selection dropdown
 * - Root: Behavior tree that runs when the bot is started
 * - Start/Stop: Called when bot execution begins/ends
 * - OnButtonPress: Opens the UI when button is clicked
 *
 * ARCHITECTURE:
 * RebornBuddy -> TheWranglerBotBase -> WranglerController -> LisbethApi
 *                          |
 *                          v
 *                    WranglerForm (UI)
 *
 * IMPORTANT - COROUTINE CONTEXT:
 * Lisbeth's ExecuteOrders MUST be called from within a coroutine context.
 * The behavior tree's Root handles this by checking for pending orders
 * and executing them via ActionRunCoroutine.
 *
 * HOW IT WORKS:
 * 1. User selects "TheWrangler" in RebornBuddy's bot dropdown
 * 2. Clicking "Settings" opens WranglerForm
 * 3. User selects a JSON file and clicks "Run" (queues the order)
 * 4. User clicks "Start" in RebornBuddy (or bot is already running)
 * 5. The behavior tree picks up the pending order and executes it
 *
 * NOTES FOR CLAUDE:
 * - Orders MUST execute in the behavior tree, not from UI thread
 * - WinForms runs on a separate STA thread (see ToggleUI method)
 * - The controller is shared between UI and behavior tree
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
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
        /// Checks for pending orders and executes them in the proper coroutine context.
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
        /// ARCHITECTURE NOTE:
        /// The behavior tree checks for pending orders from the controller.
        /// When found, it executes them within the proper coroutine context.
        /// This is necessary because Lisbeth's ExecuteOrders cannot be called
        /// from a UI thread - it must run within the behavior tree.
        ///
        /// FLOW:
        /// 1. Check if controller has pending order
        /// 2. If yes, execute via ActionRunCoroutine (proper context)
        /// 3. If no, idle and wait for next tick
        /// </summary>
        private Composite CreateRoot()
        {
            return new PrioritySelector(
                // Execute pending orders when available
                new Decorator(
                    ctx => _controller.HasPendingOrder,
                    new ActionRunCoroutine(ctx => ExecutePendingOrderAsync())
                ),
                // Idle when no orders - keeps the bot "running"
                new ActionRunCoroutine(ctx => IdleAsync())
            );
        }

        /// <summary>
        /// Executes a pending order from the controller.
        /// This runs within the behavior tree's coroutine context.
        /// </summary>
        private async Task<bool> ExecutePendingOrderAsync()
        {
            Log("Executing pending order...");
            await _controller.ExecutePendingOrder();
            return true;
        }

        /// <summary>
        /// Idle coroutine - just waits a bit to not hog CPU.
        /// </summary>
        private async Task<bool> IdleAsync()
        {
            // Small delay to prevent busy-waiting
            await Task.Delay(100);
            return true;
        }

        #endregion

        #region UI Management

        /// <summary>
        /// Toggles the UI form open/closed.
        /// Creates a new STA thread for WinForms if needed.
        ///
        /// WHY STA THREAD?
        /// WinForms requires a Single-Threaded Apartment (STA) thread
        /// because of how Windows handles COM objects and message pumping.
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
