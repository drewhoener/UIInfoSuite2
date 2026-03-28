using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.DebugMenu;

/// <summary>
///   A solid-color rectangle leaf element used to exercise the layout system visually.
///   Draws based on the element's actual bounds (after FlexGrow/Stretch are applied),
///   so the rendered size reflects the true layout result.
/// </summary>
internal class TestColorBox(int width, int height, Color color) : LayoutElement
{
  protected override Dimensions MeasureContent()
  {
    return new Dimensions(width, height);
  }

  protected override void DrawContent(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    // Draw based on actual bounds minus framing so FlexGrow and Stretch are visible.
    int w = Bounds.Width - Margin.HorizontalTotal() - Padding.HorizontalTotal();
    int h = Bounds.Height - Margin.VerticalTotal() - Padding.VerticalTotal();

    if (w > 0 && h > 0)
    {
      spriteBatch.Draw(Game1.staminaRect, new Rectangle(positionX, positionY, w, h), color);
    }
  }
}
