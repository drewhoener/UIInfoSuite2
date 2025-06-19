using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.Infrastructure.Utilities;
using UIInfoSuite2.UIElements;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Modules.Overlay;

internal enum OverlayType
{
  Sprinkler,
  Scarecrow,
  JunimoHut,
  Item
}

internal class WorldObjectRange
{
  private readonly Vector2 _centerTile;
  private readonly List<Vector2> _perimeterPoints;
  private string? _itemId;

  public WorldObjectRange(string itemId, Vector2 centerTile, GridPatternOptions gridPatternOptions) : this(
    itemId,
    OverlayType.Item,
    centerTile,
    gridPatternOptions
  ) { }

  public WorldObjectRange(OverlayType overlayType, Vector2 centerTile, GridPatternOptions gridPatternOptions) : this(
    null,
    overlayType,
    centerTile,
    gridPatternOptions
  ) { }

  public WorldObjectRange(
    string? itemId,
    OverlayType overlayType,
    Vector2 centerTile,
    GridPatternOptions gridPatternOptions
  )
  {
    _itemId = itemId;
    Type = overlayType;
    _centerTile = centerTile;

    bool[][] grid = GridPatternGenerator.GenerateCenteredGrid(gridPatternOptions);
    // Naive assumption, let's just assume it's a square and allocate at least that much;
    _perimeterPoints = new List<Vector2>(grid.Length * 4);

    Tiles = GridPatternGenerator.MapToWorld(grid, centerTile).ToHashSet();
    GeneratePerimeter(grid);
  }

  private WorldObjectRange(string? itemId, OverlayType type, HashSet<Vector2> tiles)
  {
    _itemId = itemId;
    Type = type;
    _perimeterPoints = [];
    Tiles = tiles;
  }

  public HashSet<Vector2> Tiles { get; }
  public OverlayType Type { get; }

  public static WorldObjectRange FromSprinkler(Object selectedObject, Vector2 centerTile, bool isHeldItem)
  {
    bool[][] grid = GridPatternGenerator.FromSprinkler(selectedObject);
    IEnumerable<Vector2> sprinklerTiles = selectedObject.GetSprinklerTiles();
    if (isHeldItem)
    {
      sprinklerTiles = sprinklerTiles.Select(tile => tile - selectedObject.TileLocation + centerTile);
    }

    var range = new WorldObjectRange(selectedObject.ItemId, OverlayType.Sprinkler, sprinklerTiles.ToHashSet());
    range.GeneratePerimeter(grid);
    return range;
  }

  private void GeneratePerimeter(bool[][] grid)
  {
    _perimeterPoints.Clear();
    int midpoint = grid.Length / 2;

    for (var row = 0; row < grid.Length; row++)
    {
      for (var col = 0; col < grid[row].Length; col++)
      {
        if (!grid[row][col] || !IsEdgeCell(grid, row, col))
        {
          continue;
        }

        // Convert to world coordinates
        float x = _centerTile.X + (col - midpoint);
        float y = _centerTile.Y + (row - midpoint);
        _perimeterPoints.Add(new Vector2(x, y));
      }
    }

    // Sort edge points in clockwise order around center
    _perimeterPoints.Sort(
      (a, b) =>
      {
        double angleA = Math.Atan2(a.Y - _centerTile.Y, a.X - _centerTile.X);
        double angleB = Math.Atan2(b.Y - _centerTile.Y, b.X - _centerTile.X);
        return angleA.CompareTo(angleB);
      }
    );
  }

  private bool IsValidCell(bool[][] grid, int row, int col)
  {
    return row >= 0 && row < grid.Length && col >= 0 && col < grid[0].Length;
  }

