using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class SeasonalBerryDisplayModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<BerryIcon>(modEvents, logger, configManager, iconStorage)
{
  protected override string IconKey => "BerryIcon";

  public override bool ShouldEnable()
  {
    return Config.ShowSeasonalBerryIcon;
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.DayStarted += OnDayStarted;
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= OnDayStarted;
    base.OnDisable();
  }

  protected override BerryIcon GenerateNewIcon()
  {
    return new BerryIcon();
  }

  private void OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    SetupIcons();
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
    return I18n.Gmcm_Group_SeasonalBerries();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Berry_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Berry_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalBerryIcon,
      setValue: value => Config.ShowSeasonalBerryIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Hazelnut_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Hazelnut_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalBerryHazelnutIcon,
      setValue: value => Config.ShowSeasonalBerryHazelnutIcon = value
    );
  }
#endregion
}
