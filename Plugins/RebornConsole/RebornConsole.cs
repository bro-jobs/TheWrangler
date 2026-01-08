using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Clio.Utilities;
using Clio.Utilities.Compiler;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Interfaces;
using ff14bot.Managers;
using ff14bot.Settings;
using Newtonsoft.Json;
using static SQLite.SQLite3;


namespace HighVoltz
{
    public class RebornConsole : BotPlugin
    {
        public static string TempFolder = Path.Combine(GlobalSettings.Instance.PluginsPath, @"RebornConsole\Temp\");//Utils.AssemblyDirectory + @"\Plugins\RebornConsole\Temp\";
        internal static AppDomainDriver CodeDriver = new AppDomainDriver();
        private readonly Version _version = new Version(1, 10);
        private Thread newThread;
        public RebornConsole()
        {
            Instance = this;
            if (!_init)
            {
                //logr = Logger.GetLoggerInstanceForType();
                // using ctor so plugin doesn't need to be 'Enabled' to initialize..
                try
                {
                    WipeTempFolder();
                }
                catch (Exception ex)
                {
                    //logr.Debug(ex.ToString());
                }
                _init = true;
            }

        }


        public override string Author 
        {
            get { return "HighVoltz"; }
        }

        public override string Name
        {
            get
            {
                return "RebornConsole";
            }
        }

   

        public override bool WantButton
        {
            get { return true; }
        }

        public override string ButtonText
        {
            get { return "meow"; }
        }


        public override void OnShutdown()
        {
            CloseForm();
        }

        public override void OnEnabled()
        {
            ToggleRBConsole();
        }

        public override void OnDisabled()
        {
            CloseForm();
        }

        

        public override Version Version
        {
            get
            {
                return new Version(0, 0, 1);
            }
        }


        private static bool _init;


        public static RebornConsole Instance { get; private set; }



        private void WipeTempFolder()
        {
            if (!Directory.Exists(TempFolder))
            {
                Directory.CreateDirectory(TempFolder);
            }
            foreach (string file in Directory.GetFiles(TempFolder, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch
                {
                }
                // ReSharper restore EmptyGeneralCatchClause
            }
        }

        public override void OnButtonPress()
        {
            ToggleRBConsole();
        }


        internal Thread _guiThread;
        internal MainForm remoteForm;
        public void ToggleRBConsole()
        {
            if (_guiThread == null || !_guiThread.IsAlive)
            {
                _guiThread = new Thread(() =>
                {
                    remoteForm = new MainForm();


                    HotkeyManager.Unregister("RebornConsole");
                    HotkeyManager.Register("RebornConsole", RebornConsoleSettings.Instance.Key, RebornConsoleSettings.Instance.ModifierKey, hotkey => { remoteForm.invokeCompile(); });

                    remoteForm.ShowDialog();
                })
                { IsBackground = true };
                _guiThread.SetApartmentState(ApartmentState.STA);
                _guiThread.Start();
            }
            else
            {
                CloseForm();
            }
        }
        private void CloseForm()
        {
            if (remoteForm != null && remoteForm.Visible)
            {
                // close the form on the forms thread
                remoteForm.Invoke((System.Windows.Forms.MethodInvoker)delegate { remoteForm.Close(); });
            }
        }

        public static void Api(string apiName)
        {
            Type[] types = Assembly.GetEntryAssembly().GetExportedTypes().Where(t => t.Name.Contains(apiName)).ToArray();

            foreach (Type t in types)
            {
                Log(Colors.DarkGreen, "\n  *** {0} ***", t.FullName);
                foreach (MemberInfo mi in t.GetMembers())
                {
                    Log("{0}", mi);
                }
            }
        }

        public static void Log(string text, params object[] arg)
        {
            if (arg.Length == 0)
            {
                LogNoFormat(Colors.Black, text);
                return;
            }

            if (!string.IsNullOrEmpty(text))
                Log(Colors.Black, text, arg);
        }
        public static void Log(string text)
        {
            if (!string.IsNullOrEmpty(text))
                Log(Colors.Black, text);
        }
        public static void Logi(IntPtr obj)
        {
            //logr.Debug(text.ToString());
            try
            {
#if RB_X64
                Log(Colors.Black, "{0:X}", (ulong)obj);
#else
                Log(Colors.Black, "{0:X}", (int)obj);
#endif
            }
            catch (System.Exception)
            {

            }

        }
        public static void Log(object obj)
        {
            //logr.Debug(text.ToString());
            try
            {
                if (obj == null)
                {
                    Log(Colors.Black, "null");
                }
                else
                {
                    LogNoFormat(Colors.Black, obj.ToString());
                }
                
            }
            catch (System.Exception)
            {

            }

        }

        public static void ClearLog()
        {
            if (MainForm.Instance.OutputTextBox.InvokeRequired)
                MainForm.Instance.OutputTextBox.BeginInvoke(new Action(() => MainForm.Instance.OutputTextBox.Clear()));
            else
                MainForm.Instance.OutputTextBox.Clear();
        }


        public static void LogNoFormat(Color c, string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return;
                if (MainForm.IsValid)
                {
                    try
                    {
                        if (MainForm.Instance.OutputTextBox.InvokeRequired)
                            MainForm.Instance.OutputTextBox.BeginInvoke(new UpdateLogCallback(UpdateLog), c, text);
                        else
                        {
                            MainForm.Instance.OutputTextBox.SelectionStart = MainForm.Instance.OutputTextBox.TextLength;
                            MainForm.Instance.OutputTextBox.SelectionColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                            MainForm.Instance.OutputTextBox.SelectedText = text + Environment.NewLine;
                            MainForm.Instance.OutputTextBox.ScrollToCaret();
                        }
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch
                    {
                    }
                    // ReSharper restore EmptyGeneralCatchClause
                }
                else
                {
                    Logging.Write(text);
                }
            }
            catch (Exception e)
            {
                Logging.WriteException(e);
                //logr.Error(e);
                //logr.DebugFormat(text,arg);
            }
        }

        public static void Log(Color c, string text, params object[] arg)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    return;
                string newtext;
                try
                {
                    newtext = string.Format(text, arg);
                    text = newtext;
                }
                catch (System.Exception)
                {
                    
                }
                
                if (MainForm.IsValid)
                {
                    try
                    {
                        if (MainForm.Instance.OutputTextBox.InvokeRequired)
                            MainForm.Instance.OutputTextBox.BeginInvoke(new UpdateLogCallback(UpdateLog), c, text);
                        else                  
                        {
                            MainForm.Instance.OutputTextBox.SelectionStart = MainForm.Instance.OutputTextBox.TextLength;
                            MainForm.Instance.OutputTextBox.SelectionColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
                            MainForm.Instance.OutputTextBox.SelectedText = text + Environment.NewLine;
                            MainForm.Instance.OutputTextBox.ScrollToCaret();
                        }
                    }
                    // ReSharper disable EmptyGeneralCatchClause
                    catch
                    {
                    }
                    // ReSharper restore EmptyGeneralCatchClause
                }
                else
                {
                    Logging.Write(text, arg);
                }
            }
            catch (Exception e)
            {
                Logging.WriteException(e);
                //logr.Error(e);
                //logr.DebugFormat(text,arg);
            }
        }

