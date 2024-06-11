using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

public class CalendarQuestMenuShortcut : BaseMenuShortcut
{
  private const float InitialHeight = 20;
  private const float InitialWidth = 35;

  private readonly Lazy<Texture2D> _texture = new(
    () => Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town"))
  );

  public CalendarQuestMenuShortcut(int finalHeight) : base(finalHeight) { }

  public override int RenderedWidth => (int)(InitialWidth * ScaleFactor);
  protected override float ScaleFactor => RenderedHeight / InitialHeight;
  protected override Texture2D Texture => _texture.Value;
  protected override Rectangle SourceRectangle => new(122, 291, (int)InitialWidth, (int)InitialHeight);

  protected override string GetHoverText()
  {
    return Game1.getMouseX() < MenuButton.bounds.X + MenuButton.bounds.Width / 2 ? I18n.Calendar() : I18n.Billboard();
  }

  protected override void HandleClickEvent(object? sender, ButtonPressedEventArgs args, Vector2 mouseCoords)
  {
    if (Game1.questOfTheDay != null && string.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
    {
      Game1.questOfTheDay.currentObjective = "wat?";
    }

    bool showDailyQuest = mouseCoords.X >= MenuButton.bounds.X + MenuButton.bounds.Width / 2f;
    Game1.activeClickableMenu = new Billboard(showDailyQuest);
  }
}
