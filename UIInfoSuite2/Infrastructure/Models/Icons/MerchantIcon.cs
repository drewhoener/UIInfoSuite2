using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class MerchantIcon : ClickableIcon
{
  public enum Type
  {
    Traveler,
    Bookseller
  }

  private readonly Type _merchantType;

  private bool _merchantInTown;

  public MerchantIcon(Type merchantType) : base(Game1.mouseCursors, new Rectangle(192, 1411, 20, 20), 40)
  {
    _merchantType = merchantType;

    switch (_merchantType)
    {
      case Type.Traveler:
        HoverText = I18n.TravelingMerchantIsInTown();
        break;
      case Type.Bookseller:
        HoverText = I18n.BooksellerIsInTown();
        BaseTexture.Value = Game1.mouseCursors_1_6;
        SetSourceBounds(new Rectangle(5, 471, 23, 22));
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(merchantType));
    }

    UpdateMerchantIcon();
  }

  public bool VisitedMerchant { get; set; }

  public void UpdateMerchantIcon()
  {
    switch (_merchantType)
    {
      case Type.Traveler:
        _merchantInTown = ((Forest)Game1.getLocationFromName(nameof(Forest))).ShouldTravelingMerchantVisitToday();
        break;
      case Type.Bookseller:
        // Town location has the bookseller bool private, so we'll just do it like they do it.
        // No need to reflect if we don't have to.
        _merchantInTown = Utility.getDaysOfBooksellerThisSeason().Contains(Game1.dayOfMonth);
        break;
    }

    VisitedMerchant = false;
  }

  protected override bool _ShouldDraw()
  {
    if ((_merchantType == Type.Bookseller && !Config.ShowBooksellerIcon) ||
        (_merchantType == Type.Traveler && !Config.ShowTravelingMerchantIcon))
    {
      return false;
    }

    if (VisitedMerchant && Config.HideMerchantIconWhenVisited)
    {
      return false;
    }

    return _merchantInTown && base._ShouldDraw();
  }
}
