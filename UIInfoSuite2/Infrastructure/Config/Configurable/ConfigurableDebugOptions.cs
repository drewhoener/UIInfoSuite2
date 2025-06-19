using StardewModdingAPI;
using UIInfoSuite2.Compatibility;

namespace UIInfoSuite2.Infrastructure.Config.Configurable;

public class ConfigurableDebugOptions(ConfigManager configManager) : BaseConfigurable(configManager)
{
  public override string GetConfigPage()
  {
    return ConfigPageNames.Advanced;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Section_Advanced_DrawDebugBounds_Enable,
      tooltip: I18n.Gmcm_Section_Advanced_DrawDebugBounds_Enable_Tooltip,
      getValue: () => Config.DrawDebugBounds,
      setValue: value => Config.DrawDebugBounds = value
    );
  }
}
