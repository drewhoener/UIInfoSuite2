using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;
using StardewValley.GameData.Locations;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Containers;

public class FishingInformationCache
{
  /******************/
  /*     Caches     */
  /******************/

  private readonly MovingAverage _fishChanceVariance = new();
  private readonly Dictionary<string, FishSpawnInfo> _fishInfo = new();
  private readonly HashSet<WaterTileCacheData> _waterTileData = new();
  private int _catchChanceActionsQueued;
  private int _lastCatchableFishCount;

  /******************************/
  /*     Enumerable Helpers     */
  /******************************/
  public IEnumerable<WaterTileCacheData> WaterTiles => _waterTileData.AsEnumerable();
  public IEnumerable<FishSpawnInfo> FishInfo => _fishInfo.Values.AsEnumerable();

  public IEnumerable<FishSpawnInfo> CatchableFishUnordered => FishInfo.Where(item => item.CouldSpawn);

  public IOrderedEnumerable<FishSpawnInfo> CatchableFishDisplayOrder
  {
    get { return CatchableFishUnordered.OrderBy(fish => fish.Precedence).ThenBy(fish => fish.DisplayName); }
  }

  public IOrderedEnumerable<FishSpawnInfo> CatchableFishRandomOrder
  {
    get { return CatchableFishUnordered.OrderBy(fish => fish.Precedence).ThenBy(_ => Game1.random.Next()); }
  }

  /**************************************/
  /*     Internal Getters / Setters     */
  /**************************************/

  public int CatchChanceActionsQueued
  {
    get => _catchChanceActionsQueued;
    set => _catchChanceActionsQueued = Math.Max(value, 0);
  }

  public bool HasCatchChanceActionsQueued => CatchChanceActionsQueued > 0;

  public bool HasExplicitActionsQueued => CatchChanceActionsQueued > 0;

  public bool WaterDataInitialized { get; private set; }

  public int WaterTileCount => _waterTileData.Count;

  public int CatchableFishCount => CatchableFishUnordered.Count();

  /**************************/
  /*     Helper Methods     */
  /**************************/

  public void UpdateCatchableCount()
  {
    _lastCatchableFishCount = CatchableFishCount;
  }

  public void InitializeWaterTileData()
  {
    WaterDataInitialized = true;
  }

  public void AddWaterTile(WaterTileCacheData waterTileCacheData)
  {
    _waterTileData.Add(waterTileCacheData);
  }

  public IEnumerable<WaterTileCacheData> GetRandomWaterDataSample(int size)
  {
    return WaterTiles.Shuffle().Take(size);
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

  public void AddVarianceData(double variance)
  {
    _fishChanceVariance.AddValue(variance);
  }

  public bool FishingChancesConverged()
  {
    return _fishChanceVariance is { Count: > 1200, Variance: < 0.0002 };
  }

  public bool FishCountHasChanged()
  {
    return CatchableFishCount != _lastCatchableFishCount;
  }

  public void ResetVarianceAvg()
  {
    _fishChanceVariance.Reset();
  }
}
