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
using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;

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
                var result = await LisbethApi.ExecuteOrdersAsync(json, WranglerSettings.Instance.IgnoreHome);

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
            var zoneId = element.Attributes.GetValueOrDefault("ZoneId", "0");
            var xyz = element.Attributes.GetValueOrDefault("XYZ", "");

            _controller.SetDirective("Navigating", $"Zone: {zoneId}");
            _controller.Log($"GetTo: Zone {zoneId}, XYZ {xyz}");

            // TODO: Implement actual navigation via ff14bot
            await Task.Delay(100, token);

            return true;
        }

        private async Task<bool> ExecuteTeleportToAsync(ProfileElement element, CancellationToken token)
        {
            var aetheryteId = element.Attributes.GetValueOrDefault("AetheryteId", "0");
            var name = element.Attributes.GetValueOrDefault("Name", "");

            _controller.SetDirective("Teleporting", $"To: {name}");
            _controller.Log($"TeleportTo: {name} (Aetheryte {aetheryteId})");

            // TODO: Implement actual teleportation
            await Task.Delay(100, token);

            return true;
        }

        private async Task<bool> ExecuteChangeClassAsync(ProfileElement element, CancellationToken token)
        {
            var job = element.Attributes.GetValueOrDefault("Job", "");

            _controller.SetDirective("Changing Class", $"To: {job}");
            _controller.Log($"ChangeClass: {job}");

            // TODO: Implement actual class change
            await Task.Delay(100, token);

            _controller.RefreshClassLevels();

            return true;
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
            var npcId = element.Attributes.GetValueOrDefault("NpcId", "0");
            _controller.Log($"TalkTo: NPC {npcId}");

            // TODO: Implement NPC interaction
            await Task.Delay(100, token);

            return true;
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
            var questId = element.Attributes.GetValueOrDefault("QuestId", "0");
            var npcId = element.Attributes.GetValueOrDefault("NpcId", "0");

            _controller.SetDirective("Picking Up Quest", $"Quest {questId}");
            _controller.Log($"PickupQuest: Quest {questId} from NPC {npcId}");

            // TODO: Implement quest pickup
            await Task.Delay(100, token);

            return true;
        }

        private async Task<bool> ExecuteTurnInAsync(ProfileElement element, CancellationToken token)
        {
            var questId = element.Attributes.GetValueOrDefault("QuestId", "0");
            var itemId = element.Attributes.GetValueOrDefault("ItemId", "");

            _controller.SetDirective("Turning In Quest", $"Quest {questId}");
            _controller.Log($"TurnIn: Quest {questId}, Item {itemId}");

            // TODO: Implement quest turn-in
            await Task.Delay(100, token);

            return true;
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
            _controller.Log("AutoInventoryEquip");
            // TODO: Implement auto-equip
            await Task.Delay(100, token);
            return true;
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
