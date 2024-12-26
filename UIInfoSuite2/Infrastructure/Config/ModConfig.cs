using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace UIInfoSuite2.Infrastructure.Config;

public sealed class ModConfig
{
#region HUD Items Config
  // Icons
  public int HudIconsPerRow { get; set; } = 5;

  public int HudIconsVerticalOffset { get; set; }
  public int HudIconsHorizontalOffset { get; set; }
  public int HudIconVerticalSpacing { get; set; } = 8;
  public int HudIconHorizontalSpacing { get; set; } = 8;

  // XP Bar
  public bool AllowExperienceBarToFadeOut { get; set; } = true;
  public bool ShowExperienceBar { get; set; } = true;
  public bool ShowExperienceGain { get; set; } = true;
  public bool ShowLevelUpAnimation { get; set; } = true;

  // Luck
  public bool ShowLuckIcon { get; set; } = true;
  public bool ShowExactLuckValue { get; set; }

  // Weather
  public bool ShowWeatherIcon { get; set; } = true;
  public bool ShowIslandWeather { get; set; } = true;

  // Merchant
  public bool ShowTravelingMerchantIcon { get; set; } = true;
  public bool HideMerchantIconWhenVisited { get; set; }

  // Birthdays
  public bool ShowBirthdayIcon { get; set; } = true;
  public bool HideBirthdayIfFullFriendship { get; set; } = true;
  public bool HideAfterGiftGiven { get; set; } = true;

  // Queen of Sauce
  public bool ShowQueenOfSauceIcon { get; set; } = true;

  // Tool Upgrade Icon
  public bool ShowToolUpgradeIcon { get; set; } = true;

  // Robin Icon
  public bool ShowRobinBuildingStatusIcon { get; set; } = true;

  // Seasonal berry Icon
  public bool ShowSeasonalBerryIcon { get; set; } = true;
  public bool ShowSeasonalBerryHazelnutIcon { get; set; }

  // Animal Hands
  public bool ShowAnimalsNeedPets { get; set; } = true;
  public bool HideAnimalPetOnMaxFriendship { get; set; } = true;
#endregion

#region Menu Tweaks Config
  // Social menu exact hearts
  public bool ShowHeartFills { get; set; } = true;

  // Harvest price display for seeds
  public bool ShowHarvestPricesInShop { get; set; } = true;

  // Bundle required items on mouse over
  public bool ShowItemsRequiredForBundles { get; set; } = true;

  // Lock icon after gift today
  public bool ShowLockAfterNpcGift { get; set; } = true;

  // Display for calendar and other menu shortcuts (if unlocked)
  public bool DisplayMenuShortcuts { get; set; } = true;
  public bool DisplayCalendarAndBillboardShortcut { get; set; } = true;
  public bool DisplaySlayerQuestsShortcut { get; set; } = true;
#endregion

#region Tooltips Config
  // Crops & Machines
  public bool ShowCropTooltip { get; set; } = true;
  public bool ShowMachineTooltip { get; set; } = true;

  // Item Range
  public bool ShowItemEffectRanges { get; set; } = true;
  public bool ShowBombRanges { get; set; } = true;

  public bool ShowRangeOnKeyDownWhileHovered =>
    ShowItemRangeHoverKeybind.Keybinds.Length > 0 || ShowAllItemRangesHoverKeybind.Keybinds.Length > 0;

  public bool OnlyShowRangeOnKeyPress { get; set; }
#endregion

#region Keybinds Config
  public KeybindList OpenCalendarKeybind { get; set; } = KeybindList.ForSingle();
  public KeybindList OpenQuestBoardKeybind { get; set; } = KeybindList.ForSingle();
  public KeybindList OpenSlayerQuestKeybind { get; set; } = KeybindList.ForSingle();
  public KeybindList ToggleItemRangesKeybind { get; set; } = KeybindList.ForSingle();
  public KeybindList ShowItemRangeHoverKeybind { get; set; } = KeybindList.ForSingle(SButton.LeftControl);
  public KeybindList ShowAllItemRangesHoverKeybind { get; set; } = KeybindList.Parse("LeftControl + LeftAlt");
#endregion
}
