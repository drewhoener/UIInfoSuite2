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
  private static ConditionResolver DAY_OF_MONTH = new(
    nameof(GameStateQuery.DefaultResolvers.DAY_OF_MONTH),
    FutureResolver_DayOfMonth,
    RequirementsGenerator_DayOfMonth
  );

  private static ISet<WorldDate> FutureResolver_DayOfMonth(
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

    HashSet<int> parsedDays = ParseDaysFromQuery(query);

    int totalMonths = WorldDate.MonthsPerYear * lookupWindowYears;
    for (var monthOffset = 0; monthOffset < totalMonths; monthOffset++)
    {
      int offsetDays = WorldDate.DaysPerMonth * monthOffset;
      foreach (int dayOfMonth in parsedDays)
      {
        var newDate = new WorldDate(Game1.year, Game1.season, dayOfMonth);
        newDate.TotalDays += offsetDays;

        if (Game1.Date <= newDate)
        {
          dates.Add(newDate);
        }
      }
    }

    return dates;
  }

  private static string RequirementsGenerator_DayOfMonth(
    string joinedQueryString,
    GameStateQuery.ParsedGameStateQuery query
  )
  {
    HashSet<int> parsedDays = ParseDaysFromQuery(query);
    string parsedDaysStr = parsedDays.Count == 0 ? "???" : string.Join(", ", parsedDays.OrderBy(n => n));

    return $"{I18n.Days()} {parsedDaysStr}";
  }

  private static HashSet<int> ParseDaysFromQuery(GameStateQuery.ParsedGameStateQuery query)
  {
    HashSet<int> days = query.Negated ? new HashSet<int>(Enumerable.Range(1, 28)) : new HashSet<int>();
    HashSet<int> evenNumbers = Enumerable.Range(1, 28).Where(x => x % 2 == 0).ToHashSet();
    HashSet<int> oddNumbers = Enumerable.Range(1, 28).Where(x => x % 2 != 0).ToHashSet();

    foreach (string dayStr in query.Query.Skip(1))
    {
      if (string.Equals(dayStr, "even", StringComparison.OrdinalIgnoreCase))
      {
        UpdateDays(evenNumbers, query.Negated);
      }
      else if (string.Equals(dayStr, "odd", StringComparison.OrdinalIgnoreCase))
      {
        UpdateDays(oddNumbers, query.Negated);
      }
      else if (int.TryParse(dayStr, out int parsedInt))
      {
        if (query.Negated)
        {
          days.Remove(parsedInt);
        }
        else
        {
          days.Add(parsedInt);
        }
      }
    }

    return days;

    void UpdateDays(HashSet<int> daysSet, bool remove)
    {
      if (remove)
      {
        days.ExceptWith(daysSet);
      }
      else
      {
        days.UnionWith(daysSet);
      }
    }
  }
}
