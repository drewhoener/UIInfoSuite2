using StardewModdingAPI;
using UIInfoSuite2.Compatibility;

namespace UIInfoSuite2.Infrastructure.Config.Configurable;

public class ConfigurableHudIconPositioning(ConfigManager configManager) : BaseConfigurable(configManager)
{
  public override int GetOrder()
  {
    return -100;
  }

  public override string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.HudGlobal;
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddNumberOption(
      manifest,
      name: I18n.Gmcm_Modules_IconContainer_IconPerRow,
      tooltip: I18n.Gmcm_Modules_IconContainer_IconPerRow_Tooltip,
      getValue: () => Config.HudIconsPerRow,
      setValue: value => Config.HudIconsPerRow = value,
      min: 0,
      max: 10
    );

    modConfigMenuApi.AddNumberOption(
      manifest,
      name: I18n.Gmcm_Modules_IconContainer_YOffset,
      tooltip: I18n.Gmcm_Modules_IconContainer_YOffset_Tooltip,
      getValue: () => Config.HudIconsVerticalOffset,
      setValue: value => Config.HudIconsVerticalOffset = value,
      min: 0,
      max: 100
    );

    modConfigMenuApi.AddNumberOption(
      manifest,
      name: I18n.Gmcm_Modules_IconContainer_XOffset,
      tooltip: I18n.Gmcm_Modules_IconContainer_XOffset_Tooltip,
      getValue: () => Config.HudIconsHorizontalOffset,
      setValue: value => Config.HudIconsHorizontalOffset = value,
      min: 0,
      max: 100
    );

    modConfigMenuApi.AddNumberOption(
      manifest,
      name: I18n.Gmcm_Modules_IconContainer_YSpacing,
      tooltip: I18n.Gmcm_Modules_IconContainer_YSpacing_Tooltip,
      getValue: () => Config.HudIconVerticalSpacing,
      setValue: value => Config.HudIconVerticalSpacing = value,
      min: 0,
      max: 100
    );

    modConfigMenuApi.AddNumberOption(
      manifest,
      name: I18n.Gmcm_Modules_IconContainer_XSpacing,
      tooltip: I18n.Gmcm_Modules_IconContainer_XSpacing_Tooltip,
      getValue: () => Config.HudIconHorizontalSpacing,
      setValue: value => Config.HudIconHorizontalSpacing = value,
      min: 0,
      max: 100
    );
  }
}
