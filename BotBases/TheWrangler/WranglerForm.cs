/*
 * WranglerForm.cs - Main UI Form (Code-Behind)
 * =============================================
 *
 * PURPOSE:
 * This is the main UI window for TheWrangler. It provides a simple, clean
 * interface for selecting a JSON file and running Lisbeth orders.
 *
 * UI COMPONENTS:
 * - File selection panel with browse button and path display
 * - Run button to queue the selected JSON for execution
 * - Status log area showing operation results
 * - Ignore Home checkbox option
 *
 * ARCHITECTURE:
 * - Form logic is here, layout is in WranglerForm.Designer.cs
 * - Uses WranglerController for bot operations
 * - Settings are loaded/saved via WranglerSettings
 *
 * IMPORTANT - EXECUTION FLOW:
 * The Run button does NOT execute immediately. It queues the order:
 * 1. User clicks Run -> QueueSelectedJson() validates and queues
 * 2. The behavior tree (in TheWranglerBotBase) picks up the order
 * 3. Order executes within the proper coroutine context
 * 4. OrderCompleted event fires when done
 *
 * NOTES FOR CLAUDE:
 * - This form runs on a separate STA thread (required for WinForms)
 * - Use Invoke() when updating UI from other threads
 * - Orders queue, not execute directly - they run in the behavior tree
 * - Keep form logic minimal - complex logic goes in controller/API classes
 */

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ff14bot.Helpers;

namespace TheWrangler
{
    /// <summary>
    /// Main UI form for TheWrangler botbase.
    /// </summary>
    public partial class WranglerForm : Form
    {
        #region Fields

        private readonly WranglerController _controller;

        #endregion

        #region Singleton

        /// <summary>
        /// Current form instance (if open).
        /// </summary>
        public static WranglerForm Instance { get; private set; }

