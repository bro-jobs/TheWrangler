/*
 * WranglerForm.Designer.cs - Form Layout
 * =======================================
 *
 * PURPOSE:
 * This file contains the Windows Forms designer-generated code for the UI layout.
 * It defines all controls, their positions, sizes, and basic properties.
 *
 * LAYOUT STRUCTURE (Tabbed):
 * +-------------------------------------------+
 * |  [Title: TheWrangler]                     |
 * +-------------------------------------------+
 * | [Order Mode] [Leveling Mode]              |
 * +-------------------------------------------+
 * | ORDER MODE TAB:                           |
 * |  Selected File: [filename display]        |
 * |  [Browse...]                              |
 * |  [ ] Ignore Home                          |
 * |  [    RUN    ]    [Stop Gently]           |
 * |  Status: Ready                            |
 * |  [Log output area]                        |
 * +-------------------------------------------+
 * | LEVELING MODE TAB:                        |
 * |  [Class Levels Panel]                     |
 * |  [Current Directive Status]               |
 * |  [Missing Items Warnings]                 |
 * |  [Start/Stop Leveling]                    |
 * +-------------------------------------------+
 */

namespace TheWrangler
{
    partial class WranglerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - defines all UI controls.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlMain = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.tabControl = new System.Windows.Forms.TabControl();

            // Order Mode Tab
            this.tabOrderMode = new System.Windows.Forms.TabPage();
            this.lblSelectedFile = new System.Windows.Forms.Label();
            this.lblFilePath = new System.Windows.Forms.Label();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.chkIgnoreHome = new System.Windows.Forms.CheckBox();
            this.lblRemotePort = new System.Windows.Forms.Label();
            this.txtRemotePort = new System.Windows.Forms.TextBox();
            this.lblServerStatus = new System.Windows.Forms.Label();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnStopGently = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.RichTextBox();

            // Leveling Mode Tab
            this.tabLevelingMode = new System.Windows.Forms.TabPage();
            this.pnlLevelingStatus = new System.Windows.Forms.Panel();
            this.lblLevelingTitle = new System.Windows.Forms.Label();
            this.lblCurrentDirective = new System.Windows.Forms.Label();
            this.lblDirectiveDetail = new System.Windows.Forms.Label();
            this.pnlClassLevels = new System.Windows.Forms.Panel();
            this.lblClassLevelsHeader = new System.Windows.Forms.Label();
            this.lblCrafterLevels = new System.Windows.Forms.Label();
            this.lblGathererLevels = new System.Windows.Forms.Label();
            this.pnlMissingItems = new System.Windows.Forms.Panel();
            this.lblMissingItemsHeader = new System.Windows.Forms.Label();
            this.txtMissingItems = new System.Windows.Forms.RichTextBox();
            this.btnStartLeveling = new System.Windows.Forms.Button();
            this.btnStopLeveling = new System.Windows.Forms.Button();
            this.lblLevelingStatus = new System.Windows.Forms.Label();
            this.txtLevelingLog = new System.Windows.Forms.RichTextBox();

            // Debug Mode Tab
            this.tabDebugMode = new System.Windows.Forms.TabPage();
            this.lblDebugCommands = new System.Windows.Forms.Label();
            this.txtDebugCommand = new System.Windows.Forms.TextBox();
            this.btnRunDebugCommand = new System.Windows.Forms.Button();
            this.txtDebugLog = new System.Windows.Forms.RichTextBox();

            this.pnlMain.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.tabOrderMode.SuspendLayout();
            this.tabLevelingMode.SuspendLayout();
            this.tabDebugMode.SuspendLayout();
            this.pnlLevelingStatus.SuspendLayout();
            this.pnlClassLevels.SuspendLayout();
            this.pnlMissingItems.SuspendLayout();
            this.SuspendLayout();

            //
            // pnlMain - Main container panel
            //
            this.pnlMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlMain.Controls.Add(this.lblTitle);
            this.pnlMain.Controls.Add(this.tabControl);
            this.pnlMain.Location = new System.Drawing.Point(12, 12);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new System.Windows.Forms.Padding(15);
            this.pnlMain.Size = new System.Drawing.Size(560, 537);
            this.pnlMain.TabIndex = 0;

