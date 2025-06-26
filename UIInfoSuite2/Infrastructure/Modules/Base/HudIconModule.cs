using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;

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
    ModEvents.GameLoop.DayStarted += HudIcon_OnDayStarted;
    ModEvents.GameLoop.DayEnding += HudIcon_OnDayEnd;
    SetupIcons();
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= HudIcon_OnDayStarted;
    ModEvents.GameLoop.DayEnding -= HudIcon_OnDayEnd;
    RemoveIcons();
  }

  protected abstract void SetupIcons();
  protected abstract void RemoveIcons();

  protected void RemoveIconsWhere(Func<KeyValuePair<string, ClickableIcon>, bool> predicate, int expectedRemoved)
  {
    int removed = IconStorage.RemoveIconWhere(predicate);
    Logger.Log($"Removed {removed} icons");
    if (removed != expectedRemoved)
    {
      Logger.Log($"Expected to remove {expectedRemoved} icons, but removed {removed}", LogLevel.Warn);
    }
  }

  protected void RemoveIconsWhere(string prefix, int expectedRemoved)
  {
    RemoveIconsWhere(pair => pair.Key.StartsWith(prefix), expectedRemoved);
  }

  private void HudIcon_OnDayEnd(object? sender, DayEndingEventArgs e)
  {
    RemoveIcons();
  }

  private void HudIcon_OnDayStarted(object? sender, DayStartedEventArgs e)
  {
    RemoveIcons();
    SetupIcons();
  }

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
