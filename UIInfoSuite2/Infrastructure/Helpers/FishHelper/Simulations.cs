using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Helpers.FishHelper;

public partial class FishHelper
{
  internal static void UpdateFishForArea(
    // Needed to track fish
    IEnumerable<SpawnFishData> fishDatas,
    // Location
    GameLocation location,
    string fishAreaId,
    bool isInherited,
    // Needed for GSQ
    HashSet<string>? ignoreQueryKeys,
    // Needed for resolver
    Point playerTilePoint,
    Vector2 bobberTile,
    ItemQueryContext queryContext,
    // Needed to check generic requirements
    IReadOnlyDictionary<string, string> allFishData,
    Farmer player,
    int waterDepth,
    bool usingMagicBait,
    bool hasCuriosityLure,
    string curBait,
    bool isTutorialCatch
  )
  {
    ResetSpawnChecks(location);

    var checkGenericRequirementsInvokeArgs = new object[]
    {
      null!, // Reserved for the item we're checking
      allFishData,
      location,
      player,
      null!, // Reserved for spawn data while iterating
      waterDepth,
      usingMagicBait,
      hasCuriosityLure,
      false, // Reserved for isUsingTargetBait while iterating
      isTutorialCatch
    };

    foreach (SpawnFishData data in fishDatas)
    {
      // Spawn Data
      checkGenericRequirementsInvokeArgs[4] = data;
      // Is using Target Bait
      checkGenericRequirementsInvokeArgs[8] = data.ItemId == curBait;

      FishSpawnInfo cachedFishInfo = GetOrCreateFishInfo(location, data);
      cachedFishInfo.PopulateItemsForTile(queryContext, bobberTile, waterDepth);

      if ((isInherited && !data.CanBeInherited) || (data.FishAreaId != null && fishAreaId != data.FishAreaId))
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WrongFishingArea);
        continue;
      }

