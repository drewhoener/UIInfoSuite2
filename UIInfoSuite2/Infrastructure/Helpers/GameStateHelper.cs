using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.Delegates;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Patches;

namespace UIInfoSuite2.Infrastructure.Helpers;

public class GameStateHelper
{
  /// <inheritdoc cref="GameStateQuery.CheckConditions(string, GameLocation, Farmer, Item, Item, Random, HashSet{string})" />
  public static void CheckFishConditions(
    FishSpawnInfo cachedFishInfo,
    GameLocation location,
    HashSet<string>? ignoreQueryKeys
  )
  {
    string? queryString = cachedFishInfo.SpawnData?.Condition;

    switch (queryString)
    {
      case null:
      case "":
      case "TRUE":
        return;
      case "FALSE":
        return;
      default:
        var context = new GameStateQueryContext(location, null, null, null, null, ignoreQueryKeys);
        GameStateQueryPatches.Patched_CheckFishConditionsImpl(queryString, context, cachedFishInfo);
        break;
    }
  }
}
