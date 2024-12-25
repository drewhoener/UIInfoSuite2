using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class NpcBirthdayIcon(NPC character) : ClickableIcon(Game1.mouseCursors, new Rectangle(229, 410, 14, 14), 40)
{
  private const float HeadshotScale = 2.3f;
  private readonly Vector2 _headshotOffsetPosition = new(-10, -5);
  private readonly Rectangle _headshotRect = character.GetHeadShot();
  public readonly NPC Character = character;

  public override void Draw(SpriteBatch batch)
  {
    // Draw Present Icon
    base.Draw(batch);
    // Draw headshot offset to lower left
    batch.Draw(
      Character.Sprite.Texture,
      Position + _headshotOffsetPosition,
      _headshotRect,
      Color.White,
      0.0f,
      Vector2.Zero,
      HeadshotScale,
      SpriteEffects.None,
      1f
    );
  }
}
