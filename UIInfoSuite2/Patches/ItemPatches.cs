using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using StardewValley;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Events;

namespace UIInfoSuite2.Patches;

internal class ItemPatches
{
  private static Item? _lastItem;

  public static void Apply(Harmony harmony)
  {
    const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic;
    harmony.Patch(
      typeof(Item).GetMethod("getDescriptionWidth", flag),
      postfix: new HarmonyMethod(typeof(ItemPatches), nameof(PatchDefaultDescriptionWidth_Postfix))
    );

    harmony.Patch(
      typeof(Item).GetMethod(nameof(Item.actionWhenBeingHeld)),
      new HarmonyMethod(typeof(ItemPatches), nameof(Item_ActionWhenBeingHeld_Prefix))
    );

    harmony.Patch(
      typeof(Item).GetMethod(nameof(Item.actionWhenStopBeingHeld)),
      new HarmonyMethod(typeof(ItemPatches), nameof(Item_ActionWhenStopBeingHeld_Prefix))
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

  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
  private static void Item_ActionWhenBeingHeld_Prefix(Farmer who, Item __instance)
  {
    if (Tools.AreItemsSimilar(__instance, _lastItem))
    {
      return;
    }

    _lastItem = __instance;
    EventManager.InvokePlayerItemChange(who, __instance, ToolChangeAction.Equip);
  }

  [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
  private static void Item_ActionWhenStopBeingHeld_Prefix(Farmer who, Item __instance)
  {
    EventManager.InvokePlayerItemChange(who, __instance, ToolChangeAction.Unequip);
  }
}
