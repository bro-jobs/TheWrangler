using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ICSharpCode.SharpZipLib.Zip;
using TreeSharp;
using Action = System.Action;

namespace Lisbeth.Reborn
{
    public class OnlineLoader : BotBase
    {
        private const string ProjectName = "Lisbeth";
        private const string ProjectMainType = "Lisbeth.Reborn.LisbethBot";
        private const string ProjectAssemblyName = "Lisbeth.Reborn.dll";
        private static readonly Color _logColor = Color.FromRgb(183, 73, 91);
        public override PulseFlags PulseFlags => PulseFlags.All;
        public override bool IsAutonomous => true;
        public override bool WantButton => true;
        public override bool RequiresProfile => false;
        public object Lisbeth { get; set; }

        #if RB_TC
        public const string Locale = "TC";
        #elif RB_CN
        public const string Locale = "CN";
        #else
        public const string Locale = "EN";
        #endif

        private const string VersionUrl = $"https://lisbeth.io/downloads/{Locale}/version.txt";
        private const string DataUrl = $"https://lisbeth.io/downloads/{Locale}/Lisbeth.zip";

        private static readonly object _locker = new object();
        private static readonly string _versionPath = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\version.txt");
        private static readonly string _projectAssembly = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}\{ProjectAssemblyName}");
        private static readonly string _projectDir = Path.Combine(Environment.CurrentDirectory, $@"BotBases\{ProjectName}");
        private static readonly string _greyMagicAssembly = Path.Combine(Environment.CurrentDirectory, @"GreyMagic.dll");
        private static bool _updated;
        private static Composite _root;
        private static Action _onButtonPress, _start, _stop;

        private static readonly Composite _failsafeRoot = new TreeSharp.Action(c =>
        {
            Log($"{ProjectName} is not loaded correctly.");
            TreeRoot.Stop();
        });

        public OnlineLoader()
        {
            lock (_locker)
            {
                if (_updated) { return; }
                _updated = true;
            }

            var dispatcher = Dispatcher.CurrentDispatcher;
            Task.Run(async () => { await Update(); Load(dispatcher); });
        }

        public override string Name => ProjectName;

        public override Composite Root => _root ?? _failsafeRoot;

        public override void OnButtonPress() => _onButtonPress?.Invoke();

        public override void Start() => _start?.Invoke();

        public override void Stop() => _stop?.Invoke();

        private void Load(Dispatcher dispatcher)
        {
            RedirectAssembly();

            var assembly = LoadAssembly(_projectAssembly);
            if (assembly == null) { return; }

            Type baseType;
            try { baseType = assembly.GetType(ProjectMainType); }
            catch (Exception e)
            {
                Log(e.ToString());
                return;
            }

            dispatcher.BeginInvoke(new Action(() =>
            {
                object product;
                try { product = Activator.CreateInstance(baseType); }
                catch (Exception e)
                {
                    Log(e.ToString());
                    return;
                }

                var type = product.GetType();
                _root = (Composite)type.GetProperty("Root")?.GetValue(product);
                _start = (Action)type.GetProperty("StartAction")?.GetValue(product);
                _stop = (Action)type.GetProperty("StopAction")?.GetValue(product);
                _onButtonPress = (Action)type.GetProperty("ButtonAction")?.GetValue(product);
                Lisbeth = product;

                Log($"{ProjectName} loaded.");
            }));
        }

        public static void RedirectAssembly()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = Assembly.GetEntryAssembly()?.GetName().Name;
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != name ? null : Assembly.GetEntryAssembly();
            };

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var requestedAssembly = new AssemblyName(args.Name);
                return requestedAssembly.Name != "GreyMagic" ? null : Assembly.LoadFrom(_greyMagicAssembly);
            };
        }

        private static Assembly LoadAssembly(string path)
        {
            if (!File.Exists(path)) { return null; }

            Assembly assembly = null;
            try { assembly = Assembly.LoadFrom(path); }
            catch (Exception e) { Logging.WriteException(e); }

            return assembly;
        }

        private static void Log(string message)
        {
            message = "[" + ProjectName + "] " + message;
            Logging.Write(_logColor, message);
        }

        private static async Task Update()
        {
            var local = GetLocalVersion();
            var data = await TryUpdate(local);
            if (data == null) { return; }

            try { Clean(_projectDir); }
            catch (Exception e) { Log(e.ToString()); }

            try { Extract(data, _projectDir); }
            catch (Exception e) { Log(e.ToString()); }
        }

        private static string GetLocalVersion()
        {
            if (!File.Exists(_versionPath)) { return null; }
            try
            {
                var version = File.ReadAllText(_versionPath);
                return version;
            }
            catch { return null; }
        }

        private static void Clean(string directory)
        {
            foreach (var file in new DirectoryInfo(directory).GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in new DirectoryInfo(directory).GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public static async Task<byte[]> TryUpdate(string localVersion)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var stopwatch = Stopwatch.StartNew();
                    var version = await client.GetStringAsync(VersionUrl);
                    if (string.IsNullOrEmpty(version) || version == localVersion) { return null; }

                    Log($"Local: {localVersion} | Latest: {version}");
                    using (var response = await client.GetAsync(DataUrl))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Log($"[Error] Could not download {ProjectName}: {response.StatusCode}");
                            return null;
                        }

                        using (var inputStream = await response.Content.ReadAsStreamAsync())
                        using (var memoryStream = new MemoryStream())
                        {
                            await inputStream.CopyToAsync(memoryStream);

                            stopwatch.Stop();
                            Log($"Download took {stopwatch.ElapsedMilliseconds} ms.");

                            return memoryStream.ToArray();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log($"[Error] {e}");
                return null;
            }
        }

        private static void Extract(byte[] files, string directory)
        {
            using (var stream = new MemoryStream(files))
            {
                var zip = new FastZip();
                zip.ExtractZip(stream, directory, FastZip.Overwrite.Always, null, null, null, false, true);
            }
        }
    }
}
