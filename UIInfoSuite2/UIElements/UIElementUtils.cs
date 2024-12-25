using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.UIElements;

public static class UIElementUtils
{
  public static bool IsRenderingNormally()
  {
    bool[] conditions =
    [
      !Game1.game1.takingMapScreenshot,
      !Game1.eventUp || Game1.isFestival(),
      !Game1.viewportFreeze,
      !Game1.freezeControls,
      Game1.viewportHold <= 0,
      Game1.displayHUD,
      Context.IsWorldReady,
      Game1.currentLocation != null
    ];

    return conditions.All(condition => condition);
  }
}
