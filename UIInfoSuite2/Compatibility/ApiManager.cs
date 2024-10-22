using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;

namespace UIInfoSuite2.Compatibility;

public static class ModCompat
{
  public const string CustomBush = "furyx639.CustomBush";
  public const string Gmcm = "spacechase0.GenericModConfigMenu";
  public const string DeluxeJournal = "MolsonCAD.DeluxeJournal";
}

public class ApiManager
{
  private readonly IMonitor _logger;
  private readonly Dictionary<string, object> _registeredApis = new();

  public ApiManager(IMonitor logger)
  {
    _logger = logger;
  }

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
      _logger.Log(
        $"Requested version {minimumVersion} for mod {modId}, but got {modInfo.Manifest.Version} instead, cannot use API.",
        LogLevel.Warn
      );
      return null;
    }

    var api = helper.ModRegistry.GetApi<T>(modId);
    if (api is null)
    {
      if (warnIfNotPresent)
      {
        _logger.Log($"Could not find API for mod {modId}, but one was requested", LogLevel.Warn);
      }

      return null;
    }

    _logger.Log($"Loaded API for mod {modId}", LogLevel.Info);
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

    _logger.Log($"API was registered for mod {modId} but the requested type is not supported", LogLevel.Warn);
    return false;
  }
}
