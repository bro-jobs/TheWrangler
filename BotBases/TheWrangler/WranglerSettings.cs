/*
 * WranglerSettings.cs - Settings Persistence
 * ===========================================
 *
 * PURPOSE:
 * This class handles loading and saving TheWrangler's settings to disk.
 * Settings are stored as JSON in the RebornBuddy settings folder.
 *
 * LOCATION:
 * Settings file: %RebornBuddy%/Settings/TheWrangler/Settings.json
 *
 * HOW IT WORKS:
 * 1. Uses Newtonsoft.Json for serialization (bundled with RebornBuddy)
 * 2. Automatically loads on first access
 * 3. Call Save() to persist changes
 *
 * NOTES FOR CLAUDE:
 * - This is a self-contained settings class (doesn't inherit from JsonSettings)
 * - This avoids compilation issues with ff14bot.Settings namespace
 * - Always access via WranglerSettings.Instance (singleton pattern)
 * - Settings persist across sessions
 */

using System;
using System.IO;
using ff14bot.Helpers;
using Newtonsoft.Json;

namespace TheWrangler
{
    /// <summary>
    /// Persistent settings for TheWrangler botbase.
    /// Access via WranglerSettings.Instance
    /// </summary>
    public class WranglerSettings
    {
        #region Singleton

        private static WranglerSettings _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Singleton instance - use this to access settings.
        /// </summary>
        public static WranglerSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = Load();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private WranglerSettings()
        {
            // Initialize defaults
            LastJsonPath = "";
            IgnoreHome = false;
            LastBrowseDirectory = "";
            WindowX = -1;
            WindowY = -1;
        }

        #endregion

        #region Settings Properties

        /// <summary>
        /// Path to the last selected JSON file.
        /// Persisted so users don't have to re-select each session.
        /// </summary>
        public string LastJsonPath { get; set; }

        /// <summary>
        /// If true, Lisbeth won't return to home location after completing orders.
        /// Some users prefer to stay at their crafting spot.
        /// </summary>
        public bool IgnoreHome { get; set; }

        /// <summary>
        /// Remembers the last directory used in the file browser.
        /// Quality of life feature for users with organized JSON folders.
        /// </summary>
        public string LastBrowseDirectory { get; set; }

        /// <summary>
        /// Window X position for form state persistence.
        /// </summary>
        public int WindowX { get; set; }

        /// <summary>
        /// Window Y position for form state persistence.
        /// </summary>
        public int WindowY { get; set; }

        #endregion

        #region Helper Properties

        /// <summary>
        /// Checks if a valid JSON file is selected.
        /// </summary>
        [JsonIgnore]
        public bool HasValidJsonPath => !string.IsNullOrWhiteSpace(LastJsonPath)
                                         && File.Exists(LastJsonPath);

        /// <summary>
        /// Gets just the filename from the full path (for display).
        /// </summary>
        [JsonIgnore]
        public string JsonFileName => HasValidJsonPath
            ? Path.GetFileName(LastJsonPath)
            : "No file selected";

        #endregion

        #region File Path

        /// <summary>
        /// Gets the settings file path.
        /// </summary>
        private static string SettingsFilePath
        {
            get
            {
                var settingsDir = Path.Combine(
                    Environment.CurrentDirectory,
                    "Settings",
                    "TheWrangler");

                // Ensure directory exists
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                return Path.Combine(settingsDir, "Settings.json");
            }
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Saves settings to disk.
        /// </summary>
        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Logging.Write($"[TheWrangler] Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads settings from disk, or creates new instance with defaults.
        /// </summary>
        private static WranglerSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonConvert.DeserializeObject<WranglerSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"[TheWrangler] Error loading settings: {ex.Message}");
            }

            // Return new instance with defaults
            return new WranglerSettings();
        }

        #endregion
    }
}
