using System;
using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Infrastructure.Containers;

namespace UIInfoSuite2.Infrastructure.Helpers.FishHelper;

public partial class FishHelper
{
  private const int SimulationsPerTile = 1;

  private static readonly PerScreen<Dictionary<GameLocation, FishingInformationCache>> FishingLocationsInfo =
    new(() => new Dictionary<GameLocation, FishingInformationCache>());

  private static readonly Dictionary<string, string[]> SplitFishArrays = new();
  private static readonly int[] ValidMineFishingAreas = { 0, 10, 40, 80 };

  private static readonly Profiler WaterCacheProfiler = new("WaterCache", 100, 1, s => ModEntry.MonitorObject.Log(s));

  private static readonly Profiler FishingChanceCalcProfiler = new(
    "Fishing Chances",
    200,
    50,
    s => ModEntry.MonitorObject.Log(s)
  );

  private static readonly Profiler FishSimulationProfiler = new(
    "FishSimulation",
    1,
    1,
    s => ModEntry.MonitorObject.Log(s)
  );

  internal static readonly Lazy<MethodInfo> CheckGenericFishRequirementsMethod = new(
    () =>
    {
      return typeof(GameLocation).GetMethod(
        "CheckGenericFishRequirements",
        BindingFlags.Static | BindingFlags.NonPublic
      )!;
    }
  );
}