      Season? fishRequiredSeason = data.Season;
      if (fishRequiredSeason.HasValue && !usingMagicBait)
      {
        Season currentSeason = Game1.GetSeasonForLocation(location);
        if (fishRequiredSeason.GetValueOrDefault() != currentSeason)
        {
          cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WrongSeason);
        }
      }

      cachedFishInfo.CheckBobberAndPlayerPos(playerTilePoint, bobberTile);

      if (player.FishingLevel < data.MinFishingLevel)
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.PlayerLevelTooLow);
      }

      if (waterDepth < data.MinDistanceFromShore)
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WaterTooShallow);
      }

      if (data.MaxDistanceFromShore > -1 && waterDepth > data.MaxDistanceFromShore)
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WaterTooDeep);
      }

      if (data.RequireMagicBait && !usingMagicBait)
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.RequiresMagicBait);
      }

      foreach (Item item in cachedFishInfo.GetItems())
      {
        if (data.CatchLimit >= 0)
        {
          if (player.fishCaught.TryGetValue(item.QualifiedItemId, out int[] numArray))
          {
            if (numArray[0] >= data.CatchLimit)
            {
              cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.OverCatchLimit);
              break;
            }
          }
        }

        if (allFishData.ContainsKey(item.ItemId))
        {
          cachedFishInfo.IsOnlyNonFishItems = false;
        }

        // Set the item in our array of args so we can reuse the same object.
        checkGenericRequirementsInvokeArgs[0] = item;

        // Ignore the result of this method, we patch in the spawn requirement checks there ourselves.
        // And sometimes it just returns false, since it calculates its own spawn pct internally (ugh)
        CheckGenericFishRequirementsMethod.Value.Invoke(null, checkGenericRequirementsInvokeArgs);

        if (data.Condition != null)
        {
          GameStateHelper.CheckFishConditions(cachedFishInfo, location, ignoreQueryKeys);
        }

        // Ok should have been set by CheckGenericFishRequirements already, if it's unknown then we can mark ok
        if (!cachedFishInfo.IsSpawnConditionOnlyUnknown)
        {
          continue;
        }

        cachedFishInfo.SetSpawnAllowed();
        break;
      }
    }
  }

  public static double SimulateActualFishChance(GameLocation? location = null)
  {
    FishingChanceCalcProfiler.Start();
    FishingInformationCache fishingCache = GetOrCreateFishingCache(location ?? Game1.player.currentLocation);

    if (fishingCache.FishCountHasChanged())
    {
      fishingCache.ResetVarianceAvg();
      ModEntry.MonitorObject.Log("Resetting Cache");
      fishingCache.UpdateCatchableCount();
    }


    if (fishingCache.FishingChancesConverged() &&
        !fishingCache.HasCatchChanceActionsQueued &&
        !fishingCache.FishCountHasChanged())
    {
      // ModEntry.MonitorObject.Log("Converged, not updating chances");
      FishingChanceCalcProfiler.Stop();
      return 1.0;
    }

    // ModEntry.MonitorObject.Log($"Convergence: {fishingCache.FishChanceVarianceAvg}");
    // ModEntry.MonitorObject.Log($"Count Changed: {fishingCache.FishCountHasChanged()}");

    fishingCache.CatchChanceActionsQueued--;

    var chanceToNotCatchAcc = 1.0;
    var cumulativeChances = 0.0;
    var reachedEndFlag = false;
    foreach (FishSpawnInfo fish in fishingCache.CatchableFishRandomOrder)
    {
      if (reachedEndFlag)
      {
        fish.AddBlockedReason(FishSpawnBlockedReason.ReachedGuaranteedItem);
        continue;
      }

      double chance;
      if (fish.IsOnlyNonFishItems && ColorExtensions.AlmostEqual(1.0, fish.ChanceToSpawn))
      {
        chance = 1 - cumulativeChances;
        reachedEndFlag = true;
      }
      else
      {
        chance = fish.ChanceToSpawn * chanceToNotCatchAcc;
      }

      fish.ActualHookChance = chance;
      cumulativeChances += chance;
      chanceToNotCatchAcc *= fish.ChanceToNotSpawn;
      fishingCache.AddVarianceData(fish.ActualHookChanceVariance);
    }

    FishingChanceCalcProfiler.Stop();
    return cumulativeChances;
  }

  public static void SimulateNFishingOperations(
    GameLocation location,
    int iterationsPerTile,
    int numberOfTiles = -1,
    Farmer? farmer = null
  )
  {
    (FishingInformationCache fishingInformationCache, bool wasCreated) = GetOrCreateFishingCacheRaw(location);

    if (wasCreated)
    {
      return;
    }

    Farmer who = farmer ?? Game1.player;
    if (numberOfTiles == -1)
    {
      SimulateFishingAtGameLocation(fishingInformationCache.WaterTiles, location, iterationsPerTile, who);
      return;
    }

    IEnumerable<WaterTileCacheData> sampledData = fishingInformationCache.GetRandomWaterDataSample(numberOfTiles);
    SimulateFishingAtGameLocation(sampledData, location, iterationsPerTile, who);
  }

  public static void SimulateFishingAtGameLocation(
    GameLocation location,
    Farmer? farmer = null,
    int numberOfIterations = SimulationsPerTile
  )
  {
    SimulateNFishingOperations(location, numberOfIterations, farmer: farmer);
  }


  public static void SimulateFishingAtGameLocation(
    IEnumerable<WaterTileCacheData> waterDataEnumerable,
    GameLocation location,
    int numberOfIterations,
    Farmer? farmer = null
  )
  {
    FishSimulationProfiler.Start();
    (FishingInformationCache fishingInformationCache, bool wasCreated) = GetOrCreateFishingCacheRaw(location);

    if (wasCreated)
    {
      return;
    }

    Farmer who = farmer ?? Game1.player;
    foreach (WaterTileCacheData waterTileData in waterDataEnumerable)
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

    foreach (FishSpawnInfo fishSpawnInfo in fishingInformationCache.FishInfo)
    {
      fishSpawnInfo.CheckBobberAndPlayerPos(who.TilePoint);
    }

    if (fishingInformationCache.FishCountHasChanged())
    {
      fishingInformationCache.ResetVarianceAvg();
      ModEntry.MonitorObject.Log("Resetting Cache");
    }

    fishingInformationCache.UpdateCatchableCount();
    FishSimulationProfiler.Stop();
  }

  public static void ParseFailedQuery(FishSpawnInfo info, string[] query)
  {
    if (query.Length < 2)
    {
      info.AddBlockedReason(FishSpawnBlockedReason.WrongGameState);
      return;
    }

    switch (query[0])
    {
      case "LOCATION_SEASON":
      case "SEASON": // Fall Through Cases
        info.AddBlockedReason(FishSpawnBlockedReason.WrongSeason);
        break;
      case "TIME":
        info.AddBlockedReason(FishSpawnBlockedReason.WrongTime);
        break;
      case "SEASON_DAY":
      case "DAY_OF_WEEK":
      case "DAY_OF_MONTH": // Fall Through Cases
        info.AddBlockedReason(FishSpawnBlockedReason.WrongDay);
        break;
      default:
        info.AddBlockedReason(FishSpawnBlockedReason.WrongGameState);
        break;
    }
  }
}