        internal static void UpdateLog(Color c, string text)
        {
            MainForm.Instance.textOutput.SelectionStart = MainForm.Instance.textOutput.TextLength;
            MainForm.Instance.textOutput.SelectionColor = System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
            MainForm.Instance.textOutput.SelectedText = text + Environment.NewLine;
            MainForm.Instance.textOutput.ScrollToCaret();
        }

        #region Nested type: UpdateLogCallback

        internal delegate void UpdateLogCallback(Color color, string text);

        #endregion


    }


    public class AppDomainDriver
    {

        public void Compile(string code)
        {
            try
            {
                var codeDriver = (CodeDriver)Activator.CreateInstance(typeof(CodeDriver));
                codeDriver.Compile(code);
            }
            catch (Exception ex)
            {
            }
        }
        public bool CompileAndRun(string code)
        {
            try
            {

                var codeDriver = (CodeDriver)Activator.CreateInstance(typeof(CodeDriver));
                Pulsator.Pulse(PulseFlags.All ^ PulseFlags.Avoidance);
                //FateManager.Update();
                return codeDriver.CompileAndRun(code);
            }
            catch (Exception ex)
            {
                RebornConsole.Log(Colors.Red, ex.ToString());
                Logging.Write(ex);
                return false;
            }
        }
    }


