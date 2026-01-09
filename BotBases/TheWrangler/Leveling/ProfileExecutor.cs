/*
 * ProfileExecutor.cs - Profile Parsing and Execution
 * ===================================================
 *
 * PURPOSE:
 * Parses XML profiles (like Start.xml) and executes them step by step.
 * This is a custom interpreter that bypasses RebornBuddy's OrderBot system
 * to provide more control over execution.
 *
 * SUPPORTED TAGS:
 * - If/While: Conditional execution with expression evaluation
 * - Lisbeth: Execute Lisbeth orders with JSON
 * - GetTo: Navigate to a location
 * - TeleportTo: Teleport to an aetheryte
 * - ChangeClass: Switch to a different class
 * - WaitTimer: Wait for a specified time
 * - LLoadProfile: Load and execute a sub-profile
 * - LLTalkTo/LLSmallTalk: NPC interactions
 * - LLPickupQuest/LLTurnIn: Quest handling
 * - LogMessage: Log a message
 * - RunCode: Execute code chunks (limited support)
 *
 * TODO:
 * - [ ] Complete expression evaluator for all condition types
 * - [ ] Implement all behavior handlers
 * - [ ] Handle CodeChunks and RunCode tags
 * - [ ] Add error recovery and retry logic
 * - [ ] Support for entity variables (like &crp;)
 *
 * KNOWN LIMITATIONS:
 * - CodeChunks that use C# code cannot be directly executed
 * - Entity declarations from DOCTYPE are pre-processed
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using TreeSharp;
using TheWrangler.Leveling;

namespace TheWrangler
{
    /// <summary>
    /// Parses and executes XML profiles.
    /// </summary>
    public class ProfileExecutor
    {
        #region Fields

        private readonly LevelingController _controller;
        private readonly Dictionary<string, string> _entities = new Dictionary<string, string>();
        private readonly Stack<string> _profileStack = new Stack<string>();

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new ProfileExecutor.
        /// </summary>
        public ProfileExecutor(LevelingController controller)
        {
            _controller = controller;
        }

        #endregion

        #region Profile Loading

        /// <summary>
        /// Loads and parses a profile XML file.
        /// </summary>
        public async Task<ParsedProfile> LoadProfileAsync(string path, CancellationToken token)
        {
            try
            {
                if (!File.Exists(path))
                {
                    _controller.Log($"Profile not found: {path}");
                    return null;
                }

                // Push profile onto stack to track nested profiles
                _profileStack.Push(path);

                // Read the XML content
                var content = await Task.Run(() => File.ReadAllText(path), token);

                // Pre-process entity declarations
                content = PreprocessEntities(content);

                // Parse the XML
                var profile = ParseProfileXml(content, path);

                return profile;
            }
            catch (Exception ex)
            {
                _controller.Log($"Error loading profile: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Pre-processes DOCTYPE ENTITY declarations and expands them in the content.
        /// </summary>
        private string PreprocessEntities(string content)
        {
            // Extract entity declarations from DOCTYPE
            // Format: <!ENTITY name "value">
            var entityPattern = @"<!ENTITY\s+(\w+)\s+""([^""]*)""\s*>";
            var matches = Regex.Matches(content, entityPattern);

            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                _entities[name] = value;
            }

            // Remove the DOCTYPE declaration to allow XDocument parsing
            content = Regex.Replace(content, @"<!DOCTYPE[^>]*>", "", RegexOptions.Singleline);

            // Expand entity references
            foreach (var entity in _entities)
            {
                content = content.Replace($"&{entity.Key};", entity.Value);
            }

            return content;
        }

        /// <summary>
        /// Parses the profile XML content.
        /// </summary>
        private ParsedProfile ParseProfileXml(string content, string path)
        {
            var profile = new ParsedProfile
            {
                Path = path,
                Directory = Path.GetDirectoryName(path)
            };

            try
            {
                var doc = XDocument.Parse(content);
                var root = doc.Root;

                // Get profile name
                var nameElement = root.Element("Name");
                profile.Name = nameElement?.Value ?? Path.GetFileNameWithoutExtension(path);

                // Find the Order element (contains the behavior sequence)
                var orderElement = root.Element("Order");
                if (orderElement != null)
                {
                    profile.Elements = ParseElements(orderElement.Elements().ToList());
                }
                else
                {
                    // If no Order element, parse root children directly
                    profile.Elements = ParseElements(root.Elements().ToList());
                }

                // Parse CodeChunks if present
                var codeChunksElement = root.Element("CodeChunks");
                if (codeChunksElement != null)
                {
                    profile.CodeChunks = ParseCodeChunks(codeChunksElement);
                }

                _controller.Log($"Parsed profile '{profile.Name}' with {profile.Elements.Count} elements");
            }
            catch (Exception ex)
            {
                _controller.Log($"Error parsing profile XML: {ex.Message}");
            }

            return profile;
        }

        /// <summary>
        /// Parses a list of XML elements into ProfileElement objects.
        /// </summary>
        private List<ProfileElement> ParseElements(List<XElement> xmlElements)
        {
            var elements = new List<ProfileElement>();

            foreach (var xml in xmlElements)
            {
                var element = ParseElement(xml);
                if (element != null)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }

        /// <summary>
        /// Parses a single XML element into a ProfileElement.
        /// </summary>
        private ProfileElement ParseElement(XElement xml)
        {
            var element = new ProfileElement
            {
                TagName = xml.Name.LocalName,
                Attributes = xml.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value)
            };

            // Parse children for container elements
            if (xml.HasElements)
            {
                element.Children = ParseElements(xml.Elements().ToList());
            }

            // Store the raw XML for complex elements
            element.RawXml = xml.ToString();

            return element;
        }

        /// <summary>
        /// Parses CodeChunks into a dictionary.
        /// </summary>
        private Dictionary<string, CodeChunk> ParseCodeChunks(XElement codeChunksElement)
        {
            var chunks = new Dictionary<string, CodeChunk>();

            foreach (var chunkElement in codeChunksElement.Elements("CodeChunk"))
            {
                var name = chunkElement.Attribute("Name")?.Value;
                var type = chunkElement.Attribute("Type")?.Value ?? "Code";

                if (!string.IsNullOrEmpty(name))
                {
                    chunks[name] = new CodeChunk
                    {
                        Name = name,
                        Type = type,
                        Code = chunkElement.Value
                    };
                }
            }

            return chunks;
        }

        #endregion

        #region Profile Execution

        /// <summary>
        /// Executes a parsed profile.
        /// </summary>
        public async Task<bool> ExecuteProfileAsync(ParsedProfile profile, CancellationToken token)
        {
            try
            {
                _controller.Log($"Executing profile: {profile.Name}");

                foreach (var element in profile.Elements)
                {
                    if (token.IsCancellationRequested)
                    {
                        return false;
                    }

                    var result = await ExecuteElementAsync(element, profile, token);
                    if (!result)
                    {
                        _controller.Log($"Element execution failed: {element.TagName}");
                        // Continue execution - some failures are expected (conditions not met, etc.)
                    }
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _controller.Log($"Error executing profile: {ex.Message}");
                return false;
            }
            finally
            {
                if (_profileStack.Count > 0)
                {
                    _profileStack.Pop();
                }
            }
        }

        /// <summary>
        /// Executes a single profile element.
        /// </summary>
        private async Task<bool> ExecuteElementAsync(ProfileElement element, ParsedProfile profile, CancellationToken token)
        {
            switch (element.TagName.ToLower())
            {
                case "if":
                    return await ExecuteIfAsync(element, profile, token);

                case "while":
                    return await ExecuteWhileAsync(element, profile, token);

                case "lisbeth":
                    return await ExecuteLisbethAsync(element, token);

                case "getto":
                    return await ExecuteGetToAsync(element, token);

                case "teleportto":
                    return await ExecuteTeleportToAsync(element, token);

                case "changeclass":
                    return await ExecuteChangeClassAsync(element, token);

                case "waittimer":
                    return await ExecuteWaitTimerAsync(element, token);

                case "lloadprofile":
                case "aloadprofile":
                    return await ExecuteLoadProfileAsync(element, profile, token);

                case "lltalkto":
                case "talkto":
                    return await ExecuteTalkToAsync(element, token);

                case "llsmalltalk":
                    return await ExecuteSmallTalkAsync(element, token);

                case "llpickupquest":
                case "pickupquest":
                    return await ExecutePickupQuestAsync(element, token);

                case "llturnin":
                case "turnin":
                    return await ExecuteTurnInAsync(element, token);

                case "logmessage":
                    return ExecuteLogMessage(element);

                case "runcode":
                    return await ExecuteRunCodeAsync(element, profile, token);

                case "autoinventoryequip":
                    return await ExecuteAutoEquipAsync(element, token);

                case "name":
                    // Skip name element
                    return true;

                default:
                    _controller.Log($"Unhandled tag: {element.TagName}");
                    return true; // Continue execution
            }
        }

        #endregion

        #region Condition Evaluation

        /// <summary>
        /// Evaluates a condition expression.
        /// </summary>
        private bool EvaluateCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition))
            {
                return true;
            }

            try
            {
                // Handle NOT operator
                if (condition.TrimStart().StartsWith("not ", StringComparison.OrdinalIgnoreCase))
                {
                    var innerCondition = condition.Substring(condition.IndexOf("not ", StringComparison.OrdinalIgnoreCase) + 4).Trim();
                    return !EvaluateCondition(innerCondition);
                }

                // Handle AND operator
                if (condition.Contains(" and ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = Regex.Split(condition, @"\s+and\s+", RegexOptions.IgnoreCase);
                    return parts.All(p => EvaluateCondition(p.Trim()));
                }

                // Handle OR operator
                if (condition.Contains(" or ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = Regex.Split(condition, @"\s+or\s+", RegexOptions.IgnoreCase);
                    return parts.Any(p => EvaluateCondition(p.Trim()));
                }

                // Handle parentheses
                if (condition.StartsWith("(") && condition.EndsWith(")"))
                {
                    return EvaluateCondition(condition.Substring(1, condition.Length - 2));
                }

                // Handle specific condition functions
                return EvaluateConditionFunction(condition);
            }
            catch (Exception ex)
            {
                _controller.Log($"Error evaluating condition '{condition}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates a condition function like IsQuestCompleted(12345).
        /// </summary>
        private bool EvaluateConditionFunction(string condition)
        {
            // IsQuestCompleted(questId)
            var questCompletedMatch = Regex.Match(condition, @"IsQuestCompleted\((\d+)\)");
            if (questCompletedMatch.Success)
            {
                var questId = uint.Parse(questCompletedMatch.Groups[1].Value);
                return IsQuestCompleted(questId);
            }

            // HasQuest(questId)
            var hasQuestMatch = Regex.Match(condition, @"HasQuest\((\d+)\)");
            if (hasQuestMatch.Success)
            {
                var questId = uint.Parse(hasQuestMatch.Groups[1].Value);
                return HasQuest(questId);
            }

            // HasItem(itemId)
            var hasItemMatch = Regex.Match(condition, @"HasItem\((\d+)\)");
            if (hasItemMatch.Success)
            {
                var itemId = uint.Parse(hasItemMatch.Groups[1].Value);
                return HasItem(itemId);
            }

            // HqHasAtLeast(itemId, count)
            var hqHasMatch = Regex.Match(condition, @"HqHasAtLeast\((\d+),\s*(\d+)\)");
            if (hqHasMatch.Success)
            {
                var itemId = uint.Parse(hqHasMatch.Groups[1].Value);
                var count = int.Parse(hqHasMatch.Groups[2].Value);
                return HqHasAtLeast(itemId, count);
            }

            // NqHasAtLeast(itemId, count)
            var nqHasMatch = Regex.Match(condition, @"NqHasAtLeast\((\d+),\s*(\d+)\)");
            if (nqHasMatch.Success)
            {
                var itemId = uint.Parse(nqHasMatch.Groups[1].Value);
                var count = int.Parse(nqHasMatch.Groups[2].Value);
                return NqHasAtLeast(itemId, count);
            }

            // IsQuestAcceptQualified(questId)
            var qualifiedMatch = Regex.Match(condition, @"IsQuestAcceptQualified\((\d+)\)");
            if (qualifiedMatch.Success)
            {
                var questId = uint.Parse(qualifiedMatch.Groups[1].Value);
                return IsQuestAcceptQualified(questId);
            }

            // GetQuestStep(questId) == step
            var questStepMatch = Regex.Match(condition, @"GetQuestStep\((\d+)\)\s*==\s*(\d+)");
            if (questStepMatch.Success)
            {
                var questId = uint.Parse(questStepMatch.Groups[1].Value);
                var step = int.Parse(questStepMatch.Groups[2].Value);
                return GetQuestStep(questId) == step;
            }

            // Core.Me.Levels[ClassJobType.X] comparison
            var levelMatch = Regex.Match(condition, @"Core\.Me\.Levels\[ClassJobType\.(\w+)\]\s*([<>=!]+)\s*(\d+)");
            if (levelMatch.Success)
            {
                var className = levelMatch.Groups[1].Value;
                var op = levelMatch.Groups[2].Value;
                var level = int.Parse(levelMatch.Groups[3].Value);
                return EvaluateLevelComparison(className, op, level);
            }

            // Core.Player.ClassLevel comparison
            var classLevelMatch = Regex.Match(condition, @"Core\.Player\.ClassLevel\s*([<>=!]+)\s*(\d+)");
            if (classLevelMatch.Success)
            {
                var op = classLevelMatch.Groups[1].Value;
                var level = int.Parse(classLevelMatch.Groups[2].Value);
                return EvaluateCurrentLevelComparison(op, level);
            }

            // ClassName == ClassJobType.X
            var classNameMatch = Regex.Match(condition, @"ClassName\s*([!=]=)\s*ClassJobType\.(\w+)");
            if (classNameMatch.Success)
            {
                var op = classNameMatch.Groups[1].Value;
                var className = classNameMatch.Groups[2].Value;
                return EvaluateClassName(op, className);
            }

            // Default: log unhandled condition
            _controller.Log($"Unhandled condition: {condition}");
            return true; // Default to true to continue execution
        }

        #endregion

        #region Condition Helpers

        private bool IsQuestCompleted(uint questId)
        {
            try
            {
                return QuestLogManager.IsQuestCompleted(questId);
            }
            catch
            {
                return false;
            }
        }

        private bool HasQuest(uint questId)
        {
            try
            {
                return QuestLogManager.HasQuest((int)questId);
            }
            catch
            {
                return false;
            }
        }

        private bool HasItem(uint itemId)
        {
            try
            {
                return InventoryManager.FilledSlots.Any(s => s.RawItemId == itemId);
            }
            catch
            {
                return false;
            }
        }

        private bool HqHasAtLeast(uint itemId, int count)
        {
            try
            {
                var hqCount = InventoryManager.FilledSlots
                    .Where(s => s.RawItemId == itemId && s.IsHighQuality)
                    .Sum(s => (int)s.Count);
                return hqCount >= count;
            }
            catch
            {
                return false;
            }
        }

        private bool NqHasAtLeast(uint itemId, int count)
        {
            try
            {
                var nqCount = InventoryManager.FilledSlots
                    .Where(s => s.RawItemId == itemId && !s.IsHighQuality)
                    .Sum(s => (int)s.Count);
                return nqCount >= count;
            }
            catch
            {
                return false;
            }
        }

        private bool IsQuestAcceptQualified(uint questId)
        {
            try
            {
                // TODO: Implement proper qualification check
                return !IsQuestCompleted(questId) && !HasQuest(questId);
            }
            catch
            {
                return false;
            }
        }

        private int GetQuestStep(uint questId)
        {
            try
            {
                var quest = QuestLogManager.GetQuestById((int)questId);
                return quest?.Step ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool EvaluateLevelComparison(string className, string op, int level)
        {
            try
            {
                if (!Enum.TryParse<ClassJobType>(className, out var classJob))
                {
                    return false;
                }

                var currentLevel = Core.Me.Levels[classJob];
                return CompareValues(currentLevel, op, level);
            }
            catch
            {
                return false;
            }
        }

        private bool EvaluateCurrentLevelComparison(string op, int level)
        {
            try
            {
                var currentLevel = Core.Player.ClassLevel;
                return CompareValues(currentLevel, op, level);
            }
            catch
            {
                return false;
            }
        }

        private bool EvaluateClassName(string op, string className)
        {
            try
            {
                if (!Enum.TryParse<ClassJobType>(className, out var classJob))
                {
                    return false;
                }

                var currentClass = Core.Player.CurrentJob;
                var isEqual = currentClass == classJob;
                return op == "==" ? isEqual : !isEqual;
            }
            catch
            {
                return false;
            }
        }

        private bool CompareValues(int left, string op, int right)
        {
            switch (op)
            {
                case "==": return left == right;
                case "!=": return left != right;
                case "<": return left < right;
                case "<=": return left <= right;
                case ">": return left > right;
                case ">=": return left >= right;
                case "&lt;": return left < right;
                case "&gt;": return left > right;
                default: return false;
            }
        }

        #endregion

        #region Element Executors

        private async Task<bool> ExecuteIfAsync(ProfileElement element, ParsedProfile profile, CancellationToken token)
        {
            var condition = element.Attributes.GetValueOrDefault("Condition", "");
            if (EvaluateCondition(condition))
            {
                foreach (var child in element.Children)
                {
                    if (token.IsCancellationRequested) return false;
                    await ExecuteElementAsync(child, profile, token);
                }
            }
            return true;
        }

        private async Task<bool> ExecuteWhileAsync(ProfileElement element, ParsedProfile profile, CancellationToken token)
        {
            var condition = element.Attributes.GetValueOrDefault("Condition", "");
            var maxIterations = 10000; // Safety limit
            var iterations = 0;

            while (EvaluateCondition(condition) && iterations < maxIterations)
            {
                if (token.IsCancellationRequested) return false;

                foreach (var child in element.Children)
                {
                    if (token.IsCancellationRequested) return false;
                    await ExecuteElementAsync(child, profile, token);
                }

                iterations++;

                // Refresh class levels periodically
                if (iterations % 5 == 0)
                {
                    _controller.RefreshClassLevels();
                }
            }

            if (iterations >= maxIterations)
            {
                _controller.Log($"While loop exceeded maximum iterations: {condition}");
            }

            return true;
        }

        private async Task<bool> ExecuteLisbethAsync(ProfileElement element, CancellationToken token)
        {
            var json = element.Attributes.GetValueOrDefault("Json", "");
            if (string.IsNullOrEmpty(json))
            {
                _controller.Log("Lisbeth tag missing Json attribute");
                return false;
            }

            // Parse the JSON to get item info for the directive display
            UpdateDirectiveFromLisbethJson(json);

            _controller.Log($"Executing Lisbeth order...");

            try
            {
                // Use the existing LisbethApi to execute
                var result = await _controller.LisbethApi.ExecuteOrdersAsync(json, WranglerSettings.Instance.IgnoreHome);

                if (!result)
                {
                    _controller.Log("Lisbeth order failed");
                    // TODO: Implement retry logic for Max Sessions error
                }

                return result;
            }
            catch (Exception ex)
            {
                _controller.Log($"Lisbeth error: {ex.Message}");
                return false;
            }
        }

        private void UpdateDirectiveFromLisbethJson(string json)
        {
            try
            {
                // Simple parsing to extract item info from JSON
                // Format: [{'Item': 5333, 'Amount': 1, 'Type': 'Weaver', ...}]
                var itemMatch = Regex.Match(json, @"'Item':\s*(\d+)");
                var amountMatch = Regex.Match(json, @"'Amount':\s*(\d+)");
                var typeMatch = Regex.Match(json, @"'Type':\s*'(\w+)'");

                if (typeMatch.Success)
                {
                    var craftType = typeMatch.Groups[1].Value;
                    var amount = amountMatch.Success ? amountMatch.Groups[1].Value : "?";
                    var itemId = itemMatch.Success ? itemMatch.Groups[1].Value : "?";

                    _controller.SetDirective(
                        $"Crafting with {craftType}",
                        $"Item ID: {itemId}, Amount: {amount}"
                    );
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        private async Task<bool> ExecuteGetToAsync(ProfileElement element, CancellationToken token)
        {
            var zoneIdStr = element.Attributes.GetValueOrDefault("ZoneId", "0");
            var xyzStr = element.Attributes.GetValueOrDefault("XYZ", "");

            if (!uint.TryParse(zoneIdStr, out var zoneId) || zoneId == 0)
            {
                _controller.Log($"GetTo: Invalid ZoneId '{zoneIdStr}'");
                return false;
            }

            // Parse XYZ coordinates
            if (!TryParseVector3(xyzStr, out var targetLocation))
            {
                _controller.Log($"GetTo: Invalid XYZ '{xyzStr}'");
                return false;
            }

            _controller.SetDirective("Navigating", $"Zone: {zoneId}");
            _controller.Log($"GetTo: Zone {zoneId}, XYZ {targetLocation}");

            return await NavigateToLocationAsync(zoneId, targetLocation, token);
        }

        /// <summary>
        /// Navigate to a location, handling zone transitions if needed.
        /// </summary>
        public async Task<bool> NavigateToLocationAsync(uint zoneId, Vector3 targetLocation, CancellationToken token)
        {
            try
            {
                // If in different zone, need to handle zone transition
                if (WorldManager.ZoneId != zoneId)
                {
                    // First try to teleport to the zone
                    var aetherytes = WorldManager.AetheryteIdsForZone(zoneId);
                    if (aetherytes.Length > 0)
                    {
                        // Find closest aetheryte to target
                        var closest = aetherytes.OrderBy(a => a.Item2.DistanceSqr(targetLocation)).First();

                        _controller.Log($"GetTo: Teleporting to zone {zoneId} first");
                        WorldManager.TeleportById(closest.Item1);

                        // Wait for teleport
                        await WaitForConditionAsync(() => Core.Me.IsCasting, 5000, token);
                        await WaitForConditionAsync(() => !Core.Me.IsCasting, 15000, token);
                        await WaitForConditionAsync(() => CommonBehaviors.IsLoading, 5000, token);
                        await WaitForConditionAsync(() => !CommonBehaviors.IsLoading, 60000, token);
                        await Task.Delay(1000, token);
                    }
                    else
                    {
                        // Try to get a path that includes zone transitions
                        var path = await NavGraph.GetPathAsync(zoneId, targetLocation);
                        if (path == null)
                        {
                            _controller.Log($"GetTo: Cannot find path to zone {zoneId}");
                            return false;
                        }

                        // Execute the path
                        return await ExecuteNavGraphPathAsync(path, token);
                    }
                }

                // Now navigate to the specific location
                if (WorldManager.ZoneId == zoneId)
                {
                    // Check if we're already close
                    if (Core.Me.Location.Distance(targetLocation) < 5f)
                    {
                        _controller.Log("GetTo: Already at destination");
                        return true;
                    }

                    // Get path within the zone
                    var path = await NavGraph.GetPathAsync(zoneId, targetLocation);
                    if (path != null && path.Count > 0)
                    {
                        return await ExecuteNavGraphPathAsync(path, token);
                    }
                    else
                    {
                        // Try direct movement
                        return await MoveToLocationAsync(targetLocation, token);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _controller.Log($"GetTo error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute a NavGraph path.
        /// </summary>
        private async Task<bool> ExecuteNavGraphPathAsync(Queue<NavGraph.INode> path, CancellationToken token)
        {
            var composite = NavGraph.NavGraphConsumer(r => path);
            var context = new object();

            while (path.Count > 0)
            {
                if (token.IsCancellationRequested) return false;

                composite.Start(context);
                await Task.Yield();

                while (composite.Tick(context) == RunStatus.Running)
                {
                    if (token.IsCancellationRequested)
                    {
                        composite.Stop(context);
                        Navigator.Stop();
                        return false;
                    }
                    await Task.Yield();
                }

                composite.Stop(context);
                await Task.Yield();
            }

            Navigator.Stop();
            return true;
        }

        /// <summary>
        /// Move directly to a location within current zone.
        /// </summary>
        private async Task<bool> MoveToLocationAsync(Vector3 targetLocation, CancellationToken token, float tolerance = 3f)
        {
            var timeout = 60000; // 60 seconds
            var elapsed = 0;

            while (Core.Me.Location.Distance(targetLocation) > tolerance && elapsed < timeout)
            {
                if (token.IsCancellationRequested) return false;

                Navigator.PlayerMover.MoveTowards(targetLocation);
                await Task.Delay(100, token);
                elapsed += 100;
            }

            Navigator.PlayerMover.MoveStop();
            Navigator.Stop();

            return Core.Me.Location.Distance(targetLocation) <= tolerance;
        }

        /// <summary>
        /// Parse a Vector3 string like "1.23, 4.56, 7.89"
        /// </summary>
        private bool TryParseVector3(string input, out Vector3 result)
        {
            result = Vector3.Zero;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Handle formats like "1.23, 4.56, 7.89" or "<1.23, 4.56, 7.89>"
            input = input.Trim().Trim('<', '>', '(', ')');
            var parts = input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
                return false;

            if (float.TryParse(parts[0].Trim(), out var x) &&
                float.TryParse(parts[1].Trim(), out var y) &&
                float.TryParse(parts[2].Trim(), out var z))
            {
                result = new Vector3(x, y, z);
                return true;
            }

            return false;
        }

        private async Task<bool> ExecuteTeleportToAsync(ProfileElement element, CancellationToken token)
        {
            var aetheryteIdStr = element.Attributes.GetValueOrDefault("AetheryteId", "0");
            var zoneIdStr = element.Attributes.GetValueOrDefault("ZoneId", "0");
            var name = element.Attributes.GetValueOrDefault("Name", "");
            var force = element.Attributes.GetValueOrDefault("Force", "false").ToLower() == "true";

            if (!uint.TryParse(aetheryteIdStr, out var aetheryteId))
            {
                _controller.Log($"TeleportTo: Invalid AetheryteId '{aetheryteIdStr}'");
                return false;
            }

            // If only ZoneId provided, find the aetheryte for that zone
            if (aetheryteId == 0 && uint.TryParse(zoneIdStr, out var zoneId) && zoneId > 0)
            {
                var aetherytes = WorldManager.AetheryteIdsForZone(zoneId);
                if (aetherytes.Length > 0)
                {
                    aetheryteId = aetherytes[0].Item1;
                }
            }

            if (aetheryteId == 0)
            {
                _controller.Log("TeleportTo: No valid aetheryte specified");
                return false;
            }

            // Get target zone
            var targetZoneId = WorldManager.GetZoneForAetheryteId(aetheryteId);
            if (targetZoneId == 0)
            {
                _controller.Log($"TeleportTo: Could not find zone for AetheryteId {aetheryteId}");
                return false;
            }

            // Check if already in target zone (unless Force)
            if (!force && WorldManager.ZoneId == targetZoneId)
            {
                _controller.Log($"TeleportTo: Already in zone {targetZoneId}");
                return true;
            }

            _controller.SetDirective("Teleporting", $"To: {name}");
            _controller.Log($"TeleportTo: {name} (Aetheryte {aetheryteId}, Zone {targetZoneId})");

            try
            {
                // Check if we can teleport
                if (!WorldManager.CanTeleport())
                {
                    _controller.Log("TeleportTo: Cannot teleport right now, waiting...");
                    var canTeleport = await WaitForConditionAsync(() => WorldManager.CanTeleport(), 10000, token);
                    if (!canTeleport)
                    {
                        _controller.Log("TeleportTo: Still cannot teleport");
                        return false;
                    }
                }

                // Check if we have the aetheryte unlocked
                var locations = WorldManager.AvailableLocations;
                if (!locations.Any(l => l.AetheryteId == aetheryteId))
                {
                    _controller.Log($"TeleportTo: Aetheryte {aetheryteId} is not unlocked");
                    return false;
                }

                // Teleport
                WorldManager.TeleportById(aetheryteId);

                // Wait for casting
                await WaitForConditionAsync(() => Core.Me.IsCasting, 5000, token);

                // Wait for cast to complete
                await WaitForConditionAsync(() => !Core.Me.IsCasting, 15000, token);

                // Wait for loading screen
                await WaitForConditionAsync(() => CommonBehaviors.IsLoading, 5000, token);

                // Wait for loading to finish
                await WaitForConditionAsync(() => !CommonBehaviors.IsLoading, 60000, token);

                // Verify we arrived
                if (WorldManager.ZoneId == targetZoneId)
                {
                    _controller.Log($"TeleportTo: Arrived in zone {targetZoneId}");
                    await Task.Delay(1000, token); // Brief settle time
                    return true;
                }
                else
                {
                    _controller.Log($"TeleportTo: Failed to arrive in zone {targetZoneId}, current zone: {WorldManager.ZoneId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _controller.Log($"TeleportTo error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Waits for a condition to be true with timeout.
        /// </summary>
        private async Task<bool> WaitForConditionAsync(Func<bool> condition, int timeoutMs, CancellationToken token)
        {
            var elapsed = 0;
            while (!condition() && elapsed < timeoutMs)
            {
                if (token.IsCancellationRequested) return false;
                await Task.Delay(100, token);
                elapsed += 100;
            }
            return condition();
        }

        private async Task<bool> ExecuteChangeClassAsync(ProfileElement element, CancellationToken token)
        {
            var job = element.Attributes.GetValueOrDefault("Job", "");
            var force = element.Attributes.GetValueOrDefault("Force", "false").ToLower() == "true";

            return await ChangeClassAsync(job, force, token);
        }

        /// <summary>
        /// Switch to a different class/job using gearsets.
        /// </summary>
        public async Task<bool> ChangeClassAsync(string jobName, bool force, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                _controller.Log("ChangeClass: No job specified");
                return false;
            }

            // Parse the job name to ClassJobType
            if (!Enum.TryParse<ClassJobType>(jobName.Trim(), true, out var targetJob))
            {
                _controller.Log($"ChangeClass: Unknown job '{jobName}'");
                return false;
            }

            // Check if already on the target class
            if (!force && Core.Me.CurrentJob == targetJob)
            {
                _controller.Log($"ChangeClass: Already on {targetJob}");
                return true;
            }

            _controller.SetDirective("Changing Class", $"To: {targetJob}");
            _controller.Log($"ChangeClass: {targetJob}");

            try
            {
                // Find a gearset for the target job
                var gearSets = GearsetManager.GearSets.Where(gs => gs.InUse && gs.Class == targetJob).ToList();

                if (gearSets.Count > 0)
                {
                    // Use the first matching gearset
                    var gearSet = gearSets.First();
                    _controller.Log($"ChangeClass: Activating gearset {gearSet.Index} ({gearSet.Class})");

                    gearSet.Activate();

                    // Wait for dialog to potentially appear
                    await Task.Delay(2000, token);

                    // Handle "item not found, replace?" dialogs - there may be multiple
                    // Keep clicking Yes until no more dialogs appear or we've changed class
                    var dialogTimeout = 10000;
                    var dialogElapsed = 0;
                    while (dialogElapsed < dialogTimeout && Core.Me.CurrentJob != targetJob)
                    {
                        if (token.IsCancellationRequested) return false;

                        if (SelectYesno.IsOpen)
                        {
                            _controller.Log("ChangeClass: Confirming gear replacement");
                            SelectYesno.ClickYes();
                            await Task.Delay(500, token);
                            dialogElapsed += 500;
                            continue;
                        }

                        await Task.Delay(100, token);
                        dialogElapsed += 100;
                    }

                    // Additional wait for class change to complete
                    await WaitForConditionAsync(() => Core.Me.CurrentJob == targetJob, 3000, token);

                    if (Core.Me.CurrentJob == targetJob)
                    {
                        _controller.Log($"ChangeClass: Successfully changed to {targetJob}");
                        _controller.RefreshClassLevels();
                        return true;
                    }
                    else
                    {
                        _controller.Log($"ChangeClass: Failed to change to {targetJob}, still on {Core.Me.CurrentJob}");
                        return false;
                    }
                }
                else
                {
                    // No gearset found - try equipping a weapon for the class
                    _controller.Log($"ChangeClass: No gearset found for {targetJob}, trying to equip weapon");
                    return await EquipWeaponForClassAsync(targetJob, token);
                }
            }
            catch (Exception ex)
            {
                _controller.Log($"ChangeClass error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to equip a weapon for a class when no gearset exists.
        /// </summary>
        private async Task<bool> EquipWeaponForClassAsync(ClassJobType targetJob, CancellationToken token)
        {
            // Map job to weapon category
            var weaponCategory = GetWeaponCategoryForJob(targetJob);
            if (weaponCategory == null)
            {
                _controller.Log($"ChangeClass: Unknown weapon category for {targetJob}");
                return false;
            }

            // Find a weapon for this class in inventory/armory
            var weapon = InventoryManager.FilledInventoryAndArmory
                .Where(i => i.Item.EquipmentCatagory == weaponCategory.Value)
                .OrderByDescending(i => i.Item.ItemLevel)
                .FirstOrDefault();

            if (weapon == null)
            {
                _controller.Log($"ChangeClass: No weapon found for {targetJob}");
                return false;
            }

            // Get main hand slot
            var mainHand = InventoryManager.GetBagByInventoryBagId(InventoryBagId.EquippedItems)[EquipmentSlot.MainHand];

            _controller.Log($"ChangeClass: Equipping {weapon.Name}");
            weapon.Move(mainHand);

            await Task.Delay(1500, token);

            // Save gearset
            ChatManager.SendChat("/gs save");
            await Task.Delay(1000, token);

            _controller.RefreshClassLevels();
            return Core.Me.CurrentJob == targetJob;
        }

        /// <summary>
        /// Get the weapon category for a job.
        /// </summary>
        private ItemUiCategory? GetWeaponCategoryForJob(ClassJobType job)
        {
            return job switch
            {
                ClassJobType.Carpenter => ItemUiCategory.Carpenters_Primary_Tool,
                ClassJobType.Blacksmith => ItemUiCategory.Blacksmiths_Primary_Tool,
                ClassJobType.Armorer => ItemUiCategory.Armorers_Primary_Tool,
                ClassJobType.Goldsmith => ItemUiCategory.Goldsmiths_Primary_Tool,
                ClassJobType.Leatherworker => ItemUiCategory.Leatherworkers_Primary_Tool,
                ClassJobType.Weaver => ItemUiCategory.Weavers_Primary_Tool,
                ClassJobType.Alchemist => ItemUiCategory.Alchemists_Primary_Tool,
                ClassJobType.Culinarian => ItemUiCategory.Culinarians_Primary_Tool,
                ClassJobType.Miner => ItemUiCategory.Miners_Primary_Tool,
                ClassJobType.Botanist => ItemUiCategory.Botanists_Primary_Tool,
                ClassJobType.Fisher => ItemUiCategory.Fishers_Primary_Tool,
                _ => null
            };
        }

        private async Task<bool> ExecuteWaitTimerAsync(ProfileElement element, CancellationToken token)
        {
            var waitTimeStr = element.Attributes.GetValueOrDefault("WaitTime", "1");
            if (int.TryParse(waitTimeStr, out int waitTime))
            {
                _controller.Log($"Waiting {waitTime} seconds...");
                await Task.Delay(waitTime * 1000, token);
            }
            return true;
        }

        private async Task<bool> ExecuteLoadProfileAsync(ProfileElement element, ParsedProfile currentProfile, CancellationToken token)
        {
            var path = element.Attributes.GetValueOrDefault("Path", "");
            if (string.IsNullOrEmpty(path))
            {
                _controller.Log("LLoadProfile missing Path attribute");
                return false;
            }

            // Resolve relative path
            var fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(currentProfile.Directory, path);

            fullPath = Path.GetFullPath(fullPath);

            _controller.Log($"Loading sub-profile: {Path.GetFileName(fullPath)}");
            _controller.SetDirective("Loading Profile", Path.GetFileName(fullPath));

            var subProfile = await LoadProfileAsync(fullPath, token);
            if (subProfile == null)
            {
                _controller.Log($"Failed to load sub-profile: {fullPath}");
                return false;
            }

            return await ExecuteProfileAsync(subProfile, token);
        }

        private async Task<bool> ExecuteTalkToAsync(ProfileElement element, CancellationToken token)
        {
            var npcIdStr = element.Attributes.GetValueOrDefault("NpcId", "0");
            var xyzStr = element.Attributes.GetValueOrDefault("XYZ", "");
            var selectStringSlot = element.Attributes.GetValueOrDefault("SelectStringOverride", "0");

            if (!uint.TryParse(npcIdStr, out var npcId) || npcId == 0)
            {
                _controller.Log($"TalkTo: Invalid NpcId '{npcIdStr}'");
                return false;
            }

            return await TalkToNpcAsync(npcId, xyzStr, selectStringSlot, token);
        }

        /// <summary>
        /// Talk to an NPC.
        /// </summary>
        public async Task<bool> TalkToNpcAsync(uint npcId, string xyzHint, string selectStringSlot, CancellationToken token)
        {
            var npcName = DataManager.GetLocalizedNPCName((int)npcId);
            _controller.Log($"TalkTo: NPC {npcName} ({npcId})");

            try
            {
                // Find the NPC
                var npc = GameObjectManager.GetObjectsByNPCId(npcId)
                    .FirstOrDefault(n => n.IsVisible && n.IsTargetable);

                // If NPC not found and we have a location hint, navigate there
                if (npc == null && !string.IsNullOrEmpty(xyzHint) && TryParseVector3(xyzHint, out var location))
                {
                    _controller.Log($"TalkTo: NPC not visible, navigating to hint location");
                    await MoveToLocationAsync(location, token, 5f);

                    // Try to find NPC again
                    npc = GameObjectManager.GetObjectsByNPCId(npcId)
                        .FirstOrDefault(n => n.IsVisible && n.IsTargetable);
                }

                if (npc == null)
                {
                    _controller.Log($"TalkTo: NPC {npcName} not found");
                    return false;
                }

                // Move to NPC if not in range
                if (!npc.IsWithinInteractRange)
                {
                    _controller.Log($"TalkTo: Moving to {npcName}");
                    await MoveToLocationAsync(npc.Location, token, 4f);
                }

                // Wait for NPC to become interactable
                await WaitForConditionAsync(() => npc.IsWithinInteractRange, 5000, token);

                if (!npc.IsWithinInteractRange)
                {
                    _controller.Log($"TalkTo: Could not reach {npcName}");
                    return false;
                }

                // Interact with NPC
                npc.Interact();

                // Handle dialog
                var talked = false;
                var timeout = 30000;
                var elapsed = 0;

                while (elapsed < timeout)
                {
                    if (token.IsCancellationRequested) return false;

                    // Handle various dialog windows
                    if (Talk.DialogOpen)
                    {
                        Talk.Next();
                        talked = true;
                        await Task.Delay(200, token);
                        elapsed += 200;
                        continue;
                    }

                    if (SelectYesno.IsOpen)
                    {
                        SelectYesno.ClickYes();
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (SelectString.IsOpen)
                    {
                        if (int.TryParse(selectStringSlot, out var slot))
                        {
                            SelectString.ClickSlot((uint)slot);
                        }
                        else
                        {
                            SelectString.ClickSlot(0);
                        }
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (SelectIconString.IsOpen)
                    {
                        SelectIconString.ClickSlot(0);
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    // Check if dialog is done
                    if (talked && !Talk.DialogOpen && !SelectYesno.IsOpen && !SelectString.IsOpen && !SelectIconString.IsOpen)
                    {
                        await Task.Delay(500, token);
                        if (!Talk.DialogOpen && !SelectYesno.IsOpen && !SelectString.IsOpen && !SelectIconString.IsOpen)
                        {
                            break;
                        }
                    }

                    await Task.Delay(100, token);
                    elapsed += 100;
                }

                return talked;
            }
            catch (Exception ex)
            {
                _controller.Log($"TalkTo error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ExecuteSmallTalkAsync(ProfileElement element, CancellationToken token)
        {
            var waitTime = element.Attributes.GetValueOrDefault("WaitTime", "1500");
            if (int.TryParse(waitTime, out int ms))
            {
                await Task.Delay(ms, token);
            }
            return true;
        }

        private async Task<bool> ExecutePickupQuestAsync(ProfileElement element, CancellationToken token)
        {
            var questIdStr = element.Attributes.GetValueOrDefault("QuestId", "0");
            var npcIdStr = element.Attributes.GetValueOrDefault("NpcId", "0");
            var xyzStr = element.Attributes.GetValueOrDefault("XYZ", "");

            if (!uint.TryParse(questIdStr, out var questId) || questId == 0)
            {
                _controller.Log($"PickupQuest: Invalid QuestId '{questIdStr}'");
                return false;
            }

            if (!uint.TryParse(npcIdStr, out var npcId) || npcId == 0)
            {
                _controller.Log($"PickupQuest: Invalid NpcId '{npcIdStr}'");
                return false;
            }

            return await PickupQuestAsync(questId, npcId, xyzStr, token);
        }

        /// <summary>
        /// Pick up a quest from an NPC.
        /// </summary>
        public async Task<bool> PickupQuestAsync(uint questId, uint npcId, string xyzHint, CancellationToken token)
        {
            // Check if we already have the quest
            if (QuestLogManager.HasQuest((int)questId))
            {
                _controller.Log($"PickupQuest: Already have quest {questId}");
                return true;
            }

            // Check if quest is already completed
            if (QuestLogManager.IsQuestCompleted(questId))
            {
                _controller.Log($"PickupQuest: Quest {questId} already completed");
                return true;
            }

            var npcName = DataManager.GetLocalizedNPCName((int)npcId);
            _controller.SetDirective("Picking Up Quest", $"Quest {questId} from {npcName}");
            _controller.Log($"PickupQuest: Quest {questId} from {npcName}");

            try
            {
                // Find the NPC
                var npc = GameObjectManager.GetObjectsByNPCId(npcId)
                    .FirstOrDefault(n => n.IsVisible && n.IsTargetable);

                // Navigate to hint location if NPC not found
                if (npc == null && !string.IsNullOrEmpty(xyzHint) && TryParseVector3(xyzHint, out var location))
                {
                    await MoveToLocationAsync(location, token, 5f);
                    npc = GameObjectManager.GetObjectsByNPCId(npcId)
                        .FirstOrDefault(n => n.IsVisible && n.IsTargetable);
                }

                if (npc == null)
                {
                    _controller.Log($"PickupQuest: NPC {npcName} not found");
                    return false;
                }

                // Move to NPC
                if (!npc.IsWithinInteractRange)
                {
                    await MoveToLocationAsync(npc.Location, token, 4f);
                }

                // Interact
                npc.Interact();

                // Handle quest dialog
                var timeout = 30000;
                var elapsed = 0;

                while (elapsed < timeout && !QuestLogManager.HasQuest((int)questId))
                {
                    if (token.IsCancellationRequested) return false;

                    if (Talk.DialogOpen)
                    {
                        Talk.Next();
                        await Task.Delay(200, token);
                        elapsed += 200;
                        continue;
                    }

                    if (SelectYesno.IsOpen)
                    {
                        SelectYesno.ClickYes();
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (SelectString.IsOpen)
                    {
                        SelectString.ClickSlot(0);
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (SelectIconString.IsOpen)
                    {
                        SelectIconString.ClickSlot(0);
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (JournalAccept.IsOpen)
                    {
                        JournalAccept.Accept();
                        await Task.Delay(1000, token);
                        elapsed += 1000;
                        continue;
                    }

                    await Task.Delay(100, token);
                    elapsed += 100;
                }

                return QuestLogManager.HasQuest((int)questId);
            }
            catch (Exception ex)
            {
                _controller.Log($"PickupQuest error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ExecuteTurnInAsync(ProfileElement element, CancellationToken token)
        {
            var questIdStr = element.Attributes.GetValueOrDefault("QuestId", "0");
            var npcIdStr = element.Attributes.GetValueOrDefault("NpcId", "0");
            var xyzStr = element.Attributes.GetValueOrDefault("XYZ", "");
            var rewardSlotStr = element.Attributes.GetValueOrDefault("RewardSlot", "-1");

            if (!uint.TryParse(questIdStr, out var questId) || questId == 0)
            {
                _controller.Log($"TurnIn: Invalid QuestId '{questIdStr}'");
                return false;
            }

            if (!uint.TryParse(npcIdStr, out var npcId) || npcId == 0)
            {
                _controller.Log($"TurnIn: Invalid NpcId '{npcIdStr}'");
                return false;
            }

            int.TryParse(rewardSlotStr, out var rewardSlot);

            return await TurnInQuestAsync(questId, npcId, xyzStr, rewardSlot, token);
        }

        /// <summary>
        /// Turn in a quest to an NPC.
        /// </summary>
        public async Task<bool> TurnInQuestAsync(uint questId, uint npcId, string xyzHint, int rewardSlot, CancellationToken token)
        {
            // Check if quest is already completed
            if (QuestLogManager.IsQuestCompleted(questId))
            {
                _controller.Log($"TurnIn: Quest {questId} already completed");
                return true;
            }

            // Check if we have the quest
            if (!QuestLogManager.HasQuest((int)questId))
            {
                _controller.Log($"TurnIn: Don't have quest {questId}");
                return false;
            }

            var npcName = DataManager.GetLocalizedNPCName((int)npcId);
            _controller.SetDirective("Turning In Quest", $"Quest {questId} to {npcName}");
            _controller.Log($"TurnIn: Quest {questId} to {npcName}");

            try
            {
                // Find the NPC
                var npc = GameObjectManager.GetObjectsByNPCId(npcId)
                    .FirstOrDefault(n => n.IsVisible && n.IsTargetable);

                // Navigate to hint location if NPC not found
                if (npc == null && !string.IsNullOrEmpty(xyzHint) && TryParseVector3(xyzHint, out var location))
                {
                    await MoveToLocationAsync(location, token, 5f);
                    npc = GameObjectManager.GetObjectsByNPCId(npcId)
                        .FirstOrDefault(n => n.IsVisible && n.IsTargetable);
                }

                if (npc == null)
                {
                    _controller.Log($"TurnIn: NPC {npcName} not found");
                    return false;
                }

                // Move to NPC
                if (!npc.IsWithinInteractRange)
                {
                    await MoveToLocationAsync(npc.Location, token, 4f);
                }

                // Interact
                npc.Interact();

                // Handle turn-in dialog
                var timeout = 30000;
                var elapsed = 0;
                var rewardSelected = false;

                while (elapsed < timeout && !QuestLogManager.IsQuestCompleted(questId))
                {
                    if (token.IsCancellationRequested) return false;

                    if (Talk.DialogOpen)
                    {
                        Talk.Next();
                        await Task.Delay(200, token);
                        elapsed += 200;
                        continue;
                    }

                    if (SelectYesno.IsOpen)
                    {
                        SelectYesno.ClickYes();
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (SelectString.IsOpen)
                    {
                        SelectString.ClickSlot(0);
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (SelectIconString.IsOpen)
                    {
                        SelectIconString.ClickSlot(0);
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    if (JournalResult.IsOpen)
                    {
                        // Select reward if needed
                        if (rewardSlot >= 0 && !rewardSelected)
                        {
                            JournalResult.SelectSlot(rewardSlot);
                            rewardSelected = true;
                            await Task.Delay(500, token);
                            elapsed += 500;
                        }

                        if (JournalResult.ButtonClickable)
                        {
                            JournalResult.Complete();
                            await Task.Delay(1000, token);
                            elapsed += 1000;
                        }
                        continue;
                    }

                    if (Request.IsOpen)
                    {
                        // Hand over requested items
                        await CommonTasks.HandOverRequestedItems();
                        await Task.Delay(500, token);
                        elapsed += 500;
                        continue;
                    }

                    await Task.Delay(100, token);
                    elapsed += 100;
                }

                return QuestLogManager.IsQuestCompleted(questId);
            }
            catch (Exception ex)
            {
                _controller.Log($"TurnIn error: {ex.Message}");
                return false;
            }
        }

        private bool ExecuteLogMessage(ProfileElement element)
        {
            var message = element.Attributes.GetValueOrDefault("Message", "");
            _controller.Log($"[Profile] {message}");
            return true;
        }

        private async Task<bool> ExecuteRunCodeAsync(ProfileElement element, ParsedProfile profile, CancellationToken token)
        {
            var name = element.Attributes.GetValueOrDefault("Name", "");

            if (profile.CodeChunks.TryGetValue(name, out var chunk))
            {
                _controller.Log($"RunCode: {name} (Code execution not supported, skipping)");
                // TODO: Implement limited code execution support
            }
            else
            {
                _controller.Log($"RunCode: Code chunk '{name}' not found");
            }

            await Task.Delay(10, token);
            return true;
        }

        private async Task<bool> ExecuteAutoEquipAsync(ProfileElement element, CancellationToken token)
        {
            _controller.Log("AutoInventoryEquip: Equipping best gear");
            _controller.SetDirective("Equipping", "Best available gear");

            return await AutoEquipBestGearAsync(token);
        }

        /// <summary>
        /// Auto-equip the best available gear for current class.
        /// </summary>
        public async Task<bool> AutoEquipBestGearAsync(CancellationToken token)
        {
            try
            {
                var equipped = InventoryManager.EquippedItems;
                var upgraded = false;

                foreach (var slot in equipped)
                {
                    if (token.IsCancellationRequested) return false;
                    if (!slot.IsValid) continue;
                    if (slot.Slot == 0 && !slot.IsFilled)
                    {
                        // Main hand must have something
                        continue;
                    }

                    // Get valid equipment categories for this slot
                    var categories = GetEquipmentCategoriesForSlot(slot.Slot);
                    if (categories.Count == 0) continue;

                    // Find best item from inventory/armory
                    var bestItem = InventoryManager.FilledInventoryAndArmory
                        .Where(i =>
                            categories.Contains(i.Item.EquipmentCatagory) &&
                            i.Item.IsValidForCurrentClass &&
                            i.Item.RequiredLevel <= Core.Me.ClassLevel &&
                            i.BagId != InventoryBagId.EquippedItems)
                        .OrderByDescending(i => i.Item.ItemLevel)
                        .FirstOrDefault();

                    if (bestItem == null) continue;

                    // Compare with current item
                    var currentItemLevel = slot.IsFilled ? slot.Item.ItemLevel : 0;
                    if (bestItem.Item.ItemLevel <= currentItemLevel) continue;

                    // Equip the better item
                    _controller.Log($"AutoEquip: {bestItem.Name} (iLvl {bestItem.Item.ItemLevel}) -> Slot {slot.Slot}");
                    bestItem.Move(slot);
                    await Task.Delay(500, token);
                    upgraded = true;
                }

                if (upgraded)
                {
                    // Update gearset
                    _controller.Log("AutoEquip: Saving gearset");
                    ChatManager.SendChat("/gs save");
                    await Task.Delay(1000, token);
                }

                return true;
            }
            catch (Exception ex)
            {
                _controller.Log($"AutoEquip error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get valid equipment categories for a slot.
        /// </summary>
        private List<ItemUiCategory> GetEquipmentCategoriesForSlot(ushort slotId)
        {
            return slotId switch
            {
                0 => new List<ItemUiCategory> {
                    // Main hand - depends on class, but for DoH/DoL:
                    ItemUiCategory.Carpenters_Primary_Tool,
                    ItemUiCategory.Blacksmiths_Primary_Tool,
                    ItemUiCategory.Armorers_Primary_Tool,
                    ItemUiCategory.Goldsmiths_Primary_Tool,
                    ItemUiCategory.Leatherworkers_Primary_Tool,
                    ItemUiCategory.Weavers_Primary_Tool,
                    ItemUiCategory.Alchemists_Primary_Tool,
                    ItemUiCategory.Culinarians_Primary_Tool,
                    ItemUiCategory.Miners_Primary_Tool,
                    ItemUiCategory.Botanists_Primary_Tool,
                    ItemUiCategory.Fishers_Primary_Tool
                },
                1 => new List<ItemUiCategory> {
                    // Off hand
                    ItemUiCategory.Carpenters_Secondary_Tool,
                    ItemUiCategory.Blacksmiths_Secondary_Tool,
                    ItemUiCategory.Armorers_Secondary_Tool,
                    ItemUiCategory.Goldsmiths_Secondary_Tool,
                    ItemUiCategory.Leatherworkers_Secondary_Tool,
                    ItemUiCategory.Weavers_Secondary_Tool,
                    ItemUiCategory.Alchemists_Secondary_Tool,
                    ItemUiCategory.Culinarians_Secondary_Tool
                },
                2 => new List<ItemUiCategory> { ItemUiCategory.Head },
                3 => new List<ItemUiCategory> { ItemUiCategory.Body },
                4 => new List<ItemUiCategory> { ItemUiCategory.Hands },
                5 => new List<ItemUiCategory> { ItemUiCategory.Waist },
                6 => new List<ItemUiCategory> { ItemUiCategory.Legs },
                7 => new List<ItemUiCategory> { ItemUiCategory.Feet },
                8 => new List<ItemUiCategory> { ItemUiCategory.Earrings },
                9 => new List<ItemUiCategory> { ItemUiCategory.Necklace },
                10 => new List<ItemUiCategory> { ItemUiCategory.Bracelets },
                11 or 12 => new List<ItemUiCategory> { ItemUiCategory.Ring },
                13 => new List<ItemUiCategory> { ItemUiCategory.Soul_Crystal },
                _ => new List<ItemUiCategory>()
            };
        }

        #endregion

        #region Class Unlock

        /// <summary>
        /// Gets a list of DoH/DoL classes that are not yet unlocked.
        /// A class is unlocked if its level > 0.
        /// </summary>
        public List<ClassJobType> GetLockedClasses()
        {
            var locked = new List<ClassJobType>();

            if (Core.Me == null)
            {
                return locked;
            }

            foreach (var job in ClassUnlockData.AllDohDolClasses)
            {
                if (Core.Me.Levels[job] == 0)
                {
                    locked.Add(job);
                }
            }

            return locked;
        }

        /// <summary>
        /// Unlocks all locked DoH/DoL classes.
        /// </summary>
        public async Task<bool> UnlockAllClassesAsync(CancellationToken token)
        {
            var lockedClasses = GetLockedClasses();

            if (lockedClasses.Count == 0)
            {
                _controller.Log("All DoH/DoL classes are already unlocked.");
                return true;
            }

            _controller.Log($"Found {lockedClasses.Count} locked class(es) to unlock: {string.Join(", ", lockedClasses)}");

            foreach (var job in lockedClasses)
            {
                if (token.IsCancellationRequested)
                {
                    return false;
                }

                var result = await UnlockSingleClassAsync(job, token);
                if (!result)
                {
                    _controller.Log($"Failed to unlock {job}, stopping unlock sequence.");
                    return false;
                }

                // Refresh class levels after each unlock
                _controller.RefreshClassLevels();
            }

            _controller.Log("All classes successfully unlocked!");
            return true;
        }

        /// <summary>
        /// Unlocks a single DoH/DoL class by completing its unlock quest.
        /// </summary>
        public async Task<bool> UnlockSingleClassAsync(ClassJobType job, CancellationToken token)
        {
            // Check if already unlocked
            if (Core.Me.Levels[job] > 0)
            {
                _controller.Log($"{job} is already unlocked (level {Core.Me.Levels[job]}).");
                return true;
            }

            // Get unlock info
            if (!ClassUnlockData.UnlockInfo.TryGetValue(job, out var unlockInfo))
            {
                _controller.Log($"No unlock info found for {job}.");
                return false;
            }

            _controller.SetDirective($"Unlocking {job}", "Navigating to guild...");
            _controller.Log($"Starting unlock sequence for {job}...");

            try
            {
                // Step 1: Navigate to the guild zone
                _controller.Log($"Navigating to zone {unlockInfo.ZoneId}...");
                var navigated = await NavigateToLocationAsync(unlockInfo.ZoneId, unlockInfo.PickupLocation, token);
                if (!navigated)
                {
                    _controller.Log($"Failed to navigate to {job} guild.");
                    return false;
                }

                // Step 2: Handle prereq quest if not completed
                if (!QuestLogManager.IsQuestCompleted(unlockInfo.PrereqQuestId))
                {
                    _controller.SetDirective($"Unlocking {job}", "Talking to guild NPC...");
                    _controller.Log($"Completing prereq quest {unlockInfo.PrereqQuestId}...");

                    // Talk to the NPC to trigger the prereq quest
                    var locationStr = $"{unlockInfo.PickupLocation.X}, {unlockInfo.PickupLocation.Y}, {unlockInfo.PickupLocation.Z}";
                    var talked = await TalkToNpcAsync(unlockInfo.PickupNpcId, locationStr, "0", token);
                    if (!talked)
                    {
                        _controller.Log($"Failed to talk to NPC for prereq quest.");
                        return false;
                    }

                    // Wait a moment for quest completion
                    await Task.Delay(1000, token);
                }

                // Step 3: Pick up unlock quest if not already completed and not in journal
                if (!QuestLogManager.IsQuestCompleted(unlockInfo.UnlockQuestId))
                {
                    if (!QuestLogManager.HasQuest((int)unlockInfo.UnlockQuestId))
                    {
                        _controller.SetDirective($"Unlocking {job}", "Picking up unlock quest...");
                        _controller.Log($"Picking up unlock quest {unlockInfo.UnlockQuestId}...");

                        var locationStr = $"{unlockInfo.PickupLocation.X}, {unlockInfo.PickupLocation.Y}, {unlockInfo.PickupLocation.Z}";
                        var pickedUp = await PickupQuestAsync(unlockInfo.UnlockQuestId, unlockInfo.PickupNpcId, locationStr, token);
                        if (!pickedUp)
                        {
                            _controller.Log($"Failed to pick up unlock quest.");
                            return false;
                        }
                    }

                    // Step 4: Turn in unlock quest
                    if (QuestLogManager.HasQuest((int)unlockInfo.UnlockQuestId))
                    {
                        _controller.SetDirective($"Unlocking {job}", "Turning in unlock quest...");
                        _controller.Log($"Turning in unlock quest {unlockInfo.UnlockQuestId}...");

                        // Navigate to turn-in NPC if different location
                        if (unlockInfo.TurnInNpcId != unlockInfo.PickupNpcId)
                        {
                            await NavigateToLocationAsync(unlockInfo.ZoneId, unlockInfo.TurnInLocation, token);
                        }

                        var turnInLocationStr = $"{unlockInfo.TurnInLocation.X}, {unlockInfo.TurnInLocation.Y}, {unlockInfo.TurnInLocation.Z}";
                        var turnedIn = await TurnInQuestAsync(unlockInfo.UnlockQuestId, unlockInfo.TurnInNpcId, turnInLocationStr, -1, token);
                        if (!turnedIn)
                        {
                            _controller.Log($"Failed to turn in unlock quest.");
                            return false;
                        }
                    }
                }

                // Wait for quest to complete
                await Task.Delay(1000, token);

                // Verify quest is completed
                if (!QuestLogManager.IsQuestCompleted(unlockInfo.UnlockQuestId))
                {
                    _controller.Log($"Unlock quest {unlockInfo.UnlockQuestId} did not complete as expected.");
                    return false;
                }

                // Step 5: Change to the new class
                _controller.SetDirective($"Unlocking {job}", "Switching to new class...");
                _controller.Log($"Changing to {job}...");

                var changed = await ChangeClassAsync(job.ToString(), true, token);
                if (!changed)
                {
                    _controller.Log($"Failed to change to {job}.");
                    return false;
                }

                // Step 6: Auto-equip best gear
                _controller.SetDirective($"Unlocking {job}", "Equipping gear...");
                _controller.Log($"Auto-equipping gear for {job}...");
                await AutoEquipBestGearAsync(token);

                _controller.Log($"Successfully unlocked {job}!");
                return true;
            }
            catch (Exception ex)
            {
                _controller.Log($"Error unlocking {job}: {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    #region Profile Model Classes

    /// <summary>
    /// Represents a parsed profile.
    /// </summary>
    public class ParsedProfile
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Directory { get; set; }
        public List<ProfileElement> Elements { get; set; } = new List<ProfileElement>();
        public Dictionary<string, CodeChunk> CodeChunks { get; set; } = new Dictionary<string, CodeChunk>();
    }

    /// <summary>
    /// Represents a single element in a profile.
    /// </summary>
    public class ProfileElement
    {
        public string TagName { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<ProfileElement> Children { get; set; } = new List<ProfileElement>();
        public string RawXml { get; set; }
    }

    /// <summary>
    /// Represents a code chunk.
    /// </summary>
    public class CodeChunk
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
    }

    #endregion

    #region Dictionary Extension

    internal static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default)
        {
            return dict.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }

    #endregion
}
