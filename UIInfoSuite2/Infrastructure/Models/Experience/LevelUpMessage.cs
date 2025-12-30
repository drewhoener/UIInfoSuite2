using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Models.Managers;

namespace UIInfoSuite2.Infrastructure.Models.Experience;

internal class LevelUpMessage(Rectangle skillRectangle) : FloatingText(
  I18n.LevelUp(),
  120,
  new Vector2(0, 0),
  id: "LevelUpMessage",
  zIndex: 100
)
{
  private static readonly Vector2 IconOffset = new(-74, -130);

  public override void Draw(SpriteBatch spriteBatch)
  {
    Vector2 iconPosition = Game1.player.getLocalPosition(Game1.viewport) + IconOffset;

    spriteBatch.Draw(
      Game1.mouseCursors,
      Utility.ModifyCoordinatesForUIScale(iconPosition),
      skillRectangle,
      Color.White,
      0,
      Vector2.Zero,
      Game1.pixelZoom,
      SpriteEffects.None,
      0.85f
    );

    base.Draw(spriteBatch);
  }
}
