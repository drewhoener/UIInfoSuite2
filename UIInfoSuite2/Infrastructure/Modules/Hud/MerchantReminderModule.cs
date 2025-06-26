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
internal class MerchantReminderModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : HudIconModule(modEvents, logger, configManager, iconStorage)
{
  protected const string IconPrefix = "MerchantIcon";

  // Lazy init because the icon init uses textures that aren't loaded yet
  private readonly Lazy<MerchantIcon> _booksellerIcon = new(() => new MerchantIcon(MerchantIcon.Type.Bookseller));
  private readonly Lazy<MerchantIcon> _travelerIcon = new(() => new MerchantIcon(MerchantIcon.Type.Traveler));

  public override bool ShouldEnable()
  {
    return Config.ShowMerchantIcons;
  }

  protected override void SetupIcons()
  {
    IconStorage.AddIcon($"{IconPrefix}-Traveler", _travelerIcon.Value);
    IconStorage.AddIcon($"{IconPrefix}-Bookseller", _booksellerIcon.Value);
  }

  protected override void RemoveIcons()
  {
    RemoveIconsWhere(IconPrefix, 2);
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.DayStarted += OnDayStarted;
    ModEvents.Display.MenuChanged += OnMenuChanged;
    _travelerIcon.Value.UpdateMerchantIcon();
    _booksellerIcon.Value.UpdateMerchantIcon();
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= OnDayStarted;
    ModEvents.Display.MenuChanged -= OnMenuChanged;
    base.OnDisable();
  }

  private void OnDayStarted(object? sender, EventArgs e)
  {
    _travelerIcon.Value.UpdateMerchantIcon();
    _booksellerIcon.Value.UpdateMerchantIcon();
  }

  private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
  {
    if (e.NewMenu is not ShopMenu menu)
    {
      return;
    }

    // TODO this can probably be replaced with a shopid check like below, was implemented before the shop data rework
    if (menu.forSale.Any(s => s is not Hat) && Game1.currentLocation.Name == "Forest")
    {
      _travelerIcon.Value.VisitedMerchant = true;
    }

    if (menu.ShopId == "Bookseller")
    {
      _travelerIcon.Value.VisitedMerchant = true;
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
      getValue: () => Config.ShowMerchantIcons,
      setValue: value => Config.ShowMerchantIcons = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Traveler_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Traveler_Enable_Tooltip,
      getValue: () => Config.ShowTravelingMerchantIcon,
      setValue: value => Config.ShowTravelingMerchantIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Bookseller_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Bookseller_Enable_Tooltip,
      getValue: () => Config.ShowBooksellerIcon,
      setValue: value => Config.ShowBooksellerIcon = value
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
