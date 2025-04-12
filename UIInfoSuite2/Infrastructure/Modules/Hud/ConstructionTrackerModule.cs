using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.Network;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ConstructionTrackerModule : SingleHudIconModule<CarpenterIcon>
{
  // Rider insists on expanding this enormous generic for some reason
  // NetStringDictionary<BuilderData, NetRef<BuilderData>>
  private readonly
    NetDictionary<string, BuilderData, NetRef<BuilderData>, SerializableDictionary<string, BuilderData>,
      NetStringDictionary<BuilderData, NetRef<BuilderData>>>.ContentsChangeEvent _buildersDictChangedEvent;

  private readonly FieldChange<NetInt, int> _houseUpgradeChangedEvent;

  public ConstructionTrackerModule(
    IModEvents modEvents,
    IMonitor logger,
    ConfigManager configManager,
    HudIconStorage iconStorage
  ) : base(modEvents, logger, configManager, iconStorage)
  {
    _buildersDictChangedEvent = (key, _) =>
    {
      if (!key.EqualsIgnoreCase("robin"))
      {
        return;
      }

      UpdateIcon();
    };

    _houseUpgradeChangedEvent = (_, _, _) => { UpdateIcon(); };
  }

  protected override string IconKey => "ConstructionIcon";

  public override bool ShouldEnable()
  {
    return Config.ShowRobinBuildingStatusIcon;
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.DayStarted += OnDayStarted;
    ModEvents.GameLoop.SaveLoaded += OnSaveLoaded;
    ModEvents.GameLoop.ReturnedToTitle += OnReturnToTitle;
    AddFieldWatchers();
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= OnDayStarted;
    ModEvents.GameLoop.SaveLoaded -= OnSaveLoaded;
    ModEvents.GameLoop.ReturnedToTitle -= OnReturnToTitle;
    RemoveFieldWatchers();
    base.OnDisable();
  }

  protected override CarpenterIcon GenerateNewIcon()
  {
    Texture2D? iconTexture = Game1.getCharacterFromName("Robin")?.Sprite?.Texture;
    var sourceRect = new Rectangle(1, 197, 15, 15);
    if (iconTexture == null)
    {
      iconTexture = Game1.mouseCursors;
      sourceRect = new Rectangle(366, 373, 16, 16);
    }

    return new CarpenterIcon(iconTexture, sourceRect, 40);
  }

  private void UpdateIcon()
  {
    string? robinMessage = GetRobinMessage();
    if (robinMessage == null)
    {
      Icon.HoverText = "";
      Icon.IsRobinBuilding = false;
      return;
    }

    Icon.HoverText = robinMessage;
    Icon.IsRobinBuilding = true;
  }

  private static string? GetRobinMessage()
  {
    int remainingHouseDays = Game1.player.daysUntilHouseUpgrade.Value;
    if (remainingHouseDays > 0)
    {
      return string.Format(I18n.RobinHouseUpgradeStatus(), remainingHouseDays);
    }

    Building? building = Game1.GetBuildingUnderConstruction();
    if (building == null)
    {
      return null;
    }

    return string.Format(
      I18n.RobinBuildingStatus(),
      building.daysOfConstructionLeft.Value > building.daysUntilUpgrade.Value
        ? building.daysOfConstructionLeft.Value
        : building.daysUntilUpgrade.Value
    );
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    UpdateIcon();
  }

  private void OnReturnToTitle(object? sender, ReturnedToTitleEventArgs e)
  {
    RemoveFieldWatchers();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    AddFieldWatchers();
  }

  private void RemoveFieldWatchers()
  {
    Game1.netWorldState.Value.Builders.OnValueAdded -= _buildersDictChangedEvent;
    Game1.player.daysUntilHouseUpgrade.fieldChangeEvent -= _houseUpgradeChangedEvent;
  }

  private void AddFieldWatchers()
  {
    RemoveFieldWatchers();
    Game1.netWorldState.Value.Builders.OnValueAdded += _buildersDictChangedEvent;
    Game1.player.daysUntilHouseUpgrade.fieldChangeEvent += _houseUpgradeChangedEvent;
  }

#region Configuration Setup
  public override string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.StatusIcons;
  }

  public override string GetSubHeader()
  {
    return I18n.Gmcm_Group_OtherIcons();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Carpenter_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Carpenter_Enable_Tooltip,
      getValue: () => Config.ShowRobinBuildingStatusIcon,
      setValue: value => Config.ShowRobinBuildingStatusIcon = value
    );
  }
#endregion
}
