using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class NpcBirthdayIcon(NPC character) : ClickableIcon(Game1.mouseCursors, new Rectangle(229, 410, 14, 14), 40)
{
  private const float HeadshotScale = 2.3f;
  private readonly PerScreen<bool> _canBeGiftedToday = new(() => true);
  private readonly ConfigManager _configManager = ModEntry.GetSingleton<ConfigManager>();
  private readonly Vector2 _headshotOffsetPosition = new(-10, -5);
  private readonly Rectangle _headshotRect = character.GetHeadShot();

  private ModConfig Config => _configManager.Config;

  private Friendship? Friendship
  {
    get
    {
      Game1.player.friendshipData.TryGetValue(character.Name, out Friendship? friendship);
      return friendship;
    }
  }

  private bool CanBeGiftedToday
  {
    get => _canBeGiftedToday.Value;
    set => _canBeGiftedToday.Value = value;
  }

  public override void Draw(SpriteBatch batch)
  {
    // Draw Present Icon
    base.Draw(batch);
    // Draw headshot offset to lower left
    batch.Draw(
      character.Sprite.Texture,
      IconPosition + _headshotOffsetPosition,
      _headshotRect,
      Color.White,
      0.0f,
      Vector2.Zero,
      HeadshotScale,
      SpriteEffects.None,
      1f
    );
  }

  public void UpdateGiftCheck()
  {
    if (!CanBeGiftedToday)
    {
      return;
    }

    // Mark as not giftable if we've given gifts or have no friendship
    if (Friendship?.GiftsToday > 0 || Friendship is null)
    {
      CanBeGiftedToday = false;
    }
  }

  protected override bool _ShouldDraw()
  {
    return base._ShouldDraw() && (!Config.HideAfterGiftGiven || CanBeGiftedToday);
  }
}
