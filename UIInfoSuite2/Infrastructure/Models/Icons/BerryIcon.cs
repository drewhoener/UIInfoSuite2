using Microsoft.Xna.Framework;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class BerryIcon : ClickableIcon
{
  private bool _isHazelnut;
  private bool _todayHasBerry;

  public BerryIcon() : base(Game1.objectSpriteSheet, new Rectangle(128, 193, 15, 15), 40)
  {
    UpdateBerryForDay();
  }

  private void ChangeBerryIcon(Rectangle bounds, string text)
  {
    SetSourceBounds(bounds);
    HoverText = text;
    _todayHasBerry = true;
  }

  public void UpdateBerryForDay()
  {
    string? season = Game1.currentSeason;
    int day = Game1.dayOfMonth;
    switch (season)
    {
      case "spring" when day is >= 15 and <= 18:
        ChangeBerryIcon(new Rectangle(128, 193, 15, 15), I18n.CanFindSalmonberry());
        break;
      case "fall" when day is >= 8 and <= 11:
        ChangeBerryIcon(new Rectangle(32, 272, 16, 16), I18n.CanFindBlackberry());
        break;
      case "fall" when day >= 15 && Config.ShowSeasonalBerryHazelnutIcon:
        ChangeBerryIcon(new Rectangle(1, 274, 14, 14), I18n.CanFindHazelnut());
        _isHazelnut = true;
        break;
      default:
        _todayHasBerry = false;
        _isHazelnut = false;
        break;
    }
  }

  protected override bool _ShouldDraw()
  {
    if (_isHazelnut && !Config.ShowSeasonalBerryHazelnutIcon)
    {
      return false;
    }

    return _todayHasBerry && base._ShouldDraw();
  }
}
