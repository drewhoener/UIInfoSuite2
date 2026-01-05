using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;

namespace UIInfoSuite2.Compatibility.CustomBush;

internal static class CustomBushExtensions
{
  private static readonly Lazy<ApiManager> ApiManagerLazy = ModEntry.LazyGetSingleton<ApiManager>();
  private static readonly Lazy<DropsHelper> DropsHelperLazy = ModEntry.LazyGetSingleton<DropsHelper>();

  private static ICustomBushApi? CustomBushApi
  {
    get
    {
      ApiManagerLazy.Value.GetApi(ModCompat.CustomBush, out ICustomBushApi? customBushApi);
      return customBushApi;
    }
  }

  private static DropsHelper DropsHelper => DropsHelperLazy.Value;

  public static bool IsCustomBush(this Bush? bush)
  {
    ICustomBushApi? customBushApi = CustomBushApi;
    if (customBushApi is null || bush is null)
    {
      return false;
    }

    return customBushApi.IsCustomBush(bush);
  }

  public static bool GetShakeOffItemIfReady(
    this ICustomBushData customBush,
    Bush bush,
    [NotNullWhen(true)] out Item? item
  )
  {
    item = null;
    ICustomBushApi? customBushApi = CustomBushApi;
    if (customBushApi == null)
    {
      return false;
    }

    return bush.size.Value == Bush.greenTeaBush && customBushApi.TryGetShakeOffItem(bush, out item);
  }

  public static List<PossibleDroppedItem> GetCustomBushDropItems(this Bush? bush)
  {
    ICustomBushApi? customBushApi = CustomBushApi;
    if (bush is null ||
        customBushApi is null ||
        !bush.IsCustomBush() ||
        !customBushApi.TryGetBush(bush, out ICustomBushData? data, out string? id))
    {
      return [];
    }

    return customBushApi.GetCustomBushDropItems(data, id);
  }

  public static List<PossibleDroppedItem> GetCustomBushDropItems(
    this ICustomBushApi api,
    ICustomBushData bush,
    string? id
  )
  {
    if (id == null || string.IsNullOrEmpty(id) || !api.TryGetDrops(id, out IList<ICustomBushDrop>? drops))
    {
      return [];
    }

    return DropsHelper.GetGenericDropItems(drops, id, bush.DisplayName, BushDropConverter);

    DropInfo BushDropConverter(ICustomBushDrop input)
    {
      // TODO Duplicated Code Fruit Tree
      List<string> conditions = [];
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
