using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Models;

namespace UIInfoSuite2.Infrastructure.Patches.ExtensibleItemTooltips;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
internal static class ShopMenu_Patches
{
  public static void Patch(Harmony harmony)
  {
    /*
     * Some interesting behavior regarding this patch (here be dragons):
     *
     * ShopMenu#performHoverAction sets up hover text, money, etc. when hovering over an item in your inventory.
     * However, hoveredItem is only set if the player is hovering over an item FOR SALE IN THE SHOP!
     * Player inventory items don't count as hoverItem in ShopMenu.
     * So, in ShopMenu#draw when the menu tries to draw the tooltip for the hovered item,
     * item info will never be passed along to the tooltip extension registry, or rather IClickableMenu#drawHoverText
     * will always try and call TooltipExtensionRegistry#UpdateHoverItem with a null value (since that's what ShopMenu passes).
     *
     * As a sort of band-aid fix, we patch into performHoverAction and set TooltipExtensionRegistry.ShopMenuHoverItem
     * if the item is one in our inventory. ShopMenu#draw -> IClickableMenu#drawHoverText will try and call
     * TooltipExtensionRegistry#UpdateHoverItem with null after we've done this (since the call passed no item),
     * and we hook all calls into IClickableMenu#drawHoverText. So we have the registry ignore the call if the incoming value
     * is null, and ShopHoverItem is NOT null. At some point we can unset it through ShopHoverItem_set.
     *
     * That call actually has to be RIGHT IN THE MIDDLE of ShopMenu#draw, because the dialogue of the shopkeep is ALSO
     * drawn with IClickableMenu#drawHoverText. If we don't remove the ShopHoverItem, it'll be shown at the end of their dialogue.
     * So we patch right before `base.draw(spriteBatch);` in ShopMenu#draw. This has the side effect of flip-flopping the visibility
     * a couple of times a frame, which could be an issue. But in timings it doesn't seem to add more than a few μs, which is fine in my book.
     *
     * Should probably keep an eye on it in case a TON of items get added to tooltips, but for the case of UIInfoSuite
     * I don't think it'll ever come to that.
     *
     * Thank you for attending my TED Talk
     */

    try
    {
      harmony.Patch(
        AccessTools.DeclaredMethod(typeof(ShopMenu), nameof(ShopMenu.performHoverAction)),
        transpiler: new HarmonyMethod(typeof(ShopMenu_Patches), nameof(Transpile_ShopMenu_performHoverAction)),
        postfix: new HarmonyMethod(typeof(ShopMenu_Patches), nameof(Postfix_ShopMenu_performHoverAction))
      );

      harmony.Patch(
        AccessTools.DeclaredMethod(typeof(ShopMenu), nameof(ShopMenu.draw)),
        transpiler: new HarmonyMethod(typeof(ShopMenu_Patches), nameof(Transpile_ShopMenu_draw))
      );
    }
    catch (Exception)
    {
      ModEntry.Instance.Monitor.Log(
        "[ShopMenuPatcher] Couldn't find patch point for ShopMenu#performHoverAction, item hover info will not be available in Shops",
        LogLevel.Warn
      );
    }
  }

  private static IEnumerable<CodeInstruction> Transpile_ShopMenu_performHoverAction(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator gen
  )
  {
    const byte localClickableComponent = 5;

    var m = new CodeMatcher(instructions, gen);

    m.Start()
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(ShopMenu), "_isStorageShop")),
        new CodeMatch(OpCodes.Brfalse_S)
      )
      .MatchEndForward(new CodeMatch(OpCodes.Stfld, AccessTools.DeclaredField(typeof(ShopMenu), "hoverPrice")))
      .ThrowIfInvalid("");

    m.Advance(1)
      .Insert(
        new CodeInstruction(OpCodes.Ldloc_S, localClickableComponent),
        new CodeInstruction(
          OpCodes.Call,
          AccessTools.DeclaredPropertySetter(
            typeof(TooltipExtensionRegistry),
            nameof(TooltipExtensionRegistry.ShopMenuHoverItem)
          )
        )
      );

    return m.InstructionEnumeration();
  }

  private static IEnumerable<CodeInstruction> Transpile_ShopMenu_draw(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator gen
  )
  {
    var m = new CodeMatcher(instructions, gen);

    /*
     * base.draw(spriteBatch);
     *
     * IL_0b2f: ldarg.0      // this
     * IL_0b30: ldarg.1      // b
     * IL_0b31: call         instance void StardewValley.Menus.IClickableMenu::draw(class [MonoGame.Framework]Microsoft.Xna.Framework.Graphics.SpriteBatch)
     */
    m.Start()
      .MatchStartForward(
        new CodeMatch(OpCodes.Ldarg_0),
        new CodeMatch(OpCodes.Ldarg_1),
        new CodeMatch(
          OpCodes.Call,
          AccessTools.DeclaredMethod(typeof(IClickableMenu), nameof(IClickableMenu.draw), [typeof(SpriteBatch)])
        )
      )
      .ThrowIfInvalid("");

    m.Insert(
      new CodeInstruction(OpCodes.Ldnull).MoveLabelsFrom(m.Instruction), //!< Grab labels so we don't get jumped over
      new CodeInstruction(
        OpCodes.Call,
        AccessTools.DeclaredPropertySetter(
          typeof(TooltipExtensionRegistry),
          nameof(TooltipExtensionRegistry.ShopMenuHoverItem)
        )
      )
    );

    return m.InstructionEnumeration();
  }

  private static void Postfix_ShopMenu_performHoverAction(ShopMenu __instance)
  {
    if (__instance.hoverText == null)
    {
      TooltipExtensionRegistry.ShopMenuHoverItem = null;
    }
  }
}
