using System.Collections.Generic;
using System.Collections.Immutable;
using StardewValley;
using StardewValley.Delegates;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers;

internal enum QuerySpecificity
{
  VeryBroad,
  Broad,
  Neutral,
  Specific,
  VerySpecific,
  Unknown
}

internal class ConditionResolver
{
  private readonly QueryFutureResolverDelegate? _futureResolverDelegate;
  private readonly ReadableConditionRequirementsDelegate _requirementStrDelegate;
  public readonly QuerySpecificity Specificity;
  private string _resolverName;

  public ConditionResolver(
    string resolverName,
    QueryFutureResolverDelegate futureResolverDelegate,
    ReadableConditionRequirementsDelegate requirementStrDelegate,
    QuerySpecificity specificity = QuerySpecificity.Neutral
  )
  {
    _resolverName = resolverName;
    _futureResolverDelegate = futureResolverDelegate;
    _requirementStrDelegate = requirementStrDelegate;
    CanResolveToDate = true;
    Specificity = specificity;
  }

  public ConditionResolver(
    string resolverName,
    ReadableConditionRequirementsDelegate requirementStrDelegate,
    bool isErrorResolver = false
  )
  {
    _resolverName = resolverName;
    _requirementStrDelegate = requirementStrDelegate;
    CanResolveToDate = false;
    Specificity = QuerySpecificity.Unknown;
    IsErrorResolver = isErrorResolver;
  }

  public bool IsErrorResolver { get; }
  public bool CanResolveToDate { get; }


  public ISet<WorldDate> ResolveFuture(
    GameStateQuery.ParsedGameStateQuery query,
    GameStateQueryContext context,
    int lookupWindowYears
  )
  {
    return _futureResolverDelegate?.Invoke(query, context, lookupWindowYears) ?? ImmutableHashSet<WorldDate>.Empty;
  }

  public string GenerateRequirementsStr(string joinedQueryString, GameStateQuery.ParsedGameStateQuery query)
  {
    return _requirementStrDelegate(joinedQueryString, query);
  }
}
