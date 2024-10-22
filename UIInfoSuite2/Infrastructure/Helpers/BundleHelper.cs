using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Helpers;

using BundleIngredientsCache = Dictionary<string, List<List<int>>>;

public record BundleRequiredItem(string Name, int BannerWidth, int Id, string QualifiedId, int Quality);

public record BundleKeyData(string Name, int Color);

internal class BundleHelper
{
  private readonly Dictionary<int, BundleKeyData> _bundleIdToBundleKeyDataMap = new();
  private readonly IMonitor _logger;
  private readonly IReflectionHelper _reflectionHelper;

  public BundleHelper(IMonitor logger, IReflectionHelper reflectionHelper)
  {
    _logger = logger;
    _reflectionHelper = reflectionHelper;
  }

  public BundleKeyData? GetBundleKeyDataFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleNameMappings(forceRefresh);
    return _bundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx);
  }

  public int GetBundleColorFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleNameMappings(forceRefresh);
    return _bundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx)?.Color ?? 0;
  }

  public Color? GetRealColorFromIndex(int bundleIdx, bool forceRefresh = false)
  {
    PopulateBundleNameMappings(forceRefresh);
    BundleKeyData? bundleData = _bundleIdToBundleKeyDataMap.GetValueOrDefault(bundleIdx);
    if (bundleData == null)
    {
      return null;
    }

    return Bundle.getColorFromColorIndex(bundleData.Color);
  }

  private static int GetBundleBannerWidthForName(string bundleName)
  {
    return 68 + (int)Game1.dialogueFont.MeasureString(bundleName).X;
  }

  public BundleRequiredItem? GetBundleItemIfNotDonated(Item item)
  {
    if (item is not SObject donatedItem || donatedItem.bigCraftable.Value)
    {
      return null;
    }

    var communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");

    BundleIngredientsCache bundlesIngredientsInfo;
    try
    {
      IReflectedField<BundleIngredientsCache> bundlesIngredientsInfoField =
        _reflectionHelper.GetField<BundleIngredientsCache>(communityCenter, "bundlesIngredientsInfo");
      bundlesIngredientsInfo = bundlesIngredientsInfoField.GetValue();
    }
    catch (Exception e)
    {
      _logger.Log("Failed to get bundles info", LogLevel.Error);
      _logger.Log(e.ToString(), LogLevel.Error);
      return null;
    }


    BundleRequiredItem? output;
    List<List<int>>? bundleRequiredItemsList;

    if (bundlesIngredientsInfo.TryGetValue(donatedItem.QualifiedItemId, out bundleRequiredItemsList))
    {
      output = GetBundleItemIfNotDonatedFromList(bundleRequiredItemsList, donatedItem);
      if (output != null)
      {
        return output;
      }
    }

    if (donatedItem.Category >= 0 ||
        !bundlesIngredientsInfo.TryGetValue(donatedItem.Category.ToString(), out bundleRequiredItemsList))
    {
      return null;
    }

    output = GetBundleItemIfNotDonatedFromList(bundleRequiredItemsList, donatedItem);
    return output ?? null;
  }

  private BundleRequiredItem? GetBundleItemIfNotDonatedFromList(List<List<int>>? lists, ISalable obj)
  {
    if (lists == null)
    {
      return null;
    }

    foreach (List<int> list in lists)
    {
      if (list.Count < 3 || obj.Quality < list[2])
      {
        continue;
      }

      BundleKeyData? bundleKeyData = GetBundleKeyDataFromIndex(list[0]);
      if (bundleKeyData == null)
      {
        continue;
      }

      return new BundleRequiredItem(
        bundleKeyData.Name,
        GetBundleBannerWidthForName(bundleKeyData.Name),
        list[0],
        obj.QualifiedItemId,
        obj.Quality
      );
    }

    return null;
  }

  public void PopulateBundleNameMappings(bool force = false)
  {
    if (_bundleIdToBundleKeyDataMap.Count != 0 && !force)
    {
      return;
    }

    _bundleIdToBundleKeyDataMap.Clear();
    foreach (KeyValuePair<string, string> bundleInfo in Game1.netWorldState.Value.BundleData)
    {
      try
      {
        string[] bundleLocationInfo = bundleInfo.Key.Split('/');
        var bundleIdx = Convert.ToInt32(bundleLocationInfo[1]);
        string[] bundleContentsData = bundleInfo.Value.Split('/');
        string localizedName = bundleContentsData[6];
        var color = Convert.ToInt32(bundleContentsData[3]);
        _bundleIdToBundleKeyDataMap[bundleIdx] = new BundleKeyData(localizedName, color);
      }
      catch (Exception)
      {
        _logger.Log($"Failed to parse info for bundle {bundleInfo.ToString()}, some information may be unavailable");
      }
    }
  }
}
