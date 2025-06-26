using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class SeasonalForageDisplayModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<ForageIcon>(modEvents, logger, configManager, iconStorage)
{
  protected override string IconKey => "ForageIcon";

  public override bool ShouldEnable()
  {
    return Config.ShowSeasonalForageIcon;
  }

  protected override ForageIcon GenerateNewIcon()
  {
    return new ForageIcon();
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
    return I18n.Gmcm_Group_SeasonalForage();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Forage_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Forage_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalForageIcon,
      setValue: value => Config.ShowSeasonalForageIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Forage_Hazelnut_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Forage_Hazelnut_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalBerryHazelnutIcon,
      setValue: value => Config.ShowSeasonalBerryHazelnutIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Forage_Beach_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Forage_Beach_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalForageBeachIcon,
      setValue: value => Config.ShowSeasonalForageBeachIcon = value
    );
  }
#endregion
}
