using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.UIElements;

internal class ToolIcon : ClickableIcon
{
  private readonly PerScreen<Tool?> _tool = new(() => null);

  public ToolIcon() : base(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 40)
  {
    UpdateTool();
  }

  public Tool? Tool => _tool.Value;

  public void UpdateTool()
  {
    if (_tool.Value == Game1.player.toolBeingUpgraded.Value)
    {
      return;
    }

    _tool.Value = Game1.player.toolBeingUpgraded.Value;
    if (Tool is null)
    {
      return;
    }

    switch (Tool)
    {
      case Axe or Pickaxe or Hoe or WateringCan or Pan or GenericTool { IndexOfMenuItemView: >= 13 and <= 16 }:
      {
        ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(Tool.QualifiedItemId);
        BaseTexture.Value = itemData.GetTexture();
        SourceBounds.Value = itemData.GetSourceRect();
        ScalingDimensions.Value.SourceDimensions = itemData.GetSourceRect();
        ResetTextureComponent();
        break;
      }
    }

    if (Game1.player.daysLeftForToolUpgrade.Value > 0)
    {
      HoverText = string.Format(
        I18n.DaysUntilToolIsUpgraded(),
        Game1.player.daysLeftForToolUpgrade.Value,
        Tool.DisplayName
      );
    }
    else
    {
      HoverText = string.Format(I18n.ToolIsFinishedBeingUpgraded(), Tool.DisplayName);
    }
  }

  protected override bool _ShouldDraw()
  {
    return Tool is not null;
  }
}

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
    ModEvents.GameLoop.OneSecondUpdateTicked += UpdateToolInfo;
    ModEvents.GameLoop.DayStarted += UpdateToolInfo;
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.OneSecondUpdateTicked -= UpdateToolInfo;
    ModEvents.GameLoop.DayStarted -= UpdateToolInfo;
    base.OnDisable();
  }


  private void UpdateToolInfo(object? sender, EventArgs e)
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
