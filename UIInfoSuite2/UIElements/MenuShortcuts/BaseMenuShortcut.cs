using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

public abstract class BaseMenuShortcut
{
  protected readonly PerScreen<ClickableTextureComponent?> PerScreenMenuButton = new(() => null);

  protected BaseMenuShortcut(int renderedHeight)
  {
    RenderedHeight = renderedHeight;
  }

  public int RenderedHeight { get; }
  public abstract int RenderedWidth { get; }
  protected abstract float ScaleFactor { get; }
  protected abstract Texture2D Texture { get; }
  protected abstract Rectangle SourceRectangle { get; }

  protected ClickableTextureComponent MenuButton
  {
    get
    {
      PerScreenMenuButton.Value ??= new ClickableTextureComponent(
        new Rectangle(0, 0, RenderedWidth, RenderedHeight),
        Texture,
        SourceRectangle,
        ScaleFactor
      );

      return PerScreenMenuButton.Value;
    }
  }

  public virtual bool ShouldDraw => true;

  protected virtual string GetHoverText()
  {
    return "";
  }

  protected virtual SpriteFont GetHoverTextFont()
  {
    return Game1.dialogueFont;
  }

  public virtual void Draw(SpriteBatch batch, int xStart, int yStart)
  {
    if (!ShouldDraw)
    {
      return;
    }

    MenuButton.bounds.X = xStart;
    MenuButton.bounds.Y = yStart;
    MenuButton.baseScale = ScaleFactor;

    MenuButton.draw(batch);
  }

  public virtual void DrawHoverText(SpriteBatch batch)
  {
    if (!MenuButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      return;
    }

    string hoverText = GetHoverText();
    if (string.IsNullOrEmpty(hoverText))
    {
      return;
    }

    IClickableMenu.drawHoverText(batch, hoverText, GetHoverTextFont());
  }

  public virtual void OnClick(object? sender, ButtonPressedEventArgs args)
  {
    if (args.Button != SButton.MouseLeft ||
        Game1.player.CursorSlotItem is not null ||
        Game1.activeClickableMenu is not GameMenu gameMenu ||
        gameMenu.currentTab == GameMenu.mapTab)
    {
      return;
    }

    Vector2 mouseCoords = Utility.ModifyCoordinatesForUIScale(new Vector2(Game1.getMouseX(), Game1.getMouseY()));
    if (!MenuButton.containsPoint((int)mouseCoords.X, (int)mouseCoords.Y))
    {
      return;
    }

    HandleClickEvent(sender, args, mouseCoords);
  }

  protected abstract void HandleClickEvent(object? sender, ButtonPressedEventArgs args, Vector2 mouseCoords);
}
