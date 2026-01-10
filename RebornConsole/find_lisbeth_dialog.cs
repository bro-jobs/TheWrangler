// RebornConsole snippet to find and click Lisbeth's confirmation dialog
// Run this AFTER triggering RequestRestart to find the dialog window

using System;
using System.Runtime.InteropServices;
using System.Text;

// Windows API imports
[DllImport("user32.dll")]
static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32.dll")]
static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

[DllImport("user32.dll")]
static extern int GetWindowTextLength(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool IsWindowVisible(IntPtr hWnd);

[DllImport("user32.dll")]
static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

[DllImport("user32.dll")]
static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32.dll")]
static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

const uint WM_CLOSE = 0x0010;
const uint BM_CLICK = 0x00F5;

Log("=== Searching for Dialog Windows ===");
Log("Looking for MessageBox, Lisbeth dialogs, or confirmation windows...");
Log("");

EnumWindows((hWnd, lParam) =>
{
    if (!IsWindowVisible(hWnd))
        return true;

    int length = GetWindowTextLength(hWnd);
    if (length == 0)
        return true;

    var titleBuilder = new StringBuilder(length + 1);
    GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
    var title = titleBuilder.ToString();

    var classBuilder = new StringBuilder(256);
    GetClassName(hWnd, classBuilder, classBuilder.Capacity);
    var className = classBuilder.ToString();

    // Look for potential dialog windows
    if (title.ToLower().Contains("lisbeth") ||
        title.ToLower().Contains("confirm") ||
        title.ToLower().Contains("restart") ||
        title.ToLower().Contains("resume") ||
        className.Contains("#32770") ||  // Standard dialog class
        className.Contains("MessageBox"))
    {
        Log($"Window: '{title}'");
        Log($"  Class: {className}");
        Log($"  Handle: {hWnd}");

        // Enumerate child windows (buttons, etc.)
        Log("  Children:");
        EnumChildWindows(hWnd, (childHwnd, childLParam) =>
        {
            var childTitleBuilder = new StringBuilder(256);
            GetWindowText(childHwnd, childTitleBuilder, childTitleBuilder.Capacity);
            var childTitle = childTitleBuilder.ToString();

            var childClassBuilder = new StringBuilder(256);
            GetClassName(childHwnd, childClassBuilder, childClassBuilder.Capacity);
            var childClass = childClassBuilder.ToString();

            if (!string.IsNullOrEmpty(childTitle) || childClass.Contains("Button"))
            {
                Log($"    [{childClass}] '{childTitle}' (Handle: {childHwnd})");
            }
            return true;
        }, IntPtr.Zero);

        Log("");
    }

    return true;
}, IntPtr.Zero);

Log("=== Search Complete ===");
Log("If you see a Lisbeth dialog, note the button handles above.");
Log("We can use SendMessage to click them programmatically.");