  private bool IsEdgeCell(bool[][] grid, int row, int col)
  {
    // Only check cardinal directions (NESW)
    var cardinalDirections = new (int rowOffset, int colOffset)[]
    {
      (-1, 0), // North
      (0, 1),  // East
      (1, 0),  // South
      (0, -1)  // West
    };

    foreach ((int rowOffset, int colOffset) in cardinalDirections)
    {
      int newRow = row + rowOffset;
      int newCol = col + colOffset;

      // If neighbor is outside grid or is 0, this is an edge
      if (!IsValidCell(grid, newRow, newCol) || !grid[newRow][newCol])
      {
        return true;
      }
    }

    return false;
  }

  public bool IsEnclosedBy(WorldObjectRange enclosingPolygon)
  {
    if (enclosingPolygon.Tiles.Count < Tiles.Count)
    {
      return false;
    }

    return Tiles.All(tile => enclosingPolygon.Tiles.Contains(tile));
  }

  public bool Encloses(WorldObjectRange enclosedPolygon)
  {
    return enclosedPolygon.IsEnclosedBy(this);
  }
}

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ObjectEffectRangeModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  : BaseModule(modEvents, logger, configManager)
{
  private readonly List<WorldObjectRange> _discoveredObjects = new();

  private readonly PerScreen<Dictionary<Vector2, Counter<OverlayType>>> _effectiveAreaRange =
    new(() => new Dictionary<Vector2, Counter<OverlayType>>());

  /**
   * Generate a bunch of world overlay objects, store them in a dict somewhere by type
   * have a
   * update ranges for item type, clear out the dict and populate (maybe cache, maybe not idk)
   */

  private bool ButtonShowOneRange { get; set; }

  private bool ButtonShowAllRanges { get; set; }


#region Lifecycle
  public override bool ShouldEnable()
  {
    return Config.ShowItemEffectRanges;
  }

  public override void OnEnable()
  {
    ModEvents.Display.RenderingHud += OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
    ModEvents.Input.ButtonsChanged += OnButtonChanged;
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingHud -= OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
    ModEvents.Input.ButtonsChanged -= OnButtonChanged;
  }
#endregion


#region Event subscriptions
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    // Ticks can happen when the player reverts to the loading screen; defend against that.
    if (Game1.currentLocation is null)
    {
      return;
    }

    _effectiveAreaRange.Value.Clear();
    _discoveredObjects.Clear();

    if (!ShouldDisplayRanges())
    {
      return;
    }

    UpdateTilesForArea();
    ButtonShowAllRanges = false;
    ButtonShowOneRange = false;
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    foreach ((Vector2 pos, Counter<OverlayType>? counter) in _effectiveAreaRange.Value)
    {
      foreach ((OverlayType overlayType, int count) in counter.Pairs)
      {
        Vector2 position = pos * Utility.ModifyCoordinateFromUIScale(Game1.tileSize);
        e.SpriteBatch.Draw(
          Game1.mouseCursors,
          Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
          new Rectangle(194, 388, 16, 16),
          (count == 1 ? Color.White : Color.Red) * 0.7f,
          0.0f,
          Vector2.Zero,
          Utility.ModifyCoordinateForUIScale(Game1.pixelZoom),
          SpriteEffects.None,
          0.01f
        );
      }
    }
  }

  private void OnButtonChanged(object? sender, ButtonsChangedEventArgs e)
  {
    if (!Context.IsPlayerFree)
    {
      return;
    }

    if (Config.ShowItemRangeHoverKeybind.IsDown())
    {
      ButtonShowOneRange = true;
    }

    if (Config.ShowItemRangeHoverKeybind.IsDown())
    {
      ButtonShowAllRanges = true;
    }
  }
#endregion


