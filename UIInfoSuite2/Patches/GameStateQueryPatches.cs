using System;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using UIInfoSuite2.Infrastructure.Helpers;

namespace UIInfoSuite2.Patches;

public class GameStateQueryPatches
{
  public static void Apply(Harmony harmony)
  {
    try
    {
      ReversePatcher? checkConditionsImplReversePatcher = harmony.CreateReversePatcher(
        typeof(GameStateQuery).GetMethod("CheckConditionsImpl", BindingFlags.NonPublic | BindingFlags.Static),
        new HarmonyMethod(
          typeof(GameStateHelper).GetMethod("CheckFishConditionsImpl", BindingFlags.NonPublic | BindingFlags.Static)
        )
      );

      checkConditionsImplReversePatcher.Patch();
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log($"Failed to patch method in GameStateQuery. Message: {e.Message}", LogLevel.Error);
#if DEBUG
      Console.WriteLine(e);
#endif
    }
  }
}
