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
  private readonly Dictionary<string, ConditionResolver> _conditionResolverCache = new();
  private readonly Dictionary<string, ConditionFutureResult> _futureResultsCache = new();
  private readonly IMonitor _logger;
  private readonly Dictionary<string, ParsedGameStateQueryWrapper> _queryWrapperCache = new();
  private readonly Dictionary<string, List<ParsedGameStateQueryWrapper>> _queryWrapperListCache = new();
  private readonly Dictionary<string, string> _readableRequirementsCache = new();

  public GameStateResolverCaches(IMonitor monitor)
  {
    _logger = monitor;
  }

  public void Clear()
  {
    _futureResultsCache.Clear();
  }

  public ConditionResolver GetFutureResolver(GameStateQuery.ParsedGameStateQuery parsedGameStateQuery)
  {
    string queryKey = parsedGameStateQuery.Query.Length == 0 ? "qk_unknown" : parsedGameStateQuery.Query[0].ToLower();

    if (parsedGameStateQuery.Query.Length == 0 ||
        parsedGameStateQuery.Resolver == null ||
        !string.IsNullOrEmpty(parsedGameStateQuery.Error))
    {
      if (_conditionResolverCache.TryGetValue(queryKey, out ConditionResolver? errorResolver))
      {
        return errorResolver;
      }

      errorResolver = DefaultConditionResolvers.UnsupportedConditionResolver(queryKey);
      _logger.LogOnce($"Cached error resolver for unsupported query {queryKey}, please report", LogLevel.Error);
      _conditionResolverCache[queryKey] = errorResolver;

      return errorResolver;
    }

    string methodName = parsedGameStateQuery.Resolver.Method.Name;

    if (_conditionResolverCache.TryGetValue(methodName, out ConditionResolver? cachedResolver))
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
      _logger.LogOnce($"Cached {resolverIsErrorStr}resolver for {resolverIsSupportedStr}{queryKey}", LogLevel.Error);
      _conditionResolverCache[methodName] = cachedResolver;
    }

    return cachedResolver;
  }

  public bool TryGetRequirementsStr(string queryStr, [NotNullWhen(true)] out string? result)
  {
    if (!_readableRequirementsCache.TryGetValue(queryStr, out string? cachedResult))
    {
      result = null;
      return false;
    }

    _logger.LogOnce($"Using cached result for {queryStr} requirements", LogLevel.Warn);
    result = cachedResult;
    return true;
  }

  public bool TryGetFutureResult(string queryStringKey, [NotNullWhen(true)] out ConditionFutureResult? result)
  {
    if (!_futureResultsCache.TryGetValue(queryStringKey, out ConditionFutureResult? cachedResult))
    {
      result = null;
      return false;
    }

    _logger.Log($"Using cached result for query {queryStringKey}", LogLevel.Warn);
    result = cachedResult;
    return true;
  }

  public void CacheFutureResult(string queryStringKey, ConditionFutureResult futureResult)
  {
    _logger.LogOnce($"Updating cached result for query {queryStringKey}", LogLevel.Warn);
    _futureResultsCache[queryStringKey] = futureResult;
  }

  public void CacheRequirementsStr(string queryStr, string requirementsStr)
  {
    _logger.LogOnce($"Updating cached result for {queryStr} requirements", LogLevel.Warn);
    _readableRequirementsCache[queryStr] = requirementsStr;
  }

  public ParsedGameStateQueryWrapper GetParsedQueryWrapper(GameStateQuery.ParsedGameStateQuery parsedGameStateQuery)
  {
    string query = string.Join(' ', parsedGameStateQuery.Query);
    if (_queryWrapperCache.TryGetValue(query, out ParsedGameStateQueryWrapper? cachedResolver))
    {
      return cachedResolver;
    }

    ConditionResolver conditionResolver = GetFutureResolver(parsedGameStateQuery);
    if (!conditionResolver.CanResolveToDate)
    {
      _logger.LogOnce($"Query [{parsedGameStateQuery.Query[0]}] has no resolvers");
    }

    var resolver = new ParsedGameStateQueryWrapper(this, conditionResolver, parsedGameStateQuery);
    _queryWrapperCache[query] = resolver;
    return resolver;
  }

  public List<ParsedGameStateQueryWrapper> GetParsedQueryWrappers(string queryString)
  {
    if (_queryWrapperListCache.TryGetValue(queryString, out List<ParsedGameStateQueryWrapper>? cachedResolvers))
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
    _queryWrapperListCache[queryString] = conditionResolvers;

    return conditionResolvers;
  }
}