    public class CodeDriver
    {
        #region Strings

        private const string Prefix = @"
             using System;   
             using System.Reflection;   
             using System.Data;   
             using System.Threading;   
             using System.Diagnostics;   
             using System.Drawing;   
             using System.Collections.Generic;   
             using System.Collections;    
             using System.Linq;    
             using System.Text;    
             using System.IO;    
             using System.Windows.Forms;   
             using System.Text.RegularExpressions;   
             using System.Globalization;   
             using System.Xml.Linq;  
             using System.Runtime.InteropServices; 
             using System.Reflection.Emit;

            using ff14bot;
            using ff14bot.Objects;
            using ff14bot.Managers;
            using ff14bot.NeoProfiles;
            using ff14bot.RemoteWindows;
			using ff14bot.RemoteAgents;
            using ff14bot.Navigation;
            using ff14bot.Enums;
            using Clio.Utilities;
            using ff14bot.BotBases;
			using ff14bot.Helpers;

             public static class Driver   
             {   
                 
public static void Run()
{
";

        private const string Postfix = @"
              } 

             static public void Log ( string format, params object[] arg) 
             { 
                HighVoltz.RebornConsole.Log(format,arg); 
             } 

             static public void Log ( IntPtr format) 
             { 
                HighVoltz.RebornConsole.Logi(format); 
             } 
			 static public void Log ( int format) 
             { 
                HighVoltz.RebornConsole.Log(format); 
             } 
             static public void Log ( object format) 
             { 
                HighVoltz.RebornConsole.Log(format); 
             } 
             static public void Api ( string format) 
             { 
                HighVoltz.RebornConsole.Api(format); 
             } 
             static public void ClearLog () 
             { 
                HighVoltz.RebornConsole.ClearLog(); 
             } 

            public static void fLog(IEnumerable x)
            {
                foreach (var item in x)
                {
                    HighVoltz.RebornConsole.Log(item);
                }
            }



             }
";
        /*              static List<WoWGameObject> GameObjects {get{return ObjectManager.GetObjectsOfType<WoWGameObject>();}} 
                         static List<WoWObject> Objects {get{return ObjectManager.ObjectList;}} 
                         static List<WoWUnit> Units {get{return ObjectManager.GetObjectsOfType<WoWUnit>();}} 
                         static List<WoWPlayer> Players {get{return ObjectManager.GetObjectsOfType<WoWPlayer>();}} 
                         static List<WoWItem> Items {get{return ObjectManager.GetObjectsOfType<WoWItem>();}} 
                         static List<WoWDynamicObject> DynamicObjects {get{return ObjectManager.GetObjectsOfType<WoWDynamicObject>();}} */

        #endregion

        //private CodeDomProvider provider = Clio.Utilities.Compiler.CodeCompiler.CreateLatestCSharpProviderTTL(900);

        private static readonly Dictionary<string, CompileResult> CompileCache = new Dictionary<string, CompileResult>();


