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
        return location.IsOutdoors;
    }
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

    foreach (KeyValuePair<Vector2, SObject> kvp in e.Added)
    {
      switch (kvp.Value.QualifiedItemId)
      {
        case ArtifactSpotId:
          _trackedArtifactSpots.GetOrCreate(e.Location).Add(kvp.Key);
          break;
        case SeedSpotId:
          _trackedSeedSpots.GetOrCreate(e.Location).Add(kvp.Key);
          break;
      }
    }

    foreach (KeyValuePair<Vector2, SObject> kvp in e.Removed)
    {
      switch (kvp.Value.QualifiedItemId)
      {
        case ArtifactSpotId:
          _trackedArtifactSpots.GetOrCreate(e.Location).Remove(kvp.Key);
          break;
        case SeedSpotId:
          _trackedSeedSpots.GetOrCreate(e.Location).Remove(kvp.Key);
          break;
      }
    }

    Icon.UpdateText(_trackedArtifactSpots, _trackedSeedSpots);
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
        switch (obj.QualifiedItemId)
        {
          case ArtifactSpotId:
            _trackedArtifactSpots.GetOrCreate(gameLocation).Add(tile);
            break;
          case SeedSpotId:
            _trackedSeedSpots.GetOrCreate(gameLocation).Add(tile);
            break;
        }
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
