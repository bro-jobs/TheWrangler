using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using ff14bot.Behavior;
using ff14bot.Managers;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace HighVoltz
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Instance = this;

            if (!RebornConsoleSettings.Instance.ShowLuaTab_ThisCanBeDangerous)
            {
                tabControl1.TabPages.RemoveAt(1);
            }

        }

        public static MainForm Instance { get; private set; }

        public static bool IsValid
        {
            get { return Instance != null && Instance.Visible && !Instance.Disposing && !Instance.IsDisposed; }
        }

        private void btnCompile_Click(object sender, EventArgs e)
        {
            CompileAndRun();
        }


        private void textCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.F5)
            {
                CompileAndRun();
            }

        }


        public RichTextBox OutputTextBox;
        private void CompileAndRun()
        {

            if (RebornConsoleSettings.Instance.TabIndex == 0)
            {
                // C#
                try
                {
                    btnCompile.BackColor = HighVoltz.RebornConsole.CodeDriver.CompileAndRun(csharpCode.Text) ? System.Drawing.Color.White : System.Drawing.Color.Red;
                    RebornConsoleSettings.Instance.CSharpSniplets[RebornConsoleSettings.Instance.CSharpSelectedIndex] = csharpCode.Text;
                }
                catch (Exception ex)
                {
                   RebornConsole.Log(Colors.Red, ex.ToString());
                }
            }
            else
            {
                //LUA
                try
                {
                    Pulsator.Pulse(PulseFlags.All);
                    var tmp = luaCode.Text;
                    if (tmp.Contains("{T}"))
                        tmp = tmp.Replace("{T}", ff14bot.Core.Target.LuaString);
                    
                    foreach (var value in Lua.GetReturnValues(tmp))
                    {
                        RebornConsole.Log(value);
                    }
                }
                catch (Exception ex)
                {
                    HighVoltz.RebornConsole.Log(Colors.Red, ex.ToString());
                }
            }



        }

        private void SetupComboBox()
        {
            if (RebornConsoleSettings.Instance.TabIndex == 0)
            {
                // C#
                savedSnipletsCombo.Items.Clear();
                savedSnipletsCombo.Items.AddRange(RebornConsoleSettings.Instance.CSharpSnipletNames);
                savedSnipletsCombo.SelectedIndex = RebornConsoleSettings.Instance.CSharpSelectedIndex;
                csharpCode.Text = RebornConsoleSettings.Instance.CSharpSniplets[savedSnipletsCombo.SelectedIndex != -1 ? savedSnipletsCombo.SelectedIndex : 0];
                OutputTextBox = textOutput;
            }
            else
            {
                //LUA
                savedSnipletsCombo.Items.Clear();
                savedSnipletsCombo.Items.AddRange(RebornConsoleSettings.Instance.LuaSnipletNames);
                savedSnipletsCombo.SelectedIndex = RebornConsoleSettings.Instance.LuaSelectedIndex;
                luaCode.Text = RebornConsoleSettings.Instance.LuaSniplets[savedSnipletsCombo.SelectedIndex != -1 ? savedSnipletsCombo.SelectedIndex : 0];
                OutputTextBox = luaOutput;
            }
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = RebornConsoleSettings.Instance.TabIndex;
            SetupComboBox();
        }

        protected override void OnShown(EventArgs e)
        {
            btnKeybind.Text = RebornConsoleSettings.Instance.Keybind == Keys.None ? "Click to set Keybind" : GetKeyString(RebornConsoleSettings.Instance.Keybind);
            base.OnShown(e);
            
        }

        private void savedSnipletsCombo_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (RebornConsoleSettings.Instance.TabIndex == 0)
            {
                // C#
                if (savedSnipletsCombo.SelectedIndex < RebornConsoleSettings.Instance.CSharpSniplets.Length && savedSnipletsCombo.SelectedIndex != -1)
                {
                    csharpCode.Text = RebornConsoleSettings.Instance.CSharpSniplets[savedSnipletsCombo.SelectedIndex];
                    RebornConsoleSettings.Instance.CSharpSelectedIndex = savedSnipletsCombo.SelectedIndex;
                }
            }
            else
            {
                //LUA
                if (savedSnipletsCombo.SelectedIndex < RebornConsoleSettings.Instance.LuaSniplets.Length && savedSnipletsCombo.SelectedIndex != -1)
                {
                    luaCode.Text = RebornConsoleSettings.Instance.LuaSniplets[savedSnipletsCombo.SelectedIndex];
                    RebornConsoleSettings.Instance.LuaSelectedIndex = savedSnipletsCombo.SelectedIndex;
                }
            }

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            RebornConsoleSettings.Instance.Save();
        }

        private void NewSnipletButton_Click(object sender, EventArgs e)
        {
            AddNewSnipplet(GetNewScriptName());
        }

        private void AddNewSnipplet(string name)
        {


            if (RebornConsoleSettings.Instance.TabIndex == 0)
            {
                // C#
                RebornConsoleSettings.Instance.CSharpSniplets = new List<string>(RebornConsoleSettings.Instance.CSharpSniplets).Concat(new[] { "" }).ToArray();
                RebornConsoleSettings.Instance.CSharpSnipletNames = new List<string>(RebornConsoleSettings.Instance.CSharpSnipletNames).Concat(new[] { name }).ToArray();
                savedSnipletsCombo.Items.Add(name);
                savedSnipletsCombo.SelectedIndex = RebornConsoleSettings.Instance.CSharpSniplets.Length - 1;
                RebornConsoleSettings.Instance.CSharpSelectedIndex = savedSnipletsCombo.SelectedIndex;
            }
            else
            {
                //LUA
                RebornConsoleSettings.Instance.LuaSniplets = new List<string>(RebornConsoleSettings.Instance.LuaSniplets).Concat(new[] { "" }).ToArray();
                RebornConsoleSettings.Instance.LuaSnipletNames = new List<string>(RebornConsoleSettings.Instance.LuaSnipletNames).Concat(new[] { name }).ToArray();
                savedSnipletsCombo.Items.Add(name);
                savedSnipletsCombo.SelectedIndex = RebornConsoleSettings.Instance.LuaSniplets.Length - 1;
                RebornConsoleSettings.Instance.LuaSelectedIndex = savedSnipletsCombo.SelectedIndex;
            }


        }

        private static string GetNewScriptName()
        {

            if (RebornConsoleSettings.Instance.TabIndex == 0)
            {
                // C#
                for (int i = 1; i < int.MaxValue; i++)
                {
                    string name = "Untitled" + i;
                    if (!RebornConsoleSettings.Instance.CSharpSnipletNames.Contains(name))
                        return name;
                }
            }
            else
            {
                //LUA
                for (int i = 1; i < int.MaxValue; i++)
                {
                    string name = "Untitled" + i;
                    if (!RebornConsoleSettings.Instance.LuaSnipletNames.Contains(name))
                        return name;
                }
            }

            return Path.GetRandomFileName();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Delete this?", MessageBoxButtons.YesNo) == DialogResult.Yes &&savedSnipletsCombo.SelectedIndex >= 0)
            {
                var temp1 = new List<string>(RebornConsoleSettings.Instance.CSharpSniplets);
                var temp2 = new List<string>(RebornConsoleSettings.Instance.CSharpSnipletNames);
                temp1.RemoveAt(savedSnipletsCombo.SelectedIndex);
                temp2.RemoveAt(savedSnipletsCombo.SelectedIndex);
                RebornConsoleSettings.Instance.CSharpSniplets = temp1.ToArray();
                RebornConsoleSettings.Instance.CSharpSnipletNames = temp2.ToArray();

                int index = savedSnipletsCombo.SelectedIndex;
                savedSnipletsCombo.Items.RemoveAt(savedSnipletsCombo.SelectedIndex);

                if (index == savedSnipletsCombo.Items.Count)
                    index--;

                if (index < 0)
                {
                    AddNewSnipplet(GetNewScriptName());
                    index++;
                }
                RebornConsoleSettings.Instance.CSharpSelectedIndex = savedSnipletsCombo.SelectedIndex = index;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            RebornConsoleSettings.Instance.Save();
            HotkeyManager.Unregister("RebornConsole");
        }


        private void CompileAsync()
        {
            var text = csharpCode.Text;
            Task.Run(() =>
            {

                try
                {
                    RebornConsole.CodeDriver.Compile(text);
                }
                catch (Exception ex)
                {
                }

            });
        }
        private void csharpCode_TextChanged(object sender, EventArgs e)
        {

            if (RebornConsoleSettings.Instance.TabIndex == 0)
            {
                // C#
                if (savedSnipletsCombo.SelectedIndex >= 0)
                    RebornConsoleSettings.Instance.CSharpSniplets[savedSnipletsCombo.SelectedIndex] = csharpCode.Text;

                timer1.Stop();
                timer1.Start();

            }
            else
            {
                //LUA
                if (savedSnipletsCombo.SelectedIndex >= 0)
                    RebornConsoleSettings.Instance.LuaSniplets[savedSnipletsCombo.SelectedIndex] = luaCode.Text;
            }

        }

        private void RenameButton_Click(object sender, EventArgs e)
        {
            savedSnipletsCombo.Items[RebornConsoleSettings.Instance.CSharpSelectedIndex] = savedSnipletsCombo.Text;
            RebornConsoleSettings.Instance.CSharpSnipletNames[RebornConsoleSettings.Instance.CSharpSelectedIndex] =savedSnipletsCombo.Text;
            savedSnipletsCombo.SelectedIndex = RebornConsoleSettings.Instance.CSharpSelectedIndex;
        }

        private bool _keyBindMode;

        private void btnKeybind_Click(object sender, EventArgs e)
        {
            if (!_keyBindMode)
            {
                SetKeybindButtonToEditMode((Button)sender);
                _keyBindMode = true;
            }
        }

        void SetKeybindButtonToEditMode(Button btn)
        {
            btn.BackColor = SystemColors.GradientInactiveCaption;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Text = "Press key combination";
        }

        private Keys _cachedKey;
        private void btnKeybind_KeyDown(object sender, KeyEventArgs e)
        {
            // return if not changing keybinds.
            if (_keyBindMode)
            {
                _cachedKey = e.KeyData;
                e.SuppressKeyPress = true;
                if (_cachedKey != Keys.None)
                {
                    if (_cachedKey == Keys.Escape)
                    {
                        SetKeybindButtonToNormalMode((Button)sender);
                        return;
                    }
                    var btn = (Button)sender;
                    btn.Text = GetKeyString(_cachedKey);
                }
            }
        }

        void SetKeybindButtonToNormalMode(Button btn)
        {
            btn.FlatStyle = FlatStyle.Standard;
            btn.BackColor = SystemColors.Control;
            btn.Text = RebornConsoleSettings.Instance.Keybind != Keys.None ? GetKeyString(RebornConsoleSettings.Instance.Keybind) : "Click to set Keybind"; ;
        }

        static string GetKeyString(Keys keys)
        {
            var key = keys & Keys.KeyCode;
            var mod = keys & Keys.Modifiers;
            string returnVal = string.Empty;
            if (mod != Keys.None)
            {
                returnVal = Enum.GetValues(typeof(Keys)).Cast<Keys>().Where(k => mod.HasFlag(k)).Aggregate(returnVal, (current, k) => k != Keys.None ? current + " " + k.ToString() : string.Empty);
            }
            if (key != Keys.None)
                returnVal += string.Format(" {0}", key >= Keys.D0 && keys <= Keys.D9 ? ((int)key - 48).ToString() : key.ToString());
            return returnVal;
        }

        public void invokeCompile()
        {
            this.Invoke((MethodInvoker)this.CompileAndRun);
        }

        private void btnKeybind_KeyUp(object sender, KeyEventArgs e)
        {
            if (_keyBindMode)
            {
                _keyBindMode = false;
                if (_cachedKey != Keys.None && _cachedKey != Keys.Escape)
                {
                    RebornConsoleSettings.Instance.Keybind = _cachedKey;
                    
                    HotkeyManager.Unregister("RebornConsole");
                    HotkeyManager.Register("RebornConsole", RebornConsoleSettings.Instance.Key, RebornConsoleSettings.Instance.ModifierKey, hotkey => { invokeCompile(); });
                }
                SetKeybindButtonToNormalMode((Button)sender);
            }
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RebornConsoleSettings.Instance.TabIndex = tabControl1.SelectedIndex;
            SetupComboBox();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OutputTextBox.Text = "";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            CompileAsync();
        }
    }
}
