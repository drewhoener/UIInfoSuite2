using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

internal static partial class DefaultConditionResolvers
{

  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Accessed via Reflection")]
  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Must match Stardew GSQ Resolvers")]
  public static ConditionResolver SEASON = new(
    nameof(GameStateQuery.DefaultResolvers.SEASON),
    FutureResolver_Season,
    RequirementsGenerator_Season,
    QuerySpecificity.VeryBroad
  );

  private static ISet<WorldDate> FutureResolver_Season(
    GameStateQuery.ParsedGameStateQuery query,
    GameStateQueryContext context,
    int lookupWindowYears
  )
  {
    HashSet<WorldDate> dates = new();

    if (query.Query.Length < 2)
    {
      return dates;
    }

    for (var index = 1; index < query.Query.Length; index++)
    {
      if (!ArgUtility.TryGetEnum(query.Query, index, out Season season, out _))
      {
        continue;
      }

      for (var yearOffset = 0; yearOffset < lookupWindowYears; yearOffset++)
      {
        int dateYear = Game1.year + yearOffset;
        dates.AddRange(
          Enumerable.Range(1, 28).Select(day => new WorldDate(dateYear, season, day)).Where(date => Game1.Date <= date)
        );
      }
    }

    return dates;
  }

  private static string RequirementsGenerator_Season(
    string joinedQueryString,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    if (query.Query.Length < 2)
    {
      return I18n.GSQ_Requirements_ParseError().Format(joinedQueryString);
    }


    List<string> seasonStrings = new();
    for (var index = 1; index < query.Query.Length; index++)
    {
      if (!ArgUtility.TryGetEnum(query.Query, index, out Season season, out _))
      {
        seasonStrings.Add(query.Query[index]);
        continue;
      }
      seasonStrings.Add(Tools.GetLocalizedSeasonName(season));
    }

    return I18n.GSQ_Requirements_Season().Format(string.Join(", ", seasonStrings));
  }
}
