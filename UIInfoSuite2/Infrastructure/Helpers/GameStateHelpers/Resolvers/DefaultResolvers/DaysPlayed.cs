using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewValley;
using StardewValley.Delegates;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

internal static partial class DefaultConditionResolvers
{
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Accessed via Reflection")]
  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Must match Stardew GSQ Resolvers")]
  private static ConditionResolver DAYS_PLAYED = new(
    nameof(GameStateQuery.DefaultResolvers.DAYS_PLAYED),
    FutureResolver_DaysPlayed,
    RequirementsGenerator_DaysPlayed,
    QuerySpecificity.Broad
  );

  private static ISet<WorldDate> FutureResolver_DaysPlayed(
    GameStateQuery.ParsedGameStateQuery query,
    GameStateQueryContext context,
    int lookupWindowYears
  )
  {
    HashSet<WorldDate> dates = new();
    int defaultMaxDate = (int)Game1.stats.DaysPlayed + lookupWindowYears * WorldDate.DaysPerYear;
    if (!ArgUtility.TryGetInt(query.Query, 1, out int minDays, out _) ||
        !ArgUtility.TryGetOptionalInt(query.Query, 2, out int maxDays, out _, defaultMaxDate))
    {
      return dates;
    }

    for (int totalDays = Math.Max(minDays, (int)Game1.stats.DaysPlayed); totalDays <= maxDays; totalDays++)
    {
      WorldDate date = new(1, Season.Spring, 1) { TotalDays = totalDays };
      dates.Add(date);
    }

    return dates;
  }

  private static string RequirementsGenerator_DaysPlayed(
    string joinedQueryString,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    if (!ArgUtility.TryGetInt(query.Query, 1, out int minDays, out _) ||
        !ArgUtility.TryGetOptionalInt(query.Query, 2, out int maxDays, out _, int.MaxValue))
    {
      return I18n.GSQ_Requirements_ParseError().Format(joinedQueryString);
    }

    return maxDays == int.MaxValue
      ? I18n.GSQ_Requirements_DaysPlayedMinimum().Format(minDays)
      : I18n.GSQ_Requirements_DaysPlayedRange().Format(minDays, maxDays);
  }
}
