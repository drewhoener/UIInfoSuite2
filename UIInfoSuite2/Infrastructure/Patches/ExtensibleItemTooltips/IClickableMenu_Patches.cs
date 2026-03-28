using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Models;

namespace UIInfoSuite2.Infrastructure.Patches.ExtensibleItemTooltips;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
internal static class IClickableMenu_Patches
{
  // ==============================================================================
  // region Locals and Args
  //
  // Locals (from .locals init):
  //   [1]  num1 (before IL_0727) → width1 (after)   ← innerWidth source
  //   [2]  startingHeight
  //   [5]  x
  //   [6]  y1 (before title) → y2 (content cursor)
  //
  // Args (static, 0-indexed):
  //   [0]  b          [9]  hoveredItem   [15] alpha
  //   [23] boxWidthOverride              [24] boxHeightOverride

  private const byte LocalWidth = 1;
  private const byte LocalStartingHeight = 2;
  private const byte LocalX = 5;
  private const byte LocalDrawY = 6;

  private const byte ArgHoveredItem = 9;
  private const byte ArgAlpha = 15;
  private const byte ArgBoxWidthOverride = 23;

  private const byte ArgBoxHeightOverride = 24;
  // endregion

// region Reflected Method Helpers
  private static Profiler _profiler = null!;

  private static readonly MethodInfo _draw = AccessTools.Method(
    typeof(TooltipExtensionRegistry),
    nameof(TooltipExtensionRegistry.Draw)
  );

  private static readonly MethodInfo _updateHoveredItem = AccessTools.Method(
    typeof(TooltipExtensionRegistry),
    nameof(TooltipExtensionRegistry.UpdateHoveredItem)
  );

  private static readonly MethodInfo _adjustLayout = AccessTools.Method(
    typeof(TooltipExtensionRegistry),
    nameof(TooltipExtensionRegistry.AdjustLayout)
  );

  private static readonly MethodInfo _adjustTitleBoxHeight = AccessTools.Method(
    typeof(TooltipExtensionRegistry),
    nameof(TooltipExtensionRegistry.AdjustTitleBoxHeight)
  );

  // Anchor methods for size-injection matching
  private static readonly MethodInfo _getOldMouseX = AccessTools.Method(typeof(Game1), nameof(Game1.getOldMouseX));

  private static readonly MethodInfo _colorGetWhite = AccessTools.PropertyGetter(typeof(Color), nameof(Color.White));

  // Anchor methods for draw-hook matching (same as previous)
  private static readonly MethodInfo _dialogueFontMeasureStr = AccessTools.Method(
    typeof(SpriteFont),
    nameof(SpriteFont.MeasureString),
    [typeof(string)]
  );

  private static readonly MethodInfo _measureSb = AccessTools.Method(
    typeof(SpriteFont),
    nameof(SpriteFont.MeasureString),
    [typeof(StringBuilder)]
  );

  private static readonly FieldInfo _dialogueFont = AccessTools.Field(typeof(Game1), nameof(Game1.dialogueFont));

  private static readonly FieldInfo _staminaRect = AccessTools.Field(typeof(Game1), nameof(Game1.staminaRect));

  private static readonly FieldInfo _vecY = AccessTools.Field(typeof(Vector2), nameof(Vector2.Y));

  private static readonly MethodInfo _getCategoryColor = AccessTools.Method(
    typeof(Item),
    nameof(Item.getCategoryColor)
  );

  private static readonly MethodInfo _drawTooltip = AccessTools.Method(typeof(Item), nameof(Item.drawTooltip));

  // endregion
  // ==============================================================================

