using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using StardewValley;
using UIInfoSuite2.Infrastructure.Models;

namespace UIInfoSuite2.Infrastructure.Patches.ExtensibleItemTooltips;

[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
internal static class Item_Patches
{
  public static void Patch(Harmony harmony)
  {
    harmony.Patch(
      AccessTools.DeclaredMethod(typeof(Item), "getDescriptionWidth"),
      postfix: new HarmonyMethod(typeof(Item_Patches), nameof(Item_getDescriptionWidth_Postfix))
    );
  }

  /// <summary>
  ///   Patch into get description width to make sure that the width is enough to accomodate the name
  ///   of the bundles it can be donated to
  /// </summary>
  /// <param name="__result"></param>
  private static void Item_getDescriptionWidth_Postfix(ref int __result)
  {
    int maxWidth = TooltipExtensionRegistry.MaxWidth(TooltipExtensionRegistry.AllPoints);
    __result = Math.Max(__result, maxWidth);
  }
}
