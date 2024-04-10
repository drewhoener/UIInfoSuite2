using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewValley;
using StardewValley.Delegates;
using UIInfoSuite2.Infrastructure.Containers;
using static UIInfoSuite2.Infrastructure.Helpers.PatchHelper;


namespace UIInfoSuite2.Infrastructure.Helpers;

public class GameStateHelper
{
  /// <inheritdoc cref="GameStateQuery.CheckConditions(string, GameLocation, Farmer, Item, Item, Random, HashSet{string})" />
  public static bool CheckFishConditions(
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
        return true;
      case "FALSE":
        return false;
      default:
        var context = new GameStateQueryContext(location, null, null, null, null, ignoreQueryKeys);
        return CheckFishConditionsImpl(queryString, context, cachedFishInfo);
    }
  }

  /// <inheritdoc cref="GameStateQuery.CheckConditionsImpl(string, GameStateQueryContext)" />
  private static bool CheckFishConditionsImpl(
    string queryString,
    GameStateQueryContext context,
    FishSpawnInfo cachedFishInfo
  )
  {
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
