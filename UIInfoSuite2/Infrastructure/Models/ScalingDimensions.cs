using Microsoft.Xna.Framework;

namespace UIInfoSuite2.Infrastructure.Models;

public enum PrimaryDimension
{
  Height,
  Width
}

public class ScalingDimensions
{
  private readonly float _finalSize;
  private PrimaryDimension _primaryDimension;
  private Rectangle _sourceDimensions;

  public ScalingDimensions(Rectangle sourceBounds, float finalSize, PrimaryDimension primaryDimension)
  {
    _sourceDimensions = sourceBounds;
    _primaryDimension = primaryDimension;
    _finalSize = finalSize;

    RecalculateScaling();
  }

  public Rectangle SourceDimensions
  {
    get => _sourceDimensions;
    set
    {
      _sourceDimensions = value;
      RecalculateScaling();
    }
  }

  public PrimaryDimension PrimaryDimension
  {
    get => _primaryDimension;
    set
    {
      _primaryDimension = value;
      RecalculateScaling();
    }
  }

  public (float, float) Bounds => (Width, Height);

  public float Width { get; private set; }

  public float Height { get; private set; }

  public int WidthInt => (int)Width;

  public int HeightInt => (int)Height;
  public float ScaleFactor { get; private set; }

  private void RecalculateScaling()
  {
    if (_primaryDimension == PrimaryDimension.Height)
    {
      Height = _finalSize;
      ScaleFactor = Height / _sourceDimensions.Height;
      Width = _sourceDimensions.Width * ScaleFactor;
    }
    else
    {
      Width = _finalSize;
      ScaleFactor = Width / _sourceDimensions.Width;
      Height = _sourceDimensions.Height * ScaleFactor;
    }
  }
}
