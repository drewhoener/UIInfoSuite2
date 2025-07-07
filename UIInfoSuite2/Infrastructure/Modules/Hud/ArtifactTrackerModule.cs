using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

internal class ArtifactTrackerModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<ArtifactIcon>(modEvents, logger, configManager, iconStorage)
{
  private const string ArtifactSpotId = "(O)590";
  private const string SeedSpotId = "(O)SeedSpot";
  private static Rectangle QuarryRect = new(106, 13, 22, 22);
  private readonly Dictionary<GameLocation, HashSet<Vector2>> _trackedArtifactSpots = new();
  private readonly Dictionary<GameLocation, HashSet<Vector2>> _trackedSeedSpots = new();
  protected override string IconKey => "ArtifactIcon";

  private static bool ShouldIncludeLocation(GameLocation location)
  {
    switch (location)
    {
      case Desert:
        return Game1.MasterPlayer.mailReceived.Contains("ccVault");
      case IslandLocation:
        return Game1.MasterPlayer.mailReceived.Contains("willyBoatFixed") && location.IsOutdoors;
      case Railroad:
        return Game1.MasterPlayer.stats.DaysPlayed > 31;
      case Woods:
        return Game1.MasterPlayer.mailReceived.Contains("beenToWoods");
      default:
        // Weird fix for SVE using real locations for their events
        return location.IsOutdoors && location.warps.Count > 0 && !location.DisplayName.StartsWith("Custom_");
    }
  }

  private static bool ShouldTrackTile(GameLocation gameLocation, Vector2 tile)
  {
    if (gameLocation is Mountain && QuarryRect.Contains(tile))
    {
      return Game1.MasterPlayer.mailReceived.Contains("ccCraftsRoom");
    }

    return true;
  }

  public override bool ShouldEnable()
  {
    return Config.ShowArtifactSpotCount;
  }

  protected override ArtifactIcon GenerateNewIcon()
  {
    return new ArtifactIcon();
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.DayStarted += OnDayStarted;
    ModEvents.World.ObjectListChanged += OnObjectListUpdated;
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= OnDayStarted;
    ModEvents.World.ObjectListChanged -= OnObjectListUpdated;
    base.OnDisable();
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    ScanArtifactSpots();
  }

  private void OnObjectListUpdated(object? sender, ObjectListChangedEventArgs e)
  {
    if (!ShouldIncludeLocation(e.Location))
    {
      return;
    }

    foreach ((Vector2 tile, SObject obj) in e.Added)
    {
      TrackTile(e.Location, tile, obj);
    }

    foreach (KeyValuePair<Vector2, SObject> kvp in e.Removed)
    {
      UntrackTile(e.Location, kvp.Key);
    }

    Icon.UpdateText(_trackedArtifactSpots, _trackedSeedSpots);
  }

  private void TrackTile(GameLocation location, Vector2 tile, SObject obj)
  {
    if (!ShouldTrackTile(location, tile))
    {
      return;
    }

    switch (obj.QualifiedItemId)
    {
      case ArtifactSpotId:
        _trackedArtifactSpots.GetOrCreate(location).Add(tile);
        break;
      case SeedSpotId:
        _trackedSeedSpots.GetOrCreate(location).Add(tile);
        break;
    }
  }

  private void UntrackTile(GameLocation location, Vector2 tile)
  {
    _trackedArtifactSpots.GetOrCreate(location).Remove(tile);
    _trackedSeedSpots.GetOrCreate(location).Remove(tile);
  }

  private void ScanArtifactSpots()
  {
    _trackedArtifactSpots.Clear();
    _trackedSeedSpots.Clear();

    foreach (GameLocation gameLocation in Game1.locations)
    {
      if (!ShouldIncludeLocation(gameLocation))
      {
        continue;
      }

      foreach ((Vector2 tile, SObject obj) in gameLocation.Objects.Pairs)
      {
        TrackTile(gameLocation, tile, obj);
      }
    }

    Icon.UpdateText(_trackedArtifactSpots, _trackedSeedSpots);
  }

#region Configuration Setup
  public override string? GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.NotificationIcons;
  }

  public override string? GetSubHeader()
  {
    return I18n.Gmcm_Group_OtherIcons();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Artifacts_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Artifacts_Enable_Tooltip,
      getValue: () => Config.ShowArtifactSpotCount,
      setValue: value => Config.ShowArtifactSpotCount = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Artifacts_Seeds_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Artifacts_Seeds_Enable_Tooltip,
      getValue: () => Config.ShowSeedSpotCount,
      setValue: value => Config.ShowSeedSpotCount = value
    );
  }
#endregion
}
