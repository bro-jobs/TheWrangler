// RebornConsole snippet to explore Lisbeth's API for Home-related functionality
// Run this in RebornConsole to discover available methods and properties

// Get the Lisbeth bot
var loader = ff14bot.Managers.BotManager.Bots.FirstOrDefault(c => c.Name == "Lisbeth");
if (loader == null)
{
    Log("Lisbeth bot not found!");
    return;
}

// Get the Lisbeth object via reflection
var lisbethProperty = loader.GetType().GetProperty("Lisbeth");
var lisbeth = lisbethProperty?.GetValue(loader);
if (lisbeth == null)
{
    Log("Could not get Lisbeth object!");
    return;
}

Log("=== LISBETH OBJECT ===");
Log("Type: " + lisbeth.GetType().FullName);
Log("");

// List all properties on Lisbeth
Log("=== LISBETH PROPERTIES ===");
foreach (var prop in lisbeth.GetType().GetProperties())
{
    try
    {
        var value = prop.GetValue(lisbeth);
        Log($"  {prop.Name} ({prop.PropertyType.Name}) = {value}");
    }
    catch
    {
        Log($"  {prop.Name} ({prop.PropertyType.Name}) = [error reading]");
    }
}
Log("");

// Get the Api object
var apiObject = lisbeth.GetType().GetProperty("Api")?.GetValue(lisbeth);
if (apiObject == null)
{
    Log("Could not get Api object!");
    return;
}

Log("=== LISBETH API OBJECT ===");
Log("Type: " + apiObject.GetType().FullName);
Log("");

// List all methods on API
Log("=== API METHODS ===");
foreach (var method in apiObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
{
    var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Log($"  {method.ReturnType.Name} {method.Name}({paramStr})");
}
Log("");

// List all properties on API
Log("=== API PROPERTIES ===");
foreach (var prop in apiObject.GetType().GetProperties())
{
    try
    {
        var value = prop.GetValue(apiObject);
        Log($"  {prop.Name} ({prop.PropertyType.Name}) = {value}");
    }
    catch
    {
        Log($"  {prop.Name} ({prop.PropertyType.Name}) = [error reading]");
    }
}
Log("");

// Look specifically for anything with "Home" in the name
Log("=== SEARCHING FOR 'HOME' ===");
var allMembers = apiObject.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
foreach (var member in allMembers.Where(m => m.Name.ToLower().Contains("home")))
{
    Log($"  Found: {member.MemberType} {member.Name}");
}

// Also check the main Lisbeth object
foreach (var member in lisbeth.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
{
    if (member.Name.ToLower().Contains("home"))
    {
        Log($"  Found on Lisbeth: {member.MemberType} {member.Name}");
    }
}
Log("");

// Check for Settings object
Log("=== CHECKING FOR SETTINGS ===");
var settingsProp = lisbeth.GetType().GetProperty("Settings") ?? lisbeth.GetType().GetProperty("Configuration");
if (settingsProp != null)
{
    var settings = settingsProp.GetValue(lisbeth);
    if (settings != null)
    {
        Log($"Found Settings object: {settings.GetType().FullName}");
        foreach (var prop in settings.GetType().GetProperties())
        {
            if (prop.Name.ToLower().Contains("home"))
            {
                try
                {
                    var value = prop.GetValue(settings);
                    Log($"  HOME SETTING: {prop.Name} = {value}");
                }
                catch
                {
                    Log($"  HOME SETTING: {prop.Name} = [error reading]");
                }
            }
        }
    }
}
else
{
    Log("No Settings property found directly.");
}

// Check for any method with "Travel", "Go", "Return" in the name
Log("");
Log("=== SEARCHING FOR TRAVEL/TELEPORT METHODS ===");
foreach (var method in apiObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
{
    var name = method.Name.ToLower();
    if (name.Contains("travel") || name.Contains("teleport") || name.Contains("goto") ||
        name.Contains("return") || name.Contains("go_to") || name.Contains("navigate"))
    {
        var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Log($"  {method.ReturnType.Name} {method.Name}({paramStr})");
    }
}

Log("");
Log("=== DONE ===");
