using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using UIInfoSuite2.Infrastructure.Utilities;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Models.ObjectRange;

/// <summary>
///   Represents an object in the game world with a defined range of influence or effect.
/// </summary>
internal class WorldObjectRange
{
  // TODO: This for sure isn't the right way to do this, should probably be
  // some sort of cache that holds the ranges at 0,0. Implement a vector
  // pool to map to world space vectors and use that to avoid GC pressure.
  private static readonly DataCache<string, WorldObjectRange> DataCache = new(
    range => GetDataCacheKey(range._itemId ?? range.Type, range.CenterTile),
    StringComparer.OrdinalIgnoreCase
  );

  private readonly string? _itemId;

  public readonly Vector2 CenterTile;

  /// <summary>
  ///   Represents an object in the game world with a defined range of influence or effect,
  ///   allowing for tracking and manipulation of its properties.
  /// </summary>
  public WorldObjectRange(string? itemId, string overlayType, Vector2 centerTile, GridPatternOptions gridPatternOptions)
  {
    _itemId = itemId;
    Type = overlayType;
    CenterTile = centerTile;
    bool[][] grid = GridPatternGenerator.GenerateCenteredGrid(gridPatternOptions);
    Tiles = GridPatternGenerator.MapToWorld(grid, centerTile).ToHashSet();
    DataCache.Add(this);
  }

  /// <summary>
  ///   Represents an object in the game world with a defined range of influence or effect,
  ///   allowing for tracking, manipulation, and identification of objects based on their range.
  /// </summary>
  private WorldObjectRange(string? itemId, string type, Vector2 centerTile, HashSet<Vector2> tiles)
  {
    _itemId = itemId;
    CenterTile = centerTile;
    Type = type;
    Tiles = tiles;
    DataCache.Add(this);
  }

  /// <summary>
  ///   A collection of tiles in the game world representing the range of influence
  ///   of a specific object or entity. The tiles are stored as a set of 2D vector positions.
  /// </summary>
  public HashSet<Vector2> Tiles { get; }

  /// <summary>
  ///   Defines the type of overlay or effect associated with a game world object,
  ///   used to identify and categorize the functionality or influence of the object.
  /// </summary>
  public string Type { get; }

  public static void ClearCache()
  {
    DataCache.Clear();
  }

  private static string GetDataCacheKey(string itemOrType, Vector2 centerTile)
  {
    return $"{itemOrType}-{centerTile.ToString()}";
  }

  /// <summary>
  ///   Creates a <see cref="WorldObjectRange" /> instance based on the properties of the provided game object,
  ///   defining its range of effect or influence within the game world.
  /// </summary>
  /// <param name="tileObject">
  ///   The game object for which the range is being created. It contains identifying properties used
  ///   to determine the range.
  /// </param>
  /// <param name="centerTile">The central tile from which the range is calculated.</param>
  /// <param name="gridPatternOptions">Options defining the pattern, shape, and configuration of the range grid.</param>
  /// <returns>A <see cref="WorldObjectRange" /> instance representing the range of the provided object in the game world.</returns>
  public static WorldObjectRange FromItem(Object tileObject, Vector2 centerTile, GridPatternOptions gridPatternOptions)
  {
    return FromItem(tileObject, tileObject.ItemId, centerTile, gridPatternOptions);
  }

  public static WorldObjectRange FromItem(
    Object tileObject,
    string overlayType,
    Vector2 centerTile,
    GridPatternOptions gridPatternOptions
  )
  {
    WorldObjectRange? item = FromCache(tileObject, centerTile);
    if (item != null)
    {
      ModEntry.Instance.Monitor.Log($"Using Cached Object {item._itemId} at {item.CenterTile}");
      return item;
    }

    return new WorldObjectRange(tileObject.ItemId, overlayType, centerTile, gridPatternOptions);
  }

  public static WorldObjectRange? FromCache(Object tileObject, Vector2 centerTile)
  {
    return DataCache.Get(GetDataCacheKey(tileObject.ItemId, centerTile));
  }

