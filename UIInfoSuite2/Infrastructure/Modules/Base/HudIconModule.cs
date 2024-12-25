using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;

namespace UIInfoSuite2.Infrastructure.Modules.Base;

public abstract class HudIconModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : BaseModule(modEvents, logger, configManager)
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
}
