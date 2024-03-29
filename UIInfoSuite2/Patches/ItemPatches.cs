using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.Patches;

internal class ItemPatches
{
  public static void Apply(Harmony harmony)
  {
    const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
    harmony.Patch(
      typeof(Item).GetMethod("getDescriptionWidth", flag),
      postfix: new HarmonyMethod(typeof(ItemPatches), nameof(PatchDefaultDescriptionWidth_Postfix))
    );
  }


  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
  private static void PatchDefaultDescriptionWidth_Postfix(ref int __result, Item __instance)
  {
    BundleRequiredItem? bundleInfo = BundleHelper.GetBundleItemIfNotDonated(__instance);
    if (bundleInfo == null)
    {
      return;
    }

    __result = Math.Max(__result, bundleInfo.BannerWidth);
  }
}
