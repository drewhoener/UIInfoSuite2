using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Models;

public class ClickableIcon
{
  private readonly Texture2D _baseTexture;
  private readonly PerScreen<string> _perScreenHoverText = new(() => "");
  private readonly PerScreen<ClickableTextureComponent?> _perScreenIcon = new(() => null);
  private readonly ScalingDimensions _scalingDimensions;
  private readonly Rectangle _sourceBounds;

  public ClickableIcon(
    Texture2D baseTexture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    Action<object?, ButtonPressedEventArgs, Vector2>? clickHandlerAction = null,
    SpriteFont? hoverFont = null
  )
  {
    _baseTexture = baseTexture;
    _sourceBounds = sourceBounds;
    _scalingDimensions = new ScalingDimensions(_sourceBounds, finalSize, primaryDimension);
    ClickHandlerAction = clickHandlerAction;
    HoverFont = hoverFont ?? Game1.dialogueFont;
  }

  public string HoverText
  {
    get => _perScreenHoverText.Value;
    set => _perScreenHoverText.Value = value;
  }

  public SpriteFont HoverFont { get; }

  public Action<object?, ButtonPressedEventArgs, Vector2>? ClickHandlerAction { private get; set; }

  protected ClickableTextureComponent Icon
  {
    get
    {
      _perScreenIcon.Value ??= new ClickableTextureComponent(
        new Rectangle(0, 0, _scalingDimensions.WidthInt, _scalingDimensions.HeightInt),
        _baseTexture,
        _sourceBounds,
        _scalingDimensions.ScaleFactor
      );

      return _perScreenIcon.Value;
    }
  }

  public void MoveTo(int x, int y)
  {
    Icon.setPosition(x, y);
    Icon.baseScale = _scalingDimensions.ScaleFactor;
  }

  public void MoveTo(Point point)
  {
    Icon.setPosition(point.X, point.Y);
    Icon.baseScale = _scalingDimensions.ScaleFactor;
  }

  public virtual void Draw(SpriteBatch batch)
  {
    Icon.draw(batch);
  }

  public virtual void Draw(
    SpriteBatch b,
    Color c,
    float layerDepth,
    int frameOffset = 0,
    int xOffset = 0,
    int yOffset = 0
  )
  {
    Icon.draw(b, c, layerDepth, frameOffset, xOffset, yOffset);
  }

  public virtual void DrawHoverText(SpriteBatch batch)
  {
    if (!Icon.IsHoveredOver())
    {
      return;
    }

    if (string.IsNullOrEmpty(HoverText))
    {
      return;
    }

    IClickableMenu.drawHoverText(batch, HoverText, HoverFont);
  }

  public virtual void OnClick(object? sender, ButtonPressedEventArgs args)
  {
    if (ClickHandlerAction is null ||
        args.Button != SButton.MouseLeft ||
        Game1.player.CursorSlotItem is not null ||
        Game1.activeClickableMenu is not GameMenu gameMenu ||
        gameMenu.currentTab == GameMenu.mapTab)
    {
      return;
    }

    Vector2 mouseCoords = Utility.ModifyCoordinatesForUIScale(new Vector2(Game1.getMouseX(), Game1.getMouseY()));
    if (!Icon.containsPoint((int)mouseCoords.X, (int)mouseCoords.Y))
    {
      return;
    }

    ClickHandlerAction.Invoke(sender, args, mouseCoords);
  }
}
