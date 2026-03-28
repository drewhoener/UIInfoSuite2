using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts;

/// <summary>
///   Base class for all menu shortcut buttons. Extends LayoutElement so that a LayoutContainer
///   handles positioning; the element itself handles rendering and input.
/// </summary>
internal abstract class MenuShortcutElement : LayoutElement
{
  protected readonly PerScreen<ClickableTextureComponent?> PerScreenMenuButton = new(() => null);

  protected MenuShortcutElement(int renderedHeight, string? identifier = null) : base(identifier)
  {
    RenderedHeight = renderedHeight;
  }

  protected int RenderedHeight { get; }

  protected abstract Texture2D Texture { get; }
  protected abstract Rectangle SourceRectangle { get; }
  protected abstract float ScaleFactor { get; }

  /// <summary>
  ///   The per-frame game-state condition that controls whether this shortcut should appear.
  ///   The module syncs this into <see cref="LayoutElement.IsHidden" /> before each draw pass.
  /// </summary>
  public virtual bool ShouldDraw => true;

  /// <summary>
  ///   The clickable component used for hit-testing and rendering. Lazily initialized once the
  ///   element has been measured so the initial bounds are correct.
  /// </summary>
  protected ClickableTextureComponent MenuButton
  {
    get
    {
      PerScreenMenuButton.Value ??= new ClickableTextureComponent(
        new Rectangle(0, 0, ContentSize.Width, ContentSize.Height),
        Texture,
        SourceRectangle,
        ScaleFactor
      );

      return PerScreenMenuButton.Value;
    }
  }

  // ──────────────────────────────────────────────────────────
  // LayoutElement implementation
  // ──────────────────────────────────────────────────────────

  protected override Dimensions MeasureContent()
  {
    return new Dimensions((int)(SourceRectangle.Width * ScaleFactor), RenderedHeight);
  }

  protected override void DrawContent(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    // Keep the component bounds in sync with the rendered position so that
    // hit-testing in OnClick / DrawHoverText is always current.
    MenuButton.bounds = new Rectangle(positionX, positionY, ContentSize.Width, ContentSize.Height);
    MenuButton.baseScale = ScaleFactor;
    MenuButton.draw(spriteBatch);
  }

  // ──────────────────────────────────────────────────────────
  // Hover text — drawn in a second pass by the module so it
  // renders on top of all shortcut icons
  // ──────────────────────────────────────────────────────────

  protected virtual string GetHoverText()
  {
    return "";
  }

  protected virtual SpriteFont GetHoverTextFont()
  {
    return Game1.dialogueFont;
  }

  public void DrawHoverText(SpriteBatch batch)
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

  // ──────────────────────────────────────────────────────────
  // Input handling
  // ──────────────────────────────────────────────────────────

  public virtual void OnClick(object? sender, ButtonPressedEventArgs args)
  {
    if (args.Button != SButton.MouseLeft ||
        Game1.player.CursorSlotItem is not null ||
        !Tools.IsGameMenuOpen() ||
        Tools.GetCurrentMenuPage() is MapPage)
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
