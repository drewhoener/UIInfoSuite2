using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts;

internal class SpecialOrderMenuShortcut(int finalHeight) : MenuShortcutElement(finalHeight)
{
  private const float InitialHeight = 31;
  private const float InitialWidth = 48;

  private readonly Lazy<Texture2D> _texture =
    new(() => Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town")));

  protected override float ScaleFactor => RenderedHeight / InitialHeight;
  protected override Texture2D Texture => _texture.Value;
  protected override Rectangle SourceRectangle => new(464, 993, (int)InitialWidth, (int)InitialHeight);

  public override bool ShouldDraw
  {
    get
    {
#if DEBUG
      return true;
#else
      return Game1.player.eventsSeen.Contains("15389722");
#endif
    }
  }

  protected override string GetHoverText()
  {
    return I18n.SpecialOrders();
  }

  protected override void HandleClickEvent(object? sender, ButtonPressedEventArgs args, Vector2 mouseCoords)
  {
    Game1.activeClickableMenu.SetChildMenu(new SpecialOrdersBoard());
  }
}
