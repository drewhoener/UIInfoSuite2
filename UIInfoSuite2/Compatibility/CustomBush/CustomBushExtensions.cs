using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;

namespace UIInfoSuite2.Compatibility.CustomBush;

internal static class CustomBushExtensions
{
  private const string ShakeOffItem = $"{ModCompat.CustomBush}/ShakeOff";

  public static bool GetShakeOffItemIfReady(
    this ICustomBush customBush,
    Bush bush,
    [NotNullWhen(true)] out ParsedItemData? item
  )
  {
    item = null;
    if (bush.size.Value != Bush.greenTeaBush)
    {
      return false;
    }

    if (!bush.modData.TryGetValue(ShakeOffItem, out string itemId))
    {
      return false;
    }

    item = ItemRegistry.GetData(itemId);
    return true;
  }

  public static List<PossibleDroppedItem> GetCustomBushDropItems(
    this ICustomBushApi api,
    ICustomBush bush,
    string? id
  )
  {
    if (id == null || string.IsNullOrEmpty(id))
    {
      return new List<PossibleDroppedItem>();
    }

    api.TryGetDrops(id, out IList<ICustomBushDrop>? drops);
    return drops == null
      ? new List<PossibleDroppedItem>()
      : DropsHelper.GetGenericDropItems(drops, id, bush.DisplayName, BushDropConverter);

    DropInfo BushDropConverter(ICustomBushDrop input)
    {
      // TODO Duplicated Code Fruit Tree
      List<string> conditions = new();
      conditions.AddIfNotNull(input.Condition);

      if (input.Season.HasValue)
      {
        conditions.Add($"SEASON {Utility.getSeasonKey(input.Season.Value)}");
      }
      else if (bush.Seasons.Count != 0)
      {
        var seasonsCondition = $"SEASON {string.Join(' ', bush.Seasons.Select(Utility.getSeasonKey))}";
        conditions.Add(seasonsCondition);
      }

      return new DropInfo(string.Join(", ", conditions), input.Chance, input.ItemId);
    }
  }
}
