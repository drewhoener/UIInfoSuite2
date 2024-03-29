using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.UIElements;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Patches;

internal class ClickableMenuPatches
{
  /***
   * Transpiler Constants
   ***/
  private const int HeightOffset = 10;
  private const byte BoxHeightOverrideParamIdx = 24;
  private const byte XLocalIdx = 5;
  private const byte Y1LocalIdx = 6;
  private const byte Width2LocalIdx = 24;
  private const byte Height1LocalIdx = 25;

  private const BindingFlags AllFlags =
    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

  private static readonly Vector2 _shippingBinDimensions = new(30, 24);
  private static Profiler _profiler = null!;

  public static void Apply(Harmony harmony)
  {
    _profiler = new Profiler("drawHoverText", 400, 100, str => ModEntry.MonitorObject.Log(str, LogLevel.Debug));

    MethodInfo? drawHoverTextMethod = typeof(IClickableMenu).GetMethod(
      "drawHoverText",
      new[]
      {
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
      }
    );

    harmony.Patch(
      drawHoverTextMethod,
#if DEBUG
      new HarmonyMethod(typeof(ClickableMenuPatches), nameof(Prefix_IClickableMenu_DrawHoverText)),
      new HarmonyMethod(typeof(ClickableMenuPatches), nameof(Postfix_IClickableMenu_DrawHoverText)),
#endif
      new HarmonyMethod(typeof(ClickableMenuPatches), nameof(Transpile_IClickableMenu_DrawHoverText))
    );
  }

  private static CodeMatch CreateLocMatcher(OpCode code, int locIdx)
  {
    return new CodeMatch(
      instruction => instruction.opcode == code &&
                     instruction.operand is LocalBuilder builder &&
                     builder.LocalIndex == locIdx
    );
  }

  private static bool Prefix_IClickableMenu_DrawHoverText()
  {
    _profiler.Start();
    return true;
  }

  private static void Postfix_IClickableMenu_DrawHoverText()
  {
    _profiler.Stop();
  }

  private static IEnumerable<CodeInstruction>? Transpile_IClickableMenu_DrawHoverText(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator gen
  )
  {
    CodeMatcher matcher = new(instructions, gen);
    LocalBuilder textureBoxDimensionModifiers = gen.DeclareLocal(typeof(Vector2));
    LocalBuilder boxDrawYPos = gen.DeclareLocal(typeof(int));

    try
    {
      PatchTextureBoxDimensions(matcher, textureBoxDimensionModifiers, boxDrawYPos);
      PatchDrawUIIconsHook(matcher, boxDrawYPos);
    }
    catch (Exception e)
    {
      ModEntry.MonitorObject.Log($"Failed to patch drawHoverText. Message: {e.Message}");
      return null;
    }

    return matcher.InstructionEnumeration();
  }