  public static bool FromCache(Object tileObject, Vector2 centerTile, [NotNullWhen(true)] out WorldObjectRange? range)
  {
    return DataCache.TryGet(GetDataCacheKey(tileObject.ItemId, centerTile), out range);
  }

  /// <summary>
  ///   Creates a <see cref="WorldObjectRange" /> for a sprinkler object, representing its range of influence
  ///   based on the tiles it affects, with optional adjustments for held items.
  /// </summary>
  /// <param name="selectedObject">The sprinkler object to calculate the range for.</param>
  /// <param name="centerTile">The tile location to use as the center of the sprinkler's range.</param>
  /// <param name="isHeldItem">Indicates whether the sprinkler is currently a held item, requiring tile adjustments.</param>
  /// <returns>A <see cref="WorldObjectRange" /> representing the sprinkler's area of effect.</returns>
  public static WorldObjectRange FromSprinkler(Object selectedObject, Vector2 centerTile, bool isHeldItem)
  {
    if (FromCache(selectedObject, centerTile, out WorldObjectRange? range))
    {
      ModEntry.Instance.Monitor.Log($"Using Cached Object {range._itemId} at {range.CenterTile}");
      return range;
    }

    IEnumerable<Vector2> sprinklerTiles = selectedObject.GetSprinklerTiles();
    if (isHeldItem)
    {
      sprinklerTiles = sprinklerTiles.Select(tile => tile - selectedObject.TileLocation + centerTile);
    }

    return new WorldObjectRange(selectedObject.ItemId, OverlayType.Sprinkler, centerTile, sprinklerTiles.ToHashSet());
  }

  public static WorldObjectRange FromTreasureTotem(Object selectedObject, Vector2 centerTile)
  {
    if (FromCache(selectedObject, centerTile, out WorldObjectRange? range))
    {
      ModEntry.Instance.Monitor.Log($"Using Cached Object {range._itemId} at {range.CenterTile}");
      return range;
    }

    const int radius = 3;
    int gridSize = 2 * radius + 1;
    var grid = new bool[gridSize][];

    for (var row = 0; row < gridSize; row++)
    {
      grid[row] = new bool[gridSize];
      for (var col = 0; col < gridSize; col++)
      {
        int dy = Math.Abs(radius - row);
        int dx = Math.Abs(radius - col);
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // Include points at exactly radius distance (with small tolerance for floating point)
        grid[row][col] = Math.Abs(distance - radius) < 0.5;
      }
    }

    HashSet<Vector2> tiles = GridPatternGenerator.MapToWorld(grid, centerTile).ToHashSet();
    return new WorldObjectRange(selectedObject.ItemId, OverlayType.Sprinkler, centerTile, tiles);
  }

  /// <summary>
  ///   Determines if the current object's range of tiles is completely enclosed by another object's range of tiles.
  /// </summary>
  /// <param name="enclosingPolygon">
  ///   The object with the range of tiles that is checked for fully enclosing the current
  ///   object's range.
  /// </param>
  /// <returns>
  ///   True if all tiles of the current object are contained within the tiles of the specified object; otherwise,
  ///   false.
  /// </returns>
  public bool IsEnclosedBy(WorldObjectRange enclosingPolygon)
  {
    if (enclosingPolygon.Tiles.Count < Tiles.Count)
    {
      return false;
    }

    return Tiles.All(tile => enclosingPolygon.Tiles.Contains(tile));
  }

  /// <summary>
  ///   Determines if the current object's range of tiles fully encloses the range of tiles from another object.
  /// </summary>
  /// <param name="enclosedPolygon">
  ///   The object whose range of tiles is checked to determine if it is fully enclosed by the
  ///   current object's range.
  /// </param>
  /// <returns>
  ///   True if all tiles of the specified object are contained within the tiles of the current object; otherwise,
  ///   false.
  /// </returns>
  public bool Encloses(WorldObjectRange enclosedPolygon)
  {
    return enclosedPolygon.IsEnclosedBy(this);
  }
}
