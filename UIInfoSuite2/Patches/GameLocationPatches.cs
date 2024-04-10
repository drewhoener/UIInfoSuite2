using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Infrastructure.Helpers;
using static UIInfoSuite2.Infrastructure.Helpers.PatchHelper;

namespace UIInfoSuite2.Patches;

public class GameLocationPatches
{
  private static MethodInfo CheckGenericFishRequirementsMethod = null!;

  public static void Apply(Harmony harmony)
  {
    MethodInfo? checkRequirementsMethod = typeof(GameLocation).GetMethod(
      "CheckGenericFishRequirements",
      BindingFlags.Static | BindingFlags.NonPublic
    );

    if (checkRequirementsMethod == null)
    {
      throw new InvalidOperationException("Couldn't find GameLocation#CheckGenericFishRequirements");
    }

    CheckGenericFishRequirementsMethod = checkRequirementsMethod;

    MethodInfo? getFishFromLocationDataMethodInfo = typeof(GameLocation).GetMethod(
      nameof(GameLocation.GetFishFromLocationData),
      BindingFlags.Static | BindingFlags.NonPublic,
      new[]
      {
        typeof(string),
        typeof(Vector2),
        typeof(int),
        typeof(Farmer),
        typeof(bool),
        typeof(bool),
        typeof(GameLocation),
        typeof(ItemQueryContext)
      }
    );

    try
    {
      harmony.Patch(
        getFishFromLocationDataMethodInfo,
        transpiler: new HarmonyMethod(
          typeof(GameLocationPatches),
          nameof(Transpile_GameLocation_GetFishFromLocationData)
        )
      );

      harmony.Patch(
        checkRequirementsMethod,
        transpiler: new HarmonyMethod(
          typeof(GameLocationPatches),
          nameof(Transpile_GameLocation_CheckGenericFishRequirements)
        )
      );
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log($"Failed to patch method in GameLocation. Message: {e.Message}", LogLevel.Error);
#if DEBUG
      Console.WriteLine(e);
#endif
    }
  }

