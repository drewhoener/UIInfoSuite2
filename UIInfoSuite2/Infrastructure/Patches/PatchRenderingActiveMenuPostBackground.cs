using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Patches;

public class PatchRenderingActiveMenuPostBackground(IMonitor logger) : IPatchable
{
  // Patcher
  public void Patch(Harmony harmony)
  {
    MethodInfo? patchingMethod = AccessTools.DeclaredMethod(
      typeof(GameMenu),
      nameof(GameMenu.draw),
      [typeof(SpriteBatch)]
    );
    var transpilerMethod = new HarmonyMethod(
      AccessTools.DeclaredMethod(typeof(PatchRenderingActiveMenuPostBackground), nameof(TranspileGameMenuDraw))
    );

    logger.Log("Patching Active Menu Post-Background");
    harmony.Patch(patchingMethod, transpiler: transpilerMethod);
  }

  // Transpiler
  private static IEnumerable<CodeInstruction> TranspileGameMenuDraw(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(i => i.opcode == OpCodes.Ldfld),
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(i => i.opcode == OpCodes.Ldfld)
      )
      .ThrowIfNotMatch("Unable to find insertion point for drawing menu shortcuts");
    matcher.InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(matcher.Instruction),
      new CodeInstruction(OpCodes.Ldarg_1),
      new CodeInstruction(
        OpCodes.Call,
        AccessTools.DeclaredMethod(
          typeof(PatchRenderingActiveMenuPostBackground),
          nameof(CallRenderingPostBackgroundEvent)
        )
      )
    );

    return matcher.InstructionEnumeration();
  }

  // Injected Method
  private static void CallRenderingPostBackgroundEvent(GameMenu menu, SpriteBatch spriteBatch)
  {
    var eventsManager = ModEntry.GetSingleton<EventsManager>();
    eventsManager.TriggerOnRenderingActiveMenuPostBackground(menu, spriteBatch);
  }
}
