using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class DailyWeatherModule : HudIconModule
{
  private const string WeatherIconPrefix = "WeatherIcon-";

  // Lazy init because the weather icon init uses textures that aren't loaded yet
  private readonly Lazy<WeatherIcon> _islandWeatherIcon = new(() => new WeatherIcon(true));
  private readonly Lazy<WeatherIcon> _valleyWeatherIcon = new(() => new WeatherIcon(false));

  public DailyWeatherModule(
    IModEvents modEvents,
    IMonitor logger,
    ConfigManager configManager,
    HudIconStorage iconStorage
  ) : base(modEvents, logger, configManager, iconStorage) { }

  public override bool ShouldEnable()
  {
    return Config.ShowWeatherIcon;
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.OneSecondUpdateTicked += OnOneSecondTicked;
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.OneSecondUpdateTicked -= OnOneSecondTicked;
    base.OnDisable();
  }

  protected override void SetupIcons()
  {
    IconStorage.AddIcon($"{WeatherIconPrefix}Valley", _valleyWeatherIcon.Value);
    IconStorage.AddIcon($"{WeatherIconPrefix}Island", _islandWeatherIcon.Value);
  }

  protected override void RemoveIcons()
  {
    RemoveIconsWhere(WeatherIconPrefix, 2);
  }

  private void OnOneSecondTicked(object? sender, OneSecondUpdateTickedEventArgs e)
  {
    _valleyWeatherIcon.Value.DoWeatherCheck();
    _islandWeatherIcon.Value.DoWeatherCheck();
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
    return I18n.Gmcm_Group_Weather();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Weather_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Weather_Enable_Tooltip,
      getValue: () => Config.ShowWeatherIcon,
      setValue: value => Config.ShowWeatherIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Weather_Island,
      tooltip: I18n.Gmcm_Modules_Icons_Weather_Island_Tooltip,
      getValue: () => Config.ShowIslandWeather,
      setValue: value => Config.ShowIslandWeather = value
    );
  }
#endregion
}
