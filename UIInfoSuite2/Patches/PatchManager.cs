using HarmonyLib;

namespace UIInfoSuite2.Patches;

internal static class PatchManager
{
  public static void Apply(Harmony harmony)
  {
    ItemPatches.Apply(harmony);
    ClickableMenuPatches.Apply(harmony);
    GameStateQueryPatches.Apply(harmony);
  }
}
