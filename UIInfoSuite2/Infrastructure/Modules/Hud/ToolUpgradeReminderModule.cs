using System;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

internal class ToolUpgradeReminderModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<ToolIcon>(modEvents, logger, configManager, iconStorage)
{
  protected override string IconKey => "ToolIcon";

  public override bool ShouldEnable()
  {
    return Config.ShowToolUpgradeIcon;
  }

  protected override ToolIcon GenerateNewIcon()
  {
    return new ToolIcon();
  }

  public override void OnEnable()
  {
    base.OnEnable();
    Game1.player.toolBeingUpgraded.fieldChangeEvent += OnFieldUpdateTool;
    ModEvents.GameLoop.DayStarted += OnEventUpdateTool;
  }

  public override void OnDisable()
  {
    Game1.player.toolBeingUpgraded.fieldChangeEvent -= OnFieldUpdateTool;
    ModEvents.GameLoop.DayStarted -= OnEventUpdateTool;
    base.OnDisable();
  }

  private void OnFieldUpdateTool(NetRef<Tool> netRef, Tool oldTool, Tool newTool)
  {
    UpdateToolInfo();
  }

  private void OnEventUpdateTool(object? sender, EventArgs e)
  {
    UpdateToolInfo();
  }


  private void UpdateToolInfo()
  {
    if (Icon.Tool != Game1.player.toolBeingUpgraded.Value)
    {
      Icon.UpdateTool();
    }
  }

#region Configuration Setup
  public override string? GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.NotificationIcons;
  }

  public override string? GetSubHeader()
  {
    return I18n.Gmcm_Group_OtherIcons();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Tool_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Tool_Enable_Tooltip,
      getValue: () => Config.ShowToolUpgradeIcon,
      setValue: value => Config.ShowToolUpgradeIcon = value
    );
  }
#endregion
}
