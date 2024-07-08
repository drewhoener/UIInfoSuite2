using System;
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
  private static ConditionResolver DAY_OF_WEEK = new(
    nameof(GameStateQuery.DefaultResolvers.DAY_OF_WEEK),
    FutureResolver_DayOfWeek,
    RequirementsGenerator_DayOfWeek
  );

  private static ISet<WorldDate> FutureResolver_DayOfWeek(
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

    HashSet<DayOfWeek> selectedWeekdays = ParseWeekdaysFromQuery(query);

    int totalWeeks = 4 * WorldDate.MonthsPerYear * lookupWindowYears;
    for (var weekOffset = 0; weekOffset < totalWeeks; weekOffset++)
    {
      int offsetDays = 7 * weekOffset;
      foreach (DayOfWeek weekday in selectedWeekdays)
      {
        WorldDate newDate = new(Game1.Date);
        if (newDate.DayOfWeek > weekday)
        {
          continue;
        }

        newDate.TotalDays += weekday - newDate.DayOfWeek + offsetDays;

        if (Game1.Date <= newDate)
        {
          dates.Add(newDate);
        }
      }
    }

    return dates;
  }

  private static string RequirementsGenerator_DayOfWeek(
    string joinedQueryString,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    HashSet<DayOfWeek> parsedWeekdays = ParseWeekdaysFromQuery(query);
    List<string> parsedDayStrings = parsedWeekdays.OrderBy(day => day == DayOfWeek.Sunday ? 6 : (int)day - 1)
      .Select(day => Game1.shortDayDisplayNameFromDayOfSeason((int)day))
      .Where(s => !string.IsNullOrEmpty(s))
      .ToList();
    string displayStr = parsedDayStrings.Count == 0 ? "???" : string.Join(", ", parsedDayStrings);

    return $"{I18n.Days()} {displayStr}";
  }

  private static HashSet<DayOfWeek> ParseWeekdaysFromQuery(GameStateQuery.ParsedGameStateQuery query)
  {
    return query.Query.Skip(1)
      .Select(str => WorldDate.TryGetDayOfWeekFor(str, out DayOfWeek dayOfWeek) ? dayOfWeek : new DayOfWeek?())
      .Where(v => v.HasValue)
      .Select(v => v!.Value)
      .ToHashSet();
  }
}