  public static void Patch(Harmony harmony)
  {
    _profiler = new Profiler("drawHoverText", 400, 100, str => ModEntry.DebugLog(str, LogLevel.Debug));

    MethodInfo? drawHoverTextMethod = typeof(IClickableMenu).GetMethod(
      "drawHoverText",
      [
        // Yeesh...
        typeof(SpriteBatch),
        typeof(StringBuilder),
        typeof(SpriteFont),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(string),
        typeof(int),
        typeof(string[]),
        typeof(Item),
        typeof(int),
        typeof(string),
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(float),
        typeof(CraftingRecipe),
        typeof(IList<Item>),
        typeof(Texture2D),
        typeof(Rectangle?),
        typeof(Color?),
        typeof(Color?),
        typeof(float),
        typeof(int),
        typeof(int)
      ]
    );

    harmony.Patch(
      drawHoverTextMethod,
#if DEBUG
      new HarmonyMethod(typeof(IClickableMenu_Patches), nameof(Prefix_IClickableMenu_DrawHoverText)),
      new HarmonyMethod(typeof(IClickableMenu_Patches), nameof(Postfix_IClickableMenu_DrawHoverText)),
#endif
      new HarmonyMethod(typeof(IClickableMenu_Patches), nameof(Transpile_IClickableMenu_DrawHoverText))
    );
  }

  private static bool Prefix_IClickableMenu_DrawHoverText()
  {
    _profiler.Start();
    // ModEntry.Instance.Monitor.Log("Drawing Hover Text");
    return true;
  }

  private static void Postfix_IClickableMenu_DrawHoverText()
  {
    _profiler.Stop();
  }

  // ── Transpiler ─────────────────────────────────────────────────────────────


  public static IEnumerable<CodeInstruction> Transpile_IClickableMenu_DrawHoverText(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator gen
  )
  {
    var m = new CodeMatcher(instructions, gen);

    // ── Phase 1: size adjustments ──────────────────────────────────────────
    // Must happen before Phase 2 because Phase 2 collects positions from the
    // already-modified instruction list. Each size-insertion is content-matched
    // (m.Start() + MatchForward) so shifts from earlier insertions are harmless.

    InsertAdjustLayout(m);
    InsertAdjustTitleBoxHeight_SubBox(m);  // before Color.get_White
    InsertAdjustTitleBoxHeight_Divider(m); // before ldloc.1 (Rectangle width arg)

    // ── Phase 2: draw hooks ────────────────────────────────────────────────
    // Collect positions (forward pass), then insert in reverse so earlier
    // indices remain valid.

    var drawPoints = new (ContainerPatchPoint Point, Action<CodeMatcher> Finder)[]
    {
      (ContainerPatchPoint.BeforeTitle, FindBeforeTitle),
      (ContainerPatchPoint.AfterTitle, FindAfterTitle),
      (ContainerPatchPoint.AfterCategory, FindAfterCategory),
      (ContainerPatchPoint.BeforeDescription, FindBeforeDescription),
      (ContainerPatchPoint.AfterDescription, FindAfterDescription),
      (ContainerPatchPoint.AfterBuffs, FindAfterBuffs),
      (ContainerPatchPoint.BeforeFooter, FindBeforeFooter),
      (ContainerPatchPoint.AfterFooter, FindAfterFooter)
    };

    var pending = new List<(int Pos, ContainerPatchPoint Point)>();
    foreach ((ContainerPatchPoint point, Action<CodeMatcher> finder) in drawPoints)
    {
      m.Start();
      finder(m);
      if (!m.IsInvalid)
      {
        pending.Add((m.Pos, point));
      }
      else
      {
        ModEntry.Instance.Monitor.Log($"[DrawHoverTextPatcher] No match for draw point {point}");
      }
    }

    foreach ((int pos, ContainerPatchPoint point) in pending.OrderByDescending(t => t.Pos))
    {
      m.Start().Advance(pos);
      CodeInstruction[] drawCall = BuildDrawCall(point);
      // Copy the labels from the current instruction so we get jumped to
      if (drawCall.Length > 0)
      {
        drawCall[0].MoveLabelsFrom(m.Instruction);
      }

      m.Insert(drawCall);
    }

    return m.InstructionEnumeration();
  }

  // ── Phase 1 helpers ────────────────────────────────────────────────────────

