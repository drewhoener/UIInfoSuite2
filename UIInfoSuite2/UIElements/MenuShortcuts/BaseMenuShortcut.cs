using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

public abstract class BaseMenuShortcut
{
  public BaseMenuShortcut(int finalHeight)
  {
    RenderedHeight = finalHeight;
  }

  public int RenderedHeight { get; }
  public abstract int RenderedWidth { get; }

  public virtual bool ShouldDraw => true;

  public abstract void Draw(SpriteBatch batch, int xStart, int yStart);
  public abstract void DrawHoverText(SpriteBatch batch);
  public abstract void OnClick(object? sender, ButtonPressedEventArgs args);
}
