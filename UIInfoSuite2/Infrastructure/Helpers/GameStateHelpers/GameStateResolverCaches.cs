using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;

internal sealed class GameStateResolverCaches
{
  private static readonly Lazy<GameStateResolverCaches> LazyInstance = new(() => new GameStateResolverCaches());
  private readonly Dictionary<string, ConditionResolver> _conditionResolverCache = new();
  private readonly Dictionary<string, ConditionFutureResult> _futureResultsCache = new();
  private readonly Dictionary<string, ParsedGameStateQueryWrapper> _queryWrapperCache = new();
  private readonly Dictionary<string, List<ParsedGameStateQueryWrapper>> _queryWrapperListCache = new();
  private readonly Dictionary<string, string> _readableRequirementsCache = new();

  private GameStateResolverCaches() { }

  private static GameStateResolverCaches Instance => LazyInstance.Value;

  public static void Clear()
  {
    Instance._futureResultsCache.Clear();
  }

  public static ConditionResolver GetFutureResolver(GameStateQuery.ParsedGameStateQuery parsedGameStateQuery)
  {
    string queryKey = parsedGameStateQuery.Query.Length == 0 ? "qk_unknown" : parsedGameStateQuery.Query[0].ToLower();

    if (parsedGameStateQuery.Query.Length == 0 ||
        parsedGameStateQuery.Resolver == null ||
        !string.IsNullOrEmpty(parsedGameStateQuery.Error))
    {
      if (Instance._conditionResolverCache.TryGetValue(queryKey, out ConditionResolver? errorResolver))
      {
        return errorResolver;
      }

      errorResolver = DefaultConditionResolvers.UnsupportedConditionResolver(queryKey);
      ModEntry.MonitorObject.LogOnce(
        $"Cached error resolver for unsupported query {queryKey}, please report",
        LogLevel.Error
      );
      Instance._conditionResolverCache[queryKey] = errorResolver;

      return errorResolver;
    }

    string methodName = parsedGameStateQuery.Resolver.Method.Name;

    if (Instance._conditionResolverCache.TryGetValue(methodName, out ConditionResolver? cachedResolver))
    {
      return cachedResolver;
    }


    {
      FieldInfo? field = AccessTools.GetDeclaredFields(typeof(DefaultConditionResolvers))
        .FirstOrDefault(f => f.Name.Equals(methodName) && f.FieldType == typeof(ConditionResolver));

      cachedResolver = field?.GetValue(null) as ConditionResolver ??
                       DefaultConditionResolvers.UnsupportedConditionResolver(methodName);


      string resolverIsErrorStr = cachedResolver.CanResolveToDate ? "" : "error ";
      string resolverIsSupportedStr = cachedResolver.CanResolveToDate ? "" : "unsupported ";
      ModEntry.MonitorObject.LogOnce(
        $"Cached {resolverIsErrorStr}resolver for {resolverIsSupportedStr}{queryKey}",
        LogLevel.Error
      );
      Instance._conditionResolverCache[methodName] = cachedResolver;
    }

    return cachedResolver;
  }

  public static bool TryGetRequirementsStr(string queryStr, [NotNullWhen(true)] out string? result)
  {
    if (!Instance._readableRequirementsCache.TryGetValue(queryStr, out string? cachedResult))
    {
      result = null;
      return false;
    }

    ModEntry.MonitorObject.LogOnce($"Using cached result for {queryStr} requirements", LogLevel.Warn);
    result = cachedResult;
    return true;
  }

  public static bool TryGetFutureResult(string queryStringKey, [NotNullWhen(true)] out ConditionFutureResult? result)
  {
    if (!Instance._futureResultsCache.TryGetValue(queryStringKey, out ConditionFutureResult? cachedResult))
    {
      result = null;
      return false;
    }

    ModEntry.MonitorObject.Log($"Using cached result for query {queryStringKey}", LogLevel.Warn);
    result = cachedResult;
    return true;
  }

  public static void CacheFutureResult(string queryStringKey, ConditionFutureResult futureResult)
  {
    ModEntry.MonitorObject.LogOnce($"Updating cached result for query {queryStringKey}", LogLevel.Warn);
    Instance._futureResultsCache[queryStringKey] = futureResult;
  }

  public static void CacheRequirementsStr(string queryStr, string requirementsStr)
  {
    ModEntry.MonitorObject.LogOnce($"Updating cached result for {queryStr} requirements", LogLevel.Warn);
    Instance._readableRequirementsCache[queryStr] = requirementsStr;
  }

  public static ParsedGameStateQueryWrapper GetParsedQueryWrapper(
    GameStateQuery.ParsedGameStateQuery parsedGameStateQuery
  )
  {
    string query = string.Join(' ', parsedGameStateQuery.Query);
    if (Instance._queryWrapperCache.TryGetValue(query, out ParsedGameStateQueryWrapper? cachedResolver))
    {
      return cachedResolver;
    }

    var resolver = new ParsedGameStateQueryWrapper(parsedGameStateQuery);
    Instance._queryWrapperCache[query] = resolver;
    return resolver;
  }

  public static List<ParsedGameStateQueryWrapper> GetParsedQueryWrappers(string queryString)
  {
    if (Instance._queryWrapperListCache.TryGetValue(
          queryString,
          out List<ParsedGameStateQueryWrapper>? cachedResolvers
        ))
    {
      return cachedResolvers;
    }

    var conditionResolvers = new List<ParsedGameStateQueryWrapper>();
    GameStateQuery.ParsedGameStateQuery[] parsedGameStateQueries = GameStateQuery.Parse(queryString);
    if (parsedGameStateQueries.Length == 0)
    {
      return conditionResolvers;
    }

    conditionResolvers.AddRange(parsedGameStateQueries.Select(GetParsedQueryWrapper));
    Instance._queryWrapperListCache[queryString] = conditionResolvers;

    return conditionResolvers;
  }
}
