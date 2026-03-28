using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers;

using BundleIngredientsCache = Dictionary<string, List<List<int>>>;

public record BundleRequiredItem(BundleData Bundle, BundleItem ItemData);

public record BundleReward(string ItemType, string UnqualifiedId, int Count)
{
  public static BundleReward? FromDataString(string data)
  {
    string[] pieces = ArgUtility.SplitBySpace(data);


    return pieces.Length == 3 ? new BundleReward(pieces[0], pieces[1], Convert.ToInt32(pieces[2])) : null;
  }
}

/// <summary>
/// </summary>
/// <param name="BundleId">The ID of the bundle, for looking up elsewhere</param>
/// <param name="ItemId">
///   Could be qualified or unqualified depending on the data source. An id of `-1` usually represents
///   money (like for the Vault bundle)
/// </param>
/// <param name="Count"></param>
/// <param name="MinQuality"></param>
public record BundleItem(int BundleId, string ItemId, int Count, int MinQuality);

public record TextureOverride(string TextureName, int SpriteIndex)
{
  public static TextureOverride? FromDataString(string s)
  {
    string[] parts = s.Split(':');
    return parts.Length != 2 ? null : new TextureOverride(parts[0], Convert.ToInt32(parts[1]));
  }
}

public record BundleData(
  string RoomName,
  int BundleId,
  string InternalName,
  BundleReward? Reward,
  List<BundleItem> RequirementOptions,
  int BundleColorId,
  int BundleSlots,
  TextureOverride? TextureOverride,
  string DisplayName
)
{
  public static bool FromDataString(string key, string data, [NotNullWhen(true)] out BundleData? bundleData)
  {
    bundleData = null;
    try
    {
      string[] bundleKeyData = key.Split('/');
      var bundleIdx = Convert.ToInt32(bundleKeyData[1]);
      string[] bundleContentsData = data.Split('/');

      List<BundleItem> requiredItems = [];
      string[]? requiredItemParts = ArgUtility.SplitBySpace(bundleContentsData[2]);
      if (requiredItemParts.Length % 3 != 0)
      {
        throw new ArgumentException("Invalid bundle requirement data, expected groups of 3");
      }

      for (var i = 0; i < requiredItemParts.Length; i += 3)
      {
        requiredItems.Add(
          new BundleItem(
            bundleIdx,
            requiredItemParts[i],
            Convert.ToInt32(requiredItemParts[i + 1]),
            Convert.ToInt32(requiredItemParts[i + 2])
          )
        );
      }

      bundleData = new BundleData(
        bundleKeyData[0],
        bundleIdx,
        bundleContentsData[0],
        BundleReward.FromDataString(bundleContentsData[1]),
        requiredItems,
        ParseOrDefault(() => Convert.ToInt32(bundleContentsData[3]), 0),
        ParseOrDefault(() => Convert.ToInt32(bundleContentsData[4]), requiredItems.Count),
        TextureOverride.FromDataString(bundleContentsData[5]),
        bundleContentsData[6]
      );
    }
    catch (Exception)
    {
      ModEntry.Instance.Monitor.LogOnce(
        $"Failed to parse info for bundle {key}, some information may be unavailable",
        LogLevel.Error
      );
      return false;
    }

    return true;

    T ParseOrDefault<T>(Func<T> parseFunc, T defaultValue)
    {
      try
      {
        return parseFunc();
      }
      catch (Exception)
      {
        return defaultValue;
      }
    }
  }

  public Texture2D GetTexture()
  {
    string textureName = TextureOverride?.TextureName ?? "LooseSprites\\JunimoNote";
    return Game1.content.Load<Texture2D>(textureName);
  }

  public Rectangle GetSourceRect()
  {
    // Grabbed from Bundle.cs in the Stardew source
    return new Rectangle(BundleColorId * 256 % 512, 244 + BundleColorId * 256 / 512 * 16, 16, 16);
  }

  public Color RealColor()
  {
    return Bundle.getColorFromColorIndex(BundleColorId);
  }
}

internal class BundleHelper
{
  private readonly Dictionary<int, BundleData> _bundleDataMap = new();
  private readonly Dictionary<string, BundleData> _bundleDataParseCache = new();
  private readonly IMonitor _logger;
  private readonly IReflectionHelper _reflectionHelper;
  private readonly Dictionary<string, List<BundleItem>> _requiredBundleItems = new();

  public BundleHelper(IMonitor logger, IReflectionHelper reflectionHelper)
  {
    _logger = logger;
    _reflectionHelper = reflectionHelper;
  }

  public void LoadBundleDataMap()
  {
    var bundlesParsed = 0;
    var bundlesFromCache = 0;
    _bundleDataMap.Clear();
    Game1.netWorldState.Value.UpdateBundleDisplayNames();

    Dictionary<string, string>? gameData = Game1.netWorldState.Value.BundleData;
    int totalBundles = gameData.Count;
    foreach ((string bundleKey, string bundleInfo) in gameData)
    {
      var cacheKey = $"{bundleKey}/{bundleInfo}";
      if (_bundleDataParseCache.TryGetValue(cacheKey, out BundleData? data))
      {
        _bundleDataMap[data.BundleId] = data;
        bundlesFromCache++;
        continue;
      }

      // ReSharper disable once InvertIf
      if (BundleData.FromDataString(bundleKey, bundleInfo, out BundleData? bundleData))
      {
        bundlesParsed++;
        _bundleDataMap[bundleData.BundleId] = bundleData;
        _bundleDataParseCache[cacheKey] = bundleData;
      }
    }

    _logger.Log(
      $"Processed {totalBundles} bundles from game data, {bundlesParsed} parsed, {bundlesFromCache} from cache"
    );
  }

  public void SyncBundleInformation()
  {
    LoadBundleDataMap();

    if (!GetCommunityCenterRequiredItems(out BundleIngredientsCache? bundlesIngredientsInfo))
    {
      return;
    }

    _requiredBundleItems.Clear();
    foreach ((string key, List<List<int>> bundlesRequiringItem) in bundlesIngredientsInfo)
    {
      _requiredBundleItems.GetOrCreate(key)
        .AddRange(bundlesRequiringItem.Select(infoList => new BundleItem(infoList[0], key, infoList[1], infoList[2])));
    }
  }

  private bool GetCommunityCenterRequiredItems([NotNullWhen(true)] out BundleIngredientsCache? bundlesIngredientsInfo)
  {
    bundlesIngredientsInfo = null;

    try
    {
      var communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
      communityCenter.refreshBundlesIngredientsInfo();
      IReflectedField<BundleIngredientsCache> bundlesIngredientsInfoField =
        _reflectionHelper.GetField<BundleIngredientsCache>(communityCenter, "bundlesIngredientsInfo");
      bundlesIngredientsInfo = bundlesIngredientsInfoField.GetValue();
      return true;
    }
    catch (Exception e)
    {
      _logger.Log("Failed to get bundles info", LogLevel.Error);
      _logger.Log(e.ToString(), LogLevel.Error);
      return false;
    }
  }

  public List<BundleRequiredItem> BundlesRequiringItem(Item item)
  {
    List<BundleRequiredItem> output = [];
    if (_requiredBundleItems.TryGetValue(item.QualifiedItemId, out List<BundleItem>? bundleItems))
    {
      output = bundleItems.Select(bundleItem => new BundleRequiredItem(_bundleDataMap[bundleItem.BundleId], bundleItem))
        .ToList();
    }

    return output;
  }
}
