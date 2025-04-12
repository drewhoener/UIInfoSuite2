using System;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class TravelingMerchantReminderModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<MerchantIcon>(modEvents, logger, configManager, iconStorage)
{
  protected override string IconKey => "MerchantIcon";

  public override bool ShouldEnable()
  {
    return Config.ShowTravelingMerchantIcon;
  }

  protected override MerchantIcon GenerateNewIcon()
  {
    return new MerchantIcon();
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.DayStarted += OnDayStarted;
    ModEvents.Display.MenuChanged += OnMenuChanged;
    Icon.UpdateMerchantIcon();
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= OnDayStarted;
    ModEvents.Display.MenuChanged -= OnMenuChanged;
    base.OnDisable();
  }

  private void OnDayStarted(object? sender, EventArgs e)
  {
    Icon.UpdateMerchantIcon();
  }

  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    if (e.NewMenu is ShopMenu menu && menu.forSale.Any(s => s is not Hat) && Game1.currentLocation.Name == "Forest")
    {
      Icon.VisitedMerchant = true;
    }
  }

#region Configuration Setup
  public override string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.NotificationIcons;
  }

  public override string GetSubHeader()
  {
    return I18n.Gmcm_Group_Merchant();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Enable_Tooltip,
      getValue: () => Config.ShowTravelingMerchantIcon,
      setValue: value => Config.ShowTravelingMerchantIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_HideOnVisit,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_HideOnVisit_Tooltip,
      getValue: () => Config.HideMerchantIconWhenVisited,
      setValue: value => Config.HideMerchantIconWhenVisited = value
    );
  }
#endregion
}