  /// <summary>
  ///   Patches the drawHoverText method to add more space to the "title" box when hovering over an item.
  ///   Makes room for things like the Community Center bundle banner.
  ///   TODO: Add some sort of event listener to allow the mod to decide whether or not to shift the title down or up.
  /// </summary>
  /// <param name="matcher">CodeMatcher with the drawHoverText instructions</param>
  /// <param name="bundleNameHeightOffset">A LocalBuilder to hold the height being added to the box</param>
  /// <param name="boxDrawYPos">A LocalBuilder to hold the y position to draw the box, since we shift the normal one down</param>
  private static void PatchTextureBoxDimensions(
    CodeMatcher matcher,
    LocalBuilder textureBoxDimensionModifiers,
    LocalBuilder boxDrawYPos
  )
  {
    MethodInfo? mathMax = typeof(Math).GetMethod(
      nameof(Math.Max),
      BindingFlags.Public | BindingFlags.Static,
      null,
      new[] { typeof(int), typeof(int) },
      null
    );

    matcher.Start();
    matcher.MatchStartForward(
             new CodeMatch(OpCodes.Call, typeof(Game1).GetMethod(nameof(Game1.getOldMouseY), Array.Empty<Type>()))
           )
           .ThrowIfNotMatch("Unable to find insertion point for bundleNameHeightOffset")
           // Patch startingHeight and set the textureBoxOffsets local for usage later
           .Insert(
             new CodeInstruction(OpCodes.Ldarg_S, 9),
             new CodeInstruction(OpCodes.Ldc_I4, HeightOffset),
             new CodeInstruction(OpCodes.Ldloca_S, (byte)2),
             new CodeInstruction(
               OpCodes.Call,
               typeof(ClickableMenuPatches).GetMethod(nameof(AddOffsetsIfBundleItem), AllFlags)
             ),
             new CodeInstruction(OpCodes.Stloc_S, textureBoxDimensionModifiers)
           );

    matcher.MatchEndForward(
             new CodeMatch(OpCodes.Ldloc_1),
             new CodeMatch(OpCodes.Ldc_I4_4),
             new CodeMatch(OpCodes.Add),
             new CodeMatch(OpCodes.Stloc_1)
           )
           .ThrowIfNotMatch("Failed to find insertion point to set window box max width")
           // Take over jump labels from stloc.1 instruction so we get jumped to (instead of over)
           // width1 = Math.max(stack, textureBoxDimensionModifiers.X)
           .InsertAndAdvance(
             new CodeInstruction(OpCodes.Ldloc_S, textureBoxDimensionModifiers).MoveLabelsFrom(matcher.Instruction),
             new CodeInstruction(OpCodes.Ldfld, typeof(Vector2).GetField(nameof(Vector2.X))),
             new CodeInstruction(OpCodes.Conv_I4),
             new CodeInstruction(OpCodes.Call, mathMax)
           );

    matcher.MatchStartForward(
             CreateLocMatcher(OpCodes.Stloc_S, Width2LocalIdx),
             new CodeMatch(OpCodes.Ldarg_S, BoxHeightOverrideParamIdx),
             new CodeMatch(OpCodes.Ldc_I4_M1)
           )
           .ThrowIfNotMatch("Unable to find insertion point for textureBoxWidth")
           // Make sure we have enough width to accommodate our width if we have override dimensions
           // [ldarg.s boxWidthOverride] (from above)
           // push Math.Max(stack, textureBoxDimensionModifiers.X)
           .Insert(
             new CodeInstruction(OpCodes.Ldloc_S, textureBoxDimensionModifiers),
             new CodeInstruction(OpCodes.Ldfld, typeof(Vector2).GetField(nameof(Vector2.X))),
             new CodeInstruction(OpCodes.Conv_I4),
             new CodeInstruction(OpCodes.Call, mathMax)
           );

    matcher.MatchEndForward(
             new CodeMatch(OpCodes.Ldarg_S, BoxHeightOverrideParamIdx),
             CreateLocMatcher(OpCodes.Stloc_S, Height1LocalIdx)
           )
           .ThrowIfNotMatch("Unable to find insertion point to add bundleHeight to height1")
           // Add the new height to the total height (if we have an override)
           .InsertAndAdvance(
             // height1 = boxHeightOverride != -1 ? bundleNameHeightOffset + boxHeightOverride : startingHeight
             new CodeInstruction(OpCodes.Ldloc_S, textureBoxDimensionModifiers),
             new CodeInstruction(OpCodes.Ldfld, typeof(Vector2).GetField(nameof(Vector2.Y))),
             new CodeInstruction(OpCodes.Conv_I4),
             new CodeInstruction(OpCodes.Add)
           )
           .Advance(1)
           // Add the height offset to y1 so that every subsequent call will be at the right offset.
           // We now have to store the original y1 so that the boxes draw at the mouse cursor, but this
           // is easier than setting both inside and outside the if statement.
           .Insert(
             // boxDrawYPos = y1;
             new CodeInstruction(OpCodes.Ldloc_S, Y1LocalIdx),
             new CodeInstruction(OpCodes.Stloc_S, boxDrawYPos),
             new CodeInstruction(OpCodes.Ldloc_S, Y1LocalIdx),
             // y1 += textureBoxDimensionModifiers.X
             new CodeInstruction(OpCodes.Ldloc_S, textureBoxDimensionModifiers),
             new CodeInstruction(OpCodes.Ldfld, typeof(Vector2).GetField(nameof(Vector2.Y))),
             new CodeInstruction(OpCodes.Conv_I4),
             new CodeInstruction(OpCodes.Add),
             new CodeInstruction(OpCodes.Stloc_S, Y1LocalIdx)
           );

    // Replace Y1 for the next 2 instances.
    //    drawTextureBox (main box)
    //    drawTextureBox (title box)
    matcher.MatchStartForward(CreateLocMatcher(OpCodes.Ldloc_S, Y1LocalIdx))
           .ThrowIfNotMatch("Unable to find Y1 main box")
           // Replace the instruction with our saved drawYPos
           .Set(OpCodes.Ldloc_S, boxDrawYPos);

    // drawTextureBox (title box)
    matcher.MatchStartForward(CreateLocMatcher(OpCodes.Ldloc_S, Y1LocalIdx))
           .ThrowIfNotMatch("Unable to find Y1 title box")
           // Replace the instruction with our saved drawYPos
           .Set(OpCodes.Ldloc_S, boxDrawYPos);

    // Modify title texture box height
    matcher.MatchEndForward(
             new CodeMatch(OpCodes.Ldfld, typeof(Vector2).GetField(nameof(Vector2.Y))),
             new CodeMatch(OpCodes.Conv_I4),
             new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)32),
             new CodeMatch(OpCodes.Add)
           )
           .ThrowIfNotMatch("Couldn't find insertion point to modify title box height")
           .Insert(
             new CodeInstruction(OpCodes.Add),
             new CodeInstruction(OpCodes.Ldloc_S, textureBoxDimensionModifiers).MoveLabelsFrom(matcher.Instruction),
             new CodeInstruction(OpCodes.Ldfld, typeof(Vector2).GetField(nameof(Vector2.Y))),
             new CodeInstruction(OpCodes.Conv_I4)
           );
  }

  private static void PatchDrawUIIconsHook(CodeMatcher matcher, LocalBuilder boxDrawYPos)
  {
    matcher.Start()
           .MatchEndForward(
             new CodeMatch(OpCodes.Conv_I4),
             new CodeMatch(OpCodes.Add),
             CreateLocMatcher(OpCodes.Stloc_S, Y1LocalIdx),
             new CodeMatch(OpCodes.Ldarg_S, (byte)9),
             new CodeMatch(i => i.opcode == OpCodes.Brfalse)
           )
           .ThrowIfNotMatch("Failed to find insertion point for UI Icon Draw Hook");

    matcher.Advance(1)
           .Insert(
             // Load and call DrawBundlesAndIcons, the match has already checked if hoverItem != null
             new CodeInstruction(OpCodes.Ldarg_0),
             new CodeInstruction(OpCodes.Ldarg_S, 9),
             new CodeInstruction(OpCodes.Ldloc_S, XLocalIdx),
             new CodeInstruction(OpCodes.Ldloc_S, boxDrawYPos),
             new CodeInstruction(OpCodes.Ldloc_S, Height1LocalIdx),
             new CodeInstruction(OpCodes.Ldloc_S, Width2LocalIdx),
             new CodeInstruction(
               OpCodes.Call,
               typeof(ClickableMenuPatches).GetMethod(nameof(DrawBundlesAndIcons), AllFlags)
             )
           );
  }