        /// <summary>
        /// Returns true if form is open and valid.
        /// </summary>
        public static bool IsValid => Instance != null && Instance.Visible && !Instance.Disposing && !Instance.IsDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates and initializes the form.
        /// </summary>
        /// <param name="controller">The controller handling bot operations</param>
        public WranglerForm(WranglerController controller)
        {
            _controller = controller;
            Instance = this;

            InitializeComponent();
            SetupForm();
            LoadSettings();
            UpdateUIState();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Additional form setup not in designer.
        /// </summary>
        private void SetupForm()
        {
            // Set up event handlers
            _controller.StatusChanged += OnStatusChanged;
            _controller.LogMessage += OnLogMessage;
            _controller.OrderCompleted += OnOrderCompleted;

            // Set form properties
            this.Text = "TheWrangler - Lisbeth Order Runner";
            this.StartPosition = FormStartPosition.CenterScreen;

            // Apply modern styling
            ApplyModernStyling();
        }

        /// <summary>
        /// Applies modern visual styling to the form.
        /// </summary>
        private void ApplyModernStyling()
        {
            // Modern color scheme
            this.BackColor = Color.FromArgb(45, 45, 48);

            // Style the main panel
            pnlMain.BackColor = Color.FromArgb(37, 37, 38);

            // Style buttons
            StyleButton(btnBrowse, Color.FromArgb(0, 122, 204), Color.White);
            StyleButton(btnRun, Color.FromArgb(46, 204, 113), Color.White);
            StyleButton(btnStopGently, Color.FromArgb(230, 126, 34), Color.White); // Orange for stop

            // Style labels
            lblTitle.ForeColor = Color.White;
            lblSelectedFile.ForeColor = Color.FromArgb(200, 200, 200);
            lblFilePath.ForeColor = Color.FromArgb(150, 200, 255);
            lblStatus.ForeColor = Color.FromArgb(200, 200, 200);

            // Style checkbox
            chkIgnoreHome.ForeColor = Color.FromArgb(200, 200, 200);

            // Style remote port controls
            lblRemotePort.ForeColor = Color.FromArgb(200, 200, 200);
            txtRemotePort.BackColor = Color.FromArgb(60, 60, 60);
            txtRemotePort.ForeColor = Color.FromArgb(220, 220, 220);
            txtRemotePort.BorderStyle = BorderStyle.FixedSingle;
            lblServerStatus.ForeColor = Color.FromArgb(150, 150, 150);

            // Style log area
            txtLog.BackColor = Color.FromArgb(30, 30, 30);
            txtLog.ForeColor = Color.FromArgb(220, 220, 220);
        }

        /// <summary>
        /// Helper to style a button with modern flat look.
        /// </summary>
        private void StyleButton(Button btn, Color backColor, Color foreColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.Cursor = Cursors.Hand;

            // Hover effects
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(backColor);
            btn.MouseLeave += (s, e) => btn.BackColor = backColor;
        }

        /// <summary>
        /// Loads saved settings into the form.
        /// </summary>
        private void LoadSettings()
        {
            var settings = WranglerSettings.Instance;

            // Restore selected file
            if (settings.HasValidJsonPath)
            {
                lblFilePath.Text = settings.JsonFileName;
            }

            // Restore checkbox state
            chkIgnoreHome.Checked = settings.IgnoreHome;

            // Restore remote port
            txtRemotePort.Text = settings.RemoteServerPort.ToString();

            // Update server status display
            UpdateServerStatus();

            // Restore window position if valid
            if (settings.WindowX >= 0 && settings.WindowY >= 0)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(settings.WindowX, settings.WindowY);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Browse button click - opens file dialog.
        /// </summary>
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Lisbeth Order JSON";
                dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;

                // Start in last directory if available
                if (!string.IsNullOrEmpty(WranglerSettings.Instance.LastBrowseDirectory)
                    && Directory.Exists(WranglerSettings.Instance.LastBrowseDirectory))
                {
                    dialog.InitialDirectory = WranglerSettings.Instance.LastBrowseDirectory;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SelectJsonFile(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// Run button click - queues the selected JSON and starts bot if needed.
        /// </summary>
        private void btnRun_Click(object sender, EventArgs e)
        {
            if (!WranglerSettings.Instance.HasValidJsonPath)
            {
                LogToUI("Please select a JSON file first.", Color.Orange);
                return;
            }

            if (_controller.IsExecuting)
            {
                LogToUI("An order is already executing.", Color.Orange);
                return;
            }

            // Queue the order for execution by the behavior tree
            bool queued = _controller.QueueSelectedJson();

            if (queued)
            {
                // Update UI to show order is queued/running
                btnRun.Enabled = false;
                btnRun.Text = "Running...";
                btnStopGently.Enabled = true; // Enable stop button while running

                // Auto-start the bot if it's not running
                if (!TheWranglerBotBase.IsBotRunning)
                {
                    LogToUI("Starting bot...", Color.LightGreen);
                    TheWranglerBotBase.StartBot();
                }
                else
                {
                    LogToUI("Order queued, executing...", Color.LightGreen);
                }
            }
        }

        /// <summary>
        /// Checkbox changed - updates setting.
        /// </summary>
        private void chkIgnoreHome_CheckedChanged(object sender, EventArgs e)
        {
            WranglerSettings.Instance.IgnoreHome = chkIgnoreHome.Checked;
            WranglerSettings.Instance.Save();
        }

        /// <summary>
        /// Stop Gently button click - signals Lisbeth to stop after current action.
        /// </summary>
        private void btnStopGently_Click(object sender, EventArgs e)
        {
            if (!_controller.IsExecuting)
            {
                LogToUI("Nothing is currently executing.", Color.Orange);
                return;
            }

            LogToUI("Requesting gentle stop...", Color.FromArgb(230, 126, 34));
            btnStopGently.Enabled = false;
            btnStopGently.Text = "Stopping...";

            // Request stop - this signals Lisbeth to stop gracefully
            _controller.RequestStopGently();
        }

        /// <summary>
        /// Remote port text box lost focus - validates and applies port change.
        /// </summary>
        private void txtRemotePort_Leave(object sender, EventArgs e)
        {
            if (int.TryParse(txtRemotePort.Text, out int port) && port >= 1 && port <= 65535)
            {
                var settings = WranglerSettings.Instance;

                // Only restart if port actually changed
                if (settings.RemoteServerPort != port)
                {
                    settings.RemoteServerPort = port;
                    settings.Save();

                    // Restart the remote server with new port
                    if (TheWranglerBotBase.Instance != null)
                    {
                        LogToUI($"Restarting remote server on port {port}...", Color.LightBlue);
                        TheWranglerBotBase.Instance.RestartRemoteServer();
                        UpdateServerStatus();
                    }
                }
            }
            else
            {
                // Invalid port - reset to current setting
                LogToUI("Invalid port number. Using previous value.", Color.Orange);
                txtRemotePort.Text = WranglerSettings.Instance.RemoteServerPort.ToString();
            }
        }

        /// <summary>
        /// Form closing - saves settings.
        /// </summary>
        private void WranglerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save window position
            WranglerSettings.Instance.WindowX = this.Location.X;
            WranglerSettings.Instance.WindowY = this.Location.Y;
            WranglerSettings.Instance.Save();

            // Cleanup
            _controller.StatusChanged -= OnStatusChanged;
            _controller.LogMessage -= OnLogMessage;
            _controller.OrderCompleted -= OnOrderCompleted;
            Instance = null;
        }

        /// <summary>
        /// Controller status changed event.
        /// </summary>
        private void OnStatusChanged(object sender, string status)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnStatusChanged(sender, status)));
                return;
            }

