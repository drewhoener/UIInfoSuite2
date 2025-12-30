using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace UIInfoSuite2.Infrastructure.Extensions;

public static class ObjectExtensions
{
  public static Rectangle GetHeadShot(this NPC npc)
  {
    int size = NpcHeadShotSize.GetValueOrDefault(npc.Name, 4);

    Rectangle mugShotSourceRect = npc.getMugShotSourceRect();
    mugShotSourceRect.Height -= size / 2;
    mugShotSourceRect.Y -= size / 2;
    return mugShotSourceRect;
  }

  public static string GetCropString(this Crop? crop)
  {
    if (crop == null)
    {
      return "null";
    }

    return $"Crop[" +
           $"Type={(!string.IsNullOrEmpty(crop.netSeedIndex.Value) ? crop.netSeedIndex.Value : crop.whichForageCrop.Value)}, " +
           $"Phase={crop.currentPhase.Value}/{crop.phaseDays.Count - 1}, " + // -1 because last phase is finalPhaseLength
           $"DayOfPhase={crop.dayOfCurrentPhase.Value}, " +
           $"Harvest={crop.indexOfHarvest.Value}, " +
           $"FullyGrown={crop.fullyGrown.Value}, " +
           $"Dead={crop.dead.Value}, " +
           $"ForageCrop={crop.forageCrop.Value}" +
           (crop.forageCrop.Value ? $", ForageType={crop.whichForageCrop.Value}" : "") +
           (crop.programColored.Value ? $", Tint={crop.tintColor.Value}" : "") +
           "]";
  }

  public static string SafeGetString(this IModHelper helper, string key)
  {
    var result = string.Empty;

    if (!string.IsNullOrEmpty(key) && helper != null)
    {
      result = helper.Translation.Get(key);
    }

    return result;
  }

  public static int OrZero(this int? nullable)
  {
    return nullable ?? 0;
  }

  public static float OrZero(this float? nullable)
  {
    return nullable ?? 0.0f;
  }

  public static double OrZero(this double? nullable)
  {
    return nullable ?? 0.0;
  }

  public static bool IsSolarPanel(this Object? tileObject)
  {
    if (tileObject == null)
    {
      return false;
    }

    return tileObject.bigCraftable.Value && tileObject.ItemId == SolarPanelId;
  }

  public static bool IsWorking(this Object? tileObject)
  {
    if (tileObject == null)
    {
      return false;
    }

    if (tileObject is Cask cask)
    {
      return cask.daysToMature.Value > 0;
    }

    if (!tileObject.bigCraftable.Value)
    {
      return false;
    }

    if (tileObject.IsSolarPanel())
    {
      return tileObject.MinutesUntilReady > 0 || tileObject.heldObject.Value == null;
    }

    return tileObject.MinutesUntilReady > 0;
  }

  public static Rectangle ToRectangle(this Vector2 vector, int width, int height)
  {
    return new Rectangle((int)vector.X, (int)vector.Y, width, height);
  }

  public static Rectangle ToRectangle(this Point vector, int width, int height)
  {
    return new Rectangle(vector.X, vector.Y, width, height);
  }

#region Properties
  private const string SolarPanelId = "231";

  private static readonly Dictionary<string, int> NpcHeadShotSize = new()
  {
    { "Pierre", 9 },
    { "Sebastian", 7 },
    { "Evelyn", 5 },
    { "Penny", 6 },
    { "Jas", 6 },
    { "Caroline", 5 },
    { "Dwarf", 5 },
    { "Sam", 9 },
    { "Maru", 6 },
    { "Wizard", 9 },
    { "Jodi", 7 },
    { "Krobus", 7 },
    { "Alex", 8 },
    { "Kent", 10 },
    { "Linus", 4 },
    { "Harvey", 9 },
    { "Shane", 8 },
    { "Haley", 6 },
    { "Robin", 7 },
    { "Marlon", 2 },
    { "Emily", 8 },
    { "Marnie", 5 },
    { "Abigail", 7 },
    { "Leah", 6 },
    { "George", 5 },
    { "Elliott", 9 },
    { "Gus", 7 },
    { "Lewis", 8 },
    { "Demetrius", 11 },
    { "Pam", 5 },
    { "Vincent", 6 },
    { "Sandy", 7 },
    { "Clint", 10 },
    { "Willy", 10 }
  };
#endregion
}