            //
            // lblTitle - Main title label
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(15, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(168, 32);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "TheWrangler";

            //
            // tabControl - Main tab control
            //
            this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl.Controls.Add(this.tabOrderMode);
            this.tabControl.Controls.Add(this.tabLevelingMode);
            this.tabControl.Controls.Add(this.tabDebugMode);
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl.Location = new System.Drawing.Point(15, 55);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(530, 467);
            this.tabControl.TabIndex = 1;

            // ============================================
            // ORDER MODE TAB
            // ============================================

            //
            // tabOrderMode - Order Mode tab page
            //
            this.tabOrderMode.Controls.Add(this.lblSelectedFile);
            this.tabOrderMode.Controls.Add(this.lblFilePath);
            this.tabOrderMode.Controls.Add(this.btnBrowse);
            this.tabOrderMode.Controls.Add(this.chkIgnoreHome);
            this.tabOrderMode.Controls.Add(this.lblRemotePort);
            this.tabOrderMode.Controls.Add(this.txtRemotePort);
            this.tabOrderMode.Controls.Add(this.lblServerStatus);
            this.tabOrderMode.Controls.Add(this.btnRun);
            this.tabOrderMode.Controls.Add(this.btnStopGently);
            this.tabOrderMode.Controls.Add(this.lblStatus);
            this.tabOrderMode.Controls.Add(this.txtLog);
            this.tabOrderMode.Location = new System.Drawing.Point(4, 28);
            this.tabOrderMode.Name = "tabOrderMode";
            this.tabOrderMode.Padding = new System.Windows.Forms.Padding(10);
            this.tabOrderMode.Size = new System.Drawing.Size(522, 435);
            this.tabOrderMode.TabIndex = 0;
            this.tabOrderMode.Text = "Order Mode";
            this.tabOrderMode.UseVisualStyleBackColor = true;

            //
            // lblSelectedFile - "Selected File:" label
            //
            this.lblSelectedFile.AutoSize = true;
            this.lblSelectedFile.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSelectedFile.Location = new System.Drawing.Point(10, 15);
            this.lblSelectedFile.Name = "lblSelectedFile";
            this.lblSelectedFile.Size = new System.Drawing.Size(87, 19);
            this.lblSelectedFile.TabIndex = 1;
            this.lblSelectedFile.Text = "Selected File:";

            //
            // lblFilePath - Shows selected filename
            //
            this.lblFilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFilePath.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFilePath.Location = new System.Drawing.Point(10, 37);
            this.lblFilePath.Name = "lblFilePath";
            this.lblFilePath.Size = new System.Drawing.Size(390, 23);
            this.lblFilePath.TabIndex = 2;
            this.lblFilePath.Text = "No file selected";

            //
            // btnBrowse - Browse button
            //
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowse.Location = new System.Drawing.Point(406, 25);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(100, 35);
            this.btnBrowse.TabIndex = 3;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);

            //
            // chkIgnoreHome - Ignore home checkbox
            //
            this.chkIgnoreHome.AutoSize = true;
            this.chkIgnoreHome.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkIgnoreHome.Location = new System.Drawing.Point(13, 75);
            this.chkIgnoreHome.Name = "chkIgnoreHome";
            this.chkIgnoreHome.Size = new System.Drawing.Size(268, 23);
            this.chkIgnoreHome.TabIndex = 4;
            this.chkIgnoreHome.Text = "Ignore Home (stay at crafting location)";
            this.chkIgnoreHome.UseVisualStyleBackColor = true;
            this.chkIgnoreHome.CheckedChanged += new System.EventHandler(this.chkIgnoreHome_CheckedChanged);

            //
            // lblRemotePort - Remote port label
            //
            this.lblRemotePort.AutoSize = true;
            this.lblRemotePort.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRemotePort.Location = new System.Drawing.Point(10, 105);
            this.lblRemotePort.Name = "lblRemotePort";
            this.lblRemotePort.Size = new System.Drawing.Size(88, 19);
            this.lblRemotePort.TabIndex = 10;
            this.lblRemotePort.Text = "Remote Port:";

            //
            // txtRemotePort - Remote port text box
            //
            this.txtRemotePort.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtRemotePort.Location = new System.Drawing.Point(108, 102);
            this.txtRemotePort.Name = "txtRemotePort";
            this.txtRemotePort.Size = new System.Drawing.Size(70, 25);
            this.txtRemotePort.TabIndex = 11;
            this.txtRemotePort.Text = "7800";
            this.txtRemotePort.Leave += new System.EventHandler(this.txtRemotePort_Leave);

