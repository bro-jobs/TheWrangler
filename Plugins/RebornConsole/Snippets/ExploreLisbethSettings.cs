// RebornConsole snippet to explore Lisbeth's Settings/Configuration
// Run this in RebornConsole to discover home-related settings

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

Log("=== EXPLORING ALL LISBETH PROPERTIES FOR SETTINGS ===");

// Look for any property that might contain settings
foreach (var prop in lisbeth.GetType().GetProperties())
{
    var name = prop.Name.ToLower();
    if (name.Contains("setting") || name.Contains("config") || name.Contains("option") ||
        name.Contains("pref") || name.Contains("home") || name.Contains("location"))
    {
        Log($"");
        Log($"Found potential settings property: {prop.Name} ({prop.PropertyType.Name})");

        try
        {
            var value = prop.GetValue(lisbeth);
            if (value != null)
            {
                Log($"  Value type: {value.GetType().FullName}");

                // Try to list properties of this object
                foreach (var innerProp in value.GetType().GetProperties())
                {
                    try
                    {
                        var innerValue = innerProp.GetValue(value);
                        var innerName = innerProp.Name.ToLower();

                        // Highlight home-related properties
                        if (innerName.Contains("home"))
                        {
                            Log($"    *** HOME: {innerProp.Name} ({innerProp.PropertyType.Name}) = {innerValue}");
                        }
                        else
                        {
                            Log($"    {innerProp.Name} ({innerProp.PropertyType.Name}) = {innerValue}");
                        }
                    }
                    catch
                    {
                        Log($"    {innerProp.Name} = [error reading]");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"  Error reading: {ex.Message}");
        }
    }
}

Log("");
Log("=== LOOKING AT BOT LOADER PROPERTIES ===");
// Also check the loader itself
foreach (var prop in loader.GetType().GetProperties())
{
    var name = prop.Name.ToLower();
    if (name.Contains("setting") || name.Contains("config") || name.Contains("home"))
    {
        Log($"Loader.{prop.Name} ({prop.PropertyType.Name})");
        try
        {
            var value = prop.GetValue(loader);
            Log($"  = {value}");
        }
        catch
        {
            Log($"  = [error]");
        }
    }
}

// Check for LisbethSettings class in the assemblies
Log("");
Log("=== SEARCHING FOR LISBETHSETTINGS CLASS ===");
foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
{
    try
    {
        foreach (var type in asm.GetTypes())
        {
            if (type.Name.Contains("LisbethSetting"))
            {
                Log($"Found type: {type.FullName} in {asm.GetName().Name}");

                // Check for Instance/Default property (common singleton pattern)
                var instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static) ??
                                   type.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp != null)
                {
                    Log($"  Has Instance property: {instanceProp.Name}");
                    try
                    {
                        var instance = instanceProp.GetValue(null);
                        if (instance != null)
                        {
                            Log($"  Instance type: {instance.GetType().FullName}");
                            foreach (var p in instance.GetType().GetProperties())
                            {
                                if (p.Name.ToLower().Contains("home"))
                                {
                                    try
                                    {
                                        Log($"    *** HOME: {p.Name} = {p.GetValue(instance)}");
                                    }
                                    catch
                                    {
                                        Log($"    *** HOME: {p.Name} = [error]");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"  Error getting instance: {ex.Message}");
                    }
                }
            }
        }
    }
    catch { } // Ignore assembly loading errors
}

Log("");
Log("=== DONE ===");
