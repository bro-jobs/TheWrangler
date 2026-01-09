/*
 * WranglerForm.cs - Main UI Form (Code-Behind)
 * =============================================
 *
 * PURPOSE:
 * This is the main UI window for TheWrangler. It provides two modes:
 * 1. Order Mode: Run individual Lisbeth JSON orders
 * 2. Leveling Mode: Automated DoH/DoL leveling to 100
 *
 * UI COMPONENTS:
 * - TabControl with Order Mode and Leveling Mode tabs
 * - File selection panel for Order Mode
 * - Class levels display for Leveling Mode
 * - Current directive status for Leveling Mode
 * - Missing items warnings for Leveling Mode
 *
 * ARCHITECTURE:
 * - Form logic is here, layout is in WranglerForm.Designer.cs
 * - Uses WranglerController for Order Mode operations
 * - Uses LevelingController for Leveling Mode operations
 * - Settings are loaded/saved via WranglerSettings
 */

using System;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private readonly LevelingController _levelingController;

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
            _levelingController = new LevelingController();
            Instance = this;

            InitializeComponent();
            SetupForm();
            LoadSettings();
            UpdateUIState();

            // Initialize class levels display
            RefreshClassLevels();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Additional form setup not in designer.
        /// </summary>
        private void SetupForm()
        {
            // Set up Order Mode event handlers
            _controller.StatusChanged += OnStatusChanged;
            _controller.LogMessage += OnLogMessage;
            _controller.OrderCompleted += OnOrderCompleted;

            // Set up Leveling Mode event handlers
            _levelingController.DirectiveChanged += OnDirectiveChanged;
            _levelingController.LogMessage += OnLevelingLogMessage;
            _levelingController.LevelingCompleted += OnLevelingCompleted;
            _levelingController.ClassLevelsUpdated += OnClassLevelsUpdated;

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

            // Style tab control
            tabControl.BackColor = Color.FromArgb(37, 37, 38);

            // Style tab pages
            tabOrderMode.BackColor = Color.FromArgb(37, 37, 38);
            tabLevelingMode.BackColor = Color.FromArgb(37, 37, 38);

            // Style Order Mode buttons
            StyleButton(btnBrowse, Color.FromArgb(0, 122, 204), Color.White);
            StyleButton(btnRun, Color.FromArgb(46, 204, 113), Color.White);
            StyleButton(btnStopGently, Color.FromArgb(230, 126, 34), Color.White);

            // Style Leveling Mode buttons
            StyleButton(btnStartLeveling, Color.FromArgb(46, 204, 113), Color.White);
            StyleButton(btnStopLeveling, Color.FromArgb(230, 126, 34), Color.White);

            // Style labels
            lblTitle.ForeColor = Color.White;

            // Order Mode labels
            lblSelectedFile.ForeColor = Color.FromArgb(200, 200, 200);
            lblFilePath.ForeColor = Color.FromArgb(150, 200, 255);
            lblStatus.ForeColor = Color.FromArgb(200, 200, 200);
            chkIgnoreHome.ForeColor = Color.FromArgb(200, 200, 200);
            lblRemotePort.ForeColor = Color.FromArgb(200, 200, 200);
            txtRemotePort.BackColor = Color.FromArgb(60, 60, 60);
            txtRemotePort.ForeColor = Color.FromArgb(220, 220, 220);
            txtRemotePort.BorderStyle = BorderStyle.FixedSingle;
            lblServerStatus.ForeColor = Color.FromArgb(150, 150, 150);
            txtLog.BackColor = Color.FromArgb(30, 30, 30);
            txtLog.ForeColor = Color.FromArgb(220, 220, 220);

            // Leveling Mode labels
            lblLevelingTitle.ForeColor = Color.FromArgb(200, 200, 200);
            lblCurrentDirective.ForeColor = Color.FromArgb(100, 200, 255);
            lblDirectiveDetail.ForeColor = Color.FromArgb(180, 180, 180);
            lblClassLevelsHeader.ForeColor = Color.FromArgb(200, 200, 200);
            lblCrafterLevels.ForeColor = Color.FromArgb(220, 220, 220);
            lblGathererLevels.ForeColor = Color.FromArgb(220, 220, 220);
            lblMissingItemsHeader.ForeColor = Color.FromArgb(200, 200, 200);
            lblLevelingStatus.ForeColor = Color.FromArgb(200, 200, 200);

            // Leveling Mode panels
            pnlLevelingStatus.BackColor = Color.FromArgb(50, 50, 55);
            pnlClassLevels.BackColor = Color.FromArgb(50, 50, 55);
            pnlMissingItems.BackColor = Color.FromArgb(50, 50, 55);

            // Leveling Mode text boxes
            txtMissingItems.BackColor = Color.FromArgb(40, 40, 45);
            txtMissingItems.ForeColor = Color.FromArgb(255, 200, 100); // Orange/yellow for warnings
            txtLevelingLog.BackColor = Color.FromArgb(30, 30, 30);
            txtLevelingLog.ForeColor = Color.FromArgb(220, 220, 220);

            // Debug Mode styling
            tabDebugMode.BackColor = Color.FromArgb(37, 37, 38);
            txtDebugCommands.BackColor = Color.FromArgb(40, 40, 45);
            txtDebugCommands.ForeColor = Color.FromArgb(200, 200, 200);
            txtDebugCommand.BackColor = Color.FromArgb(50, 50, 55);
            txtDebugCommand.ForeColor = Color.FromArgb(220, 220, 220);
            txtDebugLog.BackColor = Color.FromArgb(30, 30, 30);
            txtDebugLog.ForeColor = Color.FromArgb(220, 220, 220);
            StyleButton(btnRunDebugCommand, Color.FromArgb(0, 122, 204), Color.White);
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

        #region Order Mode Event Handlers

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
                btnStopGently.Enabled = true;

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

                if (settings.RemoteServerPort != port)
                {
                    settings.RemoteServerPort = port;
                    settings.Save();

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
                LogToUI("Invalid port number. Using previous value.", Color.Orange);
                txtRemotePort.Text = WranglerSettings.Instance.RemoteServerPort.ToString();
            }
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
        /// </summary>
        private void OnOrderCompleted(object sender, bool success)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnOrderCompleted(sender, success)));
                return;
            }

            btnRun.Enabled = true;
            btnRun.Text = "Run";

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

        #region Leveling Mode Event Handlers

        /// <summary>
        /// Start Leveling button click - begins DoH/DoL leveling.
        /// </summary>
        private void btnStartLeveling_Click(object sender, EventArgs e)
        {
            if (_levelingController.IsRunning)
            {
                LogToLevelingUI("Leveling is already running.", Color.Orange);
                return;
            }

            // Update UI
            btnStartLeveling.Enabled = false;
            btnStartLeveling.Text = "Running...";
            btnStopLeveling.Enabled = true;

            // Check for missing items first
            LogToLevelingUI("Checking for required items...", Color.LightBlue);
            var missingItems = _levelingController.CheckRequiredItems();
            UpdateMissingItemsDisplay(missingItems);

            // Start the leveling process
            LogToLevelingUI("Starting DoH/DoL leveling...", Color.LightGreen);
            _levelingController.StartLeveling();

            // Auto-start the bot if it's not running
            if (!TheWranglerBotBase.IsBotRunning)
            {
                LogToLevelingUI("Starting bot...", Color.LightGreen);
                TheWranglerBotBase.StartBot();
            }
        }

        /// <summary>
        /// Stop Leveling button click - stops the leveling process.
        /// </summary>
        private void btnStopLeveling_Click(object sender, EventArgs e)
        {
            if (!_levelingController.IsRunning)
            {
                LogToLevelingUI("Leveling is not running.", Color.Orange);
                return;
            }

            LogToLevelingUI("Stopping leveling...", Color.FromArgb(230, 126, 34));
            btnStopLeveling.Enabled = false;
            btnStopLeveling.Text = "Stopping...";

            _levelingController.StopLeveling();
        }

        /// <summary>
        /// Leveling directive changed event.
        /// </summary>
        private void OnDirectiveChanged(object sender, DirectiveChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnDirectiveChanged(sender, e)));
                return;
            }

            lblCurrentDirective.Text = e.Directive;
            lblDirectiveDetail.Text = e.Detail;
        }

        /// <summary>
        /// Leveling log message event.
        /// </summary>
        private void OnLevelingLogMessage(object sender, string message)
        {
            LogToLevelingUI(message, Color.FromArgb(220, 220, 220));
        }

        /// <summary>
        /// Leveling completed event.
        /// </summary>
        private void OnLevelingCompleted(object sender, bool success)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnLevelingCompleted(sender, success)));
                return;
            }

            btnStartLeveling.Enabled = true;
            btnStartLeveling.Text = "Start Leveling";
            btnStopLeveling.Enabled = false;
            btnStopLeveling.Text = "Stop";

            if (success)
            {
                lblCurrentDirective.Text = "Completed!";
                lblDirectiveDetail.Text = "All DoH/DoL classes leveled to 100!";
                LogToLevelingUI("Leveling completed successfully!", Color.LightGreen);
            }
            else
            {
                lblCurrentDirective.Text = "Stopped";
                lblDirectiveDetail.Text = "Leveling was stopped or encountered an error.";
                LogToLevelingUI("Leveling stopped.", Color.Orange);
            }
        }

        /// <summary>
        /// Class levels updated event.
        /// </summary>
        private void OnClassLevelsUpdated(object sender, ClassLevelsEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => OnClassLevelsUpdated(sender, e)));
                return;
            }

            lblCrafterLevels.Text = e.CrafterLevelsDisplay;
            lblGathererLevels.Text = e.GathererLevelsDisplay;
        }

        #endregion

        #region Debug Mode Event Handlers

        /// <summary>
        /// Run debug command button click.
        /// </summary>
        private void btnRunDebugCommand_Click(object sender, EventArgs e)
        {
            ExecuteDebugCommand(txtDebugCommand.Text.Trim());
        }

        /// <summary>
        /// Debug command text box key down - run on Enter.
        /// </summary>
        private void txtDebugCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ExecuteDebugCommand(txtDebugCommand.Text.Trim());
            }
        }

        /// <summary>
        /// Execute a debug command.
        /// </summary>
        private void ExecuteDebugCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                LogToDebugUI("Please enter a command.", Color.Orange);
                return;
            }

            LogToDebugUI($"> {command}", Color.FromArgb(150, 200, 255));

            var parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLower();
            var arg = parts.Length > 1 ? parts[1] : "";

            try
            {
                switch (cmd)
                {
                    case "/test1":
                        ExecuteTest1_ChangeClass(arg);
                        break;
                    case "/test2":
                        ExecuteTest2_TeleportTo(arg);
                        break;
                    case "/test3":
                        ExecuteTest3_Navigation();
                        break;
                    case "/test4":
                        ExecuteTest4_ListNpcs();
                        break;
                    case "/unlock":
                        ExecuteUnlockStatus();
                        break;
                    case "/stop":
                        ExecuteStopMovement();
                        break;
                    case "/help":
                        ShowDebugHelp();
                        break;
                    default:
                        LogToDebugUI($"Unknown command: {cmd}. Type /help for available commands.", Color.Orange);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogToDebugUI($"Error: {ex.Message}", Color.FromArgb(255, 100, 100));
            }
        }

        /// <summary>
        /// Test 1: Change class using gearset with auto dialog handling.
        /// </summary>
        private void ExecuteTest1_ChangeClass(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                jobName = "Carpenter";
            }

            LogToDebugUI($"Test 1: Changing class to {jobName}...", Color.LightGreen);

            if (!Enum.TryParse<ff14bot.Enums.ClassJobType>(jobName.Trim(), true, out var targetJob))
            {
                LogToDebugUI($"Unknown job: {jobName}", Color.Orange);
                return;
            }

            var currentJob = ff14bot.Core.Me.CurrentJob;
            LogToDebugUI($"Current job: {currentJob}", Color.White);

            if (currentJob == targetJob)
            {
                LogToDebugUI($"Already on {targetJob}!", Color.LightGreen);
                return;
            }

            var gearSets = ff14bot.Managers.GearsetManager.GearSets
                .Where(gs => gs.InUse && gs.Class == targetJob)
                .ToList();

            if (gearSets.Count == 0)
            {
                LogToDebugUI($"No gearset found for {targetJob}", Color.Orange);
                return;
            }

            LogToDebugUI($"Found {gearSets.Count} gearset(s) for {targetJob}", Color.White);
            LogToDebugUI($"Activating gearset {gearSets.First().Index}...", Color.White);

            gearSets.First().Activate();

            // Handle dialogs asynchronously
            LogToDebugUI("Waiting for dialogs...", Color.LightBlue);
            System.Threading.Tasks.Task.Run(async () =>
            {
                var timeout = 10000;
                var elapsed = 0;
                var dialogsHandled = 0;

                // Wait a bit for dialog to appear
                await System.Threading.Tasks.Task.Delay(500);

                while (elapsed < timeout && ff14bot.Core.Me.CurrentJob != targetJob)
                {
                    if (ff14bot.RemoteWindows.SelectYesno.IsOpen)
                    {
                        LogToDebugUI("Confirming gear replacement dialog...", Color.LightBlue);
                        ff14bot.RemoteWindows.SelectYesno.ClickYes();
                        dialogsHandled++;
                        await System.Threading.Tasks.Task.Delay(500);
                        elapsed += 500;
                        continue;
                    }
                    await System.Threading.Tasks.Task.Delay(100);
                    elapsed += 100;
                }

                if (ff14bot.Core.Me.CurrentJob == targetJob)
                {
                    LogToDebugUI($"Success! Changed to {targetJob}. Dialogs handled: {dialogsHandled}", Color.LightGreen);
                }
                else
                {
                    LogToDebugUI($"Class change may have failed or timed out. Current job: {ff14bot.Core.Me.CurrentJob}", Color.Orange);
                }
            });
        }

        /// <summary>
        /// Test 2: Teleport to an aetheryte.
        /// </summary>
        private void ExecuteTest2_TeleportTo(string aetheryteIdStr)
        {
            if (string.IsNullOrWhiteSpace(aetheryteIdStr))
            {
                aetheryteIdStr = "8"; // Default: Limsa Lominsa
            }

            if (!uint.TryParse(aetheryteIdStr, out var aetheryteId))
            {
                LogToDebugUI($"Invalid aetheryte ID: {aetheryteIdStr}", Color.Orange);
                return;
            }

            LogToDebugUI($"Test 2: Teleporting to aetheryte {aetheryteId}...", Color.LightGreen);
            LogToDebugUI($"Current zone: {ff14bot.Managers.WorldManager.ZoneId}", Color.White);

            if (!ff14bot.Managers.WorldManager.CanTeleport())
            {
                LogToDebugUI("Cannot teleport right now (combat, casting, etc.)", Color.Orange);
                return;
            }

            var locations = ff14bot.Managers.WorldManager.AvailableLocations;
            if (!locations.Any(l => l.AetheryteId == aetheryteId))
            {
                LogToDebugUI($"Aetheryte {aetheryteId} is not unlocked", Color.Orange);
                LogToDebugUI("Common IDs: 2=Gridania, 8=Limsa, 9=Ul'dah", Color.LightBlue);
                return;
            }

            ff14bot.Managers.WorldManager.TeleportById(aetheryteId);
            LogToDebugUI("Teleport initiated! Character should start casting.", Color.LightGreen);
        }

        /// <summary>
        /// Test 3: Navigation - move forward.
        /// </summary>
        private void ExecuteTest3_Navigation()
        {
            LogToDebugUI("Test 3: Starting navigation test...", Color.LightGreen);

            var currentLocation = ff14bot.Core.Me.Location;
            LogToDebugUI($"Current location: {currentLocation}", Color.White);

            var target = new Clio.Utilities.Vector3(
                currentLocation.X + 10,
                currentLocation.Y,
                currentLocation.Z);
            LogToDebugUI($"Target: {target}", Color.White);

            ff14bot.Core.Me.Face(target);
            ff14bot.Managers.MovementManager.MoveForwardStart();

            LogToDebugUI("Moving forward! Use /stop to stop movement.", Color.LightGreen);
        }

        /// <summary>
        /// Stop movement.
        /// </summary>
        private void ExecuteStopMovement()
        {
            LogToDebugUI("Stopping movement...", Color.LightBlue);

            ff14bot.Managers.MovementManager.MoveForwardStop();

            var finalLocation = ff14bot.Core.Me.Location;
            LogToDebugUI($"Stopped. Final location: {finalLocation}", Color.LightGreen);
        }

        /// <summary>
        /// Test 4: List nearby NPCs.
        /// Queues command to run on bot thread for proper memory access.
        /// </summary>
        private void ExecuteTest4_ListNpcs()
        {
            LogToDebugUI("Test 4: Listing nearby NPCs (queuing to bot thread)...", Color.LightGreen);

            // Check if bot is running
            if (!TheWranglerBotBase.IsBotRunning)
            {
                LogToDebugUI("Bot is not running. Start the bot first to use /test4.", Color.Orange);
                LogToDebugUI("(Memory reads require the bot thread to be active)", Color.LightBlue);
                return;
            }

            // Queue command to run on bot thread
            _controller.QueueDebugCommand("/test4", "", result =>
            {
                // This callback is called from bot thread, so we need to marshal to UI
                LogToDebugUI("Results from bot thread:", Color.LightGreen);
                foreach (var line in result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    LogToDebugUI($"  {line}", Color.FromArgb(180, 220, 180));
                }
            });

            LogToDebugUI("Command queued. Results will appear shortly...", Color.LightBlue);
        }

        /// <summary>
        /// Show DoH/DoL class unlock status.
        /// Queues command to run on bot thread for proper memory access.
        /// </summary>
        private void ExecuteUnlockStatus()
        {
            LogToDebugUI("Checking DoH/DoL class unlock status (queuing to bot thread)...", Color.LightGreen);

            // Check if bot is running
            if (!TheWranglerBotBase.IsBotRunning)
            {
                LogToDebugUI("Bot is not running. Start the bot first to use /unlock.", Color.Orange);
                LogToDebugUI("(Memory reads require the bot thread to be active)", Color.LightBlue);
                return;
            }

            // Queue command to run on bot thread
            _controller.QueueDebugCommand("/unlock", "", result =>
            {
                // This callback is called from bot thread, so we need to marshal to UI
                LogToDebugUI("DoH/DoL Class Unlock Status:", Color.LightGreen);
                foreach (var line in result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var color = line.Contains("LOCKED") ? Color.FromArgb(255, 150, 100) : Color.FromArgb(150, 220, 150);
                    LogToDebugUI($"  {line}", color);
                }
            });

            LogToDebugUI("Command queued. Results will appear shortly...", Color.LightBlue);
        }

        /// <summary>
        /// Show debug help.
        /// </summary>
        private void ShowDebugHelp()
        {
            LogToDebugUI("Available commands:", Color.LightGreen);
            LogToDebugUI("  /test1 [job]  - Change class (e.g. /test1 Carpenter)", Color.White);
            LogToDebugUI("  /test2 [id]   - Teleport to aetheryte (e.g. /test2 8)", Color.White);
            LogToDebugUI("  /test3        - Start navigation (move forward)", Color.White);
            LogToDebugUI("  /test4        - List nearby NPCs (requires bot running)", Color.White);
            LogToDebugUI("  /unlock       - Show DoH/DoL unlock status (requires bot running)", Color.White);
            LogToDebugUI("  /stop         - Stop movement", Color.White);
            LogToDebugUI("  /help         - Show this help", Color.White);
            LogToDebugUI("", Color.White);
            LogToDebugUI("Common Aetheryte IDs:", Color.LightBlue);
            LogToDebugUI("  2=Gridania, 8=Limsa Lominsa, 9=Ul'dah", Color.White);
            LogToDebugUI("", Color.White);
            LogToDebugUI("Note: /test4, /unlock require bot to be started for memory access.", Color.LightBlue);
        }

        #endregion

        #region Form Event Handlers

        /// <summary>
        /// Form closing - saves settings and cleans up.
        /// </summary>
        private void WranglerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save window position
            WranglerSettings.Instance.WindowX = this.Location.X;
            WranglerSettings.Instance.WindowY = this.Location.Y;
            WranglerSettings.Instance.Save();

            // Cleanup Order Mode
            _controller.StatusChanged -= OnStatusChanged;
            _controller.LogMessage -= OnLogMessage;
            _controller.OrderCompleted -= OnOrderCompleted;

            // Cleanup Leveling Mode
            _levelingController.DirectiveChanged -= OnDirectiveChanged;
            _levelingController.LogMessage -= OnLevelingLogMessage;
            _levelingController.LevelingCompleted -= OnLevelingCompleted;
            _levelingController.ClassLevelsUpdated -= OnClassLevelsUpdated;

            Instance = null;
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
            // Order Mode
            btnRun.Enabled = WranglerSettings.Instance.HasValidJsonPath && !_controller.IsExecuting;
            btnStopGently.Enabled = _controller.IsExecuting;
            if (!_controller.IsExecuting)
            {
                btnStopGently.Text = "Stop Gently";
            }

            // Leveling Mode
            btnStartLeveling.Enabled = !_levelingController.IsRunning;
            btnStopLeveling.Enabled = _levelingController.IsRunning;
            if (!_levelingController.IsRunning)
            {
                btnStopLeveling.Text = "Stop";
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
                lblServerStatus.ForeColor = Color.FromArgb(46, 204, 113);
            }
            else
            {
                lblServerStatus.Text = "Server: Stopped";
                lblServerStatus.ForeColor = Color.FromArgb(150, 150, 150);
            }
        }

        /// <summary>
        /// Updates the missing items display.
        /// </summary>
        private void UpdateMissingItemsDisplay(MissingItemsResult result)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateMissingItemsDisplay(result)));
                return;
            }

            if (result.HasMissingItems)
            {
                txtMissingItems.Clear();
                foreach (var item in result.Items)
                {
                    txtMissingItems.SelectionColor = item.IsRequired ? Color.FromArgb(255, 100, 100) : Color.FromArgb(255, 200, 100);
                    txtMissingItems.AppendText($"{item.Name}: {item.Needed}x");
                    if (!item.IsRequired)
                    {
                        txtMissingItems.AppendText(" (optional)");
                    }
                    txtMissingItems.AppendText(Environment.NewLine);
                }
            }
            else
            {
                txtMissingItems.Clear();
                txtMissingItems.SelectionColor = Color.FromArgb(46, 204, 113);
                txtMissingItems.AppendText("All required items available!");
            }
        }

        /// <summary>
        /// Appends a message to the Order Mode log area.
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

        /// <summary>
        /// Appends a message to the Leveling Mode log area.
        /// Thread-safe.
        /// </summary>
        public void LogToLevelingUI(string message, Color color)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => LogToLevelingUI(message, color)));
                return;
            }

            txtLevelingLog.SelectionStart = txtLevelingLog.TextLength;
            txtLevelingLog.SelectionColor = color;
            txtLevelingLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLevelingLog.ScrollToCaret();
        }

        /// <summary>
        /// Public method to log to leveling with default color.
        /// </summary>
        public void LogToLevelingUI(string message)
        {
            LogToLevelingUI(message, Color.FromArgb(220, 220, 220));
        }

        /// <summary>
        /// Appends a message to the Debug Mode log area.
        /// Thread-safe.
        /// </summary>
        public void LogToDebugUI(string message, Color color)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => LogToDebugUI(message, color)));
                return;
            }

            txtDebugLog.SelectionStart = txtDebugLog.TextLength;
            txtDebugLog.SelectionColor = color;
            txtDebugLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtDebugLog.ScrollToCaret();
        }

        /// <summary>
        /// Public method to log to debug with default color.
        /// </summary>
        public void LogToDebugUI(string message)
        {
            LogToDebugUI(message, Color.FromArgb(220, 220, 220));
        }

        /// <summary>
        /// Refreshes class level display. Call this periodically or on demand.
        /// </summary>
        public void RefreshClassLevels()
        {
            _levelingController.RefreshClassLevels();
        }

        #endregion
    }
}
