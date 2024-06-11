using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

public class CalendarQuestMenuShortcut : BaseMenuShortcut
{
  private const float InitialHeight = 20;
  private const float InitialWidth = 35;
  private readonly PerScreen<ClickableTextureComponent?> _menuButton = new(() => null);


  private readonly Lazy<Texture2D> _texture = new(
    () => Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town"))
  );

  public CalendarQuestMenuShortcut(int finalHeight) : base(finalHeight) { }

  private float ScaleFactor => RenderedHeight / InitialHeight;

  public override int RenderedWidth => (int)(InitialWidth * ScaleFactor);

  public override void Draw(SpriteBatch batch, int xStart, int yStart)
  {
    _menuButton.Value ??= new ClickableTextureComponent(
      new Rectangle(xStart, yStart, RenderedWidth, RenderedHeight),
      _texture.Value,
      new Rectangle(122, 291, (int)InitialWidth, (int)InitialHeight),
      ScaleFactor
    );

    _menuButton.Value.bounds.X = xStart;
    _menuButton.Value.bounds.Y = yStart;

    _menuButton.Value.draw(batch);
  }

  public override void DrawHoverText(SpriteBatch batch)
  {
    if (_menuButton.Value is null || !_menuButton.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      return;
    }

    string hoverText = Game1.getMouseX() < _menuButton.Value.bounds.X + _menuButton.Value.bounds.Width / 2
      ? I18n.Calendar()
      : I18n.Billboard();
    IClickableMenu.drawHoverText(batch, hoverText, Game1.dialogueFont);
  }

  public override void OnClick(object? sender, ButtonPressedEventArgs args)
  {
    if (args.Button != SButton.MouseLeft ||
        _menuButton.Value is null ||
        Game1.player.CursorSlotItem is not null ||
        Game1.activeClickableMenu is not GameMenu gameMenu ||
        gameMenu.currentTab == GameMenu.mapTab)
    {
      return;
    }

    Vector2 mouseCoords = Utility.ModifyCoordinatesForUIScale(new Vector2(Game1.getMouseX(), Game1.getMouseY()));
    if (!_menuButton.Value.containsPoint((int)mouseCoords.X, (int)mouseCoords.Y))
    {
      return;
    }

    if (Game1.questOfTheDay != null && string.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
    {
      Game1.questOfTheDay.currentObjective = "wat?";
    }

    bool showDailyQuest = mouseCoords.X >= _menuButton.Value.bounds.X + _menuButton.Value.bounds.Width / 2f;
    Game1.activeClickableMenu = new Billboard(showDailyQuest);
  }
}
