/*
 * WranglerController.cs - Business Logic Controller
 * ==================================================
 *
 * PURPOSE:
 * This class acts as the intermediary between the UI (WranglerForm) and the
 * Lisbeth API. It handles all the business logic, keeping the form and API clean.
 *
 * RESPONSIBILITIES:
 * - Coordinate JSON loading and validation
 * - Execute Lisbeth orders via LisbethApi
 * - Report status changes to the UI
 * - Handle errors gracefully
 *
 * ARCHITECTURE:
 * WranglerForm <--events--> WranglerController <--calls--> LisbethApi
 *
 * EVENTS:
 * - StatusChanged: Fired when operation status changes
 * - LogMessage: Fired when a log message should be displayed
 *
 * NOTES FOR CLAUDE:
 * - This is the "brain" of TheWrangler
 * - All coordination between UI and API goes through here
 * - The controller doesn't know about specific UI controls
 * - Uses events to communicate back to the UI (loose coupling)
 */

using System;
using System.IO;
using System.Threading.Tasks;
using ff14bot.Helpers;

namespace TheWrangler
{
    /// <summary>
    /// Controller class that coordinates between the UI and Lisbeth API.
    /// </summary>
    public class WranglerController
    {
        #region Fields

        private readonly LisbethApi _lisbethApi;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the operation status changes.
        /// </summary>
        public event EventHandler<string> StatusChanged;

        /// <summary>
        /// Fired when a log message should be displayed.
        /// </summary>
        public event EventHandler<string> LogMessage;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the controller is ready to execute orders.
        /// </summary>
        public bool IsReady => _lisbethApi.IsInitialized;

        /// <summary>
        /// Gets the current Lisbeth API status.
        /// </summary>
        public string ApiStatus => _lisbethApi.StatusMessage;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new controller instance.
        /// </summary>
        public WranglerController()
        {
            _lisbethApi = new LisbethApi();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the controller and Lisbeth API.
        /// Call this before any operations.
        /// </summary>
        /// <returns>True if initialization succeeded</returns>
        public bool Initialize()
        {
            OnStatusChanged("Initializing...");
            OnLogMessage("Initializing Lisbeth API...");

            bool result = _lisbethApi.Initialize();

            if (result)
            {
                OnStatusChanged("Ready");
                OnLogMessage("Lisbeth API initialized successfully.");
            }
            else
            {
                OnStatusChanged("Initialization failed");
                OnLogMessage($"Failed to initialize: {_lisbethApi.StatusMessage}");
            }

            return result;
        }

        #endregion

        #region Operations

        /// <summary>
        /// Runs the currently selected JSON file through Lisbeth.
        /// </summary>
        /// <returns>True if orders completed successfully</returns>
        public async Task<bool> RunSelectedJson()
        {
            var settings = WranglerSettings.Instance;

            // Validate file exists
            if (!settings.HasValidJsonPath)
            {
                OnLogMessage("Error: No JSON file selected.");
                return false;
            }

            string filePath = settings.LastJsonPath;
            string json;

            // Load JSON content
            try
            {
                OnStatusChanged("Loading JSON...");
                OnLogMessage($"Loading: {Path.GetFileName(filePath)}");
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                OnStatusChanged("Error loading file");
                OnLogMessage($"Error reading file: {ex.Message}");
                return false;
            }

            // Validate JSON isn't empty
            if (string.IsNullOrWhiteSpace(json))
            {
                OnStatusChanged("Error: Empty file");
                OnLogMessage("Error: JSON file is empty.");
                return false;
            }

            // Ensure API is initialized
            if (!_lisbethApi.IsInitialized)
            {
                OnLogMessage("Lisbeth API not initialized, attempting to initialize...");
                if (!Initialize())
                {
                    return false;
                }
            }

            // Execute orders
            try
            {
                OnStatusChanged("Running orders...");
                OnLogMessage("Executing Lisbeth orders...");

                bool result = await _lisbethApi.ExecuteOrders(json, settings.IgnoreHome);

                OnStatusChanged(result ? "Completed" : "Did not complete");
                return result;
            }
            catch (Exception ex)
            {
                OnStatusChanged("Error");
                OnLogMessage($"Error executing orders: {ex.Message}");
                Log($"Exception in RunSelectedJson: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Stops current Lisbeth operations gracefully.
        /// </summary>
        public async Task StopCurrentOperation()
        {
            try
            {
                OnStatusChanged("Stopping...");
                OnLogMessage("Stopping current operation...");
                await _lisbethApi.StopGently();
                OnStatusChanged("Stopped");
                OnLogMessage("Operation stopped.");
            }
            catch (Exception ex)
            {
                OnLogMessage($"Error stopping: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the Lisbeth configuration window.
        /// </summary>
        public void OpenLisbethWindow()
        {
            _lisbethApi.OpenLisbethWindow();
        }

        #endregion

        #region Event Helpers

        /// <summary>
        /// Raises the StatusChanged event.
        /// </summary>
        protected void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, $"Status: {status}");
        }

        /// <summary>
        /// Raises the LogMessage event and also logs to RebornBuddy.
        /// </summary>
        protected void OnLogMessage(string message)
        {
            LogMessage?.Invoke(this, message);
            Log(message);
        }

        /// <summary>
        /// Logs to RebornBuddy with TheWrangler prefix.
        /// </summary>
        private void Log(string message)
        {
            Logging.Write($"[TheWrangler] {message}");
        }

        #endregion
    }
}
