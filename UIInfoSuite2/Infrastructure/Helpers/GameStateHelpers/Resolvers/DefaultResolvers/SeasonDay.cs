using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;
using StardewValley.Delegates;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

internal static partial class DefaultConditionResolvers
{
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Accessed via Reflection")]
  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Must match Stardew GSQ Resolvers")]
  private static ConditionResolver SEASON_DAY = new(
    nameof(GameStateQuery.DefaultResolvers.SEASON_DAY),
    FutureResolver_SeasonDay,
    RequirementsGenerator_SeasonDay,
    QuerySpecificity.Specific
  );

  private static ISet<WorldDate> FutureResolver_SeasonDay(
    GameStateQuery.ParsedGameStateQuery query,
    GameStateQueryContext context,
    int lookupWindowYears
  )
  {
    HashSet<WorldDate> dates = new();
    for (var index = 1; index < query.Query.Length; index += 2)
    {
      if (!ArgUtility.TryGetEnum(query.Query, index, out Season season, out string _) ||
          !ArgUtility.TryGetInt(query.Query, index + 1, out int day, out string _))
      {
        continue;
      }

      for (var yearOffset = 0; yearOffset < lookupWindowYears; yearOffset++)
      {
        var newDate = new WorldDate(Game1.year + yearOffset, season, day);
        if (Game1.Date > newDate)
        {
          continue;
        }

        dates.Add(newDate);
      }
    }

    return dates;
  }

  private static string RequirementsGenerator_SeasonDay(
    string joinedQueryString,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    Dictionary<Season, ISet<int>> seasonDateList = new()
    {
      { Season.Spring, new HashSet<int>() },
      { Season.Summer, new HashSet<int>() },
      { Season.Fall, new HashSet<int>() },
      { Season.Winter, new HashSet<int>() }
    };

    // Populate from the query

    for (var index = 1; index < query.Query.Length; index += 2)
    {
      if (!ArgUtility.TryGetEnum(query.Query, index, out Season season, out string _) ||
          !ArgUtility.TryGetInt(query.Query, index + 1, out int day, out string _))
      {
        continue;
      }

      seasonDateList[season].Add(day);
    }

    List<string> seasonParts = new();
    foreach ((Season season, ISet<int> days) in seasonDateList.OrderBy(kvp => kvp.Key))
    {
      if (days.Count == 0)
      {
        continue;
      }

      string formattedSeason = Tools.GetLocalizedSeasonName(season);
      string formattedDates = string.Join(", ", days);
      seasonParts.Add($"{formattedSeason} {formattedDates}");
    }

    return seasonParts.Count == 0 ? joinedQueryString : string.Join('\n', seasonParts);
  }
}
