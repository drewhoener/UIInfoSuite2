using StardewValley;

namespace UIInfoSuite2.Infrastructure.Extensions;

internal static class ItemExtensions
{
  public static bool IsSimilar(this Item item, Item? other)
  {
    return Tools.AreItemsSimilar(item, other);
  }
}
