using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;
using StardewValley.Extensions;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;

internal class ConditionFutureResult
{
  public static readonly ConditionFutureResult Empty = FromDays();
  private readonly ISet<WorldDate> _validDates = new HashSet<WorldDate>();
  private bool _shouldIntersect;

  public ConditionFutureResult() { }

  public ConditionFutureResult(ISet<WorldDate> validDates)
  {
    _validDates = validDates;
    _shouldIntersect = true;
  }

  public ISet<string> ErroredConditions { get; } = new HashSet<string>();
  public ISet<string> FailingConditions { get; } = new HashSet<string>();
  public ISet<string> SuccessfulConditions { get; } = new HashSet<string>();

  public bool HasResolvedDate => _validDates.Count > 0 && !FailingConditions.IsEmpty() && !ErroredConditions.IsEmpty();

  public static ConditionFutureResult FromDays(params WorldDate[] days)
  {
    return new ConditionFutureResult(days.ToHashSet());
  }

  public static ConditionFutureResult Today()
  {
    var today = new WorldDate(Game1.Date);
    return FromDays(today);
  }

  public static ConditionFutureResult Tomorrow()
  {
    var tomorrow = new WorldDate(Game1.Date);
    tomorrow.TotalDays += 1;
    return FromDays(tomorrow);
  }

  public static ConditionFutureResult TodayAndTomorrow()
  {
    var today = new WorldDate(Game1.Date);
    var tomorrow = new WorldDate(Game1.Date);
    tomorrow.TotalDays += 1;
    return FromDays(today, tomorrow);
  }


  public void AddErroredCondition(string conditionStr)
  {
    ErroredConditions.Add(conditionStr);
  }

  public bool HasDate(WorldDate date)
  {
    return _validDates.Contains(date);
  }

  [MemberNotNullWhen(true, "HasResolvedDate")]
  public WorldDate? GetNextDate(bool shouldIncludeToday = true)
  {
    IEnumerable<WorldDate> datesEnumerable = _validDates;
    if (!shouldIncludeToday)
    {
      datesEnumerable = datesEnumerable.Where(date => Game1.Date != date);
    }

    return datesEnumerable.MinBy(date => date.TotalDays);
  }

  public WorldDate? GetLastDateInSeason(bool shouldIncludeToday = true)
  {
    return GetLastDateInSeason(Game1.year, shouldIncludeToday);
  }

  public WorldDate? GetLastDateInSeason(int year, bool shouldIncludeToday = true)
  {
    IEnumerable<WorldDate> datesEnumerable = _validDates;
    if (!shouldIncludeToday)
    {
      datesEnumerable = datesEnumerable.Where(date => Game1.Date != date);
    }

    return datesEnumerable.Where(date => date.Year == year && date.Season == Game1.season)
      .MaxBy(date => date.TotalDays);
  }

  /// <summary>
  ///   Intersects this result with another
  /// </summary>
  /// <param name="other"></param>
  public void MergeConditionResult(ConditionFutureResult other)
  {
    SuccessfulConditions.AddRange(other.SuccessfulConditions);
    FailingConditions.AddRange(other.FailingConditions);
    ErroredConditions.AddRange(other.ErroredConditions);

    if (_shouldIntersect)
    {
      _validDates.IntersectWith(other._validDates);
    }
    else
    {
      _validDates.AddRange(other._validDates);
      _shouldIntersect = true;
    }
  }

  public void AddSuccessfulCondition(string conditionStr)
  {
    SuccessfulConditions.Add(conditionStr);
  }

  public void AddFailedCondition(string conditionStr)
  {
    FailingConditions.Add(conditionStr);
  }

  public void AddConditionStatus(string conditionResolverQueryStr, bool success)
  {
    if (success)
    {
      AddSuccessfulCondition(conditionResolverQueryStr);
    }
    else
    {
      AddFailedCondition(conditionResolverQueryStr);
    }
  }
}
