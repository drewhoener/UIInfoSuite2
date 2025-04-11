using Microsoft.Xna.Framework;

namespace UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

public enum PrimaryDimension
{
  Height,
  Width
}

public class AspectLockedDimensions
{
  private readonly float _finalSize;
  private PrimaryDimension _primaryDimension;
  private Rectangle _sourceDimensions;

  public AspectLockedDimensions(Rectangle sourceBounds, float finalSize, PrimaryDimension primaryDimension)
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

  public Dimensions Bounds { get; private set; } = new();

  public int Width => Bounds.Width;

  public int Height => Bounds.Height;

  public float ScaleFactor { get; private set; }

  private void RecalculateScaling()
  {

    float width;
    float height;

    if (_primaryDimension == PrimaryDimension.Height)
    {
      height = _finalSize;
      ScaleFactor = height / _sourceDimensions.Height;
      width = _sourceDimensions.Width * ScaleFactor;
    }
    else
    {
      width = _finalSize;
      ScaleFactor = width / _sourceDimensions.Width;
      height = _sourceDimensions.Height * ScaleFactor;
    }

    Bounds = new Dimensions(width, height);
  }
}
