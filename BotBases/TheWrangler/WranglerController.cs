/*
 * WranglerController.cs - Business Logic Controller
 * ==================================================
 *
 * PURPOSE:
 * This class acts as the intermediary between the UI (WranglerForm) and the
 * Lisbeth API. It handles all the business logic, keeping the form and API clean.
 *
 * IMPORTANT - EXECUTION FLOW:
 * The UI queues orders by setting PendingOrderJson.
 * The behavior tree checks HasPendingOrder and calls GetPendingOrderData().
 * The behavior tree then executes the order directly (awaiting Lisbeth).
 * This avoids the "multiple coroutine tasks" issue.
 *
 * NOTES FOR CLAUDE:
 * - Don't create tasks in controller - let behavior tree handle it
 * - Controller just manages state and validation
 * - Actual Lisbeth execution happens in behavior tree coroutine
 */

using System;
using System.Collections.Concurrent;
using System.IO;
using ff14bot.Behavior;
using ff14bot.Helpers;

namespace TheWrangler
{
    /// <summary>
    /// Represents a debug command to be executed on the bot thread.
    /// </summary>
    public class DebugCommand
    {
        public string Command { get; set; }
        public string Argument { get; set; }
        public Action<string> ResultCallback { get; set; }
    }

    /// <summary>
    /// Controller class that coordinates between the UI and Lisbeth API.
    /// </summary>
    public class WranglerController
    {
        #region Fields

        private readonly LisbethApi _lisbethApi;
        private readonly LevelingController _levelingController;
        private readonly ConcurrentQueue<DebugCommand> _debugCommandQueue = new ConcurrentQueue<DebugCommand>();

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
        /// Flag indicating an order is currently executing.
        /// </summary>
        public bool IsExecuting { get; private set; }

        /// <summary>
        /// Get the Lisbeth API for direct execution by behavior tree.
        /// </summary>
        public LisbethApi LisbethApi => _lisbethApi;

        /// <summary>
        /// Get the LevelingController for leveling mode operations.
        /// </summary>
        public LevelingController LevelingController => _levelingController;

