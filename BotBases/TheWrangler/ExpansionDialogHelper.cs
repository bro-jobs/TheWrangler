/*
 * ExpansionDialogHelper.cs - Auto-Click Lisbeth Expansion Dialog
 * ==============================================================
 *
 * PURPOSE:
 * When resuming orders via RequestRestart, Lisbeth shows an "Expansion" dialog
 * that requires user confirmation. This helper automatically clicks the Yes button.
 *
 * HOW IT WORKS:
 * 1. Uses Process.GetProcessesByName to find RebornBuddy windows with "Expansion" title
 * 2. For each found window, uses UI Automation to find buttons
 * 3. Identifies the Yes button by position (leftmost of the 3 bottom buttons)
 * 4. Invokes the button click via UI Automation's InvokePattern
 *
 * NOTES FOR CLAUDE:
 * - Uses reflection to load UIAutomationClient/UIAutomationTypes assemblies
 * - Button names are empty in WPF, so we identify by position (bounding rectangle)
 * - The dialog title is "Expansion", not "Resume" (taskbar shows "Resume")
 * - Must handle multiple dialogs when resuming orders for multiple clients
 */

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ff14bot.Helpers;

namespace TheWrangler
{
    /// <summary>
    /// Helper class that automatically clicks the Yes button on Lisbeth's Expansion dialog.
    /// </summary>
    public class ExpansionDialogHelper
    {
        #region Fields

        // Cached reflection types and methods for UI Automation
        private Assembly _uiaClient;
        private Assembly _uiaTypes;
        private Type _aeType;
        private Type _propCondType;
        private Type _controlTypeType;
        private Type _treeScopeType;
        private Type _invokePatternType;
        private MethodInfo _fromHandle;
        private MethodInfo _findAllMethod;
        private MethodInfo _getPatternMethod;
        private FieldInfo _buttonField;
        private FieldInfo _ctPropField;
        private FieldInfo _boundingRectPropField;
        private FieldInfo _invokePatternField;
        private PropertyInfo _countProp;
        private PropertyInfo _indexerProp;
        private MethodInfo _getCurrentPropValue;
        private object _buttonControlType;
        private object _ctProperty;
        private object _boundingRectProperty;
        private object _invokePattern;
        private object _descendantsScope;

