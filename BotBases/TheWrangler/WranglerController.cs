/*
 * WranglerController.cs - Business Logic Controller
 * ==================================================
 *
 * PURPOSE:
 * This class acts as the intermediary between the UI (WranglerForm) and the
 * Lisbeth API. It handles all the business logic, keeping the form and API clean.
 *
 * IMPORTANT - TASK MANAGEMENT:
 * RebornBuddy's coroutine system doesn't support awaiting external tasks.
 * Instead, we start the Lisbeth task and poll its status each tick.
 * This allows long-running Lisbeth operations to complete properly.
 *
 * ARCHITECTURE:
 * WranglerForm --> WranglerController --> PendingOrder
 *                                              |
 *                                              v
 * TheWranglerBotBase (behavior tree) --> LisbethApi
 *
 * NOTES FOR CLAUDE:
 * - Use polling pattern for external tasks, not await
 * - Start task, store it, check IsCompleted each tick
 * - When task completes, extract result and report
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
        private Task<bool> _runningTask;

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
        /// Returns true if a task is currently running.
        /// </summary>
        public bool IsTaskRunning => _runningTask != null && !_runningTask.IsCompleted;

        /// <summary>
        /// Returns true if an order is currently executing (either pending or running).
        /// </summary>
        public bool IsExecuting => HasPendingOrder || IsTaskRunning;

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
        /// The actual execution happens in the behavior tree via StartPendingOrder().
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
        /// Starts the pending order. Called by behavior tree.
        /// Does NOT await - starts the task and returns immediately.
        /// Use CheckTaskStatus() to poll for completion.
        /// </summary>
        public void StartPendingOrder()
        {
            if (!HasPendingOrder)
            {
                return;
            }

            var json = PendingOrderJson;
            var ignoreHome = WranglerSettings.Instance.IgnoreHome;

            // Clear the pending order
            PendingOrderJson = null;

            OnStatusChanged("Running orders...");
            OnLogMessage("Executing Lisbeth orders...");

            // Start the task without awaiting (fire and forget within our control)
            _runningTask = _lisbethApi.ExecuteOrdersAsync(json, ignoreHome);
        }

        /// <summary>
        /// Checks if the running task has completed.
        /// Call this each tick from the behavior tree.
        /// Returns true if there's still work in progress.
        /// </summary>
        public bool CheckTaskStatus()
        {
            if (_runningTask == null)
            {
                return false; // No task running
            }

            if (!_runningTask.IsCompleted)
            {
                return true; // Still running
            }

            // Task completed - process result
            bool success = false;
            try
            {
                if (_runningTask.IsFaulted)
                {
                    var ex = _runningTask.Exception?.InnerException ?? _runningTask.Exception;
                    OnStatusChanged("Error");
                    OnLogMessage($"Error executing orders: {ex?.Message ?? "Unknown error"}");
                    Log($"Task faulted: {ex}");
                }
                else if (_runningTask.IsCanceled)
                {
                    OnStatusChanged("Cancelled");
                    OnLogMessage("Order execution was cancelled.");
                }
                else
                {
                    success = _runningTask.Result;
                    OnStatusChanged(success ? "Completed" : "Did not complete");
                    OnLogMessage(success ? "Orders completed successfully!" : "Orders did not complete.");
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged("Error");
                OnLogMessage($"Error processing result: {ex.Message}");
                Log($"Exception processing task result: {ex}");
            }

            // Cleanup
            _runningTask = null;
            OnOrderCompleted(success);

            return false; // Task done
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
        /// Reports an error from the behavior tree back to the UI.
        /// </summary>
        public void ReportError(string message)
        {
            OnStatusChanged("Error");
            OnLogMessage($"Error: {message}");
            OnOrderCompleted(false);
            _runningTask = null;
            PendingOrderJson = null;
        }

        /// <summary>
        /// Called when the bot stops. Resets state and notifies UI.
        /// </summary>
        public void OnBotStopped()
        {
            // Clear any pending/running state
            PendingOrderJson = null;
            _runningTask = null;

            OnStatusChanged("Bot stopped");
            OnOrderCompleted(false); // This resets the UI button
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
