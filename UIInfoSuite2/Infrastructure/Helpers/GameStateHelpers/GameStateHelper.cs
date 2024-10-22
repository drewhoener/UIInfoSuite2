using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Delegates;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;

internal class GameStateHelper
{
  private readonly GameStateResolverCaches _resolverCaches;

  public GameStateHelper(GameStateResolverCaches resolverCaches)
  {
    _resolverCaches = resolverCaches;
  }

  public ConditionFutureResult ResolveQueryFuture(
    string queryString,
    GameLocation? location = null,
    Farmer? player = null,
    Item? targetItem = null,
    Item? inputItem = null,
    Random? random = null,
    HashSet<string>? ignoreQueryKeys = null
  )
  {
    switch (queryString)
    {
      case null:
      case "":
      case "TRUE":
        return ConditionFutureResult.TodayAndTomorrow();
      case "FALSE":
        return ConditionFutureResult.Empty;
      default:
        var context = new GameStateQueryContext(location, player, targetItem, inputItem, random, ignoreQueryKeys);
        return ResolveQueryFutureImpl(queryString, context);
    }
  }

  public ConditionFutureResult ResolveQueryFutureImpl(string? queryString, GameStateQueryContext context)
  {
    ConditionFutureResult defaultDaysResult = ConditionFutureResult.TodayAndTomorrow();

    if (string.IsNullOrEmpty(queryString))
    {
      return defaultDaysResult;
    }

    List<ParsedGameStateQueryWrapper> queries = _resolverCaches.GetParsedQueryWrappers(queryString);

    if (queries.Count == 0)
    {
      return defaultDaysResult;
    }

    var resultForCondition = new ConditionFutureResult();

    foreach (ParsedGameStateQueryWrapper conditionResolver in queries)
    {
      if (conditionResolver.IsErrorHandler)
      {
        resultForCondition.AddErroredCondition(conditionResolver.QueryStr);
        continue;
      }

      if (!conditionResolver.CanResolveToDate)
      {
        resultForCondition.AddConditionStatus(conditionResolver.QueryStr, conditionResolver.Resolve(context));
        continue;
      }

      ConditionFutureResult resultForResolver = conditionResolver.ResolveFuture(context);
      resultForCondition.MergeConditionResult(resultForResolver);
      resultForCondition.AddConditionStatus(conditionResolver.QueryStr, resultForResolver.HasResolvedDate);
    }

    return resultForCondition;
  }
}
