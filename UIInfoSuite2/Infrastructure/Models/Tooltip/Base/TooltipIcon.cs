using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

internal class TooltipIcon : LayoutElement
{
  private readonly TrackableValue<float> _finalSize;
  private readonly TrackableValue<PrimaryDimension> _primaryDimension;
  private readonly TrackableValue<Rectangle> _sourceBounds;
  private readonly TrackableValue<Texture2D> _texture;
  private AspectLockedDimensions _dimensions;

  public TooltipIcon(
    Texture2D texture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    string? identifier = null
  ) : base(identifier)
  {
    _dimensions = new AspectLockedDimensions(sourceBounds, finalSize, primaryDimension);
    _texture = new TrackableValue<Texture2D>(texture, MeasureAndUpdate, "Texture");
    _sourceBounds = new TrackableValue<Rectangle>(sourceBounds, MeasureAndUpdate, "SourceBounds");
    _primaryDimension = new TrackableValue<PrimaryDimension>(primaryDimension, MeasureAndUpdate, "StretchDimension");
    _finalSize = new TrackableValue<float>(finalSize, MeasureAndUpdate, "FinalSize");

    MeasureAndUpdate("init");
  }

  public static TooltipIcon FromBundle(
    BundleRequiredItem bundleItem,
    int finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width
  )
  {
    return new TooltipIcon(
      bundleItem.Bundle.GetTexture(),
      bundleItem.Bundle.GetSourceRect(),
      finalSize,
      primaryDimension,
      $"bundle-tooltip-{bundleItem.ItemData.BundleId}"
    );
  }

  public static TooltipIcon FromNpc(NPC npc, int finalSize, PrimaryDimension primaryDimension = PrimaryDimension.Width)
  {
    return new TooltipIcon(npc.Sprite.Texture, npc.GetHeadShot(), finalSize, primaryDimension);
  }

  private void MeasureAndUpdate(string? sender)
  {
    AspectLockedDimensions previousDimensions = _dimensions;
    _dimensions = new AspectLockedDimensions(_sourceBounds.Value, _finalSize.Value, _primaryDimension.Value);
    if (previousDimensions.Bounds != _dimensions.Bounds)
    {
      MarkLayoutDirty(sender ?? "unknown");
    }
  }

  public void SetIcon(
    Texture2D texture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width
  )
  {
    _texture.SetAndMark(texture, runCallback: false);
    _sourceBounds.SetAndMark(sourceBounds, runCallback: false);
    _finalSize.SetAndMark(finalSize, runCallback: false);
    _primaryDimension.SetAndMark(primaryDimension, runCallback: false);

    MeasureAndUpdate("SetIcon");
  }

  public void SetSize(float finalSize, PrimaryDimension? primaryDimension = null)
  {
    PrimaryDimension dimension = primaryDimension ?? _primaryDimension.Value;
    _finalSize.SetAndMark(finalSize, runCallback: false);
    _primaryDimension.SetAndMark(dimension, runCallback: false);
    MeasureAndUpdate("SetSize");
  }


  protected override void DrawContent(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    Rectangle sourceRect = _sourceBounds.Value;

    var position = new Vector2(
      positionX + sourceRect.Width / 2f * _dimensions.ScaleFactor,
      positionY + sourceRect.Height / 2f * _dimensions.ScaleFactor
    );

    var origin = new Vector2(sourceRect.Width / 2f, sourceRect.Height / 2f);

    spriteBatch.Draw(
      _texture.Value,
      position,
      sourceRect,
      Color.White,
      0.0f,
      origin,
      _dimensions.ScaleFactor,
      SpriteEffects.None,
      0
    );
  }

  protected override Dimensions MeasureContent()
  {
    return _dimensions.Bounds;
  }
}
