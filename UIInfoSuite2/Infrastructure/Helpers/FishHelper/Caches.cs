using System.Linq;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Locations;
using StardewValley.Tools;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.FishHelper;

public partial class FishHelper
{
  /****************************/
  /*     Game Data Caches     */
  /****************************/
  public static string[] GetSplitFishArray(string fishData)
  {
    string[] data;
    if (SplitFishArrays.TryGetValue(fishData, out string[]? foundDataArr))
    {
      data = foundDataArr;
    }
    else
    {
      data = fishData.Split('/');
      SplitFishArrays[fishData] = data;
    }

    return data;
  }

  /*****************************************/
  /*     Fish Spawn Data Cache Helpers     */
  /*****************************************/

  public static GetOrCreateResult<FishingInformationCache> GetOrCreateFishingCacheRaw(GameLocation location)
  {
    GetOrCreateResult<FishingInformationCache> createResult = FishingLocationsInfo.Value.GetOrCreate(location);
    return createResult;
  }

  public static FishingInformationCache GetOrCreateFishingCache(GameLocation location)
  {
    GetOrCreateResult<FishingInformationCache> createResult = FishingLocationsInfo.Value.GetOrCreate(location);
    return createResult.Result;
  }

  public static FishSpawnInfo GetOrCreateFishInfo(GameLocation location, SpawnFishData data)
  {
    FishingInformationCache fishingInformationCache = GetOrCreateFishingCache(location);
    FishSpawnInfo fishInfo = fishingInformationCache.GetOrCreateFishInfo(data);

    return fishInfo;
  }

  public static void SetFishEntryPickedChance(GameLocation location, SpawnFishData spawnFishData, float chance)
  {
    FishSpawnInfo fishInfo = GetOrCreateFishInfo(location, spawnFishData);
    fishInfo.EntryPickedChance = chance;
  }

  public static void ResetSpawnChecks(GameLocation location)
  {
    FishingInformationCache locationInfo = GetOrCreateFishingCache(location);
    foreach (FishSpawnInfo fishSpawnInfo in locationInfo.FishInfo)
    {
      fishSpawnInfo.ResetSpawnChecks();
    }
  }

  public static void ResetStatistics(GameLocation location)
  {
    FishingInformationCache fishingCache = GetOrCreateFishingCache(location);
    foreach (FishSpawnInfo fishSpawnInfo in fishingCache.FishInfo)
    {
      fishSpawnInfo.ResetProbabilities();
    }

    fishingCache.ResetVarianceAvg();
  }

  public static bool CouldFishSpawnAtLocation(GameLocation location, SpawnFishData data)
  {
    FishSpawnInfo fishInfo = GetOrCreateFishInfo(location, data);
    return fishInfo.CouldSpawn;
  }

  public static IOrderedEnumerable<FishSpawnInfo> GetCatchableFishDisplayOrder(GameLocation location)
  {
    return GetOrCreateFishingCache(location).CatchableFishDisplayOrder;
  }

  /************************************/
  /*     Water Tile Cache Helpers     */
  /************************************/

  public static void PopulateWaterCacheForLocation(GameLocation location)
  {
    if (location is MineShaft mineShaft && !ValidMineFishingAreas.Contains(mineShaft.getMineArea()))
    {
      ModEntry.LogExDebug($"Not making water cache for location {location.Name} since it's the wrong mine floor");
      return;
    }

    (FishingInformationCache fishingInformationCache, bool wasCreated) = GetOrCreateFishingCacheRaw(location);
    if (!wasCreated && fishingInformationCache.WaterDataInitialized)
    {
      ModEntry.LogExDebug($"Not making water cache for location {location.Name}, data is already initialized.");
      return;
    }

    ModEntry.LogExDebug($"Creating water cache for location {location.Name}");
    WaterCacheProfiler.Start();

    fishingInformationCache.InitializeWaterTileData();

    if (location.IsOutdoors ||
        location.HasMapPropertyWithValue("indoorWater") ||
        location is Sewer ||
        location is Submarine)
    {
      for (var i = 0; i < location.map.Layers[0].LayerWidth; ++i)
      {
        for (var j = 0; j < location.map.Layers[0].LayerHeight; ++j)
        {
          string str = location.doesTileHaveProperty(i, j, "Water", "Back");
          if (str is null or "I")
          {
            continue;
          } // Skip invisible water tiles

          int distanceToLand = FishingRod.distanceToLand(i, j, location);
          if (distanceToLand > 8)
          {
            continue;
          }

          fishingInformationCache.AddWaterTile(new WaterTileCacheData(i, j, distanceToLand));
        }
      }
    }

    ModEntry.LogExDebug(
      $"\tDone with water cache for location {location.Name}. {fishingInformationCache.WaterTileCount} entries added."
    );
    WaterCacheProfiler.Stop();
  }
}
