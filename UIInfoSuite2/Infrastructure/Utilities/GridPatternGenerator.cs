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
  /// <summary>
  ///   Generates a boolean grid centered around the origin, based on the defined grid pattern options.
  ///   The resulting grid adheres to the specified range, shape, and additional configuration in the options.
  /// </summary>
  /// <param name="options">
  ///   The options that define the size, shape, and range of the grid, including additional placement settings and maximum
  ///   grid size.
  /// </param>
  /// <returns>
  ///   A two-dimensional boolean array representing the generated grid, where each cell indicates if it's included in the
  ///   pattern.
  /// </returns>
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

  /// <summary>
  ///   Generates a list of world positions based on a boolean grid and a specified center point.
  ///   The grid is created using the provided grid pattern options and then mapped to world coordinates.
  /// </summary>
  /// <param name="center">The center point in world coordinates around which the grid will be generated and mapped.</param>
  /// <param name="options">The options defining the properties of the grid, such as shape, size, and range.</param>
  /// <returns>
  ///   A list of `Vector2` objects representing the world positions of all `true` values in the generated grid.
  /// </returns>
  public static List<Vector2> GenerateMappedGrid(Vector2 center, GridPatternOptions options)
  {
    bool[][] grid = GenerateCenteredGrid(options);
    return MapToWorld(grid, center);
  }

  /// <summary>
  ///   Maps a grid of boolean values to world coordinates based on a specified center tile.
  ///   Each `true` value in the grid is converted to a corresponding world coordinate position.
  /// </summary>
  /// <param name="grid">A 2D array of boolean values representing the grid, where `true` indicates included tiles.</param>
  /// <param name="centerTile">The center point in world coordinates used as a reference for mapping the grid.</param>
  /// <returns>
  ///   A list of `Vector2` objects representing the world positions of all `true` values in the input grid.
  /// </returns>
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

  /// <summary>
  ///   Calculates the distance from a specified point, defined by its row and column
  ///   indices, to the center of the grid, based on the specified grid shape and radius.
  /// </summary>
  /// <param name="row">The row index of the point within the grid.</param>
  /// <param name="col">The column index of the point within the grid.</param>
  /// <param name="gridRadius">The radius of the grid used as the center reference for distance calculation.</param>
  /// <param name="shape">The shape of the grid pattern, which determines the calculation method for the distance.</param>
  /// <returns>
  ///   Returns the calculated distance from the specified point to the center of the grid,
  ///   based on the grid's shape and radius.
  /// </returns>
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

  /// <summary>
  ///   Determines whether a point, defined by its distance from the center and its coordinates,
  ///   falls within the defined grid pattern based on the given grid options.
  /// </summary>
  /// <param name="distance">The distance of the point from the center of the grid.</param>
  /// <param name="row">The row index of the point in the grid.</param>
  /// <param name="col">The column index of the point in the grid.</param>
  /// <param name="gridRadius">The radius of the grid under consideration, typically derived from the maximum range.</param>
  /// <param name="options">
  ///   The options specifying the pattern configuration, including range, shape, and additional
  ///   placement settings.
  /// </param>
  /// <returns>
  ///   Returns <c>true</c> if the point is included in the grid pattern based on the distance,
  ///   specified shape, and additional placement conditions. Otherwise, returns <c>false</c>.
  /// </returns>
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
