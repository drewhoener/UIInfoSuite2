using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace UIInfoSuite2.Compatibility;

public static class ModCompat
{
  public const string CustomBush = "furyx639.CustomBush";
  public const string Gmcm = "spacechase0.GenericModConfigMenu";
  public const string DeluxeJournal = "MolsonCAD.DeluxeJournal";
  public const string BetterGameMenu = "leclair.bettergamemenu";
}

public class ApiManager(IMonitor logger)
{
  private readonly Dictionary<string, object> _registeredApis = new();

  public T? TryRegisterApi<T>(
    IModHelper helper,
    string modId,
    string? minimumVersion = null,
    bool warnIfNotPresent = false
  ) where T : class
  {
    IModInfo? modInfo = helper.ModRegistry.Get(modId);
    if (modInfo == null)
    {
      return null;
    }

    if (minimumVersion != null && modInfo.Manifest.Version.IsOlderThan(minimumVersion))
    {
      logger.Log(
        $"Requested version {minimumVersion} for mod {modId}, but got {modInfo.Manifest.Version} instead, cannot use API.",
        LogLevel.Warn
      );
      return null;
    }

    T? api;
    try
    {
      // This can throw if the API cannot be mapped to type T by Pintail.
      api = helper.ModRegistry.GetApi<T>(modId);
    }
    catch (Exception ex)
    {
      logger.Log($"Could not get API for mod {modId} due to error: {ex}", LogLevel.Warn);
      api = null;
    }

    if (api is null)
    {
      if (warnIfNotPresent)
      {
        logger.Log($"Could not find API for mod {modId}, but one was requested", LogLevel.Warn);
      }

      return null;
    }

    logger.Log($"Loaded API for mod {modId}", LogLevel.Info);
    _registeredApis[modId] = api;
    return api;
  }

  public bool GetApi<T>(string modId, [NotNullWhen(true)] out T? apiInstance) where T : class
  {
    apiInstance = null;
    if (!_registeredApis.TryGetValue(modId, out object? api))
    {
      return false;
    }

    if (api is T apiVal)
    {
      apiInstance = apiVal;
      return true;
    }

    logger.Log($"API was registered for mod {modId} but the requested type is not supported", LogLevel.Warn);
    return false;
  }
}
