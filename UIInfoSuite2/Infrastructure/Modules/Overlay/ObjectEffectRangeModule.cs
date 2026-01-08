using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.ObjectRange;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.Infrastructure.Utilities;
using UIInfoSuite2.UIElements;
using Object = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Modules.Overlay;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ObjectEffectRangeModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  SoundHelper soundHelper
) : BaseModule(modEvents, logger, configManager)
{
  private readonly PerScreen<RangeCache> _effectiveAreaRange = new(() => new RangeCache());

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
    ModEvents.GameLoop.DayEnding += OnDayEnding;
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingHud -= OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
    ModEvents.Input.ButtonsChanged -= OnButtonChanged;
    ModEvents.GameLoop.DayEnding -= OnDayEnding;
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
    _effectiveAreaRange.Value.Draw(e.SpriteBatch);
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

  private void OnDayEnding(object? sender, DayEndingEventArgs e)
  {
    _effectiveAreaRange.Value.Clear();
    WorldObjectRange.ClearCache();
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
    if (Game1.player.CurrentItem is not Object currentItem || !IsValidItem(currentItem))
    {
      return;
    }

    Vector2 placementTile = GetPlacementTileForItem(currentItem);

    /*
     * 1. Get placement tile.
     * 2. Register
     */

    WorldObjectRange? curObjectRange = GetEffectiveTilesForObject(currentItem, true, placementTile);
    if (curObjectRange is null)
    {
      return;
    }

    _effectiveAreaRange.Value.Add(curObjectRange);

    List<Object> otherItems = GetSimilarObjects(currentItem);
    foreach (Object areaObject in otherItems)
    {
      WorldObjectRange? otherObjRange = GetEffectiveTilesForObject(areaObject, false, placementTile);
      if (otherObjRange is null)
      {
        continue;
      }

      _effectiveAreaRange.Value.Add(otherObjRange);
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

  private static bool IsValidItem(Object currentItem)
  {
    return currentItem.isPlaceable() || currentItem.ItemId == "TreasureTotem";
  }

  /// <summary>
  ///   Get a map of tiles that represent the coverage of the requested object.
  /// </summary>
  /// <param name="selectedObject">The object to calculate the coverage tiles for.</param>
  /// <param name="isHeldItem">Indicates whether the player is currently holding the object.</param>
  /// <param name="currentMouseTile">The tile location under the player's mouse cursor.</param>
  /// <returns>
  ///   A <see cref="WorldObjectRange" /> instance representing the tiles covered by the object, or <c>null</c> if the
  ///   object is not relevant.
  /// </returns>
  private WorldObjectRange? GetEffectiveTilesForObject(Object selectedObject, bool isHeldItem, Vector2 currentMouseTile)
  {
    Vector2 centerTile = isHeldItem ? currentMouseTile : selectedObject.TileLocation;

    // Add stuff like totems here
    if (selectedObject.ItemId == "TreasureTotem")
    {
      return WorldObjectRange.FromTreasureTotem(selectedObject, Game1.player.Tile);
    }

    if (!selectedObject.isPlaceable())
    {
      return null;
    }

    if (selectedObject.IsScarecrow())
    {
      int radius = selectedObject.GetRadiusForScarecrow();
      int maxGridSize = (radius - 1) * 2 + 1;
      return WorldObjectRange.FromItem(
        selectedObject,
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
      return WorldObjectRange.FromItem(
        selectedObject,
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
      return WorldObjectRange.FromItem(
        selectedObject,
        centerTile,
        new GridPatternOptions { Shape = GridPatternShape.Square, MainRange = 7 }
      );
    }

    if (IsObjectFuzzy(selectedObject, "mossy seed"))
    {
      return WorldObjectRange.FromItem(
        selectedObject,
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
