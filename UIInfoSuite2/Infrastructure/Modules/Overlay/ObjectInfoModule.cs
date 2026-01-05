using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models.Tooltip;
using UIInfoSuite2.Infrastructure.Modules.Base;
using Object = StardewValley.Object;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace UIInfoSuite2.Infrastructure.Modules.Overlay;

internal class ObjectInfoModule : BaseModule, IConfigurable
{
  private MouseTooltipDom _mouseTooltipDom = null!;

  public ObjectInfoModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager) : base(
    modEvents,
    logger,
    configManager
  ) { }

  public override bool ShouldEnable()
  {
    return true;
  }

  public override void OnEnable()
  {
    _mouseTooltipDom = new MouseTooltipDom();
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
    ModEvents.Display.RenderingHud += OnRenderingHud;
#if DEBUG
    HotReloadService.UpdateApplicationEvent += OnHotReload;
#endif
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
    ModEvents.Display.RenderingHud -= OnRenderingHud;
#if DEBUG
    HotReloadService.UpdateApplicationEvent -= OnHotReload;
#endif
  }

  private void OnHotReload(Type[]? changedTypes)
  {
    Logger.Log("Reloading hot-reload objects");
    _mouseTooltipDom = new MouseTooltipDom();
  }

  private Vector2 GetCurrentTile()
  {
    Vector2 gamepadTile = Game1.player.CurrentTool != null
      ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
      : Utility.snapToInt(Game1.player.GetGrabTile());
    Vector2 mouseTile = Game1.currentCursorTile;

    return Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;
  }

  [EventPriority(EventPriority.Low)]
  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (Game1.activeClickableMenu != null)
    {
      return;
    }

    int overrideX = -1;
    int overrideY = -1;
    Vector2 tile = GetCurrentTile();
    if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
    {
      overrideX = (int)(tile.X + Utility.ModifyCoordinateForUIScale(32));
      overrideY = (int)(tile.Y + Utility.ModifyCoordinateForUIScale(32));
    }

    _mouseTooltipDom.DrawSafely(e.SpriteBatch, overrideX, overrideY);
  }

  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(4))
    {
      return;
    }

    Vector2 tile = GetCurrentTile();

    if (Game1.currentLocation == null)
    {
      _mouseTooltipDom.Reset();
      return;
    }

    HoeDirt? currentDirtTile = GetHoeDirtAtTile(tile);
    var currentTree = GetTerrainObjectAtTile<Tree>(tile);

    _mouseTooltipDom.Building = Game1.currentLocation.getBuildingAt(tile);
    _mouseTooltipDom.Machine = GetMachineAtTile(tile);
    _mouseTooltipDom.Crop = GetCropFromTerrain(currentDirtTile);
    _mouseTooltipDom.WildTree = currentTree;
    _mouseTooltipDom.HoeDirt = currentDirtTile;
    _mouseTooltipDom.FruitTree = GetTerrainObjectAtTile<FruitTree>(tile);
    _mouseTooltipDom.Bush = GetBushFromTile(tile);
  }

  private static Object? GetMachineAtTile(Vector2 tile)
  {
    return Game1.currentLocation.Objects.TryGetValue(tile, out Object? currentObject) ? currentObject : null;
  }

  private static IndoorPot? GetPotAtTile(Vector2 tile)
  {
    if (!Game1.currentLocation.Objects.TryGetValue(tile, out Object? currentObject) ||
        currentObject is not IndoorPot indoorPot)
    {
      return null;
    }

    return indoorPot;
  }

  private static HoeDirt? GetHoeDirtAtTile(Vector2 tile)
  {
    if (Game1.currentLocation == null)
    {
      return null;
    }

    if (Game1.currentLocation.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain))
    {
      return terrain as HoeDirt;
    }

    // Out of options, check for a pot in the world
    return GetPotAtTile(tile)?.hoeDirt.Value;
  }

  private static T? GetTerrainObjectAtTile<T>(Vector2 tile) where T : TerrainFeature
  {
    if (!Game1.currentLocation.terrainFeatures.TryGetValue(tile, out TerrainFeature? terrain))
    {
      return null;
    }

    return terrain as T;
  }

  private static Crop? GetCropFromTerrain(TerrainFeature? terrain, bool allowDeadCrops = false)
  {
    if (terrain is not HoeDirt { crop: not null } hoeDirt)
    {
      return null;
    }

    if (hoeDirt.crop.dead.Value && !allowDeadCrops)
    {
      return null;
    }

    return hoeDirt.crop;
  }

  private static Bush? GetBushFromTile(Vector2 tile)
  {
    var bush = GetTerrainObjectAtTile<Bush>(tile);
    return bush ?? GetPotAtTile(tile)?.bush.Value;
  }

#region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.Tooltips;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_ObjectTooltips();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Crops_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Crops_Enable_Tooltip,
      getValue: () => Config.ShowCropTooltip,
      setValue: value => Config.ShowCropTooltip = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Machines_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Machines_Enable_Tooltip,
      getValue: () => Config.ShowMachineTooltip,
      setValue: value => Config.ShowMachineTooltip = value
    );
  }
#endregion
}
