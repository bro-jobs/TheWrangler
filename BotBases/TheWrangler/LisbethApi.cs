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

        // API Delegates - bound at runtime via reflection
        private Func<string> _getActiveOrders;
        private Func<string> _getIncompleteOrders;
        private Func<Task> _stopGently;
        private Action _openWindow;

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

                // Step 4: Bind additional API methods
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

                Log("Additional API methods bound successfully.");
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
                Log("Starting Lisbeth order execution...");
                // Return the task directly - don't await it here
                // The caller will poll IsCompleted to check status
                return (Task<bool>)_orderMethod.Invoke(_lisbeth, new object[] { json, ignoreHome });
            }
            catch (Exception ex)
            {
                Log($"Error starting orders: {ex.Message}");
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