            lblStatus.Text = status;
        }

        /// <summary>
        /// Controller log message event.
        /// </summary>
        private void OnLogMessage(object sender, string message)
        {
            LogToUI(message, Color.FromArgb(220, 220, 220));
        }

        /// <summary>
        /// Controller order completed event.
        /// Re-enables the Run button.
        /// </summary>
        private void OnOrderCompleted(object sender, bool success)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnOrderCompleted(sender, success)));
                return;
            }

            // Re-enable the run button
            btnRun.Enabled = true;
            btnRun.Text = "Run";

            // Log result
            if (success)
            {
                LogToUI("Orders completed successfully!", Color.LightGreen);
            }
            else
            {
                LogToUI("Orders did not complete. Check the log for details.", Color.Orange);
            }

            UpdateUIState();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sets the selected JSON file and updates UI/settings.
        /// </summary>
        private void SelectJsonFile(string filePath)
        {
            var settings = WranglerSettings.Instance;

            settings.LastJsonPath = filePath;
            settings.LastBrowseDirectory = Path.GetDirectoryName(filePath);
            settings.Save();

            lblFilePath.Text = Path.GetFileName(filePath);
            LogToUI($"Selected: {filePath}", Color.LightBlue);
            UpdateUIState();
        }

        /// <summary>
        /// Updates button enabled states based on current state.
        /// </summary>
        private void UpdateUIState()
        {
            btnRun.Enabled = WranglerSettings.Instance.HasValidJsonPath && !_controller.IsExecuting;

            // Stop Gently is only enabled when executing
            btnStopGently.Enabled = _controller.IsExecuting;
            if (!_controller.IsExecuting)
            {
                btnStopGently.Text = "Stop Gently";
            }
        }

        /// <summary>
        /// Updates the server status display.
        /// </summary>
        private void UpdateServerStatus()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(UpdateServerStatus));
                return;
            }

            var instance = TheWranglerBotBase.Instance;
            if (instance != null && instance.IsRemoteServerRunning)
            {
                lblServerStatus.Text = $"Server: Running (:{instance.RemoteServerPort})";
                lblServerStatus.ForeColor = Color.FromArgb(46, 204, 113); // Green
            }
            else
            {
                lblServerStatus.Text = "Server: Stopped";
                lblServerStatus.ForeColor = Color.FromArgb(150, 150, 150); // Gray
            }
        }

        /// <summary>
        /// Appends a message to the log area.
        /// Thread-safe.
        /// </summary>
        public void LogToUI(string message, Color color)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => LogToUI(message, color)));
                return;
            }

            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionColor = color;
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// Public method to log with default color.
        /// </summary>
        public void LogToUI(string message)
        {
            LogToUI(message, Color.FromArgb(220, 220, 220));
        }

        #endregion
    }
}
