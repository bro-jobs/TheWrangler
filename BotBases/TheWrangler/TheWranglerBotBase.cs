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
 * HOW IT WORKS:
 * 1. User selects "TheWrangler" in RebornBuddy's bot dropdown
 * 2. Clicking "Settings" opens WranglerForm
 * 3. User selects a JSON file and clicks "Run"
 * 4. TheWrangler calls Lisbeth API to execute the orders
 *
 * NOTES FOR CLAUDE:
 * - The Root behavior runs continuously when the bot is active
 * - Currently Root is a no-op since orders run through the UI
 * - Future enhancement: Run orders automatically when bot starts
 * - WinForms runs on a separate STA thread (see ToggleUI method)
 */

using System;
using System.Threading;
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
        /// Currently idles - actual work is triggered via UI.
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
        /// Currently this tree just idles because TheWrangler is UI-driven.
        /// Users click "Run" in the form to execute orders.
        ///
        /// FUTURE ENHANCEMENT IDEAS:
        /// - Add auto-run mode that executes orders on start
        /// - Add scheduling/repeat functionality
        /// - Add multiple order queue support
        /// </summary>
        private Composite CreateRoot()
        {
            return new PrioritySelector(
                // Currently just a placeholder - orders run through UI
                new Decorator(
                    ctx => false, // Never runs
                    new TreeSharp.Action(ctx => RunStatus.Success)
                ),
                // Idle action - keeps the bot "running" without doing anything
                new TreeSharp.Action(ctx =>
                {
                    // Bot is idle - waiting for user to trigger via UI
                    return RunStatus.Success;
                })
            );
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