#region Logic
  private bool ShouldDisplayRanges()
  {
    bool isValidMenuState = Game1.activeClickableMenu is null;
    bool isValidPlayerState = UIElementUtils.IsRenderingNormally();
    if (Game1.activeClickableMenu is CarpenterMenu carpenterMenu)
    {
      bool isValidBuilding = carpenterMenu.currentBuilding is JunimoHut || carpenterMenu.buildingToMove is JunimoHut;
      isValidMenuState = carpenterMenu.onFarm && isValidBuilding;
      isValidPlayerState |= isValidMenuState;
    }

    return isValidMenuState && isValidPlayerState;
  }

  private void UpdateEffectiveArea()
  {
    int[][] arrayToUse;
    List<Object> similarObjects;

    // Junimo Hut is handled differently, because it is a building
    Building building = Game1.currentLocation.getBuildingAt(Game1.GetPlacementGrabTile());

    // if (building is JunimoHut)
    // {
    //   arrayToUse = GetDistanceArray(ObjectsWithDistance.JunimoHut);
    //   foreach (Building? nextBuilding in Game1.currentLocation.buildings)
    //   {
    //     if (nextBuilding is JunimoHut nextHut)
    //     {
    //       AddTilesToHighlightedArea(arrayToUse, false, nextHut.tileX.Value + 1, nextHut.tileY.Value + 1);
    //     }
    //   }
    // }
    //
    //
    //
    // // Every other item is here
    // if (Config.ShowRangeOnKeyDownWhileHovered && (ButtonShowOneRange || ButtonShowAllRanges))
    // {
    //   Vector2 gamepadTile = Game1.player.CurrentTool != null
    //     ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
    //     : Utility.snapToInt(Game1.player.GetGrabTile());
    //   Vector2 mouseTile = Game1.currentCursorTile;
    //   Vector2 tile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;
    //   if (Game1.currentLocation.Objects?.TryGetValue(tile, out Object? currentObject) ?? false)
    //   {
    //     if (currentObject != null)
    //     {
    //       Vector2 currentTile = Game1.GetPlacementGrabTile();
    //       Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
    //       Vector2 validTile = Utility.snapToInt(
    //                             Utility.GetNearbyValidPlacementPosition(
    //                               Game1.player,
    //                               Game1.currentLocation,
    //                               currentObject,
    //                               (int)currentTile.X * Game1.tileSize,
    //                               (int)currentTile.Y * Game1.tileSize
    //                             )
    //                           ) /
    //                           Game1.tileSize;
    //       Game1.isCheckingNonMousePlacement = false;
    //
    //       if (currentObject.Name.IndexOf("arecrow", StringComparison.OrdinalIgnoreCase) >= 0)
    //       {
    //         string itemName = currentObject.Name;
    //         arrayToUse = itemName.Contains("eluxe")
    //           ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow, false, currentObject)
    //           : GetDistanceArray(ObjectsWithDistance.Scarecrow, false, currentObject);
    //         AddTilesToHighlightedArea(arrayToUse, true, (int)validTile.X, (int)validTile.Y);
    //
    //         if (ButtonShowAllRanges)
    //         {
    //           similarObjects = GetSimilarObjectsInLocation("arecrow");
    //           foreach (Object next in similarObjects)
    //           {
    //             if (!next.Equals(currentObject))
    //             {
    //               int[][] arrayToUse_ = next.Name.IndexOf("eluxe", StringComparison.OrdinalIgnoreCase) >= 0
    //                 ? GetDistanceArray(ObjectsWithDistance.DeluxeScarecrow, false, next)
    //                 : GetDistanceArray(ObjectsWithDistance.Scarecrow, false, next);
    //               if (!arrayToUse_.SequenceEqual(arrayToUse))
    //               {
    //                 AddTilesToHighlightedArea(arrayToUse, false, (int)next.TileLocation.X, (int)next.TileLocation.Y);
    //               }
    //             }
    //           }
    //         }
    //       }
    //       else if (currentObject.Name.IndexOf("sprinkler", StringComparison.OrdinalIgnoreCase) >= 0)
    //       {
    //         IEnumerable<Vector2> unplacedSprinklerTiles = currentObject.GetSprinklerTiles();
    //         if (currentObject.TileLocation != validTile)
    //         {
    //           unplacedSprinklerTiles =
    //             unplacedSprinklerTiles.Select(tile => tile - currentObject.TileLocation + validTile);
    //         }
    //
    //         AddTilesToHighlightedArea(unplacedSprinklerTiles, true);
    //
    //         if (ButtonShowAllRanges)
    //         {
    //           similarObjects = GetSimilarObjectsInLocation("sprinkler");
    //           foreach (Object next in similarObjects)
    //           {
    //             if (!next.Equals(currentObject))
    //             {
    //               AddTilesToHighlightedArea(next.GetSprinklerTiles(), false);
    //             }
    //           }
    //         }
    //       }
    //       else if (currentObject.Name.IndexOf("bee house", StringComparison.OrdinalIgnoreCase) >= 0)
    //       {
    //         arrayToUse = GetDistanceArray(ObjectsWithDistance.Beehouse);
    //         AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
    //       }
    //       else if (currentObject.Name.IndexOf("mushroom log", StringComparison.OrdinalIgnoreCase) >= 0)
    //       {
    //         arrayToUse = GetDistanceArray(ObjectsWithDistance.MushroomLog);
    //         AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
    //       }
    //       else if (currentObject.Name.IndexOf("mossy seed", StringComparison.OrdinalIgnoreCase) >= 0)
    //       {
    //         arrayToUse = GetDistanceArray(ObjectsWithDistance.MossySeed);
    //         AddTilesToHighlightedArea(arrayToUse, false, (int)validTile.X, (int)validTile.Y);
    //       }
    //     }
    //   }
    // }
  }

  private Vector2 GetPlacementTileForItem(Object currentItem)
  {
    Vector2 currentTile = Game1.GetPlacementGrabTile();
    Game1.isCheckingNonMousePlacement = !Game1.IsPerformingMousePlacement();
    Vector2 validTile = Utility.snapToInt(
                          Utility.GetNearbyValidPlacementPosition(
                            Game1.player,
                            Game1.currentLocation,
                            currentItem,
                            (int)currentTile.X * Game1.tileSize,
                            (int)currentTile.Y * Game1.tileSize
                          )
                        ) /
                        Game1.tileSize;
    Game1.isCheckingNonMousePlacement = false;

    return validTile;
  }


  private void UpdateTilesForArea()
  {
    if (Game1.player.CurrentItem is not Object currentItem || !currentItem.isPlaceable())
    {
      return;
    }

    Vector2 placementTile = GetPlacementTileForItem(currentItem);

    /**
     * 1. Get placement tile.
     * 2. Register
     */

    WorldObjectRange? curObjectRange = GetEffectiveTilesForObject(currentItem, true, placementTile);
    if (curObjectRange is null)
    {
      return;
    }

    _discoveredObjects.Add(curObjectRange);
    foreach (Vector2 vector2 in curObjectRange.Tiles)
    {
      _effectiveAreaRange.Value.GetOrCreate(vector2).Result.Add(curObjectRange.Type);
    }

    List<Object> otherItems = GetSimilarObjects(currentItem);
    foreach (Object areaObject in otherItems)
    {
      WorldObjectRange? otherObjRange = GetEffectiveTilesForObject(areaObject, false, placementTile);
      if (otherObjRange is null)
      {
        continue;
      }

      _discoveredObjects.Add(otherObjRange);
      foreach (Vector2 vector2 in otherObjRange.Tiles)
      {
        _effectiveAreaRange.Value.GetOrCreate(vector2).Result.Add(curObjectRange.Type);
      }
    }

    // If buttons are down, add all nearby items to the map
  }

  private IEnumerable<Vector2> GetEffectiveTilesForBuilding(Building building)
  {
    if (building is JunimoHut)
    {
      return GridPatternGenerator.GenerateMappedGrid(
        new Vector2(building.tileX.Value + 1, building.tileY.Value + 1),
        new GridPatternOptions { MainRange = 8 }
      );
    }

    return [];
  }

  /// <summary>
  ///   Get a map of tiles that represent the coverage of the requested object.
  /// </summary>
  /// <param name="selectedObject">The object to get coverage tiles for</param>
  /// <param name="isHeldItem">If the item is being held by the player, as opposed to being placed on the ground</param>
  /// <param name="currentMouseTile">The tile that the mouse is hovering over</param>
  /// <returns>An iterable of tiles in map coordinates</returns>
  private WorldObjectRange? GetEffectiveTilesForObject(Object selectedObject, bool isHeldItem, Vector2 currentMouseTile)
  {
    Vector2 centerTile = isHeldItem ? currentMouseTile : selectedObject.TileLocation;

    // Add stuff like totems here

    if (!selectedObject.isPlaceable())
    {
      return null;
    }

    if (selectedObject.IsScarecrow())
    {
      int radius = selectedObject.GetRadiusForScarecrow();
      int maxGridSize = (radius - 1) * 2 + 1;
      return new WorldObjectRange(
        OverlayType.Scarecrow,
        centerTile,
        new GridPatternOptions { Shape = GridPatternShape.Circle, MainRange = radius, MaxGridSize = maxGridSize }
      );
    }

    if (selectedObject.IsSprinkler())
    {
      return WorldObjectRange.FromSprinkler(selectedObject, centerTile, isHeldItem);
    }

    if (IsObjectFuzzy(selectedObject, "bee house"))
    {
      return new WorldObjectRange(
        selectedObject.ItemId,
        centerTile,
        new GridPatternOptions
        {
          Shape = GridPatternShape.Diamond,
          MainRange = 5,
          AdditionalDistance = 5,
          AdditionalPointPlacement = AdditionalPointPlacement.AxialOnly
        }
      );
    }

    if (IsObjectFuzzy(selectedObject, "mushroom log"))
    {
      return new WorldObjectRange(
        selectedObject.ItemId,
        centerTile,
        new GridPatternOptions { Shape = GridPatternShape.Square, MainRange = 7 }
      );
    }

    if (IsObjectFuzzy(selectedObject, "mossy seed"))
    {
      return new WorldObjectRange(
        selectedObject.ItemId,
        centerTile,
        new GridPatternOptions { Shape = GridPatternShape.Square, MainRange = 5 }
      );
    }

    return null;
  }

  private bool IsObject(Object selectedObject, Func<Object, bool> predicate)
  {
    return predicate(selectedObject);
  }

  private bool IsObjectFuzzy(Object selectedObject, string nameSearchString)
  {
    return IsObject(selectedObject, NameSelector);

    bool NameSelector(Object o)
    {
      return o.Name.Contains(nameSearchString, StringComparison.OrdinalIgnoreCase);
    }
  }

  private bool IsSimilarObject(Object selectedObject, Object otherObject)
  {
    if (selectedObject.IsSprinkler())
    {
      return otherObject.IsSprinkler();
    }

    if (selectedObject.IsScarecrow())
    {
      return otherObject.IsScarecrow();
    }

    return selectedObject.ItemId == otherObject.ItemId;
  }

  private List<Object> GetSimilarObjects(Object selectedObject)
  {
    return GetSimilarObjectsInLocation(otherObject => IsSimilarObject(selectedObject, otherObject));
  }

  private List<Object> GetSimilarObjectsInLocation(Func<Object, bool> predicate, GameLocation? pLocation = null)
  {
    GameLocation location = pLocation ?? Game1.currentLocation;
    var similarObjects = new List<Object>(50);

    foreach (Object objectsValue in location.Objects.Values)
    {
      if (predicate(objectsValue))
      {
        similarObjects.Add(objectsValue);
      }
    }

    return similarObjects;
  }

  private List<Object> GetSimilarObjectsInLocation(string nameSearchString, GameLocation? pLocation = null)
  {
    return string.IsNullOrEmpty(nameSearchString) ? [] : GetSimilarObjectsInLocation(NameSelector, pLocation);

    bool NameSelector(Object o)
    {
      return o.Name.Contains(nameSearchString, StringComparison.OrdinalIgnoreCase);
    }
  }
#endregion
}
