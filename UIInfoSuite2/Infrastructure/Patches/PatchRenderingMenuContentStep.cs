using System;
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

public class PatchRenderingMenuContentStep(IMonitor logger) : IPatchable
{
  private static readonly Lazy<EventsManager> EventsManager = new(ModEntry.GetSingleton<EventsManager>);

  // Patcher
  public void Patch(Harmony harmony)
  {
    // Patch GameMenu
    MethodInfo? gameMenuPatchMethod = AccessTools.DeclaredMethod(
      typeof(GameMenu),
      nameof(GameMenu.draw),
      [typeof(SpriteBatch)]
    );
    var gameMenuTranspiler = new HarmonyMethod(
      AccessTools.DeclaredMethod(typeof(PatchRenderingMenuContentStep), nameof(TranspileGameMenuDraw))
    );

    // Patch ShopMenu
    MethodInfo? shopMenuPatchMethod = AccessTools.DeclaredMethod(
      typeof(ShopMenu),
      nameof(ShopMenu.draw),
      [typeof(SpriteBatch)]
    );
    var shopMenuTranspiler = new HarmonyMethod(
      AccessTools.DeclaredMethod(typeof(PatchRenderingMenuContentStep), nameof(TranspileShopMenuDraw))
    );

    logger.Log("Patching Game Menu Content Step");
    harmony.Patch(gameMenuPatchMethod, transpiler: gameMenuTranspiler);
    logger.Log("Patching Shop Menu Content Step");
    harmony.Patch(shopMenuPatchMethod, transpiler: shopMenuTranspiler);
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
      .ThrowIfNotMatch("Unable to find insertion point content rendering in GameMenu");
    InsertEventPatch(matcher);

    return matcher.InstructionEnumeration();
  }

  private static IEnumerable<CodeInstruction> TranspileShopMenuDraw(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    matcher.MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(i => i.LoadsField(AccessTools.Field(typeof(ShopMenu), nameof(ShopMenu.hoverText)))),
        new CodeMatch(OpCodes.Ldstr),
        new CodeMatch(i => i.opcode == OpCodes.Call)
      )
      .ThrowIfNotMatch("Unable to find insertion point content rendering in ShopMenu");
    InsertEventPatch(matcher);

    return matcher.InstructionEnumeration();
  }

  // Generic patch for event call
  private static void InsertEventPatch(CodeMatcher matcher)
  {
    matcher.InsertAndAdvance(
      // IClickableMenu
      new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(matcher.Instruction),
      // SpriteBatch
      new CodeInstruction(OpCodes.Ldarg_1),
      // Call Event
      new CodeInstruction(
        OpCodes.Call,
        AccessTools.DeclaredMethod(typeof(PatchRenderingMenuContentStep), nameof(CallRenderingMenuContentStepEvent))
      )
    );
  }

  // Injected Method
  private static void CallRenderingMenuContentStepEvent(IClickableMenu menu, SpriteBatch spriteBatch)
  {
    EventsManager.Value.TriggerOnRenderingMenuContentStep(menu, spriteBatch);
  }
}
