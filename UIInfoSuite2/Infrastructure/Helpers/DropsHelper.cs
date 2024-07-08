using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FruitTrees;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Helpers;

public record DropInfo(string? Condition, float Chance, string ItemId)
{
  public int? GetNextDay(bool includeToday)
  {
    return DropsHelper.GetNextDay(Condition, includeToday);
  }
}

internal record PossibleDroppedItem(
  ConditionFutureResult FutureHarvestDates,
  ParsedItemData Item,
  float Chance,
  string? CustomId = null
)
{
  public bool ReadyToPick => FutureHarvestDates.HasDate(Game1.Date);
}

internal record FruitTreeInfo(string TreeName, List<PossibleDroppedItem> Items);

internal static class DropsHelper
{
  private static readonly Dictionary<string, string> CropNamesCache = new();

  public static int? GetNextDay(string? condition, bool includeToday)
  {
    return string.IsNullOrEmpty(condition)
      ? Game1.dayOfMonth + (includeToday ? 0 : 1)
      : Tools.GetNextDayFromCondition(condition, includeToday);
  }

  public static int? GetLastDay(string? condition)
  {
    return Tools.GetLastDayFromCondition(condition);
  }

  public static string GetCropHarvestName(Crop crop)
  {
    if (crop.indexOfHarvest.Value is null)
    {
      return "Unknown Crop";
    }

    // If you look at Crop.cs in the decompiled sources, it seems that there's a special case for spring onions - that's what the =="1" is about.
    string itemId = crop.isWildSeedCrop() ? crop.whichForageCrop.Value : crop.indexOfHarvest.Value;
    if (crop.whichForageCrop.Value == "1")
    {
      itemId = "399";
    }

    if (CropNamesCache.TryGetValue(itemId, out string? harvestName))
    {
      return harvestName;
    }

    // Technically has the best compatibility for looking up items vs ItemRegistry.
    harvestName = new Object(itemId, 1).DisplayName;
    CropNamesCache.Add(itemId, harvestName);

    return harvestName;
  }

  public static List<PossibleDroppedItem> GetFruitTreeDropItems(FruitTree tree)
  {
    FruitTreeData? treeData = tree.GetData();
    return GetGenericDropItems(treeData.Fruit, null, "Fruit Tree", FruitTreeDropConverter);

    DropInfo FruitTreeDropConverter(FruitTreeFruitData input)
    {
      List<string> conditions = new();
      conditions.AddIfNotNull(input.Condition);

      if (input.Season.HasValue)
      {
        conditions.Add($"SEASON {Utility.getSeasonKey(input.Season.Value)}");
      }
      else if (treeData.Seasons.Count != 0)
      {
        var seasonsCondition = $"SEASON {string.Join(' ', treeData.Seasons.Select(Utility.getSeasonKey))}";
        conditions.Add(seasonsCondition);
      }

      return new DropInfo(string.Join(", ", conditions), input.Chance, input.ItemId);
    }
  }

  public static FruitTreeInfo GetFruitTreeInfo(FruitTree tree)
  {
    var name = "Fruit Tree";
    List<PossibleDroppedItem> drops = GetFruitTreeDropItems(tree);
    if (drops.Count == 1)
    {
      name = $"{drops[0].Item.DisplayName}{I18n.Tree()}";
    }

    return new FruitTreeInfo(name, drops);
  }

  public static List<PossibleDroppedItem> GetGenericDropItems<T>(
    IEnumerable<T> drops,
    string? customId,
    string displayName,
    Func<T, DropInfo> extractDropInfo,
    bool independentRolls = false
  )
  {
    List<PossibleDroppedItem> items = new();

    foreach (T drop in drops)
    {
      DropInfo dropInfo = extractDropInfo(drop);
      ConditionFutureResult validDays = GameStateHelper.ResolveQueryFuture(dropInfo.Condition ?? "");
      string nextDayStr = validDays.GetNextDate()?.ToString() ?? "No Next Date";

      if (!validDays.ErroredConditions.IsEmpty())
      {
        foreach (string erroredCondition in validDays.ErroredConditions)
        {
          ModEntry.MonitorObject.LogOnce(
            $"Couldn't parse the next day the {displayName} will drop {dropInfo.ItemId}. Condition: {erroredCondition}. Please report this error.",
            LogLevel.Error
          );
        }

        continue;
      }

      ParsedItemData? itemData = ItemRegistry.GetData(dropInfo.ItemId);
      if (itemData == null)
      {
        ModEntry.MonitorObject.Log(
          $"Couldn't parse the correct item {displayName} will drop. ItemId: {dropInfo.ItemId}. Please report this error.",
          LogLevel.Error
        );
        continue;
      }

      items.Add(new PossibleDroppedItem(validDays, itemData, dropInfo.Chance, customId));
    }

    return items;
  }
}
