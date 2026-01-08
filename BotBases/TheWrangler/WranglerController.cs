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
 * - Queue orders for execution by the behavior tree
 * - Report status changes to the UI
 * - Handle errors gracefully
 *
 * ARCHITECTURE:
 * WranglerForm --> WranglerController --> PendingOrder
 *                                              |
 *                                              v
 * TheWranglerBotBase (behavior tree) --> LisbethApi
 *
 * IMPORTANT - COROUTINE CONTEXT:
 * Lisbeth's ExecuteOrders MUST be called from within the bot's coroutine context
 * (behavior tree). Calling it from the UI thread will fail. Therefore:
 * 1. UI calls QueueOrder() to set up the pending order
 * 2. The behavior tree's Root checks for pending orders each tick
 * 3. When found, it executes within the proper coroutine context
 *
 * NOTES FOR CLAUDE:
 * - This is the "brain" of TheWrangler
 * - ExecuteOrders CANNOT be called from UI - must go through behavior tree
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

        /// <summary>
        /// Fired when order execution completes.
        /// </summary>
        public event EventHandler<bool> OrderCompleted;

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

        /// <summary>
        /// The pending JSON order to execute (set by UI, consumed by behavior tree).
        /// </summary>
        public string PendingOrderJson { get; private set; }

        /// <summary>
        /// Returns true if there's a pending order waiting to execute.
        /// </summary>
        public bool HasPendingOrder => !string.IsNullOrEmpty(PendingOrderJson);

        /// <summary>
        /// Returns true if an order is currently executing.
        /// </summary>
        public bool IsExecuting { get; private set; }

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
        /// Queues the selected JSON file for execution.
        /// The actual execution happens in the behavior tree via ExecutePendingOrder().
        /// </summary>
        /// <returns>True if order was queued successfully</returns>
        public bool QueueSelectedJson()
        {
            var settings = WranglerSettings.Instance;

            // Validate file exists
            if (!settings.HasValidJsonPath)
            {
                OnLogMessage("Error: No JSON file selected.");
                return false;
            }

            if (IsExecuting)
            {
                OnLogMessage("Error: An order is already executing.");
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

            // Queue the order for execution by the behavior tree
            PendingOrderJson = json;
            OnStatusChanged("Order queued - Start the bot to execute");
            OnLogMessage("Order queued. Start the bot (or it will execute on next tick if running).");

            return true;
        }

        /// <summary>
        /// Executes the pending order. MUST be called from within the behavior tree context.
        /// This is called by TheWranglerBotBase.Root during the bot tick.
        /// </summary>
        /// <returns>True if execution succeeded</returns>
        public async Task<bool> ExecutePendingOrder()
        {
            if (!HasPendingOrder)
            {
                return false;
            }

            var json = PendingOrderJson;
            var ignoreHome = WranglerSettings.Instance.IgnoreHome;

            // Clear the pending order immediately
            PendingOrderJson = null;
            IsExecuting = true;

            try
            {
                OnStatusChanged("Running orders...");
                OnLogMessage("Executing Lisbeth orders...");

                bool result = await _lisbethApi.ExecuteOrders(json, ignoreHome);

                OnStatusChanged(result ? "Completed" : "Did not complete");
                OnLogMessage(result ? "Orders completed successfully!" : "Orders did not complete.");
                OnOrderCompleted(result);

                return result;
            }
            catch (Exception ex)
            {
                OnStatusChanged("Error");
                OnLogMessage($"Error executing orders: {ex.Message}");
                Log($"Exception in ExecutePendingOrder: {ex}");
                OnOrderCompleted(false);
                return false;
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// Cancels any pending (not yet started) order.
        /// </summary>
        public void CancelPendingOrder()
        {
            if (HasPendingOrder)
            {
                PendingOrderJson = null;
                OnStatusChanged("Cancelled");
                OnLogMessage("Pending order cancelled.");
            }
        }

        /// <summary>
        /// Stops current Lisbeth operations gracefully.
        /// Note: This should be called from UI - StopGently might work outside coroutine.
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
        /// Raises the OrderCompleted event.
        /// </summary>
        protected void OnOrderCompleted(bool success)
        {
            OrderCompleted?.Invoke(this, success);
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
