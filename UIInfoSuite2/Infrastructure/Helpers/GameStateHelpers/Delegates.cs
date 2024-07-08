using System.Collections.Generic;
using StardewValley;
using StardewValley.Delegates;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;

internal delegate ISet<WorldDate> QueryFutureResolverDelegate(
  GameStateQuery.ParsedGameStateQuery query,
  GameStateQueryContext context,
  int lookupWindowYears
);

internal delegate string ReadableConditionRequirementsDelegate(
  string joinedQueryString,
  GameStateQuery.ParsedGameStateQuery query
);
