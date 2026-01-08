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
 * - Run button to execute the selected JSON
 * - Status log area showing operation results
 * - Ignore Home checkbox option
 *
 * ARCHITECTURE:
 * - Form logic is here, layout is in WranglerForm.Designer.cs
 * - Uses WranglerController for bot operations
 * - Settings are loaded/saved via WranglerSettings
 *
 * NOTES FOR CLAUDE:
 * - This form runs on a separate STA thread (required for WinForms)
 * - Use Invoke() when updating UI from other threads
 * - The form communicates with the botbase via WranglerController
 * - Keep form logic minimal - complex logic goes in controller/API classes
 */

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
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

            // Style labels
            lblTitle.ForeColor = Color.White;
            lblSelectedFile.ForeColor = Color.FromArgb(200, 200, 200);
            lblFilePath.ForeColor = Color.FromArgb(150, 200, 255);
            lblStatus.ForeColor = Color.FromArgb(200, 200, 200);

            // Style checkbox
            chkIgnoreHome.ForeColor = Color.FromArgb(200, 200, 200);

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
        /// Run button click - executes the selected JSON.
        /// </summary>
        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (!WranglerSettings.Instance.HasValidJsonPath)
            {
                LogToUI("Please select a JSON file first.", Color.Orange);
                return;
            }

            // Disable button during execution
            btnRun.Enabled = false;
            btnRun.Text = "Running...";

            try
            {
                bool success = await _controller.RunSelectedJson();
                if (success)
                {
                    LogToUI("Orders completed successfully!", Color.LightGreen);
                }
                else
                {
                    LogToUI("Orders did not complete. Check the log for details.", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                LogToUI($"Error: {ex.Message}", Color.Red);
            }
            finally
            {
                btnRun.Enabled = true;
                btnRun.Text = "Run";
                UpdateUIState();
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
            btnRun.Enabled = WranglerSettings.Instance.HasValidJsonPath;
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
