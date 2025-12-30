using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Patches;

public class PatchMasteryXpGainEvent(IMonitor logger) : IPatchable
{
  private static readonly Lazy<EventsManager> EventsManager = new(ModEntry.GetSingleton<EventsManager>);

  // Patcher
  public void Patch(Harmony harmony)
  {
    // Patch Farmer
    MethodInfo? gainExperiencePatchMethod = AccessTools.DeclaredMethod(typeof(Farmer), nameof(Farmer.gainExperience));
    var gainExperienceTranspiler = new HarmonyMethod(
      AccessTools.DeclaredMethod(typeof(PatchMasteryXpGainEvent), nameof(TranspileGainExperience))
    );

    logger.Log("Patching Farmer gainExperience");
    harmony.Patch(gainExperiencePatchMethod, transpiler: gainExperienceTranspiler);
  }

  // Transpiler
  private static IEnumerable<CodeInstruction> TranspileGainExperience(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    matcher.MatchStartForward(
        new CodeMatch(
          OpCodes.Call,
          AccessTools.DeclaredMethod(typeof(MasteryTrackerMenu), nameof(MasteryTrackerMenu.getCurrentMasteryLevel))
        ),
        new CodeMatch(OpCodes.Stloc_2)
      )
      .ThrowIfNotMatch("Unable to find insertion point for mastery xp increment");

    // Get the player, fallback skill, and mastery XP and put them on the stack before we start incrementing
    matcher.InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldarg_0),
      new CodeInstruction(OpCodes.Ldarg_1),
      new CodeInstruction(
        OpCodes.Call,
        AccessTools.DeclaredMethod(typeof(PatchMasteryXpGainEvent), nameof(GetTotalMasteryXp))
      )
    );

    // Search to the end of the increment call, the value is discarded with a pop
    matcher.MatchEndForward(new CodeMatch(OpCodes.Pop)).ThrowIfNotMatch("Unable to find end of mastery increment");
    // Replace that instruction with our call, new XP is still on the stack.
    matcher.SetInstructionAndAdvance(
      new CodeInstruction(
        OpCodes.Call,
        AccessTools.DeclaredMethod(typeof(PatchMasteryXpGainEvent), nameof(CallMasteryXpGainEvent))
      )
    );

    return matcher.InstructionEnumeration();
  }

  // Injected Methods
  private static int GetTotalMasteryXp()
  {
    return (int)Game1.stats.Get("MasteryExp");
  }

  private static void CallMasteryXpGainEvent(Farmer player, int skillType, int oldXp, int newXp)
  {
    EventsManager.Value.TriggerOnMasteryXpGain(player, skillType, oldXp, newXp);
  }
}
