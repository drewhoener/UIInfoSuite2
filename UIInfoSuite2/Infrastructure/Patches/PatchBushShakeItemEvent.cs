using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Patches;

public class PatchBushShakeItemEvent(IMonitor logger) : IPatchable
{
  private static readonly Lazy<EventsManager> EventsManager = new(ModEntry.GetSingleton<EventsManager>);

  // Patcher
  public void Patch(Harmony harmony)
  {
    // Patch Bush
    logger.Log("Patching Bush shake");
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.shake)),
      transpiler: new HarmonyMethod(typeof(PatchBushShakeItemEvent), nameof(Transpile_Bush_draw))
    );
  }

  private static bool IsItemShakeCall(CodeInstruction instruction)
  {
    if (instruction.operand is not MethodInfo method)
    {
      return false;
    }

    bool codeMatches = instruction.opcode == OpCodes.Call;
    // This breaks if CustomBush renames its transpiled shake-off call
    bool funcMatches = method.Name.ToLowerInvariant().Contains("createobjectdebris");
    return codeMatches && funcMatches;
  }

  // Transpiler
  private static IEnumerable<CodeInstruction> Transpile_Bush_draw(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    matcher.MatchEndForward(new CodeMatch(IsItemShakeCall), new CodeMatch(OpCodes.Br))
      .ThrowIfNotMatch("Unable to find item debris generation in Bush, tooltips may be unstable");

    matcher.Insert(
      new CodeInstruction(OpCodes.Ldarg_0),
      new CodeInstruction(
        OpCodes.Call,
        AccessTools.DeclaredMethod(typeof(PatchBushShakeItemEvent), nameof(CallBushShakeItemEvent))
      )
    );

    return matcher.InstructionEnumeration();
  }

  // Injected Methods

  private static void CallBushShakeItemEvent(Bush bush)
  {
    EventsManager.Value.TriggerBushShakeItem(bush);
  }
}
