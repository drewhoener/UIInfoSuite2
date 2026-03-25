namespace UIInfoSuite2.Infrastructure.Models.Layout.Enums;

/// <summary>
///   Represents a 9-position alignment grid for laying out children within a container.
///   The horizontal axis maps to justify-content (main axis) and the vertical axis maps to
///   align-items (cross axis), both interpreted relative to the container's layout direction.
/// </summary>
public enum Alignment
{
  TopLeft,
  TopCenter,
  TopRight,
  MiddleLeft,
  Center,
  MiddleRight,
  BottomLeft,
  BottomCenter,
  BottomRight
}
