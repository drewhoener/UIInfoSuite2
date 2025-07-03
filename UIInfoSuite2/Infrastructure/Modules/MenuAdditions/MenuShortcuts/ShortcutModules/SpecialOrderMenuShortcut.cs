using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts.ShortcutModules;

public class SpecialOrderMenuShortcut : BaseMenuShortcut
{
  private const float InitialHeight = 31;
  private const float InitialWidth = 48;

  private readonly Lazy<Texture2D> _texture =
    new(() => Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town")));

  public SpecialOrderMenuShortcut(int finalHeight) : base(finalHeight) { }

  public override int RenderedWidth => (int)(InitialWidth * ScaleFactor);
  protected override float ScaleFactor => RenderedHeight / InitialHeight;
  protected override Texture2D Texture => _texture.Value;
  protected override Rectangle SourceRectangle => new(464, 993, (int)InitialWidth, (int)InitialHeight);

  protected override string GetHoverText()
  {
    return I18n.SpecialOrders();
  }

  protected override void HandleClickEvent(object? sender, ButtonPressedEventArgs args, Vector2 mouseCoords)
  {
    Game1.activeClickableMenu.SetChildMenu(new SpecialOrdersBoard());
  }
}
