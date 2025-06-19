using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Object = StardewValley.Object;


namespace UIInfoSuite2.Infrastructure.Utilities;

public enum GridPatternShape
{
  BasicDistance,
  Circle,
  Diamond,
  Square
}

public enum AdditionalPointPlacement
{
  None,          // No additional points
  AllDirections, // Additional points in all directions at the specified distance
  AxialOnly      // Additional points only on vertical and horizontal axes
}

public struct GridPatternOptions
{
  public double MainRange;
  public GridPatternShape Shape;

  // Additional points configuration
  public double? AdditionalDistance;
  public AdditionalPointPlacement AdditionalPointPlacement = AdditionalPointPlacement.None;

  public int? MaxGridSize;

  public GridPatternOptions()
  {
    MainRange = 0;
    Shape = GridPatternShape.Circle;
    AdditionalDistance = null;
    MaxGridSize = null;
  }
}

public static class GridPatternGenerator
{
  public static bool[][] GenerateCenteredGrid(GridPatternOptions options)
  {
    var fullGridRadius = (int)Math.Ceiling(Math.Max(options.MainRange, options.AdditionalDistance ?? 0));

    // Actual grid size based on MaxGridSize
    int actualGridRadius = options.MaxGridSize.HasValue
      ? Math.Min(fullGridRadius, options.MaxGridSize.Value / 2)
      : fullGridRadius;

    int gridSize = 2 * actualGridRadius + 1;
    var grid = new bool[gridSize][];

    // Calculate offset from full theoretical grid to actual grid
    int offset = fullGridRadius - actualGridRadius;

    for (var row = 0; row < gridSize; row++)
    {
      grid[row] = new bool[gridSize];
      for (var col = 0; col < gridSize; col++)
      {
        // Fill grid from center instead of top left using offset
        double distance = CalculateDistance(
          row + offset,   // Offset the row to match full grid
          col + offset,   // Offset the col to match full grid
          fullGridRadius, // Use the full grid radius for distance calc
          options.Shape
        );

        grid[row][col] = IsPointInPattern(distance, row + offset, col + offset, fullGridRadius, options);
      }
    }

    return grid;
  }

  public static bool[][] FromSprinkler(Object sprinklerObject)
  {
    int radiusForSprinkler = sprinklerObject.GetModifiedRadiusForSprinkler();
    return radiusForSprinkler switch
    {
      0 => [[false, true, false], [true, true, true], [false, true, false]],
      <= 0 => [],
      _ => GenerateCenteredGrid(
        new GridPatternOptions { MainRange = radiusForSprinkler, Shape = GridPatternShape.Square }
      )
    };
  }

  public static List<Vector2> GenerateMappedGrid(Vector2 center, GridPatternOptions options)
  {
    bool[][] grid = GenerateCenteredGrid(options);
    return MapToWorld(grid, center);
  }

  public static List<Vector2> MapToWorld(bool[][] grid, Vector2 centerTile)
  {
    var mappedGrid = new List<Vector2>();
    int midpoint = grid.Length / 2;

    for (var i = 0; i < grid.Length; i++)
    {
      for (var j = 0; j < grid[i].Length; j++)
      {
        if (!grid[i][j])
        {
          continue;
        }

        mappedGrid.Add(new Vector2(centerTile.X - (j - midpoint), centerTile.Y - (i - midpoint)));
      }
    }

    return mappedGrid;
  }

  private static double CalculateDistance(int row, int col, int gridRadius, GridPatternShape shape)
  {
    int dy = Math.Abs(gridRadius - row);
    int dx = Math.Abs(gridRadius - col);

    return shape switch
    {
      GridPatternShape.Circle or GridPatternShape.BasicDistance => Math.Sqrt(dx * dx + dy * dy),
      GridPatternShape.Diamond => dx + dy,
      // Manhattan distance
      GridPatternShape.Square => Math.Max(dx, dy),
      _ => throw new ArgumentException($"Unsupported shape: {shape}")
    };
  }

  private static bool IsPointInPattern(double distance, int row, int col, int gridRadius, GridPatternOptions options)
  {
    // Point is in pattern if either:
    // 1. It's within the main range
    bool withinMainRange = distance <= options.MainRange;

    if (options.Shape == GridPatternShape.BasicDistance)
    {
      // For BasicDistance, we want exact range matching
      return withinMainRange;
    }

    // 2. OR it's at the additional distance (if specified) and meets placement criteria
    var atAdditionalDistance = false;
    if (!options.AdditionalDistance.HasValue ||
        !(Math.Abs(distance - options.AdditionalDistance.Value) < double.Epsilon))
    {
      return withinMainRange || atAdditionalDistance;
    }

    switch (options.AdditionalPointPlacement)
    {
      case AdditionalPointPlacement.AllDirections:
        atAdditionalDistance = true;
        break;

      case AdditionalPointPlacement.AxialOnly:
        // Check if point is on vertical or horizontal axis
        bool isOnAxis = row == gridRadius || col == gridRadius;
        atAdditionalDistance = isOnAxis;
        break;

      case AdditionalPointPlacement.None:
      default:
        atAdditionalDistance = false;
        break;
    }

    return withinMainRange || atAdditionalDistance;
  }
}
