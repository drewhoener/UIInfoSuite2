﻿using StardewValley;
using StardewValley.Delegates;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;

internal class ParsedGameStateQueryWrapper
{
  public ParsedGameStateQueryWrapper(GameStateQuery.ParsedGameStateQuery query)
  {
    Query = query;
    QueryStr = string.Join(' ', QueryStrArr);

    Resolver = GameStateResolverCaches.GetFutureResolver(Query);
    if (!Resolver.CanResolveToDate)
    {
      ModEntry.MonitorObject.LogOnce($"Query [{QueryStrArr[0]}] has no resolvers");
    }
  }

  public ConditionResolver Resolver { get; }

  public bool IsErrorHandler => Resolver.IsErrorResolver;

  public GameStateQuery.ParsedGameStateQuery Query { get; }

  public string[] QueryStrArr => Query.Query;

  public string QueryStr { get; }

  public string ConditionRequirements => GetOrCreateRequirementsStr();
  public bool CanResolveToDate => Resolver.CanResolveToDate;

  public bool Resolve(GameStateQueryContext context)
  {
    return Query.Resolver(QueryStrArr, context);
  }

  public ConditionFutureResult ResolveFuture(GameStateQueryContext context)
  {
    if (GameStateResolverCaches.TryGetFutureResult(QueryStr, out ConditionFutureResult? cachedResult))
    {
      return cachedResult;
    }

    var futureResult = new ConditionFutureResult(Resolver.ResolveFuture(Query, context, 6));
    GameStateResolverCaches.CacheFutureResult(QueryStr, futureResult);
    return futureResult;
  }

  private string GetOrCreateRequirementsStr()
  {
    //TODO check if this query matches us

    if (GameStateResolverCaches.TryGetRequirementsStr(QueryStr, out string? cachedRequirements))
    {
      return cachedRequirements;
    }

    string requirementsStr = Resolver.GenerateRequirementsStr(QueryStr, Query);
    GameStateResolverCaches.CacheRequirementsStr(QueryStr, requirementsStr);

    return requirementsStr;
  }
}