#region Patch Helpers and Hooks
  private static Vector2 AddOffsetsIfBundleItem(Item? hoverItem, int heightToAdd, ref int startingHeightRef)
  {
    if (hoverItem == null)
    {
      return Vector2.Zero;
    }

    BundleRequiredItem? bundleInfo = BundleHelper.GetBundleItemIfNotDonated(hoverItem);
    if (bundleInfo == null)
    {
      return Vector2.Zero;
    }

    startingHeightRef += heightToAdd;
    return new Vector2(bundleInfo.BannerWidth, heightToAdd);
  }

  private static void DrawBundlesAndIcons(
    SpriteBatch spriteBatch,
    Item hoverItem,
    int x1,
    int y1,
    int boxHeight,
    int boxWidth
  )
  {
    DrawBundleTitle(spriteBatch, hoverItem, x1, y1, boxWidth);
    DrawShipmentBox(spriteBatch, hoverItem, x1, y1, boxWidth);
  }

  private static void DrawBundleTitle(SpriteBatch spriteBatch, Item hoverItem, int x1, int y1, int boxWidth)
  {
    BundleRequiredItem? bundleInfo = BundleHelper.GetBundleItemIfNotDonated(hoverItem);
    if (bundleInfo == null)
    {
      return;
    }

    Color? bundleColor = BundleHelper.GetRealColorFromIndex(bundleInfo.Id);

    ShowItemHoverInformation.DrawBundleBanner(
      spriteBatch,
      bundleInfo.Name,
      new Vector2(x1 - 7, y1 - 13),
      boxWidth,
      bundleColor?.Desaturate(0.35f) // TODO cache these colors so we're not doing it every time
    );
  }

  private static void DrawShipmentBox(SpriteBatch spriteBatch, Item hoverItem, int x1, int y1, int boxWidth)
  {
    if (hoverItem is not Object obj ||
        !obj.countsForShippedCollection() ||
        !Object.isPotentialBasicShipped(obj.ItemId, obj.Category, obj.Type) ||
        Game1.player.basicShipped.ContainsKey(obj.ItemId))
    {
      return;
    }

    ShowItemHoverInformation.DrawShippingBin(
      spriteBatch,
      new Vector2(x1 + boxWidth - 6, y1 + 8),
      _shippingBinDimensions / 2
    );
  }
#endregion
}
