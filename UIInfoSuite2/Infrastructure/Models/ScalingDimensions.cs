using Microsoft.Xna.Framework;

namespace UIInfoSuite2.Infrastructure.Models;

public enum PrimaryDimension
{
  Height,
  Width
}

public class ScalingDimensions
{
  private readonly PrimaryDimension _primaryDimension;

  private readonly Rectangle _sourceDimensions;

  public ScalingDimensions(Rectangle sourceBounds, float finalSize, PrimaryDimension primaryDimension)
  {
    _sourceDimensions = sourceBounds;
    _primaryDimension = primaryDimension;

    if (_primaryDimension == PrimaryDimension.Height)
    {
      Height = finalSize;
      ScaleFactor = Height / _sourceDimensions.Height;
      Width = _sourceDimensions.Width * ScaleFactor;
    }
    else
    {
      Width = finalSize;
      ScaleFactor = Width / _sourceDimensions.Width;
      Height = _sourceDimensions.Height * ScaleFactor;
    }
  }

  public (float, float) Bounds => (Width, Height);

  public float Width { get; }

  public float Height { get; }

  public int WidthInt => (int)Width;

  public int HeightInt => (int)Height;
  public float ScaleFactor { get; }
}