  /// Before `x = Game1.getOldMouseX() + 32 + xOffset`:
  /// adjust both startingHeight and num1 now that all vanilla height/width
  /// accumulation is complete.
  private static void InsertAdjustLayout(CodeMatcher m)
  {
    m.Start().MatchStartForward(new CodeMatch(OpCodes.Call, _getOldMouseX));

    if (m.IsInvalid)
    {
      ModEntry.Instance.Monitor.Log("[DrawHoverTextPatcher] AdjustLayout anchor not found");
      return;
    }

    m.Insert(
      new CodeInstruction(OpCodes.Ldarg_S, ArgHoveredItem).MoveLabelsFrom(m.Instruction),
      new CodeInstruction(OpCodes.Call, _updateHoveredItem),
      new CodeInstruction(OpCodes.Ldloca_S, LocalStartingHeight), // ref startingHeight
      new CodeInstruction(OpCodes.Ldloca_S, LocalWidth),          // ref num1
      new CodeInstruction(OpCodes.Ldarg_S, ArgBoxWidthOverride),
      new CodeInstruction(OpCodes.Ldarg_S, ArgBoxHeightOverride),
      new CodeInstruction(OpCodes.Call, _adjustLayout)
    );
  }

  /// Title sub-box height arg: `... ldc.i4.4, sub, [call AdjustTitleBoxHeight,] call Color.get_White, ...`
  /// The pattern `ldc.i4.4, sub` followed immediately by `call Color.get_White` is unique
  /// to this height expression in the whole method body.
  private static void InsertAdjustTitleBoxHeight_SubBox(CodeMatcher m)
  {
    m.Start()
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldc_I4_4),
        new CodeMatch(OpCodes.Sub),
        new CodeMatch(OpCodes.Call, _colorGetWhite)
      );

    if (m.IsInvalid)
    {
      ModEntry.Instance.Monitor.Log("[DrawHoverTextPatcher] AdjustTitleBoxHeight(sub-box) anchor not found");
      return;
    }

    m.Advance(2) // land after `sub`, before `call Color.get_White`
      .Insert(new CodeInstruction(OpCodes.Call, _adjustTitleBoxHeight).MoveLabelsFrom(m.Instruction));
  }

  /// Divider Rectangle y-offset: `y1 + titleBoxHeight` where titleBoxHeight ends with
  /// `ldc.i4.4, sub` followed immediately by `ldloc.1` (start of Rectangle width arg).
  /// This is the second `ldc.i4.4, sub` in the method. After InsertAdjustTitleBoxHeight_SubBox
  /// the first occurrence now has `call AdjustTitleBoxHeight, call Color.get_White` after it,
  /// so the unique discriminator `sub → ldloc.1` still finds only the divider.
  private static void InsertAdjustTitleBoxHeight_Divider(CodeMatcher m)
  {
    m.Start()
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldc_I4_4),
        new CodeMatch(OpCodes.Sub),
        new CodeMatch(OpCodes.Ldloc_1)
      ); // width1 = first arg of Rectangle width expr

    if (m.IsInvalid)
    {
      ModEntry.Instance.Monitor.Log("[DrawHoverTextPatcher] AdjustTitleBoxHeight(divider) anchor not found");
      return;
    }

    m.Advance(2) // land after `sub`, before `ldloc.1`
      .Insert(new CodeInstruction(OpCodes.Call, _adjustTitleBoxHeight).MoveLabelsFrom(m.Instruction));
  }

