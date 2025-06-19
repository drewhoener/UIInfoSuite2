using Microsoft.Xna.Framework;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Layout;

internal struct LayoutBounds()
{
  // Positioning Offsets
  public Point Offsets = new();

  public int OffsetX
  {
    get => Offsets.X;
    set => Offsets.X = value;
  }

  public int OffsetY
  {
    get => Offsets.Y;
    set => Offsets.Y = value;
  }

  public bool IsAbsolute { get; set; }

  // Absolute Coordinates
  public Insets Position = new();

  // Size
  public Dimensions Size = new();

  public int Width
  {
    get => Size.Width;
    set => Size.Width = value;
  }

  public int Height
  {
    get => Size.Height;
    set => Size.Height = value;
  }

  public void SetPosition(IInsets insets)
  {
    Position.SetInsets(insets);
  }

  public override string ToString()
  {
    return
      $"{nameof(Offsets)}: {Offsets}, {nameof(Position)}: {Position}, {nameof(Size)}: {Size}, {nameof(IsAbsolute)}: {IsAbsolute}";
  }
}