            //
            // lblServerStatus - Server status indicator
            //
            this.lblServerStatus.AutoSize = true;
            this.lblServerStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblServerStatus.Location = new System.Drawing.Point(188, 107);
            this.lblServerStatus.Name = "lblServerStatus";
            this.lblServerStatus.Size = new System.Drawing.Size(95, 15);
            this.lblServerStatus.TabIndex = 12;
            this.lblServerStatus.Text = "Server: Stopped";

            //
            // btnRun - Main run button
            //
            this.btnRun.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRun.Enabled = false;
            this.btnRun.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRun.Location = new System.Drawing.Point(13, 140);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(350, 50);
            this.btnRun.TabIndex = 5;
            this.btnRun.Text = "Run";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);

            //
            // btnStopGently - Stop Gently button
            //
            this.btnStopGently.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStopGently.Enabled = false;
            this.btnStopGently.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStopGently.Location = new System.Drawing.Point(373, 140);
            this.btnStopGently.Name = "btnStopGently";
            this.btnStopGently.Size = new System.Drawing.Size(133, 50);
            this.btnStopGently.TabIndex = 6;
            this.btnStopGently.Text = "Stop Gently";
            this.btnStopGently.UseVisualStyleBackColor = true;
            this.btnStopGently.Click += new System.EventHandler(this.btnStopGently_Click);

            //
            // lblStatus - Status display label
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Location = new System.Drawing.Point(10, 200);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(79, 15);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Status: Ready";

            //
            // txtLog - Log output area
            //
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.Location = new System.Drawing.Point(13, 225);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(493, 195);
            this.txtLog.TabIndex = 7;
            this.txtLog.Text = "";

            // ============================================
            // LEVELING MODE TAB
            // ============================================

            //
            // tabLevelingMode - Leveling Mode tab page
            //
            this.tabLevelingMode.Controls.Add(this.pnlLevelingStatus);
            this.tabLevelingMode.Controls.Add(this.pnlClassLevels);
            this.tabLevelingMode.Controls.Add(this.pnlMissingItems);
            this.tabLevelingMode.Controls.Add(this.btnStartLeveling);
            this.tabLevelingMode.Controls.Add(this.btnStopLeveling);
            this.tabLevelingMode.Controls.Add(this.lblLevelingStatus);
            this.tabLevelingMode.Controls.Add(this.txtLevelingLog);
            this.tabLevelingMode.Location = new System.Drawing.Point(4, 28);
            this.tabLevelingMode.Name = "tabLevelingMode";
            this.tabLevelingMode.Padding = new System.Windows.Forms.Padding(10);
            this.tabLevelingMode.Size = new System.Drawing.Size(522, 435);
            this.tabLevelingMode.TabIndex = 1;
            this.tabLevelingMode.Text = "Leveling Mode";
            this.tabLevelingMode.UseVisualStyleBackColor = true;

            //
            // pnlLevelingStatus - Current directive status panel
            //
            this.pnlLevelingStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlLevelingStatus.Controls.Add(this.lblLevelingTitle);
            this.pnlLevelingStatus.Controls.Add(this.lblCurrentDirective);
            this.pnlLevelingStatus.Controls.Add(this.lblDirectiveDetail);
            this.pnlLevelingStatus.Location = new System.Drawing.Point(10, 10);
            this.pnlLevelingStatus.Name = "pnlLevelingStatus";
            this.pnlLevelingStatus.Size = new System.Drawing.Size(502, 70);
            this.pnlLevelingStatus.TabIndex = 0;

            //
            // lblLevelingTitle - "Current Directive" header
            //
            this.lblLevelingTitle.AutoSize = true;
            this.lblLevelingTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLevelingTitle.Location = new System.Drawing.Point(5, 5);
            this.lblLevelingTitle.Name = "lblLevelingTitle";
            this.lblLevelingTitle.Size = new System.Drawing.Size(106, 15);
            this.lblLevelingTitle.TabIndex = 0;
            this.lblLevelingTitle.Text = "Current Directive:";

            //
            // lblCurrentDirective - Main directive display
            //
            this.lblCurrentDirective.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurrentDirective.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCurrentDirective.Location = new System.Drawing.Point(5, 22);
            this.lblCurrentDirective.Name = "lblCurrentDirective";
            this.lblCurrentDirective.Size = new System.Drawing.Size(492, 25);
            this.lblCurrentDirective.TabIndex = 1;
            this.lblCurrentDirective.Text = "Not Started";

            //
            // lblDirectiveDetail - Directive detail/progress
            //
            this.lblDirectiveDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDirectiveDetail.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDirectiveDetail.Location = new System.Drawing.Point(5, 47);
            this.lblDirectiveDetail.Name = "lblDirectiveDetail";
            this.lblDirectiveDetail.Size = new System.Drawing.Size(492, 18);
            this.lblDirectiveDetail.TabIndex = 2;
            this.lblDirectiveDetail.Text = "Click Start Leveling to begin DoH/DoL leveling to 100";

            //
            // pnlClassLevels - Class levels display panel
            //
            this.pnlClassLevels.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlClassLevels.Controls.Add(this.lblClassLevelsHeader);
            this.pnlClassLevels.Controls.Add(this.lblCrafterLevels);
            this.pnlClassLevels.Controls.Add(this.lblGathererLevels);
            this.pnlClassLevels.Location = new System.Drawing.Point(10, 85);
            this.pnlClassLevels.Name = "pnlClassLevels";
            this.pnlClassLevels.Size = new System.Drawing.Size(502, 55);
            this.pnlClassLevels.TabIndex = 1;

            //
            // lblClassLevelsHeader - "Class Levels" header
            //
            this.lblClassLevelsHeader.AutoSize = true;
            this.lblClassLevelsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblClassLevelsHeader.Location = new System.Drawing.Point(5, 5);
            this.lblClassLevelsHeader.Name = "lblClassLevelsHeader";
            this.lblClassLevelsHeader.Size = new System.Drawing.Size(76, 15);
            this.lblClassLevelsHeader.TabIndex = 0;
            this.lblClassLevelsHeader.Text = "Class Levels:";

            //
            // lblCrafterLevels - Crafter levels display
            //
            this.lblCrafterLevels.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCrafterLevels.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCrafterLevels.Location = new System.Drawing.Point(5, 22);
            this.lblCrafterLevels.Name = "lblCrafterLevels";
            this.lblCrafterLevels.Size = new System.Drawing.Size(492, 15);
            this.lblCrafterLevels.TabIndex = 1;
            this.lblCrafterLevels.Text = "CRP:-- BSM:-- ARM:-- GSM:-- LTW:-- WVR:-- ALC:-- CUL:--";

            //
            // lblGathererLevels - Gatherer levels display
            //
            this.lblGathererLevels.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblGathererLevels.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGathererLevels.Location = new System.Drawing.Point(5, 37);
            this.lblGathererLevels.Name = "lblGathererLevels";
            this.lblGathererLevels.Size = new System.Drawing.Size(492, 15);
            this.lblGathererLevels.TabIndex = 2;
            this.lblGathererLevels.Text = "MIN:-- BTN:-- FSH:--";

            //
            // pnlMissingItems - Missing items warning panel
            //
            this.pnlMissingItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlMissingItems.Controls.Add(this.lblMissingItemsHeader);
            this.pnlMissingItems.Controls.Add(this.txtMissingItems);
            this.pnlMissingItems.Location = new System.Drawing.Point(10, 145);
            this.pnlMissingItems.Name = "pnlMissingItems";
            this.pnlMissingItems.Size = new System.Drawing.Size(502, 70);
            this.pnlMissingItems.TabIndex = 2;

            //
            // lblMissingItemsHeader - "Missing Items" header
            //
            this.lblMissingItemsHeader.AutoSize = true;
            this.lblMissingItemsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMissingItemsHeader.Location = new System.Drawing.Point(5, 5);
            this.lblMissingItemsHeader.Name = "lblMissingItemsHeader";
            this.lblMissingItemsHeader.Size = new System.Drawing.Size(183, 15);
            this.lblMissingItemsHeader.TabIndex = 0;
            this.lblMissingItemsHeader.Text = "Required Items (Manual Obtain):";

            //
            // txtMissingItems - Missing items list
            //
            this.txtMissingItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMissingItems.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtMissingItems.Font = new System.Drawing.Font("Consolas", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMissingItems.Location = new System.Drawing.Point(5, 22);
            this.txtMissingItems.Name = "txtMissingItems";
            this.txtMissingItems.ReadOnly = true;
            this.txtMissingItems.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtMissingItems.Size = new System.Drawing.Size(492, 43);
            this.txtMissingItems.TabIndex = 1;
            this.txtMissingItems.Text = "Click Start Leveling to check for required items...";

            //
            // btnStartLeveling - Start leveling button
            //
            this.btnStartLeveling.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartLeveling.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStartLeveling.Location = new System.Drawing.Point(13, 225);
            this.btnStartLeveling.Name = "btnStartLeveling";
            this.btnStartLeveling.Size = new System.Drawing.Size(350, 50);
            this.btnStartLeveling.TabIndex = 3;
            this.btnStartLeveling.Text = "Start Leveling";
            this.btnStartLeveling.UseVisualStyleBackColor = true;
            this.btnStartLeveling.Click += new System.EventHandler(this.btnStartLeveling_Click);

            //
            // btnStopLeveling - Stop leveling button
            //
            this.btnStopLeveling.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStopLeveling.Enabled = false;
            this.btnStopLeveling.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStopLeveling.Location = new System.Drawing.Point(373, 225);
            this.btnStopLeveling.Name = "btnStopLeveling";
            this.btnStopLeveling.Size = new System.Drawing.Size(133, 50);
            this.btnStopLeveling.TabIndex = 4;
            this.btnStopLeveling.Text = "Stop";
            this.btnStopLeveling.UseVisualStyleBackColor = true;
            this.btnStopLeveling.Click += new System.EventHandler(this.btnStopLeveling_Click);

            //
            // lblLevelingStatus - Leveling status label
            //
            this.lblLevelingStatus.AutoSize = true;
            this.lblLevelingStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLevelingStatus.Location = new System.Drawing.Point(10, 285);
            this.lblLevelingStatus.Name = "lblLevelingStatus";
            this.lblLevelingStatus.Size = new System.Drawing.Size(79, 15);
            this.lblLevelingStatus.TabIndex = 5;
            this.lblLevelingStatus.Text = "Status: Ready";

            //
            // txtLevelingLog - Leveling log output
            //
            this.txtLevelingLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLevelingLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLevelingLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLevelingLog.Location = new System.Drawing.Point(13, 305);
            this.txtLevelingLog.Name = "txtLevelingLog";
            this.txtLevelingLog.ReadOnly = true;
            this.txtLevelingLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLevelingLog.Size = new System.Drawing.Size(493, 115);
            this.txtLevelingLog.TabIndex = 6;
            this.txtLevelingLog.Text = "";

            // ============================================
            // DEBUG MODE TAB
            // ============================================

            //
            // tabDebugMode - Debug Mode tab page
            //
            this.tabDebugMode.Controls.Add(this.lblDebugCommands);
            this.tabDebugMode.Controls.Add(this.txtDebugCommand);
            this.tabDebugMode.Controls.Add(this.btnRunDebugCommand);
            this.tabDebugMode.Controls.Add(this.txtDebugLog);
            this.tabDebugMode.Location = new System.Drawing.Point(4, 28);
            this.tabDebugMode.Name = "tabDebugMode";
            this.tabDebugMode.Padding = new System.Windows.Forms.Padding(10);
            this.tabDebugMode.Size = new System.Drawing.Size(522, 435);
            this.tabDebugMode.TabIndex = 2;
            this.tabDebugMode.Text = "Debug Mode";
            this.tabDebugMode.UseVisualStyleBackColor = true;

            //
            // lblDebugCommands - Debug commands help label
            //
            this.lblDebugCommands.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblDebugCommands.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDebugCommands.Location = new System.Drawing.Point(10, 10);
            this.lblDebugCommands.Name = "lblDebugCommands";
            this.lblDebugCommands.Size = new System.Drawing.Size(502, 60);
            this.lblDebugCommands.TabIndex = 0;
            this.lblDebugCommands.Text = "Available commands:\r\n/test1 [job] - Test ChangeClass (e.g. /test1 Carpenter)\r\n/test2 [id] - Test TeleportTo (e.g. /test2 8 for Limsa)\r\n/test3 - Test Navigation (move forward 10 units)\r\n/test4 - Test NPC listing (list nearby NPCs)";

            //
            // txtDebugCommand - Debug command input
            //
            this.txtDebugCommand.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugCommand.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDebugCommand.Location = new System.Drawing.Point(13, 80);
            this.txtDebugCommand.Name = "txtDebugCommand";
            this.txtDebugCommand.Size = new System.Drawing.Size(393, 26);
            this.txtDebugCommand.TabIndex = 1;
            this.txtDebugCommand.Text = "/test1 Carpenter";
            this.txtDebugCommand.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtDebugCommand_KeyDown);

            //
            // btnRunDebugCommand - Run debug command button
            //
            this.btnRunDebugCommand.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRunDebugCommand.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRunDebugCommand.Location = new System.Drawing.Point(412, 75);
            this.btnRunDebugCommand.Name = "btnRunDebugCommand";
            this.btnRunDebugCommand.Size = new System.Drawing.Size(100, 35);
            this.btnRunDebugCommand.TabIndex = 2;
            this.btnRunDebugCommand.Text = "Run";
            this.btnRunDebugCommand.UseVisualStyleBackColor = true;
            this.btnRunDebugCommand.Click += new System.EventHandler(this.btnRunDebugCommand_Click);

            //
            // txtDebugLog - Debug log output
            //
            this.txtDebugLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDebugLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDebugLog.Location = new System.Drawing.Point(13, 120);
            this.txtDebugLog.Name = "txtDebugLog";
            this.txtDebugLog.ReadOnly = true;
            this.txtDebugLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtDebugLog.Size = new System.Drawing.Size(493, 300);
            this.txtDebugLog.TabIndex = 3;
            this.txtDebugLog.Text = "";

            //
            // WranglerForm - Main form
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 561);
            this.Controls.Add(this.pnlMain);
            this.MinimumSize = new System.Drawing.Size(500, 500);
            this.Name = "WranglerForm";
            this.Text = "TheWrangler";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WranglerForm_FormClosing);
            this.pnlLevelingStatus.ResumeLayout(false);
            this.pnlLevelingStatus.PerformLayout();
            this.pnlClassLevels.ResumeLayout(false);
            this.pnlClassLevels.PerformLayout();
            this.pnlMissingItems.ResumeLayout(false);
            this.pnlMissingItems.PerformLayout();
            this.tabLevelingMode.ResumeLayout(false);
            this.tabLevelingMode.PerformLayout();
            this.tabDebugMode.ResumeLayout(false);
            this.tabDebugMode.PerformLayout();
            this.tabOrderMode.ResumeLayout(false);
            this.tabOrderMode.PerformLayout();
            this.tabControl.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.pnlMain.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        // Main container controls
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TabControl tabControl;

        // Order Mode Tab controls
        private System.Windows.Forms.TabPage tabOrderMode;
        private System.Windows.Forms.Label lblSelectedFile;
        private System.Windows.Forms.Label lblFilePath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.CheckBox chkIgnoreHome;
        private System.Windows.Forms.Label lblRemotePort;
        private System.Windows.Forms.TextBox txtRemotePort;
        private System.Windows.Forms.Label lblServerStatus;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnStopGently;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.RichTextBox txtLog;

        // Leveling Mode Tab controls
        private System.Windows.Forms.TabPage tabLevelingMode;
        private System.Windows.Forms.Panel pnlLevelingStatus;
        private System.Windows.Forms.Label lblLevelingTitle;
        private System.Windows.Forms.Label lblCurrentDirective;
        private System.Windows.Forms.Label lblDirectiveDetail;
        private System.Windows.Forms.Panel pnlClassLevels;
        private System.Windows.Forms.Label lblClassLevelsHeader;
        private System.Windows.Forms.Label lblCrafterLevels;
        private System.Windows.Forms.Label lblGathererLevels;
        private System.Windows.Forms.Panel pnlMissingItems;
        private System.Windows.Forms.Label lblMissingItemsHeader;
        private System.Windows.Forms.RichTextBox txtMissingItems;
        private System.Windows.Forms.Button btnStartLeveling;
        private System.Windows.Forms.Button btnStopLeveling;
        private System.Windows.Forms.Label lblLevelingStatus;
        private System.Windows.Forms.RichTextBox txtLevelingLog;

        // Debug Mode Tab controls
        private System.Windows.Forms.TabPage tabDebugMode;
        private System.Windows.Forms.Label lblDebugCommands;
        private System.Windows.Forms.TextBox txtDebugCommand;
        private System.Windows.Forms.Button btnRunDebugCommand;
        private System.Windows.Forms.RichTextBox txtDebugLog;
    }
}
