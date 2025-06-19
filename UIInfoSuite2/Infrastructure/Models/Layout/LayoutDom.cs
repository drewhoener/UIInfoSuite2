using Microsoft.Xna.Framework.Graphics;

namespace UIInfoSuite2.Infrastructure.Models.Layout;

internal class LayoutDom(params LayoutElement[] elements) : LayoutContainer(elements)
{
  public void DrawSafely(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    if (IsHidden)
    {
      return;
    }

    Layout();
    (int x, int y) = Tools.CalculateTooltipPosition(
      Bounds.Width,
      Bounds.Height,
      overrideX: positionX,
      overrideY: positionY
    );
    Draw(spriteBatch, x, y);
  }
}
