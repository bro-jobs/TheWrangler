// RebornConsole snippet to explore Lisbeth's API for resume methods
// Run this to find alternative ways to resume orders without confirmation

using System;
using System.Linq;
using System.Reflection;
using ff14bot.Managers;

// Find Lisbeth
var loader = BotManager.Bots.FirstOrDefault(c => c.Name == "Lisbeth");
if (loader == null)
{
    Log("Lisbeth not found in BotManager.Bots");
    return;
}

// Get the Lisbeth object
var lisbethProp = loader.GetType().GetProperty("Lisbeth");
if (lisbethProp == null)
{
    Log("Lisbeth property not found on loader");
    return;
}
var lisbeth = lisbethProp.GetValue(loader);

// Get the API object
var apiProp = lisbeth.GetType().GetProperty("Api");
if (apiProp == null)
{
    Log("Api property not found on Lisbeth");
    return;
}
var api = apiProp.GetValue(lisbeth);

Log("=== Lisbeth API Methods ===");
var methods = api.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
    .Where(m => !m.IsSpecialName)  // Exclude property getters/setters
    .OrderBy(m => m.Name);

foreach (var method in methods)
{
    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Log($"  {method.ReturnType.Name} {method.Name}({parameters})");
}

Log("");
Log("=== Lisbeth API Properties ===");
var props = api.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .OrderBy(p => p.Name);

foreach (var prop in props)
{
    try
    {
        var value = prop.GetValue(api);
        Log($"  {prop.PropertyType.Name} {prop.Name} = {value}");
    }
    catch
    {
        Log($"  {prop.PropertyType.Name} {prop.Name} = (error reading)");
    }
}

// Look specifically for resume-related methods
Log("");
Log("=== Resume/Restart Related Methods ===");
var resumeMethods = methods.Where(m =>
    m.Name.ToLower().Contains("resume") ||
    m.Name.ToLower().Contains("restart") ||
    m.Name.ToLower().Contains("continue") ||
    m.Name.ToLower().Contains("incomplete"));

foreach (var method in resumeMethods)
{
    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Log($"  {method.ReturnType.Name} {method.Name}({parameters})");
}

// Check for settings object
Log("");
Log("=== Looking for Settings ===");
var settingsProps = api.GetType().GetProperties()
    .Where(p => p.Name.ToLower().Contains("setting") || p.PropertyType.Name.ToLower().Contains("setting"));

foreach (var prop in settingsProps)
{
    Log($"  {prop.PropertyType.Name} {prop.Name}");

    // Try to get the settings object and list its properties
    try
    {
        var settings = prop.GetValue(api);
        if (settings != null)
        {
            var settingProps = settings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var sp in settingProps.Take(20))  // Limit output
            {
                try
                {
                    var val = sp.GetValue(settings);
                    Log($"    {sp.Name} = {val}");
                }
                catch { }
            }
        }
    }
    catch { }
}
