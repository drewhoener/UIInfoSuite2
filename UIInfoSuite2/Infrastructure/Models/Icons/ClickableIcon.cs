using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

public class ClickableIcon
{
  // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
  private readonly PerScreen<Texture2D> _baseTexture;
  private readonly PerScreen<string> _hoverText = new(() => string.Empty);
  private readonly PerScreen<ClickableTextureComponent> _icon;
  private readonly PerScreen<ScalingDimensions> _scalingDimensions;

  // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
  private readonly PerScreen<Rectangle> _sourceBounds;

  public ClickableIcon(
    Texture2D baseTexture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    Action<object?, ButtonPressedEventArgs, Vector2>? clickHandlerAction = null,
    SpriteFont? hoverFont = null
  )
  {
    _baseTexture = new PerScreen<Texture2D>(() => baseTexture);
    _sourceBounds = new PerScreen<Rectangle>(() => sourceBounds);
    _scalingDimensions =
      new PerScreen<ScalingDimensions>(() => new ScalingDimensions(_sourceBounds.Value, finalSize, primaryDimension));

    _icon = new PerScreen<ClickableTextureComponent>(
      () => new ClickableTextureComponent(
        new Rectangle(0, 0, Dimensions.WidthInt, Dimensions.HeightInt),
        _baseTexture.Value,
        _sourceBounds.Value,
        Dimensions.ScaleFactor
      )
    );

    ClickHandlerAction = clickHandlerAction;
    HoverFont = hoverFont ?? Game1.dialogueFont;

    AutoDrawDelegate = Draw;
  }

  public string HoverText
  {
    get => _hoverText.Value;
    set => _hoverText.Value = value;
  }

  public ScalingDimensions Dimensions => _scalingDimensions.Value;

  public SpriteFont HoverFont { get; }

  public Action<SpriteBatch> AutoDrawDelegate { get; set; }

  public Action<object?, ButtonPressedEventArgs, Vector2>? ClickHandlerAction { private get; set; }

  public Func<bool> ShouldDraw { get; set; } = UIElementUtils.IsRenderingNormally;

  protected ClickableTextureComponent Icon => _icon.Value;

  public Vector2 Position => new(Icon.bounds.X, Icon.bounds.Y);

  public void MoveTo(int x, int y)
  {
    Icon.setPosition(x, y);
    Icon.baseScale = Dimensions.ScaleFactor;
  }

  public void MoveTo(Point point)
  {
    Icon.setPosition(point.X, point.Y);
    Icon.baseScale = Dimensions.ScaleFactor;
  }

  public virtual void Draw(SpriteBatch batch)
  {
    if (!ShouldDraw())
    {
      return;
    }

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
    if (!ShouldDraw())
    {
      return;
    }

    Icon.draw(b, c, layerDepth, frameOffset, xOffset, yOffset);
  }

  public virtual void DrawHoverText(SpriteBatch batch)
  {
    if (!Icon.IsHoveredOver() || !ShouldDraw())
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
