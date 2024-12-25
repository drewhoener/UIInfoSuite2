using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Events;

namespace UIInfoSuite2.Infrastructure.Config;

public class ConfigManager
{
  private readonly ApiManager _apiManager;
  private readonly EventsManager _eventsManager;
  private readonly IModHelper _helper;
  private readonly IManifest _manifest;


  public ConfigManager(
    IModHelper helper,
    IModEvents events,
    IManifest manifest,
    EventsManager eventsManager,
    ApiManager apiManager
  )
  {
    _helper = helper;
    _manifest = manifest;
    _apiManager = apiManager;
    _eventsManager = eventsManager;
    Config = _helper.ReadConfig<ModConfig>();

    events.GameLoop.GameLaunched += OnGameLaunched;
  }

  public ModConfig Config { get; private set; }

  public void SaveConfig()
  {
    _helper.WriteConfig(Config);
    _eventsManager.TriggerOnConfigChange();
  }

  private void AddGroupHeader(IGenericModConfigMenuApi api, Func<string> text)
  {
    api.AddSubHeader(_manifest, text);
  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    var modConfigMenuApi = _apiManager.TryRegisterApi<IGenericModConfigMenuApi>(_helper, ModCompat.Gmcm, "1.6.0");
    if (modConfigMenuApi == null)
    {
      return;
    }

    modConfigMenuApi.Register(_manifest, () => { Config = new ModConfig(); }, SaveConfig);

    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_Overlays_Title,
      I18n.Gmcm_Section_Overlays_Title_Tooltip
    );

// Main menu options
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_MainMenu_Title,  // "Main Settings"
      I18n.Gmcm_MainMenu_Tooltip // "Main configuration options"
    );

    // Add links to subpages
    modConfigMenuApi.AddPageLink(
      _manifest,
      "hud-icons",
      I18n.Gmcm_Page_HudIcons_Title,  // "HUD Icons"
      I18n.Gmcm_Page_HudIcons_Tooltip // "Configure status icons and indicators"
    );

    modConfigMenuApi.AddPageLink(
      _manifest,
      "tooltips",
      I18n.Gmcm_Page_Tooltips_Title,  // "Tooltips"
      I18n.Gmcm_Page_Tooltips_Tooltip // "Configure item and object tooltips"
    );

    modConfigMenuApi.AddPageLink(
      _manifest,
      "menu-features",
      I18n.Gmcm_Page_MenuFeatures_Title,  // "Menu Features"
      I18n.Gmcm_Page_MenuFeatures_Tooltip // "Configure additional menu features"
    );

    modConfigMenuApi.AddPageLink(
      _manifest,
      "keybinds",
      I18n.Gmcm_Page_Keybinds_Title,  // "Keybinds"
      I18n.Gmcm_Page_Keybinds_Tooltip // "Configure keyboard shortcuts"
    );

    // HUD Icons Page
    modConfigMenuApi.AddPage(_manifest, "hud-icons", I18n.Gmcm_Page_HudIcons_Title);

    // Global HUD settings
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_HudGlobal_Title,  // "Global HUD Settings"
      I18n.Gmcm_Section_HudGlobal_Tooltip // "Settings that affect all HUD icons"
    );

    modConfigMenuApi.AddNumberOption(
      _manifest,
      name: I18n.Gmcm_Modules_IconContainer_IconPerRow,
      tooltip: I18n.Gmcm_Modules_IconContainer_IconPerRow_Tooltip,
      getValue: () => Config.HudIconsPerRow,
      setValue: value => Config.HudIconsPerRow = value,
      min: 0,
      max: 10
    );

    modConfigMenuApi.AddNumberOption(
      _manifest,
      name: I18n.Gmcm_Modules_IconContainer_YOffset,
      tooltip: I18n.Gmcm_Modules_IconContainer_YOffset_Tooltip,
      getValue: () => Config.HudIconsVerticalOffset,
      setValue: value => Config.HudIconsVerticalOffset = value,
      min: 0,
      max: 100
    );

    modConfigMenuApi.AddNumberOption(
      _manifest,
      name: I18n.Gmcm_Modules_IconContainer_XOffset,
      tooltip: I18n.Gmcm_Modules_IconContainer_XOffset_Tooltip,
      getValue: () => Config.HudIconsHorizontalOffset,
      setValue: value => Config.HudIconsHorizontalOffset = value,
      min: 0,
      max: 100
    );

    modConfigMenuApi.AddNumberOption(
      _manifest,
      name: I18n.Gmcm_Modules_IconContainer_YSpacing,
      tooltip: I18n.Gmcm_Modules_IconContainer_YSpacing_Tooltip,
      getValue: () => Config.HudIconVerticalSpacing,
      setValue: value => Config.HudIconVerticalSpacing = value,
      min: 0,
      max: 100
    );

    modConfigMenuApi.AddNumberOption(
      _manifest,
      name: I18n.Gmcm_Modules_IconContainer_XSpacing,
      tooltip: I18n.Gmcm_Modules_IconContainer_XSpacing_Tooltip,
      getValue: () => Config.HudIconHorizontalSpacing,
      setValue: value => Config.HudIconHorizontalSpacing = value,
      min: 0,
      max: 100
    );

    // Status Icons
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_StatusIcons_Title,  // "Status Icons"
      I18n.Gmcm_Section_StatusIcons_Tooltip // "Configure status indicator icons"
    );

    // Luck
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_Luck); // "Luck Icon"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Luck_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Luck_Enable_Tooltip,
      getValue: () => Config.ShowLuckIcon,
      setValue: value => Config.ShowLuckIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Luck_Exact,
      tooltip: I18n.Gmcm_Modules_Icons_Luck_Exact_Tooltip,
      getValue: () => Config.ShowExactLuckValue,
      setValue: value => Config.ShowExactLuckValue = value
    );

    // Weather
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_Weather); // "Weather Icons"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Weather_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Weather_Enable_Tooltip,
      getValue: () => Config.ShowWeatherIcon,
      setValue: value => Config.ShowWeatherIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Weather_Island,
      tooltip: I18n.Gmcm_Modules_Icons_Weather_Island_Tooltip,
      getValue: () => Config.ShowIslandWeather,
      setValue: value => Config.ShowIslandWeather = value
    );

    // Experience Bar
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_XpBar); // "Experience Bar"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Xpbar_Enable,
      tooltip: I18n.Gmcm_Modules_Xpbar_Enable_Tooltip,
      getValue: () => Config.ShowExperienceBar,
      setValue: value => Config.ShowExperienceBar = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Xpbar_Fadeout,
      tooltip: I18n.Gmcm_Modules_Xpbar_Fadeout_Tooltip,
      getValue: () => Config.AllowExperienceBarToFadeOut,
      setValue: value => Config.AllowExperienceBarToFadeOut = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Xpbar_Gain,
      tooltip: I18n.Gmcm_Modules_Xpbar_Gain_Tooltip,
      getValue: () => Config.ShowExperienceGain,
      setValue: value => Config.ShowExperienceGain = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Xpbar_Levelup,
      tooltip: I18n.Gmcm_Modules_Xpbar_Levelup_Tooltip,
      getValue: () => Config.ShowLevelUpAnimation,
      setValue: value => Config.ShowLevelUpAnimation = value
    );

    // Notification Icons
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_NotificationIcons_Title,  // "Notification Icons"
      I18n.Gmcm_Section_NotificationIcons_Tooltip // "Configure notification icons"
    );

    // Merchant
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_Merchant); // "Traveling Merchant"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_Enable_Tooltip,
      getValue: () => Config.ShowTravelingMerchantIcon,
      setValue: value => Config.ShowTravelingMerchantIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Merchant_HideOnVisit,
      tooltip: I18n.Gmcm_Modules_Icons_Merchant_HideOnVisit_Tooltip,
      getValue: () => Config.HideMerchantIconWhenVisited,
      setValue: value => Config.HideMerchantIconWhenVisited = value
    );

    // Birthday
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_Birthday); // "Birthday Reminder"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Birthday_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Birthday_Enable_Tooltip,
      getValue: () => Config.ShowBirthdayIcon,
      setValue: value => Config.ShowBirthdayIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Birthday_HideOnFriends,
      tooltip: I18n.Gmcm_Modules_Icons_Birthday_HideOnFriends_Tooltip,
      getValue: () => Config.HideBirthdayIfFullFriendShip,
      setValue: value => Config.HideBirthdayIfFullFriendShip = value
    );

    // Other Icons
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_OtherIcons); // "Other Icons"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Recipes_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Recipes_Enable_Tooltip,
      getValue: () => Config.ShowQueenOfSauceIcon,
      setValue: value => Config.ShowQueenOfSauceIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Tool_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Tool_Enable_Tooltip,
      getValue: () => Config.ShowToolUpgradeIcon,
      setValue: value => Config.ShowToolUpgradeIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Carpenter_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Carpenter_Enable_Tooltip,
      getValue: () => Config.ShowRobinBuildingStatusIcon,
      setValue: value => Config.ShowRobinBuildingStatusIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Berry_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Berry_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalBerryIcon,
      setValue: value => Config.ShowSeasonalBerryIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Hazelnut_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Hazelnut_Enable_Tooltip,
      getValue: () => Config.ShowSeasonalBerryHazelnutIcon,
      setValue: value => Config.ShowSeasonalBerryHazelnutIcon = value
    );

    // Tooltips Page
    modConfigMenuApi.AddPage(_manifest, "tooltips", I18n.Gmcm_Page_Tooltips_Title);

    // Animals
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_AnimalTooltips); // "Animal Tooltips"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Animals_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Animals_Enable_Tooltip,
      getValue: () => Config.ShowAnimalsNeedPets,
      setValue: value => Config.ShowAnimalsNeedPets = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Animals_HideOnFriends,
      tooltip: I18n.Gmcm_Modules_Tooltips_Animals_HideOnFriends_Tooltip,
      getValue: () => Config.HideAnimalPetOnMaxFriendship,
      setValue: value => Config.HideAnimalPetOnMaxFriendship = value
    );

    // Objects
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_ObjectTooltips); // "Object Tooltips"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Crops_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Crops_Enable_Tooltip,
      getValue: () => Config.ShowCropTooltip,
      setValue: value => Config.ShowCropTooltip = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Machines_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Machines_Enable_Tooltip,
      getValue: () => Config.ShowMachineTooltip,
      setValue: value => Config.ShowMachineTooltip = value
    );

    // Range Indicators
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_RangeTooltips); // "Range Indicators"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Ranges_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Ranges_Enable_Tooltip,
      getValue: () => Config.ShowItemEffectRanges,
      setValue: value => Config.ShowItemEffectRanges = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Ranges_Bombs_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Ranges_Bombs_Enable_Tooltip,
      getValue: () => Config.ShowBombRanges,
      setValue: value => Config.ShowBombRanges = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Ranges_KeybindOnly_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Ranges_KeybindOnly_Enable_Tooltip,
      getValue: () => Config.OnlyShowRangeOnKeyPress,
      setValue: value => Config.OnlyShowRangeOnKeyPress = value
    );

    // Menu Features Page
    modConfigMenuApi.AddPage(_manifest, "menu-features", I18n.Gmcm_Page_MenuFeatures_Title);

    // Social Menu Features
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_SocialFeatures); // "Social Menu Features"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Hearts_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Hearts_Enable_Tooltip,
      getValue: () => Config.ShowHeartFills,
      setValue: value => Config.ShowHeartFills = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_GiftLock_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_GiftLock_Enable_Tooltip,
      getValue: () => Config.ShowLockAfterNpcGift,
      setValue: value => Config.ShowLockAfterNpcGift = value
    );

    // Shop Features
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_ShopFeatures); // "Shop Features"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_HarvestPrices_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_HarvestPrices_Enable_Tooltip,
      getValue: () => Config.ShowHarvestPricesInShop,
      setValue: value => Config.ShowHarvestPricesInShop = value
    );

    // Bundle Features
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_BundleFeatures); // "Bundle Features"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Bundles_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Bundles_Enable_Tooltip,
      getValue: () => Config.ShowItemsRequiredForBundles,
      setValue: value => Config.ShowItemsRequiredForBundles = value
    );

    // Menu Shortcuts
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_MenuShortcuts); // "Menu Shortcuts"
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Shortcuts_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Shortcuts_Enable_Tooltip,
      getValue: () => Config.DisplayMenuShortcuts,
      setValue: value => Config.DisplayMenuShortcuts = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Shortcuts_Calendar,
      tooltip: I18n.Gmcm_Modules_Menus_Shortcuts_Calendar_Tooltip,
      getValue: () => Config.DisplayCalendarAndBillboardShortcut,
      setValue: value => Config.DisplayCalendarAndBillboardShortcut = value
    );
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Shortcuts_Slayer,
      tooltip: I18n.Gmcm_Modules_Menus_Shortcuts_Slayer_Tooltip,
      getValue: () => Config.DisplaySlayerQuestsShortcut,
      setValue: value => Config.DisplaySlayerQuestsShortcut = value
    );

    // Keybinds Page
    modConfigMenuApi.AddPage(_manifest, "keybinds", I18n.Gmcm_Page_Keybinds_Title);

    // Menu Keybinds
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_MenuKeybinds); // "Menu Shortcuts"
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_Calendar,
      tooltip: I18n.Gmcm_Section_Keybinds_Calendar_Tooltip,
      getValue: () => Config.OpenCalendarKeybind,
      setValue: value => Config.OpenCalendarKeybind = value
    );
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_Billboard,
      tooltip: I18n.Gmcm_Section_Keybinds_Billboard_Tooltip,
      getValue: () => Config.OpenQuestBoardKeybind,
      setValue: value => Config.OpenQuestBoardKeybind = value
    );
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_Slayer,
      tooltip: I18n.Gmcm_Section_Keybinds_Slayer_Tooltip,
      getValue: () => Config.OpenSlayerQuestKeybind,
      setValue: value => Config.OpenSlayerQuestKeybind = value
    );

    // Range Display Keybinds
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_RangeKeybinds); // "Range Display Shortcuts"
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_ItemRange,
      tooltip: I18n.Gmcm_Section_Keybinds_ItemRange_Tooltip,
      getValue: () => Config.ToggleItemRangesKeybind,
      setValue: value => Config.ToggleItemRangesKeybind = value
    );
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybindsd_ItemRange_ShowHover,
      tooltip: I18n.Gmcm_Section_Keybindsd_ItemRange_ShowHover_Tooltip,
      getValue: () => Config.ShowItemRangeHoverKeybind,
      setValue: value => Config.ShowItemRangeHoverKeybind = value
    );
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybindsd_ItemRange_ShowAll,
      tooltip: I18n.Gmcm_Section_Keybindsd_ItemRange_ShowAll_Tooltip,
      getValue: () => Config.ShowAllItemRangesHoverKeybind,
      setValue: value => Config.ShowAllItemRangesHoverKeybind = value
    );
  }
}
