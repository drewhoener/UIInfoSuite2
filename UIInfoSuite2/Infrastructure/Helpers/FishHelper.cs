using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Locations;
using StardewValley.Tools;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers;

public record WaterTileCacheData(int X, int Y, int WaterDepth)
{
  public Vector2 BobberTile => new(X, Y);
}

public class FishingInformationCache
{
  private readonly Dictionary<string, FishSpawnInfo> _fishInfo = new();
  private readonly HashSet<WaterTileCacheData> _waterTileData = new();
  public int TotalTiles { get; set; }
  public int WaterTileCount => _waterTileData.Count;
  public IEnumerable<WaterTileCacheData> WaterTiles => _waterTileData.AsEnumerable();
  public IEnumerable<FishSpawnInfo> FishInfo => _fishInfo.Values.AsEnumerable();

  public void AddWaterTile(WaterTileCacheData waterTileCacheData)
  {
    _waterTileData.Add(waterTileCacheData);
  }

  public FishSpawnInfo GetOrCreateFishInfo(SpawnFishData data)
  {
    FishSpawnInfo info;
    if (_fishInfo.TryGetValue(data.Id, out FishSpawnInfo? foundFishInfo))
    {
      info = foundFishInfo;
    }
    else
    {
      info = new FishSpawnInfoFromData(data);
      _fishInfo[data.Id] = info;
    }

    return info;
  }
}

public class FishHelper
{
  private const int SimulationsPerTile = 1;

  private static readonly PerScreen<Dictionary<GameLocation, FishingInformationCache>> FishingLocationsInfo =
    new(() => new Dictionary<GameLocation, FishingInformationCache>());

  private static readonly Dictionary<string, string[]> SplitFishArrays = new();

  private static readonly int[] ValidMineFishingAreas = { 0, 10, 40, 80 };
  private static readonly Profiler WaterCacheProfiler = new("WaterCache", 100, 1, s => ModEntry.MonitorObject.Log(s));

  private static readonly Profiler FishSimulationProfiler = new(
    "FishSimulation",
    1,
    1,
    s => ModEntry.MonitorObject.Log(s)
  );

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

  /*  Water Stuff  */

  public static void PopulateWaterCacheForLocation(GameLocation location)
  {
    if (location.waterTiles == null)
    {
      ModEntry.LogExDebug($"Not making water cache for location {location.Name} since it doesn't have any water tiles");
      return;
    }

    if (location is MineShaft mineShaft && !ValidMineFishingAreas.Contains(mineShaft.getMineArea()))
    {
      ModEntry.LogExDebug($"Not making water cache for location {location.Name} since it's the wrong mine floor");
      return;
    }

    (FishingInformationCache fishingInformationCache, bool wasCreated) = GetOrCreateFishingCacheRaw(location);
    if (!wasCreated && fishingInformationCache.TotalTiles == location.waterTiles.waterTiles.Length)
    {
      ModEntry.LogExDebug($"Not making water cache for location {location.Name}, tile count hasn't changed");
      return;
    }

    fishingInformationCache.TotalTiles = location.waterTiles.waterTiles.Length;
    int lengthBefore = fishingInformationCache.WaterTileCount;

    ModEntry.LogExDebug($"Creating water cache for location {location.Name}");
    WaterCacheProfiler.Start();

    for (var i = 0; i < location.waterTiles.waterTiles.GetLength(0); i++)
    {
      for (var j = 0; j < location.waterTiles.waterTiles.GetLength(1); j++)
      {
        WaterTiles.WaterTileData tile = location.waterTiles.waterTiles[i, j];
        if (!tile.isVisible || !tile.isWater)
        {
          continue;
        }

        int distanceToLand = FishingRod.distanceToLand(i, j, location);
        if (distanceToLand > 8)
        {
          continue;
        }

        fishingInformationCache.AddWaterTile(new WaterTileCacheData(i, j, distanceToLand));
      }
    }

    ModEntry.LogExDebug(
      $"\tDone with water cache for location {location.Name}. {fishingInformationCache.WaterTileCount - lengthBefore} entries added."
    );
    WaterCacheProfiler.Stop();
  }

  /*  Fish Stuff  */


  public static void SimulateFishingAtGameLocation(
    GameLocation location,
    Farmer? farmer = null,
    int numberOfIterations = SimulationsPerTile
  )
  {
    FishSimulationProfiler.Start();
    (FishingInformationCache fishingInformationCache, bool wasCreated) = GetOrCreateFishingCacheRaw(location);

    if (wasCreated)
    {
      return;
    }

    Farmer who = farmer ?? Game1.player;
    foreach (WaterTileCacheData waterTileData in fishingInformationCache.WaterTiles)
    {
      Vector2 bobberTile = waterTileData.BobberTile;
      bool isTutorialCatch = who.fishCaught.Length == 0;
      for (var i = 0; i < numberOfIterations; i++)
      {
        GameLocation.GetFishFromLocationData(
          location.Name,
          bobberTile,
          waterTileData.WaterDepth,
          who,
          isTutorialCatch,
          false,
          location
        );
      }
    }

    FishSimulationProfiler.Stop();
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

  public static bool CouldFishSpawnAtLocation(GameLocation location, SpawnFishData data)
  {
    FishSpawnInfo fishInfo = GetOrCreateFishInfo(location, data);
    return fishInfo.CouldSpawn;
  }

  public static IOrderedEnumerable<FishSpawnInfo> GetCatchableFish(GameLocation location)
  {
    return GetOrCreateFishingCache(location).FishInfo.Where(item => item.CouldSpawn).OrderBy(fish => fish.Precedence);
  }

  public static void ParseFailedQuery(FishSpawnInfo info, string[] query)
  {
    ModEntry.MonitorObject.Log($"{info.GetDisplayName()} failed check {string.Join(" ", query)}");
    if (query.Length < 2)
    {
      info.AddBlockedReason(FishSpawnBlockedReason.WrongGameState);
      return;
    }

    switch (query[0])
    {
      default:
        info.AddBlockedReason(FishSpawnBlockedReason.WrongGameState);
        break;
    }
  }
}
