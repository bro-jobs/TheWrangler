// RebornConsole snippet to explore teleport/travel methods
// Looking for ways to send player to their home location

Log("=== EXPLORING TELEPORT METHODS ===");
Log("");

// Get the Lisbeth bot and API
var loader = ff14bot.Managers.BotManager.Bots.FirstOrDefault(c => c.Name == "Lisbeth");
var lisbeth = loader?.GetType().GetProperty("Lisbeth")?.GetValue(loader);
var apiObject = lisbeth?.GetType().GetProperty("Api")?.GetValue(lisbeth);

// 1. Check ff14bot WorldManager for teleport methods
Log("=== WORLDMANAGER METHODS ===");
var wm = typeof(ff14bot.Managers.WorldManager);
foreach (var method in wm.GetMethods(BindingFlags.Public | BindingFlags.Static))
{
    var name = method.Name.ToLower();
    if (name.Contains("teleport") || name.Contains("aetheryte"))
    {
        var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Log($"  {method.ReturnType.Name} WorldManager.{method.Name}({paramStr})");
    }
}

// 2. If we have Lisbeth API, check for travel-related methods
if (apiObject != null)
{
    Log("");
    Log("=== LISBETH API - ALL METHODS ===");
    foreach (var method in apiObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
    {
        var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Log($"  {method.ReturnType.Name} {method.Name}({paramStr})");
    }

    // Check if there's a Travel or TravelTo method
    Log("");
    Log("=== CHECKING FOR SPECIFIC METHODS ===");
    var methods = new[] { "GoHome", "TravelToHome", "ReturnHome", "TravelHome",
                          "GoToHome", "SetHome", "GetHome", "Travel", "TravelTo",
                          "Teleport", "TeleportTo", "TeleportHome" };
    foreach (var name in methods)
    {
        var method = apiObject.GetType().GetMethod(name);
        if (method != null)
        {
            var paramStr = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            Log($"  FOUND: {method.ReturnType.Name} {name}({paramStr})");
        }
    }
}

// 3. Look at available aetherytes/locations
Log("");
Log("=== AVAILABLE AETHERYTES ===");
try
{
    var locations = ff14bot.Managers.WorldManager.AvailableLocations;
    Log($"Total available: {locations.Count()}");
    foreach (var loc in locations.Take(10))
    {
        Log($"  [{loc.AetheryteId}] {loc.Name} @ Zone {loc.ZoneId}");
    }
    if (locations.Count() > 10)
    {
        Log($"  ... and {locations.Count() - 10} more");
    }
}
catch (Exception ex)
{
    Log($"Error reading aetherytes: {ex.Message}");
}

// 4. Check if there's a way to get the "home" aetheryte from Lisbeth settings
Log("");
Log("=== LOOKING FOR HOME AETHERYTE ID ===");
if (lisbeth != null)
{
    // Check all properties for anything that looks like an aetheryte ID for home
    foreach (var prop in lisbeth.GetType().GetProperties())
    {
        var name = prop.Name.ToLower();
        if (name.Contains("home") || name.Contains("aetheryte") || name.Contains("destination"))
        {
            try
            {
                var value = prop.GetValue(lisbeth);
                Log($"  {prop.Name} = {value}");
            }
            catch
            {
                Log($"  {prop.Name} = [error]");
            }
        }
    }

    // Try to get settings
    var settingsProp = lisbeth.GetType().GetProperty("Settings");
    if (settingsProp != null)
    {
        var settings = settingsProp.GetValue(lisbeth);
        if (settings != null)
        {
            Log("");
            Log("  Settings found, checking for home:");
            foreach (var prop in settings.GetType().GetProperties())
            {
                var name = prop.Name.ToLower();
                if (name.Contains("home") || name.Contains("aetheryte") || name.Contains("location") || name.Contains("return"))
                {
                    try
                    {
                        var value = prop.GetValue(settings);
                        Log($"    {prop.Name} = {value}");

                        // If it's a complex type, try to get its properties too
                        if (value != null && !value.GetType().IsPrimitive && value.GetType() != typeof(string))
                        {
                            foreach (var innerProp in value.GetType().GetProperties())
                            {
                                try
                                {
                                    Log($"      .{innerProp.Name} = {innerProp.GetValue(value)}");
                                }
                                catch { }
                            }
                        }
                    }
                    catch
                    {
                        Log($"    {prop.Name} = [error]");
                    }
                }
            }
        }
    }
}

Log("");
Log("=== HOW TO TELEPORT ===");
Log("To teleport, you can use:");
Log("  ff14bot.Managers.WorldManager.TeleportById(aetheryteId)");
Log("  Example: WorldManager.TeleportById(8) // Ul'dah - Steps of Nald");
Log("");
Log("=== DONE ===");