  private static IEnumerable<CodeInstruction>? Transpile_GameLocation_GetFishFromLocationData(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    var matcher = new CodeMatcher(instructions, generator);

    const byte bobberTileArgIdx = 1;
    const byte tutorialCatchArgIdx = 4;
    const byte isInheritedArgIdx = 4;
    const byte itemQueryContextArgIdx = 7;

    const byte idLocIdx = 4;
    const byte hasMagicBaitLocIdx = 5;
    const byte hasCuriosityLureLocIdx = 6;
    const byte baitStrLocIdx = 7;
    const byte tilePointLocIdx = 10;
    const byte spawnFishDataEnumerableLocIdx = 11;
    const int baitKeysLocIdx = 13;
    const int fromLocationDataLocIdx = 14;
    const int spawnDataCompilerClassLocIdx = 22;
    const int entryPickedChanceLocIdx = 24;

    MethodInfo? containsMethod = typeof(Rectangle).GetMethod("Contains", new[] { typeof(int), typeof(int) });
    MethodInfo? updateFishForAreaMethod = typeof(GameLocationPatches).GetMethod(
      nameof(UpdateFishForArea),
      BindingFlags.NonPublic | BindingFlags.Static
    );
    (CodeInstruction ldSpawnData, CodeInstruction ldfldSpawnData) = LdlocCustomFieldInstructions(
      matcher,
      CreateLocMatcher(OpCodes.Ldloc_S, spawnDataCompilerClassLocIdx),
      typeof(SpawnFishData)
    );
    (CodeInstruction ldGameLocation, CodeInstruction ldfldGameLocation) = LdlocCustomFieldInstructions(
      matcher,
      new CodeMatch(OpCodes.Ldloc_0),
      typeof(GameLocation)
    );

    matcher.Start();

    matcher.MatchEndForward(
      new CodeMatch(OpCodes.Call, containsMethod),
      new CodeMatch(OpCodes.Ldc_I4_0),
      new CodeMatch(OpCodes.Ceq),
      new CodeMatch(op => op.opcode == OpCodes.Brtrue)
    );

    matcher.Start();
    matcher.MatchEndForward(
             CreateLocMatcher(OpCodes.Stloc_S, baitKeysLocIdx),
             new CodeMatch(OpCodes.Ldnull),
             CreateLocMatcher(OpCodes.Stloc_S, fromLocationDataLocIdx)
           )
           .ThrowIfNotMatch("Unable to find insertion point for fish spawn update function");
    matcher.Advance(1)
           .Insert(
             // Enumerable
             new CodeInstruction(OpCodes.Ldloc_S, spawnFishDataEnumerableLocIdx),
             // Location
             new CodeInstruction(ldGameLocation),
             new CodeInstruction(ldfldGameLocation),
             new CodeInstruction(OpCodes.Ldloc_S, idLocIdx),
             new CodeInstruction(OpCodes.Ldarg_S, isInheritedArgIdx),
             new CodeInstruction(OpCodes.Ldloc_S, baitKeysLocIdx),
             new CodeInstruction(OpCodes.Ldloc_S, tilePointLocIdx),
             new CodeInstruction(OpCodes.Ldarg_S, bobberTileArgIdx),
             new CodeInstruction(OpCodes.Ldarg_S, itemQueryContextArgIdx),
             // All Fish data
             new CodeInstruction(OpCodes.Ldloc_2),
             // Player
             new CodeInstruction(OpCodes.Ldarg_3),
             // Water Depth
             new CodeInstruction(OpCodes.Ldarg_2),
             new CodeInstruction(OpCodes.Ldloc_S, hasMagicBaitLocIdx),
             new CodeInstruction(OpCodes.Ldloc_S, hasCuriosityLureLocIdx),
             new CodeInstruction(OpCodes.Ldloc_S, baitStrLocIdx),
             new CodeInstruction(OpCodes.Ldarg_S, tutorialCatchArgIdx),
             new CodeInstruction(OpCodes.Call, updateFishForAreaMethod)
           );

    matcher.MatchStartForward(
             new CodeMatch(ldSpawnData),
             new CodeMatch(ldfldSpawnData),
             new CodeMatch(OpCodes.Callvirt, typeof(SpawnFishData).GetMethod("get_UseFishCaughtSeededRandom"))
           )
           .ThrowIfNotMatch("Unable to find insertion point to get entry picked chance")
           .Insert(
             new CodeInstruction(ldGameLocation),
             new CodeInstruction(ldfldGameLocation),
             new CodeInstruction(ldSpawnData),
             new CodeInstruction(ldfldSpawnData),
             new CodeInstruction(OpCodes.Ldloc_S, entryPickedChanceLocIdx),
             new CodeInstruction(
               OpCodes.Call,
               typeof(FishHelper).GetMethod(
                 nameof(FishHelper.SetFishEntryPickedChance),
                 BindingFlags.Static | BindingFlags.Public
               )
             )
           );

    return matcher.InstructionEnumeration();
  }

  private static Tuple<CodeInstruction, CodeInstruction> LdlocCustomFieldInstructions(
    CodeMatcher matcher,
    CodeMatch locMatcher,
    Type fieldType
  )
  {
    matcher.Start();
    matcher.MatchStartForward(
             locMatcher,
             new CodeMatch(inst => inst.operand is FieldInfo fieldInfo && fieldInfo.FieldType == fieldType)
           )
           .ThrowIfNotMatch($"Couldn't find instructions to load loc {locMatcher}, field {fieldType} onto the stack");

    List<CodeInstruction>? instructions = matcher.InstructionsWithOffsets(0, 1);
    if (instructions == null || instructions.Count < 2)
    {
      throw new InvalidOperationException(
        $"Instructions returned were incomplete for loading loc {locMatcher.operand}, field {fieldType}"
      );
    }

    return new Tuple<CodeInstruction, CodeInstruction>(instructions[0], instructions[1]);
  }