        /// <summary>
        /// Returns true if there are pending debug commands.
        /// </summary>
        public bool HasPendingDebugCommand => !_debugCommandQueue.IsEmpty;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new controller instance.
        /// </summary>
        public WranglerController()
        {
            _lisbethApi = new LisbethApi();
            _levelingController = new LevelingController();
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

            // Sync state in case bot stopped externally
            SyncStateWithBot();

            if (IsExecuting || HasPendingOrder)
            {
                OnLogMessage("Error: An order is already executing or pending.");
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

            // Queue the order
            PendingOrderJson = json;
            OnStatusChanged("Order queued");
            OnLogMessage("Order queued. Starting execution...");

            return true;
        }

        /// <summary>
        /// Queues raw JSON content for execution.
        /// Used by the remote server to accept JSON directly.
        /// </summary>
        /// <param name="json">The raw JSON order content</param>
        /// <returns>True if order was queued successfully</returns>
        public bool QueueOrderJson(string json)
        {
            // Sync state in case bot stopped externally
            SyncStateWithBot();

            if (IsExecuting || HasPendingOrder)
            {
                OnLogMessage("Error: An order is already executing or pending.");
                return false;
            }

            // Validate JSON isn't empty
            if (string.IsNullOrWhiteSpace(json))
            {
                OnStatusChanged("Error: Empty JSON");
                OnLogMessage("Error: JSON content is empty.");
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

            // Queue the order
            PendingOrderJson = json;
            OnStatusChanged("Order queued (remote)");
            OnLogMessage("Order queued via remote API. Starting execution...");

            return true;
        }

        /// <summary>
        /// Gets and clears the pending order data.
        /// Called by behavior tree when starting execution.
        /// </summary>
        /// <returns>Tuple of (json, ignoreHome)</returns>
        public (string json, bool ignoreHome) GetPendingOrderData()
        {
            var json = PendingOrderJson;
            var ignoreHome = WranglerSettings.Instance.IgnoreHome;

            // Clear pending and mark as executing
            PendingOrderJson = null;
            IsExecuting = true;

            OnStatusChanged("Running orders...");
            OnLogMessage("Executing Lisbeth orders...");

            return (json, ignoreHome);
        }

        /// <summary>
        /// Called when order execution completes.
        /// </summary>
        public void OnOrderExecutionComplete(bool success)
        {
            IsExecuting = false;
            OnStatusChanged(success ? "Completed" : "Did not complete");
            OnLogMessage(success ? "Orders completed successfully!" : "Orders did not complete.");
            OnOrderCompleted(success);
        }

        /// <summary>
        /// Called when order execution fails with an error.
        /// </summary>
        public void OnOrderExecutionError(string error)
        {
            IsExecuting = false;
            OnStatusChanged("Error");
            OnLogMessage($"Error: {error}");
            OnOrderCompleted(false);
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
        /// Synchronizes controller state with actual bot running state.
        /// If the bot is not running but our state says we're executing,
        /// reset the state. This handles cases where the bot was stopped
        /// externally (e.g., by Lisbeth itself stopping the bot).
        /// </summary>
        public void SyncStateWithBot()
        {
            if (!TreeRoot.IsRunning)
            {
                if (IsExecuting || HasPendingOrder)
                {
                    Log("Bot is not running but controller state was dirty. Resetting state.");
                    PendingOrderJson = null;
                    IsExecuting = false;
                    OnStatusChanged("Ready");
                }
            }
        }

        /// <summary>
        /// Returns true if there are incomplete orders that can be resumed.
        /// </summary>
        public bool HasIncompleteOrders()
        {
            return _lisbethApi.HasIncompleteOrders();
        }

        /// <summary>
        /// Gets the incomplete orders JSON string.
        /// </summary>
        public string GetIncompleteOrders()
        {
            return _lisbethApi.GetIncompleteOrders();
        }

        /// <summary>
        /// Resumes incomplete orders using Lisbeth's RequestRestart.
        /// </summary>
        /// <returns>True if resume was initiated successfully</returns>
        public bool ResumeIncompleteOrders()
        {
            // Defensive: If bot is not running but state says we're executing,
            // it means the bot stopped externally (e.g., Lisbeth stopped it).
            // Reset the state to allow new operations.
            SyncStateWithBot();

            if (IsExecuting || HasPendingOrder)
            {
                OnLogMessage("Error: An order is already executing or pending.");
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

            var incompleteOrders = _lisbethApi.GetIncompleteOrders();
            if (string.IsNullOrWhiteSpace(incompleteOrders) || incompleteOrders == "{}")
            {
                OnLogMessage("No incomplete orders to resume.");
                return false;
            }

            OnStatusChanged("Resuming orders...");
            OnLogMessage("Resuming incomplete orders...");

            // Use RequestRestart to resume the incomplete orders
            _lisbethApi.RequestRestart(incompleteOrders);

            // Mark as executing since RequestRestart triggers order execution
            IsExecuting = true;

            return true;
        }

        /// <summary>
        /// Called when the bot stops. Resets state and notifies UI.
        /// Also calls Lisbeth's StopAction to clean up resources.
        /// </summary>
        public void OnBotStopped()
        {
            // Clean up Lisbeth resources
            _lisbethApi.Stop();

            PendingOrderJson = null;
            IsExecuting = false;
            OnStatusChanged("Bot stopped");
            OnOrderCompleted(false);
        }

        /// <summary>
        /// Opens the Lisbeth configuration window.
        /// </summary>
        public void OpenLisbethWindow()
        {
            _lisbethApi.OpenLisbethWindow();
        }

        /// <summary>
        /// Requests Lisbeth to stop gracefully after the current action.
        /// This can be called from the UI thread - it signals but doesn't block.
        /// </summary>
        public void RequestStopGently()
        {
            if (!IsExecuting)
            {
                OnLogMessage("Nothing is executing to stop.");
                return;
            }

            OnStatusChanged("Stopping gently...");
            OnLogMessage("Requesting Lisbeth to stop gently...");

            // Fire-and-forget - we just signal Lisbeth to stop
            _lisbethApi.RequestStopGently();
        }

        #endregion

        #region Debug Commands

        /// <summary>
        /// Queues a debug command to be executed on the bot thread.
        /// </summary>
        /// <param name="command">The command (e.g., "/test4")</param>
        /// <param name="argument">Optional argument</param>
        /// <param name="resultCallback">Callback to receive results (called on bot thread)</param>
        public void QueueDebugCommand(string command, string argument, Action<string> resultCallback)
        {
            _debugCommandQueue.Enqueue(new DebugCommand
            {
                Command = command,
                Argument = argument,
                ResultCallback = resultCallback
            });
        }

        /// <summary>
        /// Tries to dequeue a pending debug command.
        /// Called by behavior tree on bot thread.
        /// </summary>
        public bool TryGetDebugCommand(out DebugCommand command)
        {
            return _debugCommandQueue.TryDequeue(out command);
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
