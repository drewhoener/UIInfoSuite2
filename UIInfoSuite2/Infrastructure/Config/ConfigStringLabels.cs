using System;

namespace UIInfoSuite2.Infrastructure.Config;

using GMCMPageTuple = (Func<string>, Func<string>);

internal static class ConfigPageNames
{
  public const string HudIcons = "hud-icons";
  public const string Tooltips = "tooltips";
  public const string MenuFeatures = "menu-features";
  public const string Keybinds = "keybinds";
  public const string Advanced = "advanced";

  public static string[] Items =
  [
    HudIcons,
    Tooltips,
    MenuFeatures,
    Keybinds,
    Advanced
  ];

  public static GMCMPageTuple GetPageLinkStrings(string pageName)
  {
    return pageName switch
    {
      HudIcons => new GMCMPageTuple(I18n.Gmcm_Page_HudIcons_Title, I18n.Gmcm_Page_HudIcons_Tooltip),
      Tooltips => new GMCMPageTuple(I18n.Gmcm_Page_Tooltips_Title, I18n.Gmcm_Page_Tooltips_Tooltip),
      MenuFeatures => new GMCMPageTuple(I18n.Gmcm_Page_MenuFeatures_Title, I18n.Gmcm_Page_MenuFeatures_Tooltip),
      Keybinds => new GMCMPageTuple(I18n.Gmcm_Page_Keybinds_Title, I18n.Gmcm_Page_Keybinds_Tooltip),
      Advanced => new GMCMPageTuple(I18n.Gmcm_Page_Advanced_Title, I18n.Gmcm_Page_Advanced_Tooltip),
      _ => throw new NotSupportedException($"Page {pageName} is not supported")
    };
  }
}

internal static class ConfigSectionNames
{
  public const string HudGlobal = "hud-global";
  public const string StatusIcons = "status-icons";
  public const string NotificationIcons = "notification-icons";
  public const string EmptySection = "empty-section";

  public static GMCMPageTuple GetSectionTitleStrings(string sectionName)
  {
    return sectionName switch
    {
      HudGlobal => new GMCMPageTuple(I18n.Gmcm_Section_HudGlobal_Title, I18n.Gmcm_Section_HudGlobal_Tooltip),
      StatusIcons => new GMCMPageTuple(I18n.Gmcm_Section_StatusIcons_Title, I18n.Gmcm_Section_StatusIcons_Tooltip),
      NotificationIcons => new GMCMPageTuple(
        I18n.Gmcm_Section_NotificationIcons_Title,
        I18n.Gmcm_Section_NotificationIcons_Tooltip
      ),
      EmptySection => new GMCMPageTuple(() => "", () => ""),
      _ => throw new NotSupportedException($"Section {sectionName} is not supported")
    };
  }
}
