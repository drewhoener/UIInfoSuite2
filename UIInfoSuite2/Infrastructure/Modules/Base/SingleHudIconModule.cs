using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;

namespace UIInfoSuite2.Infrastructure.Modules.Base;

public abstract class SingleHudIconModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<ClickableIcon>(modEvents, logger, configManager, iconStorage);

public abstract class SingleHudIconModule<T>(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : HudIconModule(modEvents, logger, configManager, iconStorage) where T : ClickableIcon
{
  private T? _icon;

  protected T Icon
  {
    get
    {
      if (_icon == null)
      {
        SetupIcons();
      }

      return _icon!;
    }
    set => _icon = value;
  }

  protected abstract string IconKey { get; }

  protected override void RemoveIcons()
  {
    IconStorage.RemoveIcon(IconKey);
  }
}
