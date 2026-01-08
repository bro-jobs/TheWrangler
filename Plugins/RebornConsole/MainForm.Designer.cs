namespace HighVoltz
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.csharpCode = new System.Windows.Forms.RichTextBox();
            this.textOutput = new System.Windows.Forms.RichTextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.luaCode = new System.Windows.Forms.RichTextBox();
            this.luaOutput = new System.Windows.Forms.RichTextBox();
            this.btnCompile = new System.Windows.Forms.Button();
            this.NewSnipletButton = new System.Windows.Forms.Button();
            this.savedSnipletsCombo = new System.Windows.Forms.ComboBox();
            this.saveButton = new System.Windows.Forms.Button();
            this.btnKeybind = new System.Windows.Forms.Button();
            this.RenameButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 30);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(919, 441);
            this.tabControl1.TabIndex = 26;
            this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.splitContainer1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(911, 415);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "C#";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.csharpCode);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.textOutput);
            this.splitContainer1.Size = new System.Drawing.Size(905, 409);
            this.splitContainer1.SplitterDistance = 303;
            this.splitContainer1.TabIndex = 0;
            // 
            // csharpCode
            // 
            this.csharpCode.AcceptsTab = true;
            this.csharpCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.csharpCode.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.csharpCode.HideSelection = false;
            this.csharpCode.Location = new System.Drawing.Point(0, 0);
            this.csharpCode.Margin = new System.Windows.Forms.Padding(0);
            this.csharpCode.Name = "csharpCode";
            this.csharpCode.Size = new System.Drawing.Size(905, 303);
            this.csharpCode.TabIndex = 25;
            this.csharpCode.Text = "";
            this.csharpCode.WordWrap = false;
            this.csharpCode.TextChanged += new System.EventHandler(this.csharpCode_TextChanged);
            this.csharpCode.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textCode_KeyDown);
            // 
            // textOutput
            // 
            this.textOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textOutput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textOutput.Location = new System.Drawing.Point(0, 0);
            this.textOutput.MaxLength = 2100000;
            this.textOutput.Name = "textOutput";
            this.textOutput.ReadOnly = true;
            this.textOutput.Size = new System.Drawing.Size(905, 102);
            this.textOutput.TabIndex = 25;
            this.textOutput.Text = "";
            this.textOutput.WordWrap = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(911, 415);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Lua";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.luaCode);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.luaOutput);
            this.splitContainer2.Size = new System.Drawing.Size(905, 409);
            this.splitContainer2.SplitterDistance = 303;
            this.splitContainer2.TabIndex = 1;
            // 
            // luaCode
            // 
            this.luaCode.AcceptsTab = true;
            this.luaCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.luaCode.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.luaCode.HideSelection = false;
            this.luaCode.Location = new System.Drawing.Point(0, 0);
            this.luaCode.Margin = new System.Windows.Forms.Padding(0);
            this.luaCode.Name = "luaCode";
            this.luaCode.Size = new System.Drawing.Size(905, 303);
            this.luaCode.TabIndex = 25;
            this.luaCode.Text = "";
            this.luaCode.WordWrap = false;
            this.luaCode.TextChanged += new System.EventHandler(this.csharpCode_TextChanged);
            // 
            // luaOutput
            // 
            this.luaOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.luaOutput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.luaOutput.Location = new System.Drawing.Point(0, 0);
            this.luaOutput.MaxLength = 2100000;
            this.luaOutput.Name = "luaOutput";
            this.luaOutput.ReadOnly = true;
            this.luaOutput.Size = new System.Drawing.Size(905, 102);
            this.luaOutput.TabIndex = 25;
            this.luaOutput.Text = "";
            this.luaOutput.WordWrap = false;
            // 
            // btnCompile
            // 
            this.btnCompile.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCompile.Location = new System.Drawing.Point(826, 6);
            this.btnCompile.Name = "btnCompile";
            this.btnCompile.Size = new System.Drawing.Size(89, 25);
            this.btnCompile.TabIndex = 24;
            this.btnCompile.Text = "Run (F5)";
            this.btnCompile.UseVisualStyleBackColor = true;
            this.btnCompile.Click += new System.EventHandler(this.btnCompile_Click);
            // 
            // NewSnipletButton
            // 
            this.NewSnipletButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.NewSnipletButton.Location = new System.Drawing.Point(293, 5);
            this.NewSnipletButton.Name = "NewSnipletButton";
            this.NewSnipletButton.Size = new System.Drawing.Size(84, 25);
            this.NewSnipletButton.TabIndex = 19;
            this.NewSnipletButton.Text = "New";
            this.NewSnipletButton.UseVisualStyleBackColor = true;
            this.NewSnipletButton.Click += new System.EventHandler(this.NewSnipletButton_Click);
            // 
            // savedSnipletsCombo
            // 
            this.savedSnipletsCombo.FormattingEnabled = true;
            this.savedSnipletsCombo.Location = new System.Drawing.Point(23, 5);
            this.savedSnipletsCombo.Name = "savedSnipletsCombo";
            this.savedSnipletsCombo.Size = new System.Drawing.Size(264, 21);
            this.savedSnipletsCombo.TabIndex = 18;
            this.savedSnipletsCombo.SelectedIndexChanged += new System.EventHandler(this.savedSnipletsCombo_SelectedIndexChanged);
            // 
            // saveButton
            // 
            this.saveButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.saveButton.Location = new System.Drawing.Point(383, 5);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(84, 25);
            this.saveButton.TabIndex = 21;
            this.saveButton.Text = "Save";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // btnKeybind
            // 
            this.btnKeybind.Location = new System.Drawing.Point(563, 6);
            this.btnKeybind.Name = "btnKeybind";
            this.btnKeybind.Size = new System.Drawing.Size(84, 25);
            this.btnKeybind.TabIndex = 25;
            this.btnKeybind.Text = "Keybind";
            this.btnKeybind.UseVisualStyleBackColor = true;
            this.btnKeybind.Click += new System.EventHandler(this.btnKeybind_Click);
            this.btnKeybind.KeyDown += new System.Windows.Forms.KeyEventHandler(this.btnKeybind_KeyDown);
            this.btnKeybind.KeyUp += new System.Windows.Forms.KeyEventHandler(this.btnKeybind_KeyUp);
            // 
            // RenameButton
            // 
            this.RenameButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RenameButton.Location = new System.Drawing.Point(653, 6);
            this.RenameButton.Name = "RenameButton";
            this.RenameButton.Size = new System.Drawing.Size(84, 25);
            this.RenameButton.TabIndex = 20;
            this.RenameButton.Text = "Rename";
            this.RenameButton.UseVisualStyleBackColor = true;
            this.RenameButton.Click += new System.EventHandler(this.RenameButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DeleteButton.Location = new System.Drawing.Point(473, 5);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(84, 25);
            this.DeleteButton.TabIndex = 22;
            this.DeleteButton.Text = "Delete";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(749, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(71, 25);
            this.button1.TabIndex = 27;
            this.button1.Text = "Clear";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 300;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(919, 471);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnCompile);
            this.Controls.Add(this.saveButton);
            this.Controls.Add(this.RenameButton);
            this.Controls.Add(this.btnKeybind);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.NewSnipletButton);
            this.Controls.Add(this.savedSnipletsCombo);
            this.Name = "MainForm";
            this.Text = "Rebornconsole";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox csharpCode;
        public System.Windows.Forms.RichTextBox textOutput;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox luaCode;
        public System.Windows.Forms.RichTextBox luaOutput;
        private System.Windows.Forms.Button btnCompile;
        private System.Windows.Forms.Button NewSnipletButton;
        private System.Windows.Forms.ComboBox savedSnipletsCombo;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button btnKeybind;
        private System.Windows.Forms.Button RenameButton;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Timer timer1;
    }
}