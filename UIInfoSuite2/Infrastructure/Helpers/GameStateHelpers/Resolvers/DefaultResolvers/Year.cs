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
  private static ConditionResolver YEAR = new(
    nameof(GameStateQuery.DefaultResolvers.YEAR),
    FutureResolver_Year,
    RequirementsGenerator_Year,
    QuerySpecificity.VeryBroad
  );

  private static ISet<WorldDate> FutureResolver_Year(
    GameStateQuery.ParsedGameStateQuery query,
    GameStateQueryContext context,
    int lookupWindowYears
  )
  {
    int defaultMaxYear = Game1.year + lookupWindowYears;
    HashSet<WorldDate> dates = new();
    if (query.Query.Length < 2)
    {
      return dates;
    }

    if (!ArgUtility.TryGetInt(query.Query, 1, out int startYear, out _) ||
        !ArgUtility.TryGetOptionalInt(query.Query, 2, out int endYear, out _, defaultMaxYear))
    {
      return dates;
    }

    int maxOffsetDays = WorldDate.DaysPerYear * (endYear - startYear + 1);

    dates.AddRange(
      Enumerable.Range(0, maxOffsetDays)
        .Select(
          numDays =>
          {
            var date = new WorldDate(startYear, Season.Spring, 1);
            date.TotalDays += numDays;
            return date;
          }
        )
        .Where(date => Game1.Date <= date)
    );

    return dates;
  }

  private static string RequirementsGenerator_Year(string joinedQueryString, GameStateQuery.ParsedGameStateQuery query)
  {
    if (!ArgUtility.TryGetInt(query.Query, 1, out int startYear, out _) ||
        !ArgUtility.TryGetOptionalInt(query.Query, 2, out int endYear, out _, int.MaxValue))
    {
      return I18n.GSQ_Requirements_ParseError().Format(joinedQueryString);
    }

    return endYear == int.MaxValue
      ? I18n.GSQ_Requirements_YearMinimum().Format(startYear)
      : I18n.GSQ_Requirements_YearRange().Format(startYear, endYear);
  }
}
