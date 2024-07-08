using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers.Resolvers.DefaultResolvers;

internal static partial class DefaultConditionResolvers
{
  [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Accessed via Reflection")]
  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Must match Stardew GSQ Resolvers")]
  public static ConditionResolver IS_GREEN_RAIN_DAY = new(
    nameof(GameStateQuery.DefaultResolvers.IS_GREEN_RAIN_DAY),
    FutureResolver_IsGreenRainDay,
    RequirementsGenerator_IsGreenRainDay,
    QuerySpecificity.VerySpecific
  );

  private static ISet<WorldDate> FutureResolver_IsGreenRainDay(
    GameStateQuery.ParsedGameStateQuery query,
    GameStateQueryContext context,
    int lookupWindowYears
  )
  {
    HashSet<WorldDate> dates = new();
    var greenRainDays = new[] { 5, 6, 7, 14, 15, 16, 18, 23 };
    for (var yearOffset = 0; yearOffset < lookupWindowYears; yearOffset++)
    {
      Random seededRandom = Utility.CreateRandom((Game1.year + yearOffset) * 777, Game1.uniqueIDForThisGame);
      int greenRainDayThisYear = seededRandom.ChooseFrom(greenRainDays);
      var greenRainDate = new WorldDate(Game1.year + yearOffset, Season.Summer, greenRainDayThisYear);
      if (Game1.Date > greenRainDate)
      {
        continue;
      }

      dates.Add(greenRainDate);
    }

    return dates;
  }

  private static string RequirementsGenerator_IsGreenRainDay(
    string joinedQueryString,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    return I18n.GSQ_Requirements_GreenRain();
  }
}
