using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Infrastructure.Helpers.FishHelper;
using static UIInfoSuite2.Infrastructure.Helpers.PatchHelper;


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
          typeof(GameStateQueryPatches).GetMethod(
            nameof(Patched_CheckFishConditionsImpl),
            BindingFlags.NonPublic | BindingFlags.Static
          )
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

  /// <inheritdoc cref="GameStateQuery.CheckConditionsImpl(string, GameStateQueryContext)" />
  [SuppressMessage("ReSharper", "UnusedParameter.Global", Justification = "Harmony Reverse Patched")]
  internal static bool Patched_CheckFishConditionsImpl(
    string queryString,
    GameStateQueryContext context,
    FishSpawnInfo cachedFishInfo
  )
  {
    // ReSharper disable once MoveLocalFunctionAfterJumpStatement
    IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
      var matcher = new CodeMatcher(instructions);
      matcher.MatchEndForward(
               new CodeMatch(
                 OpCodes.Ldfld,
                 typeof(GameStateQuery.ParsedGameStateQuery).GetField(
                   nameof(GameStateQuery.ParsedGameStateQuery.Negated)
                 )
               ),
               new CodeMatch(i => i.opcode == OpCodes.Bne_Un_S),
               new CodeMatch(OpCodes.Ldc_I4_0),
               CreateLocMatcher(OpCodes.Stloc_S, 4)
             )
             .ThrowIfNotMatch("Couldn't find insertion point for reverse patching CheckConditionsImpl");

      // FishHelper.ParseFailedQuery(FishSpawnInfo info, string[] query)
      matcher.Insert(
        new CodeInstruction(OpCodes.Ldarg_2),
        new CodeInstruction(OpCodes.Ldloc_3),
        new CodeInstruction(
          OpCodes.Ldfld,
          typeof(GameStateQuery.ParsedGameStateQuery).GetField(nameof(GameStateQuery.ParsedGameStateQuery.Query))
        ),
        new CodeInstruction(
          OpCodes.Call,
          typeof(FishHelper).GetMethod(nameof(FishHelper.ParseFailedQuery), BindingFlags.Static | BindingFlags.Public)
        )
      );

      return matcher.InstructionEnumeration();
    }

    // make compiler happy
    _ = Transpiler(null!);
    return false;
  }
}
