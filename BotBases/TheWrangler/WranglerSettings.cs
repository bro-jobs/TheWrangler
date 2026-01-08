/*
 * WranglerSettings.cs - Settings Persistence
 * ===========================================
 *
 * PURPOSE:
 * This class handles loading and saving TheWrangler's settings to disk.
 * Settings are stored as JSON in the RebornBuddy settings folder.
 *
 * LOCATION:
 * Settings file: %RebornBuddy%/Settings/Global/TheWrangler.json
 *
 * HOW IT WORKS:
 * 1. Inherits from JsonSettings which handles JSON serialization
 * 2. Properties marked with [Setting] are automatically persisted
 * 3. Call Save() to persist changes, or they auto-save on shutdown
 *
 * EXTENDING SETTINGS:
 * To add a new setting:
 * 1. Add a property with [Setting] attribute
 * 2. Optionally add [DefaultValue(x)] for a default
 * 3. The setting is automatically loaded/saved
 *
 * NOTES FOR CLAUDE:
 * - JsonSettings handles file I/O automatically
 * - Always access via WranglerSettings.Instance (singleton pattern)
 * - Settings persist across sessions
 * - Adding new settings is safe - defaults are used for missing values
 */

using System.ComponentModel;
using ff14bot.Settings;
using Newtonsoft.Json;

namespace TheWrangler
{
    /// <summary>
    /// Persistent settings for TheWrangler botbase.
    /// Access via WranglerSettings.Instance
    /// </summary>
    public class WranglerSettings : JsonSettings
    {
        #region Singleton

        /// <summary>
        /// Singleton instance - use this to access settings.
        /// </summary>
        public static WranglerSettings Instance { get; } = new WranglerSettings();

        /// <summary>
        /// Private constructor - creates settings file path and loads existing settings.
        /// </summary>
        private WranglerSettings() : base(GetSettingsFilePath("Global", "TheWrangler.json"))
        {
            // Initialize defaults if needed
        }

        #endregion

        #region Settings Properties

        /// <summary>
        /// Path to the last selected JSON file.
        /// Persisted so users don't have to re-select each session.
        /// </summary>
        [Setting]
        [DefaultValue("")]
        public string LastJsonPath { get; set; } = "";

        /// <summary>
        /// If true, Lisbeth won't return to home location after completing orders.
        /// Some users prefer to stay at their crafting spot.
        /// </summary>
        [Setting]
        [DefaultValue(false)]
        public bool IgnoreHome { get; set; } = false;

        /// <summary>
        /// Remembers the last directory used in the file browser.
        /// Quality of life feature for users with organized JSON folders.
        /// </summary>
        [Setting]
        [DefaultValue("")]
        public string LastBrowseDirectory { get; set; } = "";

        /// <summary>
        /// Window X position for form state persistence.
        /// </summary>
        [Setting]
        [DefaultValue(-1)]
        public int WindowX { get; set; } = -1;

        /// <summary>
        /// Window Y position for form state persistence.
        /// </summary>
        [Setting]
        [DefaultValue(-1)]
        public int WindowY { get; set; } = -1;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a valid JSON file is selected.
        /// </summary>
        [JsonIgnore]
        public bool HasValidJsonPath => !string.IsNullOrWhiteSpace(LastJsonPath)
                                         && System.IO.File.Exists(LastJsonPath);

        /// <summary>
        /// Gets just the filename from the full path (for display).
        /// </summary>
        [JsonIgnore]
        public string JsonFileName => HasValidJsonPath
            ? System.IO.Path.GetFileName(LastJsonPath)
            : "No file selected";

        #endregion
    }
}
