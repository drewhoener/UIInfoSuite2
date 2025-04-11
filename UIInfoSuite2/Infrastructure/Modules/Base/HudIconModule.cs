using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models;

namespace UIInfoSuite2.Infrastructure.Modules.Base;

internal abstract class HudIconModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : BaseModule(modEvents, logger, configManager), IConfigurable
{
  protected readonly HudIconStorage IconStorage = iconStorage;

  public override void OnEnable()
  {
    SetupIcons();
  }

  public override void OnDisable()
  {
    RemoveIcons();
  }

  protected abstract void SetupIcons();
  protected abstract void RemoveIcons();

#region Configuration Setup
  public virtual int GetOrder()
  {
    return 0;
  }

  public virtual string? GetConfigPage()
  {
    return null;
  }

  public virtual string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public virtual string? GetSubHeader()
  {
    return null;
  }

  public abstract void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest);
#endregion
}
