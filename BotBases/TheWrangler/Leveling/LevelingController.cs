/*
 * LevelingController.cs - Leveling Mode Controller
 * =================================================
 *
 * PURPOSE:
 * Orchestrates the DoH/DoL leveling process using pure C# code.
 * All leveling logic is defined in LevelingSequence.cs and LevelingData.cs.
 * No XML profile parsing is needed.
 *
 * ARCHITECTURE:
 * - LevelingSequence handles the main leveling loop and execution
 * - LevelingData contains all grind items, class quests, and level breakpoints
 * - LevelingController coordinates between the UI and LevelingSequence
 * - Uses LlamaLibrary for navigation and NPC interaction
 * - Uses Lisbeth API for crafting and gathering orders
 *
 * KEY FEATURES:
 * - Class level tracking for all crafters/gatherers
 * - Current directive display (e.g., "Leveling Alchemist to 21")
 * - Direct Lisbeth integration without going through OrderBot
 * - Automatic class unlocking via guild NPCs
 * - Class quest completion for XP gains
 *
 * TODO LIST:
 * - [ ] Implement Ishgard Diadem leveling (21-100)
 * - [ ] Handle Lisbeth errors and implement retry logic for "Max Sessions"
 * - [ ] Add gear upgrade handling at breakpoints (21, 41, 53, 63, 70, 80, 90)
 * - [ ] Add progress tracking and resumption after stop
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using TreeSharp;
using TheWrangler.Leveling;

namespace TheWrangler
{
    /// <summary>
    /// Controls the DoH/DoL leveling process.
    /// </summary>
    public class LevelingController
    {
        #region Constants

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

        /// <summary>
        /// Returns true if leveling has been started and is pending execution by the behavior tree.
        /// </summary>
        public bool IsPendingStart { get; private set; }

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
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the leveling process.
        /// Sets IsPendingStart=true so the behavior tree can pick it up and run it in coroutine context.
        /// </summary>
        public void StartLeveling()
        {
            if (_isRunning || IsPendingStart)
            {
                Log("Leveling is already running or pending.");
                return;
            }

            _cts = new CancellationTokenSource();

            // Refresh class levels immediately
            RefreshClassLevels();

            // Set initial directive
            SetDirective("Initializing", "Waiting for behavior tree to start leveling...");

            // Set pending flag - the behavior tree will pick this up and run leveling in coroutine context
            IsPendingStart = true;
            Log("Leveling queued. Waiting for behavior tree to start execution...");
        }

        /// <summary>
        /// Called by the behavior tree to execute the leveling loop.
        /// MUST be called from within ActionRunCoroutine context for Navigation.GetTo to work.
        /// </summary>
        public async Task ExecuteLevelingAsync()
        {
            if (!IsPendingStart)
            {
                return;
            }

            IsPendingStart = false;
            _isRunning = true;

            Log("Behavior tree starting leveling execution...");
            await RunLevelingLoopAsync(_cts?.Token ?? CancellationToken.None);
        }

        /// <summary>
        /// Stops the leveling process.
        /// </summary>
        public void StopLeveling()
        {
            // Handle case where leveling is pending but not yet started
            if (IsPendingStart)
            {
                Log("Cancelling pending leveling start.");
                IsPendingStart = false;
                _cts?.Cancel();
                return;
            }

            if (!_isRunning)
            {
                Log("Leveling is not running.");
                return;
            }

            Log("Stop requested, waiting for current action to complete...");
            _cts?.Cancel();
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

        /// <summary>
        /// Checks for required items that must be manually obtained.
        /// Note: This is a stub - item checking will be implemented based on LevelingData.
        /// </summary>
        /// <returns>Result containing missing items information.</returns>
        public MissingItemsResult CheckRequiredItems()
        {
            // TODO: Implement item checking based on LevelingData grind items
            // For now, return empty result
            return new MissingItemsResult();
        }

        #endregion

        #region Internal Methods (for LevelingSequence)

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
        /// Uses the pure C# LevelingSequence instead of XML profiles.
        /// </summary>
        private async Task RunLevelingLoopAsync(CancellationToken token)
        {
            bool success = false;

            try
            {
                Log("Starting DoH/DoL leveling process...");

                // Create and run the leveling sequence (pure C# - no XML)
                var sequence = new LevelingSequence(this);
                success = await sequence.RunAsync(token);

                if (token.IsCancellationRequested)
                {
                    Log("Leveling was cancelled.");
                    success = false;
                }
                else if (success)
                {
                    Log("All classes leveled successfully!");
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

                // Stop the bot tree when leveling ends (success or failure)
                if (TreeRoot.IsRunning)
                {
                    Log("Stopping bot...");
                    TreeRoot.Stop("Leveling completed or stopped");
                }

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