  private static void UpdateFishForArea(
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
    Dictionary<string, string> allFishData,
    Farmer player,
    int waterDepth,
    bool usingMagicBait,
    bool hasCuriosityLure,
    string curBait,
    bool isTutorialCatch
  )
  {
    ModEntry.LogExDebug_2($"\nUpdating fish for area {location}");
    FishHelper.ResetSpawnChecks(location);

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

      FishSpawnInfo cachedFishInfo = FishHelper.GetOrCreateFishInfo(location, data);
      cachedFishInfo.PopulateItemsForTile(queryContext, bobberTile, waterDepth);

      // ModEntry.LogExDebug_2($"Processing entry: {cachedFishInfo.Id}");

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

      Rectangle? requiredPlayerTile = data.PlayerPosition;
      Rectangle? requiredBobberTile = data.BobberPosition;
      if (requiredPlayerTile.HasValue &&
          !requiredPlayerTile.GetValueOrDefault().Contains(playerTilePoint.X, playerTilePoint.Y))
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WrongPlayerPos);
      }

      if (requiredBobberTile.HasValue &&
          !requiredBobberTile.GetValueOrDefault().Contains((int)bobberTile.X, (int)bobberTile.Y))
      {
        cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WrongBobberPos);
      }

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
        CheckGenericFishRequirementsMethod.Invoke(null, checkGenericRequirementsInvokeArgs);

        if (data.Condition != null && !GameStateHelper.CheckFishConditions(cachedFishInfo, location, ignoreQueryKeys))
        {
          cachedFishInfo.AddBlockedReason(FishSpawnBlockedReason.WrongGameState);
        }

        // ModEntry.LogExDebug_2(
        //   $"{cachedFishInfo.Id} BlockReasons: {string.Join(", ", cachedFishInfo.SpawnBlockReasons)}"
        // );

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

  private static IEnumerable<CodeInstruction> Transpile_GameLocation_CheckGenericFishRequirements(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    var logFormatErrorMatch = new CodeMatch(
      i => i.opcode == OpCodes.Call && i.operand is MethodInfo methodInfo && methodInfo.Name.Contains("LogFormatError")
    );

    var matcher = new CodeMatcher(instructions, generator);
    LocalBuilder fishInfoLocal = generator.DeclareLocal(typeof(FishSpawnInfo));

    const byte numLocIdx = 4;
    const byte flag2LocIdx = 6;
    const byte flag3LocIdx = 11;
    const byte num3LocIdx = 17;
    const byte num5LocIdx = 21;

    var callGetOrCreateFishInfoMethod = new CodeInstruction(
      OpCodes.Call,
      typeof(FishHelper).GetMethod(nameof(FishHelper.GetOrCreateFishInfo), BindingFlags.Static | BindingFlags.Public)
    );

    var callAddBlockerInstruction = new CodeInstruction(
      OpCodes.Call,
      typeof(FishSpawnInfo).GetMethod(
        nameof(FishSpawnInfo.AddBlockedReason),
        BindingFlags.Instance | BindingFlags.Public
      )
    );

    var callGetFishDataArrayInstruction = new CodeInstruction(
      OpCodes.Call,
      typeof(FishHelper).GetMethod(nameof(FishHelper.GetSplitFishArray), BindingFlags.Static | BindingFlags.Public)
    );

    var callSetSpawnProbabilityInstruction = new CodeInstruction(
      OpCodes.Call,
      typeof(FishSpawnInfo).GetMethod("set_SpawnProbability", BindingFlags.Instance | BindingFlags.Public)
    );

    // fishInfoLocal = FishHelper.GetOrCreateFishInfo(location, spawnData)
    matcher.Advance(1)
           .InsertAndAdvance(
             new CodeInstruction(OpCodes.Ldarg_2),
             new CodeInstruction(OpCodes.Ldarg_S, 4),
             callGetOrCreateFishInfoMethod,
             new CodeInstruction(OpCodes.Stloc_S, fishInfoLocal)
           );

    // Cache fishInfo.Split('/'), since this will happen A LOT with simulation and the data is unlikely to change
    matcher.MatchStartForward(
             new CodeMatch(OpCodes.Ldloc_1),
             new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)47),
             new CodeMatch(OpCodes.Ldc_I4_0),
             new CodeMatch(i => i.opcode == OpCodes.Callvirt)
           )
           .ThrowIfNotMatch("Couldn't find insertion point for fish split cache")
           .Advance(1)
           .RemoveInstructions(3)
           .Insert(callGetFishDataArrayInstruction);

    // Match all the LogFormatError calls
    matcher.MatchEndForward(logFormatErrorMatch, new CodeMatch(OpCodes.Ret))
           .Repeat(
             foundMatch =>
             {
               foundMatch.Insert(
                 new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
                 new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.InvalidFormat),
                 callAddBlockerInstruction
               );
             }
           );

    // Reset the matcher
    matcher.Start();
    for (var i = 0; i < 2; i++)
    {
      // if (!fish.HasTypeObject() || !allFishData.TryGetValue(fish.ItemId, out str1))
      // if (ArgUtility.Get(array1, 1) == "trap")
      matcher.MatchEndForward(new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Ceq), new CodeMatch(OpCodes.Ret))
             .ThrowIfNotMatch($"Unable to find insertion point for missing fish data in generic check ({i})")
             .CreateLabel(out Label skipSetResultLabel)
             .Insert(
               new CodeInstruction(OpCodes.Dup),
               new CodeInstruction(OpCodes.Brtrue_S, skipSetResultLabel),
               new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
               new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.TutorialCatch),
               callAddBlockerInstruction
             );
    }

    // if (difficulty >= 50) and Training Rod
    matcher.MatchEndForward(CreateLocMatcher(OpCodes.Ldloc_S, numLocIdx), new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)50))
           .MatchEndForward(new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Ret))
           .ThrowIfNotMatch("Unable to find insertion point for weak rod generic check")
           .Insert(
             new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
             new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.PlayerRodTooWeak),
             callAddBlockerInstruction
           );

    // isTutorialCatch
    matcher.MatchEndForward(
             new CodeMatch(OpCodes.Ldloc_2),
             new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)13),
             CreateLocMatcher(OpCodes.Ldloca_S, flag2LocIdx)
           )
           .MatchEndForward(new CodeMatch(OpCodes.Ldc_I4_0), new CodeMatch(OpCodes.Ret))
           .ThrowIfNotMatch("Unable to find insertion point for tutorial fish generic check")
           .Insert(
             new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
             new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.TutorialCatch),
             callAddBlockerInstruction
           );

    // Time of day check
    matcher.MatchEndForward(
             CreateLocMatcher(OpCodes.Ldloc_S, flag3LocIdx),
             new CodeMatch(i => i.opcode == OpCodes.Brtrue_S),
             new CodeMatch(OpCodes.Ldc_I4_0),
             new CodeMatch(OpCodes.Ret)
           )
           .ThrowIfNotMatch("Unable to find insertion point for time of day check")
           .Insert(
             new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
             new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.WrongTime),
             callAddBlockerInstruction
           );

    // Weather check
    matcher
      .MatchEndForward(
        new CodeMatch(OpCodes.Callvirt, typeof(GameLocation).GetMethod(nameof(GameLocation.IsRainingHere))),
        new CodeMatch(i => i.opcode == OpCodes.Brtrue_S),
        new CodeMatch(OpCodes.Ldc_I4_0),
        new CodeMatch(OpCodes.Ret)
      )
      .ThrowIfNotMatch("Unable to find insertion point for rainy weather check")
      .InsertAndAdvance(
        new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
        new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.RequiresRain),
        callAddBlockerInstruction
      )
      .Advance(1)
      .MatchEndForward(new CodeMatch(OpCodes.Ret))
      .ThrowIfNotMatch("Unable to find insertion point for sunny weather check")
      .Insert(
        new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
        new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.RequiresSun),
        callAddBlockerInstruction
      );

    // Player level check
    matcher.MatchEndForward(
             CreateLocMatcher(OpCodes.Ldloc_S, num3LocIdx),
             new CodeMatch(i => i.opcode == OpCodes.Bge_S),
             new CodeMatch(OpCodes.Ldc_I4_0),
             new CodeMatch(OpCodes.Ret)
           )
           .ThrowIfNotMatch("Unable to find insertion point for time of day check")
           .Insert(
             new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal),
             new CodeInstruction(OpCodes.Ldc_I4_S, (int)FishSpawnBlockedReason.PlayerLevelTooLow),
             callAddBlockerInstruction
           );

    // Pct Chance check
    matcher
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldsfld, typeof(Game1).GetField(nameof(Game1.random))),
        CreateLocMatcher(OpCodes.Ldloc_S, num5LocIdx)
      )
      .ThrowIfNotMatch("Unable to find check point for random chance")
      .Insert(
        new CodeInstruction(OpCodes.Ldloc_S, fishInfoLocal).MoveLabelsFrom(matcher.Instruction),
        new CodeInstruction(OpCodes.Ldloc_S, num5LocIdx),
        callSetSpawnProbabilityInstruction
      );

    return matcher.InstructionEnumeration();
  }
}
