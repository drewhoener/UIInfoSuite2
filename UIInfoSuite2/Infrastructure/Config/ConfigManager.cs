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
    // XP
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

    // Luck
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

    // Merchant
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

    // Queen of Sauce
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Recipes_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Recipes_Enable_Tooltip,
      getValue: () => Config.ShowQueenOfSauceIcon,
      setValue: value => Config.ShowQueenOfSauceIcon = value
    );

    // Tool Upgrade
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Tool_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Tool_Enable_Tooltip,
      getValue: () => Config.ShowToolUpgradeIcon,
      setValue: value => Config.ShowToolUpgradeIcon = value
    );

    // Robin
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Icons_Carpenter_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Carpenter_Enable_Tooltip,
      getValue: () => Config.ShowRobinBuildingStatusIcon,
      setValue: value => Config.ShowRobinBuildingStatusIcon = value
    );

    // Seasonal Berries
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

    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_Tooltips_Title,
      I18n.Gmcm_Section_Tooltips_Title_Tooltip
    );

    // Animal tooltip
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

    // Crop tooltip
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Crops_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Crops_Enable_Tooltip,
      getValue: () => Config.ShowCropTooltip,
      setValue: value => Config.ShowCropTooltip = value
    );

    // Machine tooltip
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Machines_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Machines_Enable_Tooltip,
      getValue: () => Config.ShowMachineTooltip,
      setValue: value => Config.ShowMachineTooltip = value
    );

    // Ranges tooltip
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Tooltips_Ranges_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Ranges_Enable_Tooltip,
      getValue: () => Config.ShowItemEffectRanges,
      setValue: value => Config.ShowItemEffectRanges = value
    );

    modConfigMenuApi.AddSectionTitle(_manifest, I18n.Gmcm_Section_Menus_Title, I18n.Gmcm_Section_Menus_Title_Tooltip);

    // Heart fill
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Hearts_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Hearts_Enable_Tooltip,
      getValue: () => Config.ShowHeartFills,
      setValue: value => Config.ShowHeartFills = value
    );

    // Gift lock
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_GiftLock_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_GiftLock_Enable_Tooltip,
      getValue: () => Config.ShowLockAfterNpcGift,
      setValue: value => Config.ShowLockAfterNpcGift = value
    );

    // Harvest Prices
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_HarvestPrices_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_HarvestPrices_Enable_Tooltip,
      getValue: () => Config.ShowHarvestPricesInShop,
      setValue: value => Config.ShowHarvestPricesInShop = value
    );

    // Bundle info
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Bundles_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Bundles_Enable_Tooltip,
      getValue: () => Config.ShowItemsRequiredForBundles,
      setValue: value => Config.ShowItemsRequiredForBundles = value
    );

    // Menu Shortcuts
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Shortcuts_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_Shortcuts_Enable_Tooltip,
      getValue: () => Config.DisplayMenuShortcuts,
      setValue: value => Config.DisplayMenuShortcuts = value
    );

    // Calendar Shortcut
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Shortcuts_Calendar,
      tooltip: I18n.Gmcm_Modules_Menus_Shortcuts_Calendar_Tooltip,
      getValue: () => Config.DisplayCalendarAndBillboardShortcut,
      setValue: value => Config.DisplayCalendarAndBillboardShortcut = value
    );

    // Slayer Shortcut
    modConfigMenuApi.AddBoolOption(
      _manifest,
      name: I18n.Gmcm_Modules_Menus_Shortcuts_Slayer,
      tooltip: I18n.Gmcm_Modules_Menus_Shortcuts_Slayer_Tooltip,
      getValue: () => Config.DisplaySlayerQuestsShortcut,
      setValue: value => Config.DisplaySlayerQuestsShortcut = value
    );

    // Keybinds
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_Keybinds_Title,
      I18n.Gmcm_Section_Keybinds_Title_Tooltip
    );

    // Calendar keybind
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_Calendar,
      tooltip: I18n.Gmcm_Section_Keybinds_Calendar_Tooltip,
      getValue: () => Config.OpenCalendarKeybind,
      setValue: value => Config.OpenCalendarKeybind = value
    );

    // Billboard keybind
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_Billboard,
      tooltip: I18n.Gmcm_Section_Keybinds_Billboard_Tooltip,
      getValue: () => Config.OpenQuestBoardKeybind,
      setValue: value => Config.OpenQuestBoardKeybind = value
    );

    // Slayer Quests keybind
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_Slayer,
      tooltip: I18n.Gmcm_Section_Keybinds_Slayer_Tooltip,
      getValue: () => Config.OpenSlayerQuestKeybind,
      setValue: value => Config.OpenSlayerQuestKeybind = value
    );

    // Ranges keybind
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_ItemRange,
      tooltip: I18n.Gmcm_Section_Keybinds_ItemRange_Tooltip,
      getValue: () => Config.ToggleItemRangesKeybind,
      setValue: value => Config.ToggleItemRangesKeybind = value
    );
  }
}
