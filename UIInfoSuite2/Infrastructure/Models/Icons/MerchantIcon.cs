using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class MerchantIcon : ClickableIcon
{
  private bool _merchantInTown;

  public MerchantIcon() : base(Game1.mouseCursors, new Rectangle(192, 1411, 20, 20), 40)
  {
    HoverText = I18n.TravelingMerchantIsInTown();
    UpdateMerchantIcon();
  }

  public bool VisitedMerchant { get; set; }

  public void UpdateMerchantIcon()
  {
    _merchantInTown = ((Forest)Game1.getLocationFromName(nameof(Forest))).ShouldTravelingMerchantVisitToday();
    VisitedMerchant = false;
  }

  protected override bool _ShouldDraw()
  {
    if (VisitedMerchant && Config.HideMerchantIconWhenVisited)
    {
      return false;
    }

    return _merchantInTown && base._ShouldDraw();
  }
}
