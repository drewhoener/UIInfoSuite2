using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace UIInfoSuite2.UIElements.MenuShortcuts.MenuShortcutDisplay;

internal partial class MenuShortcutDisplay
{
  public void Patch(Harmony harmony)
  {
    MethodInfo? patchingMethod = AccessTools.DeclaredMethod(
      typeof(GameMenu),
      nameof(GameMenu.draw),
      new[] { typeof(SpriteBatch) }
    );
    var transpilerMethod = new HarmonyMethod(
      AccessTools.DeclaredMethod(typeof(MenuShortcutDisplay), nameof(TranspileGameMenuDraw))
    );

    harmony.Patch(patchingMethod, transpiler: transpilerMethod);
  }

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
      new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MenuShortcutDisplay), nameof(InstanceDraw)))
    );

    return matcher.InstructionEnumeration();
  }
}
