using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite2.Infrastructure.Extensions;
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

    Padding.SetInsets(5, 5, 5, 5);
    MeasureAndUpdate("init");
  }

  private void MeasureAndUpdate(string? sender)
  {
    _dimensions = new AspectLockedDimensions(_sourceBounds.Value, _finalSize.Value, _primaryDimension.Value);
    MarkLayoutDirty(sender ?? "unknown");
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


  protected override void DrawSelf(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    Rectangle sourceRect = _sourceBounds.Value;
    var debugRect = new Rectangle(0, 0, 1, 1);

    positionX += Margin.Left.OrZero() + Padding.Left.OrZero();
    positionY += Margin.Top.OrZero() + Padding.Top.OrZero();

    // spriteBatch.Draw(
    //   Game1.staminaRect,
    //   new Vector2(
    //     positionX + 0 + debugRect.Width / 2f * _dimensions.ScaleFactor,
    //     positionY + 0 + debugRect.Height / 2f * _dimensions.ScaleFactor
    //   ),
    //   sourceRect,
    //   Color.White,
    //   0.0f,
    //   new Vector2(debugRect.Width / 2f, debugRect.Height / 2f),
    //   _dimensions.ScaleFactor,
    //   SpriteEffects.None,
    //   0
    // );

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

    // spriteBatch.Draw(
    //   _texture.Value,
    //   new Vector2(positionX + (float)sourceRect.Width / 2, positionY + (float)sourceRect.Height / 2),
    //   sourceRect,
    //   Color.White,
    //   0.0f,
    //   new Vector2((float)sourceRect.Width / 2, (float)sourceRect.Height / 2),
    //   _dimensions.ScaleFactor,
    //   SpriteEffects.None,
    //   0
    // );
  }

  protected override Dimensions MeasureContent()
  {
    return _dimensions.Bounds;
  }
}
