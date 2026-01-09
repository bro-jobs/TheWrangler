/*
 * LevelingController.cs - Leveling Mode Controller
 * =================================================
 *
 * PURPOSE:
 * Orchestrates the DoH/DoL leveling process by interpreting the profile XML
 * and executing behaviors directly via TheWrangler instead of using the
 * RebornBuddy profile system. This allows for greater control and dynamic
 * item quantity calculation.
 *
 * ARCHITECTURE:
 * - Parses Start.xml and sub-profiles to understand the leveling path
 * - Evaluates conditions (If/While) to determine which actions to execute
 * - Executes behaviors like Lisbeth orders, navigation, class changes, etc.
 * - Reports status updates to the UI via events
 *
 * KEY FEATURES:
 * - Class level tracking for all crafters/gatherers
 * - Missing item detection at startup
 * - Current directive display (e.g., "Leveling Alchemist to 70 through Diadem")
 * - Direct Lisbeth integration without going through OrderBot
 *
 * TODO LIST:
 * - [ ] Parse Start.xml and all sub-profiles
 * - [ ] Implement condition evaluator for IsQuestCompleted, HasItem, etc.
 * - [ ] Implement all behavior handlers (Lisbeth, GetTo, ChangeClass, etc.)
 * - [ ] Handle Lisbeth errors and implement retry logic for "Max Sessions"
 * - [ ] Add progress tracking and resumption after stop
 * - [ ] Implement autocraft and autosell functionality
 * - [ ] Add Diadem leveling support
 * - [ ] Add class quest handling
 * - [ ] Add gear upgrade handling at breakpoints (21, 41, 53, 63, 70, 80, 90)
 *
 * KNOWN ISSUES:
 * - CodeChunks that try to restart Lisbeth on "Max Sessions" don't work in profiles
 * - Need to implement our own Lisbeth restart logic
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;

namespace TheWrangler
{
    /// <summary>
    /// Controls the DoH/DoL leveling process.
    /// </summary>
    public class LevelingController
    {
        #region Constants

        /// <summary>
        /// Path to the DoH-DoL profiles relative to the assembly location.
        /// </summary>
        private static readonly string ProfilesBasePath = Path.Combine(
            Path.GetDirectoryName(typeof(LevelingController).Assembly.Location),
            "..", "..", "Profiles", "DoH-DoL-Profiles", "DoH-DoL Leveling"
        );

        /// <summary>
        /// Target level for all classes.
        /// </summary>
        private const int TargetLevel = 100;

        #endregion

        #region Fields

        private bool _isRunning;
        private CancellationTokenSource _cts;
        private string _currentDirective = "Not Started";
        private string _currentDetail = "";
        private ProfileExecutor _executor;
        private readonly LisbethApi _lisbethApi;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the leveling process is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the Lisbeth API instance for executing orders.
        /// </summary>
        public LisbethApi LisbethApi => _lisbethApi;

        /// <summary>
        /// Gets the current directive being executed.
        /// </summary>
        public string CurrentDirective => _currentDirective;

        /// <summary>
        /// Gets additional detail about the current directive.
        /// </summary>
        public string CurrentDetail => _currentDetail;

        #endregion

        #region Events

        /// <summary>
        /// Raised when the current directive changes.
        /// </summary>
        public event EventHandler<DirectiveChangedEventArgs> DirectiveChanged;

        /// <summary>
        /// Raised when a log message should be displayed.
        /// </summary>
        public event EventHandler<string> LogMessage;

        /// <summary>
        /// Raised when leveling completes (success or failure).
        /// </summary>
        public event EventHandler<bool> LevelingCompleted;

        /// <summary>
        /// Raised when class levels are updated.
        /// </summary>
        public event EventHandler<ClassLevelsEventArgs> ClassLevelsUpdated;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new LevelingController.
        /// </summary>
        public LevelingController()
        {
            _lisbethApi = new LisbethApi();
            _executor = new ProfileExecutor(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the leveling process.
        /// </summary>
        public void StartLeveling()
        {
            if (_isRunning)
            {
                Log("Leveling is already running.");
                return;
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();

            // Refresh class levels immediately
            RefreshClassLevels();

            // Set initial directive
            SetDirective("Initializing", "Loading profiles and checking requirements...");

            // Start the leveling task
            Task.Run(() => RunLevelingLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Stops the leveling process.
        /// </summary>
        public void StopLeveling()
        {
            if (!_isRunning)
            {
                Log("Leveling is not running.");
                return;
            }

            Log("Stop requested, waiting for current action to complete...");
            _cts?.Cancel();
        }

        /// <summary>
        /// Checks for required items that must be manually obtained.
        /// </summary>
        /// <returns>Result containing missing items information.</returns>
        public MissingItemsResult CheckRequiredItems()
        {
            var result = new MissingItemsResult();

            try
            {
                // Parse GrindMats.txt for required items
                var grindMatsPath = Path.Combine(ProfilesBasePath, "GrindMats.txt");
                if (File.Exists(grindMatsPath))
                {
                    var requiredItems = ParseGrindMats(grindMatsPath);
                    foreach (var item in requiredItems)
                    {
                        // Check if player has enough of this item
                        // TODO: Implement actual inventory check via ff14bot
                        // For now, just report all items as potentially needed
                        result.Items.Add(new MissingItemInfo
                        {
                            Name = item.Key,
                            ItemId = 0, // TODO: Look up item ID from name
                            Needed = item.Value,
                            Have = 0, // TODO: Check inventory
                            IsRequired = true
                        });
                    }
                }

                // Also check MateriaBuyList.json for materia requirements
                var materiaBuyPath = Path.Combine(ProfilesBasePath, "MateriaBuyList.json");
                if (File.Exists(materiaBuyPath))
                {
                    // TODO: Parse and check materia requirements
                    Log("MateriaBuyList.json found, materia checking not yet implemented.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking required items: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Refreshes and broadcasts current class levels.
        /// </summary>
        public void RefreshClassLevels()
        {
            try
            {
                var levels = GetCurrentClassLevels();
                var crafterDisplay = FormatCrafterLevels(levels);
                var gathererDisplay = FormatGathererLevels(levels);

                ClassLevelsUpdated?.Invoke(this, new ClassLevelsEventArgs
                {
                    Levels = levels,
                    CrafterLevelsDisplay = crafterDisplay,
                    GathererLevelsDisplay = gathererDisplay
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing class levels: {ex.Message}");
            }
        }

        #endregion

        #region Internal Methods (for ProfileExecutor)

        /// <summary>
        /// Sets the current directive and raises the event.
        /// </summary>
        internal void SetDirective(string directive, string detail)
        {
            _currentDirective = directive;
            _currentDetail = detail;
            DirectiveChanged?.Invoke(this, new DirectiveChangedEventArgs
            {
                Directive = directive,
                Detail = detail
            });
        }

        /// <summary>
        /// Logs a message and raises the event.
        /// </summary>
        internal void Log(string message)
        {
            Logging.Write($"[Wrangler/Leveling] {message}");
            LogMessage?.Invoke(this, message);
        }

        /// <summary>
        /// Gets the cancellation token for the current run.
        /// </summary>
        internal CancellationToken GetCancellationToken()
        {
            return _cts?.Token ?? CancellationToken.None;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Main leveling loop that runs asynchronously.
        /// </summary>
        private async Task RunLevelingLoopAsync(CancellationToken token)
        {
            bool success = false;

            try
            {
                Log("Starting DoH/DoL leveling process...");

                // Load and parse Start.xml
                var startXmlPath = Path.Combine(ProfilesBasePath, "Start.xml");
                if (!File.Exists(startXmlPath))
                {
                    Log($"ERROR: Start.xml not found at: {startXmlPath}");
                    return;
                }

                Log($"Loading profile from: {startXmlPath}");
                SetDirective("Loading Profile", "Parsing Start.xml...");

                // Parse the profile
                var profile = await _executor.LoadProfileAsync(startXmlPath, token);
                if (profile == null)
                {
                    Log("ERROR: Failed to parse Start.xml");
                    return;
                }

                Log($"Profile loaded with {profile.Elements.Count} top-level elements");

                // Execute the profile
                SetDirective("Running", "Executing leveling profile...");
                success = await _executor.ExecuteProfileAsync(profile, token);

                if (token.IsCancellationRequested)
                {
                    Log("Leveling was cancelled.");
                    success = false;
                }
                else if (success)
                {
                    Log("All classes leveled to 100!");
                }
            }
            catch (OperationCanceledException)
            {
                Log("Leveling cancelled.");
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                Log(ex.StackTrace);
            }
            finally
            {
                _isRunning = false;
                LevelingCompleted?.Invoke(this, success);
            }
        }

        /// <summary>
        /// Gets current class levels for all DoH/DoL classes.
        /// </summary>
        private Dictionary<ClassJobType, int> GetCurrentClassLevels()
        {
            var levels = new Dictionary<ClassJobType, int>();

            try
            {
                // Check if we're in-game first
                if (Core.Me == null)
                {
                    // Return placeholder levels when not in-game
                    return GetPlaceholderLevels();
                }

                var classLevels = Core.Me.Levels;

                // Crafters (DoH)
                levels[ClassJobType.Carpenter] = classLevels[ClassJobType.Carpenter];
                levels[ClassJobType.Blacksmith] = classLevels[ClassJobType.Blacksmith];
                levels[ClassJobType.Armorer] = classLevels[ClassJobType.Armorer];
                levels[ClassJobType.Goldsmith] = classLevels[ClassJobType.Goldsmith];
                levels[ClassJobType.Leatherworker] = classLevels[ClassJobType.Leatherworker];
                levels[ClassJobType.Weaver] = classLevels[ClassJobType.Weaver];
                levels[ClassJobType.Alchemist] = classLevels[ClassJobType.Alchemist];
                levels[ClassJobType.Culinarian] = classLevels[ClassJobType.Culinarian];

                // Gatherers (DoL)
                levels[ClassJobType.Miner] = classLevels[ClassJobType.Miner];
                levels[ClassJobType.Botanist] = classLevels[ClassJobType.Botanist];
                levels[ClassJobType.Fisher] = classLevels[ClassJobType.Fisher];
            }
            catch
            {
                return GetPlaceholderLevels();
            }

            return levels;
        }

        /// <summary>
        /// Returns placeholder levels when not in-game.
        /// </summary>
        private Dictionary<ClassJobType, int> GetPlaceholderLevels()
        {
            return new Dictionary<ClassJobType, int>
            {
                { ClassJobType.Carpenter, 0 },
                { ClassJobType.Blacksmith, 0 },
                { ClassJobType.Armorer, 0 },
                { ClassJobType.Goldsmith, 0 },
                { ClassJobType.Leatherworker, 0 },
                { ClassJobType.Weaver, 0 },
                { ClassJobType.Alchemist, 0 },
                { ClassJobType.Culinarian, 0 },
                { ClassJobType.Miner, 0 },
                { ClassJobType.Botanist, 0 },
                { ClassJobType.Fisher, 0 }
            };
        }

        /// <summary>
        /// Formats crafter levels for display.
        /// </summary>
        private string FormatCrafterLevels(Dictionary<ClassJobType, int> levels)
        {
            return string.Format(
                "CRP:{0,3} BSM:{1,3} ARM:{2,3} GSM:{3,3} LTW:{4,3} WVR:{5,3} ALC:{6,3} CUL:{7,3}",
                levels.GetValueOrDefault(ClassJobType.Carpenter, 0),
                levels.GetValueOrDefault(ClassJobType.Blacksmith, 0),
                levels.GetValueOrDefault(ClassJobType.Armorer, 0),
                levels.GetValueOrDefault(ClassJobType.Goldsmith, 0),
                levels.GetValueOrDefault(ClassJobType.Leatherworker, 0),
                levels.GetValueOrDefault(ClassJobType.Weaver, 0),
                levels.GetValueOrDefault(ClassJobType.Alchemist, 0),
                levels.GetValueOrDefault(ClassJobType.Culinarian, 0)
            );
        }

        /// <summary>
        /// Formats gatherer levels for display.
        /// </summary>
        private string FormatGathererLevels(Dictionary<ClassJobType, int> levels)
        {
            return string.Format(
                "MIN:{0,3} BTN:{1,3} FSH:{2,3}",
                levels.GetValueOrDefault(ClassJobType.Miner, 0),
                levels.GetValueOrDefault(ClassJobType.Botanist, 0),
                levels.GetValueOrDefault(ClassJobType.Fisher, 0)
            );
        }

        /// <summary>
        /// Parses GrindMats.txt to extract required item quantities.
        /// </summary>
        private Dictionary<string, int> ParseGrindMats(string filePath)
        {
            var items = new Dictionary<string, int>();

            try
            {
                var lines = File.ReadAllLines(filePath);
                bool inSummarySection = false;

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();

                    // Skip empty lines and separators
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("---"))
                    {
                        continue;
                    }

                    // Look for the summary section (item names followed by quantities)
                    // Format: "Item Name: NNNx"
                    if (trimmed.Contains(":") && trimmed.EndsWith("x"))
                    {
                        var colonIndex = trimmed.LastIndexOf(':');
                        if (colonIndex > 0)
                        {
                            var itemName = trimmed.Substring(0, colonIndex).Trim();
                            var quantityStr = trimmed.Substring(colonIndex + 1).Trim().TrimEnd('x');

                            if (int.TryParse(quantityStr, out int quantity))
                            {
                                // Only include items from the summary (simple format, no level info)
                                if (!itemName.Contains("Lv") && !itemName.Contains("GEAR"))
                                {
                                    items[itemName] = quantity;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error parsing GrindMats.txt: {ex.Message}");
            }

            return items;
        }

        #endregion
    }

    #region Event Args Classes

    /// <summary>
    /// Event args for directive changes.
    /// </summary>
    public class DirectiveChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Main directive text (e.g., "Leveling Alchemist to 70 through Diadem").
        /// </summary>
        public string Directive { get; set; }

        /// <summary>
        /// Additional detail (e.g., "Crafting Bronze Ingots x20").
        /// </summary>
        public string Detail { get; set; }
    }

    /// <summary>
    /// Event args for class level updates.
    /// </summary>
    public class ClassLevelsEventArgs : EventArgs
    {
        /// <summary>
        /// Dictionary of class to level.
        /// </summary>
        public Dictionary<ClassJobType, int> Levels { get; set; }

        /// <summary>
        /// Formatted string for crafter levels display.
        /// </summary>
        public string CrafterLevelsDisplay { get; set; }

        /// <summary>
        /// Formatted string for gatherer levels display.
        /// </summary>
        public string GathererLevelsDisplay { get; set; }
    }

    #endregion

    #region Missing Items Types

    /// <summary>
    /// Result of checking for missing items.
    /// </summary>
    public class MissingItemsResult
    {
        /// <summary>
        /// List of missing item information.
        /// </summary>
        public List<MissingItemInfo> Items { get; set; } = new List<MissingItemInfo>();

        /// <summary>
        /// Gets whether any items are missing.
        /// </summary>
        public bool HasMissingItems => Items.Any(i => i.Needed > i.Have);
    }

    /// <summary>
    /// Information about a missing item.
    /// </summary>
    public class MissingItemInfo
    {
        /// <summary>
        /// Item name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Item ID (0 if unknown).
        /// </summary>
        public uint ItemId { get; set; }

        /// <summary>
        /// Quantity needed.
        /// </summary>
        public int Needed { get; set; }

        /// <summary>
        /// Quantity player has.
        /// </summary>
        public int Have { get; set; }

        /// <summary>
        /// Whether this item is required (vs optional).
        /// </summary>
        public bool IsRequired { get; set; }
    }

    #endregion
}
