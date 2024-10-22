using StardewValley;
using StardewValley.Delegates;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;

internal class ParsedGameStateQueryWrapper
{
  private readonly GameStateResolverCaches _resolverCache;

  public ParsedGameStateQueryWrapper(
    GameStateResolverCaches resolverCaches,
    ConditionResolver resolver,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    Query = query;
    QueryStr = string.Join(' ', QueryStrArr);
    _resolverCache = resolverCaches;
    Resolver = resolver;
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
    if (_resolverCache.TryGetFutureResult(QueryStr, out ConditionFutureResult? cachedResult))
    {
      return cachedResult;
    }

    var futureResult = new ConditionFutureResult(Resolver.ResolveFuture(Query, context, 6));
    _resolverCache.CacheFutureResult(QueryStr, futureResult);
    return futureResult;
  }

  private string GetOrCreateRequirementsStr()
  {
    //TODO check if this query matches us

    if (_resolverCache.TryGetRequirementsStr(QueryStr, out string? cachedRequirements))
    {
      return cachedRequirements;
    }

    string requirementsStr = Resolver.GenerateRequirementsStr(QueryStr, Query);
    _resolverCache.CacheRequirementsStr(QueryStr, requirementsStr);

    return requirementsStr;
  }
}