#region Match Helper Functions
  // ── Draw call builder ──────────────────────────────────────────────────────

  // At every draw hook point, local 1 = width1 = num1 + 4 (the transition
  // `stloc.1 // width1` at IL_0727 is always before any draw hook site).
  private static CodeInstruction[] BuildDrawCall(ContainerPatchPoint point)
  {
    return
    [
      new CodeInstruction(OpCodes.Ldarg_0),                 // b
      new CodeInstruction(OpCodes.Ldloc_S, LocalX),         // x
      new CodeInstruction(OpCodes.Ldloca_S, LocalDrawY),    // ref drawY
      new CodeInstruction(OpCodes.Ldloc_1),                 // innerWidth (= width1)
      new CodeInstruction(OpCodes.Ldarg_S, ArgAlpha),       // alpha
      new CodeInstruction(OpCodes.Ldarg_S, ArgHoveredItem), // hoveredItem
      new CodeInstruction(OpCodes.Ldc_I4, (int)point),
      new CodeInstruction(OpCodes.Call, _draw)
    ];
  }

  private static void FindBeforeTitle(CodeMatcher m)
  {
    m.MatchStartForward(
      new CodeMatch(OpCodes.Ldsfld, _dialogueFont),
      new CodeMatch(OpCodes.Ldarg_S),
      new CodeMatch(OpCodes.Callvirt, _dialogueFontMeasureStr),
      new CodeMatch(OpCodes.Stloc_S)
    );
    // → vector2_2
  }

  private static void FindAfterTitle(CodeMatcher m)
  {
    m.MatchStartForward(
        new CodeMatch(OpCodes.Ldsfld, _dialogueFont),
        new CodeMatch(OpCodes.Ldarg_S),
        new CodeMatch(OpCodes.Callvirt, _dialogueFontMeasureStr),
        new CodeMatch(OpCodes.Ldfld, _vecY),
        new CodeMatch(OpCodes.Conv_I4),
        new CodeMatch(OpCodes.Add),
        new CodeMatch(OpCodes.Stloc_S)
      )
      .Advance(7);
  }

  private static void FindAfterCategory(CodeMatcher m)
  {
    m.MatchStartForward(new CodeMatch(OpCodes.Callvirt, _getCategoryColor))
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldc_I4_4),
        new CodeMatch(OpCodes.Add),
        new CodeMatch(OpCodes.Add),
        new CodeMatch(OpCodes.Stloc_S)
      )
      .Advance(4);
  }

  private static void FindBeforeDescription(CodeMatcher m)
  {
    m.MatchStartForward(new CodeMatch(OpCodes.Callvirt, _drawTooltip))
      .MatchStartBackwards(
        new CodeMatch(OpCodes.Ldarg_S),
        new CodeMatch(OpCodes.Brfalse_S),
        new CodeMatch(OpCodes.Ldarg_S),
        new CodeMatch(OpCodes.Brtrue_S)
      );
  }

  private static void FindAfterDescription(CodeMatcher m)
  {
    m.MatchEndForward(
        new CodeMatch(OpCodes.Callvirt, _measureSb),
        new CodeMatch(OpCodes.Ldfld, _vecY),
        new CodeMatch(OpCodes.Conv_I4),
        new CodeMatch(OpCodes.Ldc_I4_4),
        new CodeMatch(OpCodes.Add),
        new CodeMatch(OpCodes.Add),
        new CodeMatch(OpCodes.Stloc_S)
      )
      .Advance(1);
  }

  private static void FindAfterBuffs(CodeMatcher m)
  {
    m.MatchStartForward(new CodeMatch(OpCodes.Ldc_I4_8), new CodeMatch(OpCodes.Sub), new CodeMatch(OpCodes.Stloc_S))
      .Advance(3);
  }

  private static void FindBeforeFooter(CodeMatcher m)
  {
    m.MatchStartForward(
      new CodeMatch(OpCodes.Ldsfld, _staminaRect),
      new CodeMatch(OpCodes.Ldloc_S),
      new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)12),
      new CodeMatch(OpCodes.Add),
      new CodeMatch(OpCodes.Ldloc_S),
      new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)22)
    );
    // 22 = money divider; buff divider uses 6
  }

  private static void FindAfterFooter(CodeMatcher m)
  {
    m.MatchStartForward(
        new CodeMatch(OpCodes.Ldsfld, _staminaRect),
        new CodeMatch(OpCodes.Ldloc_S),
        new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)22)
      )
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)48),
        new CodeMatch(OpCodes.Add),
        new CodeMatch(OpCodes.Stloc_S)
      )
      .Advance(3)
      .MatchStartForward(new CodeMatch(OpCodes.Brfalse));
    // long-form; skips entire extra-item box
  }
#endregion
}