        private bool _isInitialized;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the UI Automation reflection bindings.
        /// Call this once before using TryClickYes.
        /// </summary>
        public bool Initialize()
        {
            if (_isInitialized)
                return true;

            try
            {
                // Load UI Automation assemblies
                _uiaClient = Assembly.Load("UIAutomationClient");
                _uiaTypes = Assembly.Load("UIAutomationTypes");

                // Get types
                _aeType = _uiaClient.GetType("System.Windows.Automation.AutomationElement");
                _propCondType = _uiaClient.GetType("System.Windows.Automation.PropertyCondition");
                _controlTypeType = _uiaTypes.GetType("System.Windows.Automation.ControlType");
                _treeScopeType = _uiaTypes.GetType("System.Windows.Automation.TreeScope");
                _invokePatternType = _uiaClient.GetType("System.Windows.Automation.InvokePattern");

                // Get methods
                _fromHandle = _aeType.GetMethod("FromHandle");
                _findAllMethod = _aeType.GetMethod("FindAll");
                _getPatternMethod = _aeType.GetMethod("GetCurrentPattern");

                // Get fields for properties
                _buttonField = _controlTypeType.GetField("Button");
                _ctPropField = _aeType.GetField("ControlTypeProperty");
                _boundingRectPropField = _aeType.GetField("BoundingRectangleProperty");
                _invokePatternField = _invokePatternType.GetField("Pattern");

                // Get static values
                _buttonControlType = _buttonField.GetValue(null);
                _ctProperty = _ctPropField.GetValue(null);
                _boundingRectProperty = _boundingRectPropField.GetValue(null);
                _invokePattern = _invokePatternField.GetValue(null);
                _descendantsScope = Enum.Parse(_treeScopeType, "Descendants");

                // Get GetCurrentPropertyValue method
                _getCurrentPropValue = _aeType.GetMethod("GetCurrentPropertyValue", new[] { _ctProperty.GetType() });

                _isInitialized = true;
                Log("ExpansionDialogHelper initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize ExpansionDialogHelper: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Finds all Expansion dialogs and clicks Yes on each one.
        /// Returns the number of dialogs that were successfully clicked.
        /// </summary>
        public int TryClickAllExpansionDialogs()
        {
            if (!_isInitialized && !Initialize())
                return 0;

            int clickedCount = 0;

            try
            {
                // Find all RebornBuddy processes that might have Expansion dialogs
                var rbProcesses = Process.GetProcessesByName("RebornBuddy");

                foreach (var proc in rbProcesses)
                {
                    try
                    {
                        // Check if this process has a window titled "Expansion"
                        // Note: MainWindowTitle might show "Resume" in taskbar but actual title is "Expansion"
                        if (proc.MainWindowTitle == "Expansion" || proc.MainWindowTitle == "Resume")
                        {
                            var handle = proc.MainWindowHandle;
                            if (handle != IntPtr.Zero && TryClickYesOnWindow(handle))
                            {
                                clickedCount++;
                                Log($"Clicked Yes on Expansion dialog for PID {proc.Id}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error checking process {proc.Id}: {ex.Message}");
                    }
                }

                // Also check the current process (in case it's showing the dialog)
                var currentProc = Process.GetCurrentProcess();
                if (currentProc.MainWindowTitle == "Expansion" || currentProc.MainWindowTitle == "Resume")
                {
                    var handle = currentProc.MainWindowHandle;
                    if (handle != IntPtr.Zero && TryClickYesOnWindow(handle))
                    {
                        clickedCount++;
                        Log($"Clicked Yes on Expansion dialog for current process");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error in TryClickAllExpansionDialogs: {ex.Message}");
            }

            return clickedCount;
        }

        /// <summary>
        /// Checks if there are any Expansion dialogs currently visible.
        /// </summary>
        public bool HasExpansionDialog()
        {
            try
            {
                // Check current process first
                var currentProc = Process.GetCurrentProcess();
                if (currentProc.MainWindowTitle == "Expansion" || currentProc.MainWindowTitle == "Resume")
                    return true;

                // Check all RebornBuddy processes
                var rbProcesses = Process.GetProcessesByName("RebornBuddy");
                foreach (var proc in rbProcesses)
                {
                    try
                    {
                        if (proc.MainWindowTitle == "Expansion" || proc.MainWindowTitle == "Resume")
                            return true;
                    }
                    catch { }
                }
            }
            catch { }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Attempts to click the Yes button on a specific window.
        /// </summary>
        private bool TryClickYesOnWindow(IntPtr handle)
        {
            try
            {
                // Get the automation element from the window handle
                var rootElement = _fromHandle.Invoke(null, new object[] { handle });
                if (rootElement == null)
                    return false;

                // Create condition: ControlType == Button
                var condition = Activator.CreateInstance(_propCondType, new object[] { _ctProperty, _buttonControlType });

                // Find all buttons
                var buttons = _findAllMethod.Invoke(rootElement, new object[] { _descendantsScope, condition });
                if (buttons == null)
                    return false;

                // Get count
                if (_countProp == null)
                    _countProp = buttons.GetType().GetProperty("Count");
                var count = (int)_countProp.GetValue(buttons);

                if (count == 0)
                    return false;

                // Get indexer
                if (_indexerProp == null)
                    _indexerProp = buttons.GetType().GetProperty("Item");

                // Find the Yes button - it's the leftmost of the 3 bottom buttons
                // We need to find buttons at the same Y position (bottom row) and pick the leftmost
                object yesButton = null;
                double minX = double.MaxValue;
                double bottomY = 0;

                // First pass: find the bottom row Y coordinate (exclude title bar buttons)
                for (int i = 0; i < count; i++)
                {
                    var button = _indexerProp.GetValue(buttons, new object[] { i });
                    var bounds = _getCurrentPropValue.Invoke(button, new object[] { _boundingRectProperty });

                    if (bounds != null)
                    {
                        // bounds is a System.Windows.Rect
                        var y = (double)bounds.GetType().GetProperty("Y").GetValue(bounds);
                        var height = (double)bounds.GetType().GetProperty("Height").GetValue(bounds);

                        // Bottom buttons are typically at Y > 500 (below the list)
                        // and have height around 30-40px (not tiny minimize/close buttons)
                        if (y > 500 && height > 25 && height < 50)
                        {
                            if (y > bottomY)
                                bottomY = y;
                        }
                    }
                }

                // Second pass: find the leftmost button at the bottom Y
                for (int i = 0; i < count; i++)
                {
                    var button = _indexerProp.GetValue(buttons, new object[] { i });
                    var bounds = _getCurrentPropValue.Invoke(button, new object[] { _boundingRectProperty });

                    if (bounds != null)
                    {
                        var x = (double)bounds.GetType().GetProperty("X").GetValue(bounds);
                        var y = (double)bounds.GetType().GetProperty("Y").GetValue(bounds);

                        // Check if this button is on the bottom row (within 10px tolerance)
                        if (Math.Abs(y - bottomY) < 10 && x < minX)
                        {
                            minX = x;
                            yesButton = button;
                        }
                    }
                }

                if (yesButton == null)
                {
                    Log("Could not identify Yes button by position.");
                    return false;
                }

                // Click the button using InvokePattern
                var pattern = _getPatternMethod.Invoke(yesButton, new object[] { _invokePattern });
                if (pattern == null)
                {
                    Log("Could not get InvokePattern from Yes button.");
                    return false;
                }

                var invokeMethod = pattern.GetType().GetMethod("Invoke");
                invokeMethod.Invoke(pattern, null);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Error in TryClickYesOnWindow: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logs a message with TheWrangler prefix.
        /// </summary>
        private void Log(string message)
        {
            Logging.Write($"[TheWrangler] {message}");
        }

        #endregion
    }
}
