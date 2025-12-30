using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Config;

public class ConfigManager : IDisposable
{
  private readonly ApiManager _apiManager;
  private readonly IModEvents _events;
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
    _events = events;
    _manifest = manifest;
    _apiManager = apiManager;
    _eventsManager = eventsManager;
    Config = _helper.ReadConfig<ModConfig>();

    events.GameLoop.GameLaunched += OnGameLaunched;
    LocalizedContentManager.OnLanguageChange += OnLanguageChange;
  }

  public ModConfig Config { get; private set; }

  public void Dispose()
  {
    GC.SuppressFinalize(this);
    _events.GameLoop.GameLaunched -= OnGameLaunched;
    LocalizedContentManager.OnLanguageChange -= OnLanguageChange;
  }

  private IGenericModConfigMenuApi? GetModConfigApi()
  {
    return _apiManager.TryRegisterApi<IGenericModConfigMenuApi>(_helper, ModCompat.Gmcm, "1.6.0");
  }

  public void SaveConfig()
  {
    _helper.WriteConfig(Config);
    _eventsManager.TriggerOnConfigChange();
  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    RegisterModConfig();
  }

  private void OnLanguageChange(LocalizedContentManager.LanguageCode code)
  {
    IGenericModConfigMenuApi? modConfigMenuApi = GetModConfigApi();
    if (modConfigMenuApi is null)
    {
      return;
    }

    modConfigMenuApi.Unregister(_manifest);
    RegisterModConfig();
  }

  private void RegisterModConfig()
  {
    IGenericModConfigMenuApi? modConfigMenuApi = GetModConfigApi();
    if (modConfigMenuApi == null)
    {
      return;
    }

    modConfigMenuApi.Register(_manifest, () => { Config = new ModConfig(); }, SaveConfig);

    // Bucket configurable items into their correct pages
    List<IConfigurable> topLevelConfigs = [];
    Dictionary<string, List<IConfigurable>> configurations = new();
    string currentSection = "";
    string currentSubHeader = "";

    foreach (IConfigurable configurable in ModEntry.GetContainerCollection<IConfigurable>())
    {
      string? pageKey = configurable.GetConfigPage();
      if (pageKey is null)
      {
        topLevelConfigs.Add(configurable);
      }
      else
      {
        configurations.GetOrCreate(pageKey).Add(configurable);
      }
    }

    // Add main section and add configs
    modConfigMenuApi.AddSectionTitle(_manifest, I18n.Gmcm_MainMenu_Title, I18n.Gmcm_MainMenu_Tooltip);

    foreach (IConfigurable element in topLevelConfigs)
    {
      currentSection = UpdateSection(currentSection, element);
      string? subHeader = element.GetSubHeader();
      if (subHeader != null && subHeader != currentSubHeader)
      {
        modConfigMenuApi.AddSubHeader(_manifest, () => subHeader);
        currentSubHeader = subHeader;
      }

      element.AddConfigOptions(modConfigMenuApi, _manifest);
    }


    // Reset section tracker
    currentSection = "";
    currentSubHeader = "";
    // Add all sub-page links
    foreach (string pageKey in ConfigPageNames.Items)
    {
      (Func<string> title, Func<string> tooltip) = ConfigPageNames.GetPageLinkStrings(pageKey);
      modConfigMenuApi.AddPageLink(_manifest, pageKey, title, tooltip);
    }

    // Populate pages
    foreach ((string? pageKey, List<IConfigurable>? configElements) in configurations)
    {
      // Set up page
      (Func<string> pageTitle, _) = ConfigPageNames.GetPageLinkStrings(pageKey);
      modConfigMenuApi.AddPage(_manifest, pageKey, pageTitle);

      // Sort elements into their sections and subsections
      configElements.Sort();
      foreach (IConfigurable? element in configElements)
      {
        // Add elements
        currentSection = UpdateSection(currentSection, element);
        string? subHeader = element.GetSubHeader();
        if (subHeader != null && subHeader != currentSubHeader)
        {
          modConfigMenuApi.AddSubHeader(_manifest, () => subHeader);
          currentSubHeader = subHeader;
        }

        element.AddConfigOptions(modConfigMenuApi, _manifest);
      }
    }

    return;

    string UpdateSection(string prevSection, IConfigurable element)
    {
      if (prevSection == element.GetConfigSection())
      {
        return prevSection;
      }

      string newSection = element.GetConfigSection();
      (Func<string>? sectionTitle, Func<string>? sectionTooltip) =
        ConfigSectionNames.GetSectionTitleStrings(newSection);
      modConfigMenuApi.AddSectionTitle(_manifest, sectionTitle, sectionTooltip);

      return newSection;
    }
  }

  private void AddGroupHeader(IGenericModConfigMenuApi api, Func<string> text)
  {
    api.AddSubHeader(_manifest, text);
  }

  private void OnGameLaunchedOld(object? sender, GameLaunchedEventArgs e)
  {
    IGenericModConfigMenuApi? modConfigMenuApi = GetModConfigApi();
    if (modConfigMenuApi == null)
    {
      return;
    }

    modConfigMenuApi.Register(_manifest, () => { Config = new ModConfig(); }, SaveConfig);

// Main menu options


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

    // Status Icons
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_StatusIcons_Title,  // "Status Icons"
      I18n.Gmcm_Section_StatusIcons_Tooltip // "Configure status indicator icons"
    );

    // Notification Icons
    modConfigMenuApi.AddSectionTitle(
      _manifest,
      I18n.Gmcm_Section_NotificationIcons_Title,  // "Notification Icons"
      I18n.Gmcm_Section_NotificationIcons_Tooltip // "Configure notification icons"
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

    // Tooltips Page
    modConfigMenuApi.AddPage(_manifest, "tooltips", I18n.Gmcm_Page_Tooltips_Title);

    // Objects
    AddGroupHeader(modConfigMenuApi, I18n.Gmcm_Group_ObjectTooltips); // "Object Tooltips"

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
      name: I18n.Gmcm_Section_Keybinds_ItemRange_ShowHover,
      tooltip: I18n.Gmcm_Section_Keybinds_ItemRange_ShowHover_Tooltip,
      getValue: () => Config.ShowItemRangeHoverKeybind,
      setValue: value => Config.ShowItemRangeHoverKeybind = value
    );
    modConfigMenuApi.AddKeybindList(
      _manifest,
      name: I18n.Gmcm_Section_Keybinds_ItemRange_ShowAll,
      tooltip: I18n.Gmcm_Section_Keybinds_ItemRange_ShowAll_Tooltip,
      getValue: () => Config.ShowAllItemRangesHoverKeybind,
      setValue: value => Config.ShowAllItemRangesHoverKeybind = value
    );
  }
}
