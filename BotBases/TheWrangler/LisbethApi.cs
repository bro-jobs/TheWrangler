/*
 * LisbethApi.cs - Modular Lisbeth API Wrapper
 * ============================================
 *
 * PURPOSE:
 * This class provides a clean, modular interface to Lisbeth's internal API.
 * It uses reflection to discover and bind to Lisbeth methods at runtime,
 * allowing TheWrangler to call Lisbeth without compile-time dependencies.
 *
 * HOW IT WORKS:
 * 1. FindLisbeth() searches BotManager.Bots for a bot named "Lisbeth"
 * 2. Once found, it extracts the Lisbeth object via reflection
 * 3. The Api property is extracted and delegate methods are bound
 * 4. These delegates can then be called directly like normal methods
 *
 * USAGE:
 * var api = new LisbethApi();
 * if (api.Initialize())
 * {
 *     await api.ExecuteOrders(jsonString);
 * }
 *
 * EXTENDING THIS CLASS:
 * To add more Lisbeth API methods:
 * 1. Add a private delegate field (e.g., private Func<Task> _newMethod;)
 * 2. In Initialize(), bind it using Delegate.CreateDelegate
 * 3. Add a public wrapper method that calls the delegate
 *
 * NOTES FOR CLAUDE:
 * - This is the core interface to Lisbeth. All Lisbeth calls go through here.
 * - If Lisbeth's API changes, only this file needs updating.
 * - The reflection approach is necessary because Lisbeth is loaded dynamically.
 * - Always check IsInitialized before calling API methods.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Clio.Utilities;
using ff14bot.Helpers;
using ff14bot.Managers;

namespace TheWrangler
{
    /// <summary>
    /// Provides a modular, reflection-based interface to Lisbeth's API.
    /// This class handles all direct communication with Lisbeth.
    /// </summary>
    public class LisbethApi
    {
        #region Private Fields

        // Core Lisbeth object reference
        private object _lisbeth;
        private MethodInfo _orderMethod;

        // Lisbeth lifecycle actions - CRITICAL for proper initialization
        private Action _startAction;
        private Action _stopAction;
        private bool _hasStarted;

        // API Delegates - bound at runtime via reflection
        private Func<string> _getActiveOrders;
        private Func<string> _getIncompleteOrders;
        private Func<Task> _stopGently;
        private Action _openWindow;
        private Action<string> _requestRestart;

        // Travel API Delegates
        private Func<uint, uint, Vector3, Func<bool>, bool, Task<bool>> _travelTo;
        private Func<string, Vector3, Func<bool>, bool, Task<bool>> _travelToWithArea;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if Lisbeth was found and API methods were bound successfully.
        /// Always check this before calling API methods.
        /// </summary>
        public bool IsInitialized => _lisbeth != null && _orderMethod != null;

        /// <summary>
        /// Human-readable status message about the API state.
        /// Useful for logging/UI feedback.
        /// </summary>
        public string StatusMessage { get; private set; } = "Not initialized";

        #endregion

        #region Initialization

        /// <summary>
        /// Attempts to find and bind to Lisbeth's API.
        /// Call this once when TheWrangler starts.
        /// </summary>
        /// <returns>True if Lisbeth was found and bound successfully</returns>
        public bool Initialize()
        {
            try
            {
                // Step 1: Find the Lisbeth bot in BotManager
                var loader = BotManager.Bots.FirstOrDefault(c => c.Name == "Lisbeth");
                if (loader == null)
                {
                    StatusMessage = "Lisbeth bot not found. Is it installed?";
                    Log(StatusMessage);
                    return false;
                }

                // Step 2: Get the Lisbeth object via reflection
                var lisbethProperty = loader.GetType().GetProperty("Lisbeth");
                if (lisbethProperty == null)
                {
                    StatusMessage = "Could not find Lisbeth property on bot loader.";
                    Log(StatusMessage);
                    return false;
                }

                _lisbeth = lisbethProperty.GetValue(loader);
                if (_lisbeth == null)
                {
                    StatusMessage = "Lisbeth property was null.";
                    Log(StatusMessage);
                    return false;
                }

                // Step 3: Get the ExecuteOrders method
                _orderMethod = _lisbeth.GetType().GetMethod("ExecuteOrders");
                if (_orderMethod == null)
                {
                    StatusMessage = "Could not find ExecuteOrders method.";
                    Log(StatusMessage);
                    return false;
                }

                // Step 4: Get StartAction and StopAction - CRITICAL for Lisbeth initialization
                BindLifecycleActions();

                // Step 5: Bind additional API methods
                BindApiMethods();

                StatusMessage = "Lisbeth API initialized successfully.";
                Log(StatusMessage);
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error initializing Lisbeth API: {ex.Message}";
                Log(StatusMessage);
                return false;
            }
        }

        /// <summary>
        /// Binds the StartAction and StopAction from LisbethBot.
        /// These are CRITICAL - Lisbeth's internal systems (like SextantFlight)
        /// won't be initialized without calling StartAction first.
        /// </summary>
        private void BindLifecycleActions()
        {
            try
            {
                // Get StartAction property
                var startProp = _lisbeth.GetType().GetProperty("StartAction");
                if (startProp != null)
                {
                    _startAction = (Action)startProp.GetValue(_lisbeth);
                    Log($"DEBUG: StartAction bound: {_startAction != null}");
                }
                else
                {
                    Log("Warning: Could not find StartAction property");
                }

                // Get StopAction property
                var stopProp = _lisbeth.GetType().GetProperty("StopAction");
                if (stopProp != null)
                {
                    _stopAction = (Action)stopProp.GetValue(_lisbeth);
                    Log($"DEBUG: StopAction bound: {_stopAction != null}");
                }
                else
                {
                    Log("Warning: Could not find StopAction property");
                }
            }
            catch (Exception ex)
            {
                Log($"Warning: Error binding lifecycle actions: {ex.Message}");
            }
        }

        /// <summary>
        /// Binds additional API methods from Lisbeth's Api property.
        /// These are optional - core functionality works without them.
        /// </summary>
        private void BindApiMethods()
        {
            try
            {
                var apiObject = _lisbeth.GetType().GetProperty("Api")?.GetValue(_lisbeth);
                if (apiObject == null)
                {
                    Log("Warning: Could not access Lisbeth.Api - some features may be unavailable.");
                    return;
                }

                // Bind each API method using Delegate.CreateDelegate
                // This is safer than direct invocation and provides type checking
                _getActiveOrders = CreateDelegate<Func<string>>(apiObject, "GetActiveOrders");
                _getIncompleteOrders = CreateDelegate<Func<string>>(apiObject, "GetIncompleteOrders");
                _stopGently = CreateDelegate<Func<Task>>(apiObject, "StopGently");
                _openWindow = CreateDelegate<Action>(apiObject, "OpenWindow");
                _requestRestart = CreateDelegate<Action<string>>(apiObject, "RequestRestart");

                // Bind travel methods - these provide better navigation than LlamaLibrary
                // TravelTo(uint zone, uint subzone, Vector3 pos, Func<bool> condition, bool skipLanding)
                _travelTo = CreateDelegate<Func<uint, uint, Vector3, Func<bool>, bool, Task<bool>>>(apiObject, "TravelTo");
                // TravelToWithArea(string area, Vector3 pos, Func<bool> condition, bool skipLanding)
                _travelToWithArea = CreateDelegate<Func<string, Vector3, Func<bool>, bool, Task<bool>>>(apiObject, "TravelToWithArea");

                Log($"Additional API methods bound. TravelTo: {_travelTo != null}, TravelToWithArea: {_travelToWithArea != null}");
            }
            catch (Exception ex)
            {
                Log($"Warning: Error binding additional API methods: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to create a delegate from a method on an object.
        /// Returns null if the method doesn't exist (graceful degradation).
        /// </summary>
        private T CreateDelegate<T>(object target, string methodName) where T : Delegate
        {
            try
            {
                return (T)Delegate.CreateDelegate(typeof(T), target, methodName);
            }
            catch
            {
                Log($"Warning: Could not bind method '{methodName}'");
                return null;
            }
        }

        #endregion

        #region API Methods

        /// <summary>
        /// Executes Lisbeth orders from a JSON string.
        /// Returns the Task directly without awaiting - use for polling pattern.
        /// </summary>
        /// <param name="json">JSON string containing Lisbeth orders</param>
        /// <param name="ignoreHome">If true, skips returning to home location</param>
        /// <returns>Task that completes when orders finish</returns>
        public Task<bool> ExecuteOrdersAsync(string json, bool ignoreHome = false)
        {
            if (!IsInitialized)
            {
                Log("Error: Cannot execute orders - Lisbeth API not initialized.");
                return Task.FromResult(false);
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                Log("Error: Cannot execute orders - JSON is empty.");
                return Task.FromResult(false);
            }

            try
            {
                // Call StartAction before first execution to initialize Lisbeth's systems
                if (!_hasStarted && _startAction != null)
                {
                    Log("Calling Lisbeth StartAction to initialize systems...");
                    _startAction.Invoke();
                    _hasStarted = true;
                    Log("Lisbeth StartAction completed.");
                }

                Log("Starting Lisbeth order execution...");
                Log($"DEBUG: _lisbeth is null: {_lisbeth == null}");
                Log($"DEBUG: _orderMethod is null: {_orderMethod == null}");

                var result = _orderMethod.Invoke(_lisbeth, new object[] { json, ignoreHome });
                Log($"DEBUG: Invoke returned null: {result == null}");

                var task = (Task<bool>)result;
                Log($"DEBUG: Task created, Status: {task.Status}");

                return task;
            }
            catch (Exception ex)
            {
                Log($"Error starting orders: {ex.Message}");
                Log($"DEBUG: Exception type: {ex.GetType().FullName}");
                Log($"DEBUG: Stack trace: {ex.StackTrace}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Gets currently active orders as a JSON string.
        /// Useful for debugging or displaying status.
        /// </summary>
        public string GetActiveOrders()
        {
            return _getActiveOrders?.Invoke() ?? "{}";
        }

        /// <summary>
        /// Gets incomplete orders as a JSON string.
        /// Useful for resuming interrupted order sets.
        /// </summary>
        public string GetIncompleteOrders()
        {
            return _getIncompleteOrders?.Invoke() ?? "{}";
        }

        /// <summary>
        /// Gracefully stops current Lisbeth operations.
        /// Completes the current action before stopping.
        /// </summary>
        public async Task StopGently()
        {
            if (_stopGently != null)
            {
                await _stopGently();
            }
        }

        /// <summary>
        /// Opens the Lisbeth configuration window.
        /// Useful for advanced configuration without leaving TheWrangler.
        /// </summary>
        public void OpenLisbethWindow()
        {
            _openWindow?.Invoke();
        }

        /// <summary>
        /// Requests Lisbeth to restart/resume order execution with the provided JSON.
        /// Use this with the result from GetIncompleteOrders() to resume incomplete orders.
        /// </summary>
        /// <param name="json">JSON string containing orders to resume (typically from GetIncompleteOrders)</param>
        public void RequestRestart(string json)
        {
            if (_requestRestart == null)
            {
                Log("Warning: RequestRestart method not available.");
                return;
            }

            if (string.IsNullOrWhiteSpace(json) || json == "{}")
            {
                Log("Warning: No orders to restart.");
                return;
            }

            // Call StartAction before first execution to initialize Lisbeth's systems
            if (!_hasStarted && _startAction != null)
            {
                Log("Calling Lisbeth StartAction to initialize systems...");
                _startAction.Invoke();
                _hasStarted = true;
                Log("Lisbeth StartAction completed.");
            }

            Log("Requesting Lisbeth to restart/resume orders...");
            _requestRestart.Invoke(json);
        }

        /// <summary>
        /// Returns true if there are incomplete orders that can be resumed.
        /// </summary>
        public bool HasIncompleteOrders()
        {
            var incomplete = GetIncompleteOrders();
            return !string.IsNullOrWhiteSpace(incomplete) && incomplete != "{}";
        }

        /// <summary>
        /// Requests Lisbeth to stop gracefully - fire and forget version.
        /// This can be called from the UI thread without blocking.
        /// The stop will complete after the current action finishes.
        ///
        /// NOTE: The task may fault with "only be used from within a coroutine"
        /// because Lisbeth's internal cleanup requires coroutine context.
        /// However, the stop signal IS sent successfully - Lisbeth will stop.
        /// We suppress this specific error since it's misleading.
        /// </summary>
        public void RequestStopGently()
        {
            if (_stopGently == null)
            {
                Log("Warning: StopGently method not available.");
                return;
            }

            try
            {
                Log("Requesting Lisbeth stop gently (signal sent)...");
                // Fire-and-forget: invoke and don't await
                // The stop signal is sent immediately even if the task faults
                var task = _stopGently.Invoke();

                // The task may fault because Lisbeth's cleanup needs coroutine context,
                // but the stop signal has already been sent. Don't log the expected fault.
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        // Check if it's the expected coroutine error - don't log it
                        var msg = t.Exception?.InnerException?.Message ?? "";
                        if (!msg.Contains("coroutine"))
                        {
                            // Only log unexpected errors
                            Log($"StopGently error: {msg}");
                        }
                        // Otherwise silently ignore - stop signal was sent successfully
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error invoking StopGently: {ex.Message}");
            }
        }

        /// <summary>
        /// Requests Lisbeth to stop gracefully - async version for bot thread.
        /// This should be called from within a coroutine context (behavior tree).
        /// </summary>
        /// <returns>Task that completes when stop is signaled</returns>
        public async Task StopGentlyAsync()
        {
            if (_stopGently == null)
            {
                Log("Warning: StopGently method not available.");
                return;
            }

            try
            {
                Log("Executing StopGently on bot thread...");
                await _stopGently.Invoke();
                Log("StopGently completed.");
            }
            catch (Exception ex)
            {
                // Log but don't throw - the stop signal should have been sent
                Log($"StopGently exception (stop may still work): {ex.Message}");
            }
        }

        #endregion

        #region Travel Methods

        /// <summary>
        /// Returns true if the TravelTo method is available.
        /// </summary>
        public bool HasTravelApi => _travelTo != null || _travelToWithArea != null;

        /// <summary>
        /// Travels to a specific location using Lisbeth's navigation.
        /// This handles teleportation, flying, and complex navigation better than LlamaLibrary.
        /// </summary>
        /// <param name="zoneId">Target zone ID</param>
        /// <param name="subZoneId">Target subzone ID (use 0 if unknown)</param>
        /// <param name="position">Target position in the zone</param>
        /// <param name="stopCondition">Optional condition to stop early (null = never stop early)</param>
        /// <param name="skipLanding">If true, doesn't land after flying</param>
        /// <returns>True if navigation succeeded</returns>
        public async Task<bool> TravelToAsync(uint zoneId, uint subZoneId, Vector3 position, Func<bool> stopCondition = null, bool skipLanding = false)
        {
            if (_travelTo == null)
            {
                Log("Warning: TravelTo method not available.");
                return false;
            }

            // Call StartAction before first execution to initialize Lisbeth's systems
            if (!_hasStarted && _startAction != null)
            {
                Log("Calling Lisbeth StartAction to initialize systems...");
                _startAction.Invoke();
                _hasStarted = true;
            }

            try
            {
                Log($"TravelTo: Zone {zoneId}, SubZone {subZoneId}, Position {position}");
                // Use a non-null stop condition - Lisbeth may not handle null
                var condition = stopCondition ?? (() => false);
                return await _travelTo(zoneId, subZoneId, position, condition, skipLanding);
            }
            catch (Exception ex)
            {
                Log($"Error in TravelTo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Travels to a specific location using the area name.
        /// Useful when you have the area name (like "Gridania (The Roost)") from Lisbeth settings.
        /// </summary>
        /// <param name="area">Area name (e.g., "Gridania (The Roost)")</param>
        /// <param name="position">Target position</param>
        /// <param name="stopCondition">Optional condition to stop early</param>
        /// <param name="skipLanding">If true, doesn't land after flying</param>
        /// <returns>True if navigation succeeded</returns>
        public async Task<bool> TravelToAreaAsync(string area, Vector3 position, Func<bool> stopCondition = null, bool skipLanding = false)
        {
            if (_travelToWithArea == null)
            {
                Log("Warning: TravelToWithArea method not available.");
                return false;
            }

            // Call StartAction before first execution to initialize Lisbeth's systems
            if (!_hasStarted && _startAction != null)
            {
                Log("Calling Lisbeth StartAction to initialize systems...");
                _startAction.Invoke();
                _hasStarted = true;
            }

            try
            {
                Log($"TravelToWithArea: {area}, Position {position}");
                // Use a non-null stop condition - Lisbeth may not handle null
                var condition = stopCondition ?? (() => false);
                return await _travelToWithArea(area, position, condition, skipLanding);
            }
            catch (Exception ex)
            {
                Log($"Error in TravelToWithArea: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Calls Lisbeth's StopAction to clean up resources.
        /// Should be called when TheWrangler stops.
        /// </summary>
        public void Stop()
        {
            if (_hasStarted && _stopAction != null)
            {
                Log("Calling Lisbeth StopAction to clean up...");
                try
                {
                    _stopAction.Invoke();
                    Log("Lisbeth StopAction completed.");
                }
                catch (Exception ex)
                {
                    Log($"Warning: Error in StopAction: {ex.Message}");
                }
                _hasStarted = false;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Logs a message to the RebornBuddy log.
        /// Prefixes all messages with [TheWrangler] for easy filtering.
        /// </summary>
        private void Log(string message)
        {
            Logging.Write($"[TheWrangler] {message}");
        }

        #endregion
    }
}
