using HarmonyLib;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Patches.ExtensibleItemTooltips;

internal class ExtensibleItemTooltips : IPatchable
{
  public void Patch(Harmony harmony)
  {
    IClickableMenu_Patches.Patch(harmony);
    Item_Patches.Patch(harmony);
    ShopMenu_Patches.Patch(harmony);
  }
}
