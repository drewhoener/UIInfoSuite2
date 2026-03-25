using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Debug;

/// <summary>
///   A solid-color rectangle leaf element used to exercise the layout system visually.
///   Draws based on the element's actual bounds (after FlexGrow/Stretch are applied),
///   so the rendered size reflects the true layout result.
/// </summary>
internal class TestColorBox : LayoutElement
{
  private readonly Color _color;
  private readonly int _naturalHeight;
  private readonly int _naturalWidth;

  public TestColorBox(int width, int height, Color color) : base()
  {
    _color = color;
    _naturalWidth = width;
    _naturalHeight = height;
  }

  protected override Dimensions MeasureContent() => new(_naturalWidth, _naturalHeight);

  protected override void DrawSelf(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    int x = positionX + Margin.Left.OrZero() + Padding.Left.OrZero();
    int y = positionY + Margin.Top.OrZero() + Padding.Top.OrZero();

    // Draw based on actual bounds minus framing so FlexGrow and Stretch are visible.
    int w = Bounds.Width - Margin.HorizontalTotal() - Padding.HorizontalTotal();
    int h = Bounds.Height - Margin.VerticalTotal() - Padding.VerticalTotal();

    if (w > 0 && h > 0)
    {
      spriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y, w, h), _color);
    }
  }
}
