using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class ClickableIcon
{
  /// <summary>
  ///   If the icon has decided it shouldn't be rendered, or some other event that might
  ///   invalidate caching
  /// </summary>
  private readonly PerScreen<bool> _hasRenderingChanged = new(() => false);

  private readonly PerScreen<string> _hoverText = new(() => string.Empty);
  private readonly PerScreen<ClickableTextureComponent> _icon;
  private readonly PerScreen<bool> _lastShouldDraw = new(() => false);
  protected readonly PerScreen<Texture2D> BaseTexture;

  protected readonly ConfigManager ConfigManager = ModEntry.GetSingleton<ConfigManager>();
  protected readonly PerScreen<AspectLockedDimensions> ScalingDimensions;

  public ClickableIcon(
    Texture2D baseTexture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    Action<object?, ButtonPressedEventArgs, Vector2>? clickHandlerAction = null,
    SpriteFont? hoverFont = null
  )
  {
    BaseTexture = new PerScreen<Texture2D>(() => baseTexture);
    ScalingDimensions = new PerScreen<AspectLockedDimensions>(
      () => new AspectLockedDimensions(sourceBounds, finalSize, primaryDimension)
    );

    _icon = new PerScreen<ClickableTextureComponent>(GenerateTextureComponent);

    ResetTextureComponent();
    ClickHandlerAction = clickHandlerAction;
    HoverFont = hoverFont ?? Game1.dialogueFont;
    AutoDrawDelegate = Draw;
  }

  protected ModConfig Config => ConfigManager.Config;

  public string HoverText
  {
    get => _hoverText.Value;
    set => _hoverText.Value = value;
  }

  public AspectLockedDimensions Dimensions => ScalingDimensions.Value;

  public SpriteFont HoverFont { get; }

  public Rectangle SourceBounds
  {
    get => ScalingDimensions.Value.SourceDimensions;
    set
    {
      _hasRenderingChanged.Value = true;
      ScalingDimensions.Value.SourceDimensions = value;
    }
  }

  public Action<SpriteBatch> AutoDrawDelegate { get; set; }

  public Action<object?, ButtonPressedEventArgs, Vector2>? ClickHandlerAction { private get; set; }

  protected ClickableTextureComponent Icon => _icon.Value;

  public Vector2 IconPosition => new(Icon.bounds.X, Icon.bounds.Y);

  public void SetSourceBounds(Rectangle rectangle, bool generateComponent = true)
  {
    SourceBounds = rectangle;
    if (generateComponent)
    {
      ResetTextureComponent();
    }
  }

  private ClickableTextureComponent GenerateTextureComponent()
  {
    return new ClickableTextureComponent(
      new Rectangle(0, 0, Dimensions.Width, Dimensions.Height),
      BaseTexture.Value,
      SourceBounds,
      Dimensions.ScaleFactor
    );
  }

  protected void ResetTextureComponent()
  {
    _icon.Value = GenerateTextureComponent();
  }

  public bool HasRenderingChanged(bool markClean = true)
  {
    bool dirty = _hasRenderingChanged.Value;
    if (markClean)
    {
      _hasRenderingChanged.Value = false;
    }

    return dirty;
  }

  protected virtual bool _ShouldDraw()
  {
    return true;
  }

  public bool ShouldDraw()
  {
    bool res = _ShouldDraw();
    if (res == _lastShouldDraw.Value)
    {
      return res;
    }

    _hasRenderingChanged.Value = true;
    _lastShouldDraw.Value = res;
    return res;
  }

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
