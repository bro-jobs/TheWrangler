/*
 * LisbethHomeHelper.cs - Home Navigation Helper
 * ==============================================
 *
 * PURPOSE:
 * Reads Lisbeth's home location settings and navigates the player there.
 * Used for Timer and Schedule modes in WranglerMaster when the session ends.
 *
 * HOW IT WORKS:
 * 1. Finds the character's Lisbeth settings folder (pattern: {CharName}_World{WorldId})
 * 2. Parses lisbethV4.json to extract the Homes array
 * 3. Finds the enabled home entry
 * 4. Uses LlamaLibrary's Navigation.GetTo to travel there
 *
 * NOTES FOR CLAUDE:
 * - Character folders are at Settings/ root, not in a Lisbeth subfolder
 * - Home settings contain Zone ID, Area name, and X/Y/Z position
 * - UseAetheryteAsHome means just teleport to the zone's aetheryte
 * - This is separate from the game's /return or LocalPlayer.HomePoint
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using LlamaLibrary.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TheWrangler
{
    /// <summary>
    /// Helper class for navigating to Lisbeth's configured home location.
    /// </summary>
    public class LisbethHomeHelper
    {
        private readonly LisbethApi _lisbethApi;

        public LisbethHomeHelper()
        {
            _lisbethApi = new LisbethApi();
            _lisbethApi.Initialize();
        }

        #region Home Settings Model

        /// <summary>
        /// Represents a home location entry from Lisbeth's settings.
        /// </summary>
        public class HomeEntry
        {
            public uint ZoneId { get; set; }
            public string Area { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public bool Enabled { get; set; }

            public Vector3 Position => new Vector3(X, Y, Z);
        }

        /// <summary>
        /// Result of attempting to go home.
        /// </summary>
        public class GoHomeResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public HomeEntry HomeUsed { get; set; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to navigate to Lisbeth's configured home location.
        /// </summary>
        /// <returns>Result indicating success/failure and details</returns>
        public async Task<GoHomeResult> GoHomeAsync()
        {
            try
            {
                // Step 1: Find character's settings folder
                var settingsFolder = FindCharacterSettingsFolder();
                if (settingsFolder == null)
                {
                    return new GoHomeResult
                    {
                        Success = false,
                        Message = "Could not find Lisbeth settings folder for current character"
                    };
                }

                Log($"Found settings folder: {settingsFolder}");

                // Step 2: Parse lisbethV4.json
                var settingsFile = Path.Combine(settingsFolder, "lisbethV4.json");
                if (!File.Exists(settingsFile))
                {
                    return new GoHomeResult
                    {
                        Success = false,
                        Message = "lisbethV4.json not found in character folder"
                    };
                }

                var homeEntry = ParseHomeSettings(settingsFile);
                if (homeEntry == null)
                {
                    return new GoHomeResult
                    {
                        Success = false,
                        Message = "No enabled home location found in Lisbeth settings"
                    };
                }

                Log($"Home location: Zone {homeEntry.ZoneId} ({homeEntry.Area}) at {homeEntry.Position}");

                // Step 3: Navigate to home using Lisbeth's travel API
                var currentZone = WorldManager.ZoneId;
                Log($"Current zone: {currentZone}, Target zone: {homeEntry.ZoneId}");

                bool success;

                // Use Lisbeth's TravelToWithArea - it handles teleports, flying, local movement, everything
                if (_lisbethApi.HasTravelApi && !string.IsNullOrEmpty(homeEntry.Area))
                {
                    Log($"Using Lisbeth TravelToWithArea: {homeEntry.Area}");
                    success = await _lisbethApi.TravelToAreaAsync(homeEntry.Area, homeEntry.Position);
                }
                else
                {
                    // Fallback to LlamaLibrary Navigation if Lisbeth travel not available
                    Log("Using LlamaLibrary Navigation.GetTo as fallback");
                    success = await Navigation.GetTo((ushort)homeEntry.ZoneId, homeEntry.Position);
                }

                if (success)
                {
                    return new GoHomeResult
                    {
                        Success = true,
                        Message = $"Arrived at home in {homeEntry.Area}",
                        HomeUsed = homeEntry
                    };
                }
                else
                {
                    return new GoHomeResult
                    {
                        Success = false,
                        Message = $"Navigation to home failed",
                        HomeUsed = homeEntry
                    };
                }
            }
            catch (Exception ex)
            {
                Log($"Error in GoHomeAsync: {ex.Message}");
                return new GoHomeResult
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Gets the configured home location without navigating to it.
        /// </summary>
        /// <returns>Home entry if found, null otherwise</returns>
        public HomeEntry GetHomeLocation()
        {
            try
            {
                var settingsFolder = FindCharacterSettingsFolder();
                if (settingsFolder == null) return null;

                var settingsFile = Path.Combine(settingsFolder, "lisbethV4.json");
                if (!File.Exists(settingsFile)) return null;

                return ParseHomeSettings(settingsFile);
            }
            catch (Exception ex)
            {
                Log($"Error getting home location: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a home location is configured.
        /// </summary>
        public bool HasHomeConfigured()
        {
            return GetHomeLocation() != null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds the Lisbeth settings folder for the current character.
        /// Folder pattern: {CharacterName}_World{WorldId}
        /// </summary>
        private string FindCharacterSettingsFolder()
        {
            try
            {
                var settingsRoot = Path.Combine(Environment.CurrentDirectory, "Settings");
                if (!Directory.Exists(settingsRoot))
                {
                    Log("Settings folder not found");
                    return null;
                }

                var characterName = Core.Me.Name;
                if (string.IsNullOrEmpty(characterName))
                {
                    Log("Character name not available");
                    return null;
                }

                // Look for folders that start with the character name
                var characterFolders = Directory.GetDirectories(settingsRoot)
                    .Where(d => Path.GetFileName(d).StartsWith(characterName + "_World", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (characterFolders.Count == 0)
                {
                    Log($"No settings folder found for character: {characterName}");
                    return null;
                }

                if (characterFolders.Count > 1)
                {
                    Log($"Multiple folders found for {characterName}, using first match");
                }

                var folder = characterFolders.First();

                // Verify it has lisbethV4.json
                if (File.Exists(Path.Combine(folder, "lisbethV4.json")))
                {
                    return folder;
                }

                Log($"Folder {folder} does not contain lisbethV4.json");
                return null;
            }
            catch (Exception ex)
            {
                Log($"Error finding settings folder: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses the Lisbeth settings file and extracts the enabled home entry.
        /// </summary>
        private HomeEntry ParseHomeSettings(string settingsFile)
        {
            try
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JObject.Parse(json);

                // Check if GoHomeWhenDone is enabled
                var goHome = settings["GoHomeWhenDone"]?.Value<bool>() ?? false;
                if (!goHome)
                {
                    Log("GoHomeWhenDone is disabled in Lisbeth settings");
                    // We still return the home if it exists, as this method is for getting the location
                }

                // Check UseAetheryteAsHome
                var useAetheryte = settings["UseAetheryteAsHome"]?.Value<bool>() ?? false;
                if (useAetheryte)
                {
                    Log("UseAetheryteAsHome is enabled - teleport mode");
                    // In this case, Lisbeth would just teleport to aetheryte
                    // We can still use the Homes array to determine the zone
                }

                // Parse Homes array
                var homes = settings["Homes"]?.ToObject<List<JObject>>();
                if (homes == null || homes.Count == 0)
                {
                    Log("No Homes entries found");
                    return null;
                }

                // Find first enabled home
                foreach (var home in homes)
                {
                    var enabled = home["Enabled"]?.Value<bool>() ?? false;
                    if (!enabled) continue;

                    // Log available fields for debugging
                    var fields = string.Join(", ", home.Properties().Select(p => p.Name));
                    Log($"Home entry fields: {fields}");

                    // Try different possible field names for coordinates
                    float x = 0, y = 0, z = 0;

                    // Try direct X, Y, Z
                    if (home["X"] != null)
                    {
                        x = home["X"].Value<float>();
                        y = home["Y"]?.Value<float>() ?? 0;
                        z = home["Z"]?.Value<float>() ?? 0;
                    }
                    // Try Position object with X, Y, Z
                    else if (home["Position"] != null)
                    {
                        var pos = home["Position"];
                        x = pos["X"]?.Value<float>() ?? 0;
                        y = pos["Y"]?.Value<float>() ?? 0;
                        z = pos["Z"]?.Value<float>() ?? 0;
                    }
                    // Try Location object
                    else if (home["Location"] != null)
                    {
                        var loc = home["Location"];
                        x = loc["X"]?.Value<float>() ?? 0;
                        y = loc["Y"]?.Value<float>() ?? 0;
                        z = loc["Z"]?.Value<float>() ?? 0;
                    }

                    var entry = new HomeEntry
                    {
                        ZoneId = home["Zone"]?.Value<uint>() ?? home["ZoneId"]?.Value<uint>() ?? 0,
                        Area = home["Area"]?.Value<string>() ?? "Unknown",
                        X = x,
                        Y = y,
                        Z = z,
                        Enabled = true
                    };

                    Log($"Parsed home: Zone={entry.ZoneId}, Pos=<{x}, {y}, {z}>");

                    if (entry.ZoneId > 0)
                    {
                        return entry;
                    }
                }

                Log("No enabled home entry found with valid zone");
                return null;
            }
            catch (Exception ex)
            {
                Log($"Error parsing settings: {ex.Message}");
                return null;
            }
        }

        private void Log(string message)
        {
            Logging.Write($"[TheWrangler] [HomeHelper] {message}");
        }

        #endregion
    }
}