        public CompileResult Compile(string input)
        {
            lock (CompileCache)
            {
                if (!CompileCache.TryGetValue(input, out var results))
                {
                    //using (var provider = new CSharpCodeProvider(new Dictionary<string, string>{{"CompilerVersion", "v4.0"},}))
                    //using (CodeDomProvider provider = Clio.Utilities.Compiler.CodeCompiler.CreateLatestCSharpProviderTTL(900))
                    //{
                    //    var options = new CompilerParameters();
                    //    // most recent assembly
                    //    var myAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    //        .Where(a => a.GetName().Name.Contains(RebornConsole.Instance.Name))
                    //        .OrderByDescending(a => new FileInfo(a.Location).CreationTime).FirstOrDefault();
                    //
                    //    options.ReferencedAssemblies.Add(myAssembly.Location);
                    //    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    //    {
                    //        if (!asm.GetName().Name.Contains(RebornConsole.Instance.Name) && !asm.IsDynamic)
                    //            options.ReferencedAssemblies.Add(asm.Location);
                    //    }
                    //
                    //    options.GenerateExecutable = false;
                    //    options.TempFiles = new TempFileCollection(RebornConsole.TempFolder, false);
                    //    options.IncludeDebugInformation = true;
                    //    options.OutputAssembly = $"{RebornConsole.TempFolder}\\CodeAssembly{Guid.NewGuid():N}.dll";
                    //    options.CompilerOptions = "/optimize /unsafe";
                    //
                    //    var sb = new StringBuilder();
                    //    sb.Append(Prefix);
                    //    sb.Append(input);
                    //    sb.Append(Postfix);
                    //
                    //    results = provider.CompileAssemblyFromSource(options, sb.ToString());
                    //    CompileCache[input] = results;
                    //}

                    
                    var sb = new StringBuilder();
                    sb.Append(Prefix);
                    sb.Append(input);
                    sb.Append(Postfix);
                    results = Clio.Utilities.Compiler.CodeCompiler.CompileScript($"CodeAssembly{Guid.NewGuid():N}",sb.ToString());
                    CompileCache[input] = results;

                }
                return results;
            }
        }

        // returns true if compiled sucessfully
        public bool CompileAndRun(string input)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            CompileResult results = Compile(input);


            if (!results.Result.Success)
            {
                var errorMessage = new StringBuilder();
                var errLineOffset = Prefix.Count(c => c == '\n'); ;
                foreach (var error in results.Result.Diagnostics)
                {
                    var line = error.Location.GetLineSpan().StartLinePosition.Line+1;
                    errorMessage.AppendFormat("{0}) {1}\n", line - errLineOffset, error.GetMessage());
                }

                RebornConsole.Log(Colors.Red, errorMessage.ToString());
                return false;
            }
            Type driverType = results.Assembly.GetType("Driver");
            try
            {
                driverType.InvokeMember("Run", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, null);
            }
            catch (Exception e)
            {
                RebornConsole.Log(e);
            }

            return true;
        }
    }


    internal class RebornConsoleSettings : JsonSettings
    {
        internal static readonly RebornConsoleSettings Instance = new RebornConsoleSettings();

        public RebornConsoleSettings(): base(GetSettingsFilePath("Global", "RebornConsole.json"))
        {
            if (CSharpSniplets == null)
                CSharpSniplets = new[] { "" };

            if (CSharpSnipletNames == null)
                CSharpSnipletNames = new[] { "Untitled1" };


            if (LuaSniplets == null)
                LuaSniplets = new[] { "" };

            if (LuaSnipletNames == null)
                LuaSnipletNames = new[] { "Untitled1" };

            //CSharpSelectedIndex = 0;
            if (Keybind == Keys.None)
                Keybind = Keys.F4;
        }

        [Setting]
        public string[] CSharpSniplets { get; set; }

        [Setting]
        public string[] CSharpSnipletNames { get; set; }


        [Setting]
        public string[] LuaSniplets { get; set; }

        [Setting]
        public string[] LuaSnipletNames { get; set; }

        [Setting]
        public int CSharpSelectedIndex { get; set; }
        [Setting]
        public int LuaSelectedIndex { get; set; }

        [Setting]
        public int TabIndex { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ShowLuaTab_ThisCanBeDangerous { get; set; }

        [Setting]
        [DefaultValue(null)]
        public Keys Keybind { get; set; }



        [JsonIgnore]
        public  Keys Key
        {
            get
            {
                return (Keys)((int)Keybind & 0x0000FFFF);
            }
            
        }
        [JsonIgnore]
        public ModifierKeys ModifierKey
        {
            get
            {
                var key = (Keys)((int)Keybind & 0xFFFF0000);
                return (ModifierKeys)Enum.Parse(typeof(ModifierKeys), key.ToString());
            }
        }


        
    }
}