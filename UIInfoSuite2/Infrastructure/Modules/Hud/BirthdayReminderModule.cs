using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class BirthdayReminderModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : HudIconModule(modEvents, logger, configManager, iconStorage)
{
  private const string BirthdayIconPrefix = "BirthdayIcon-";
  private readonly PerScreen<List<NpcBirthdayIcon>> _birthdayCharacters = new(() => []);

  public override bool ShouldEnable()
  {
    return Config.ShowBirthdayIcon;
  }

  protected override void SetupIcons()
  {
    _birthdayCharacters.Value.Clear();
    IEnumerable<NPC> characters = Game1.locations.SelectMany(loc => loc.characters)
      .Where(character => character.isBirthday());

    foreach (NPC character in characters)
    {
      if (!Game1.player.friendshipData.TryGetValue(character.Name, out Friendship? friendship))
      {
        continue;
      }

      // Skip characters with full friendship if the config is set to
      if (Config.HideBirthdayIfFullFriendship &&
          friendship.Points >= Utility.GetMaximumHeartsForCharacter(character) * NPC.friendshipPointsPerHeartLevel)
      {
        continue;
      }

      // Set up character headshot icon
      var icon = new NpcBirthdayIcon(character)
      {
        HoverText = string.Format(I18n.NpcBirthday(), character.displayName)
      };
      _birthdayCharacters.Value.Add(icon);
      IconStorage.AddIcon($"{BirthdayIconPrefix}{character.Name}", icon);
    }
  }

  protected override void RemoveIcons()
  {
    RemoveIconsWhere(BirthdayIconPrefix, _birthdayCharacters.Value.Count);
    _birthdayCharacters.Value.Clear();
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.OneSecondUpdateTicked += OnUpdateTicked;
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.OneSecondUpdateTicked -= OnUpdateTicked;
    base.OnDisable();
  }


  private void OnUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
  {
    foreach (NpcBirthdayIcon npcBirthdayIcon in _birthdayCharacters.Value.Where(icon => icon.ShouldDraw()))
    {
      npcBirthdayIcon.UpdateGiftCheck();
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
    return I18n.Gmcm_Group_Birthday();
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Birthday_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Birthday_Enable_Tooltip,
      getValue: () => Config.ShowBirthdayIcon,
      setValue: value => Config.ShowBirthdayIcon = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Birthday_HideOnFriends,
      tooltip: I18n.Gmcm_Modules_Icons_Birthday_HideOnFriends_Tooltip,
      getValue: () => Config.HideBirthdayIfFullFriendship,
      setValue: value => Config.HideBirthdayIfFullFriendship = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Birthday_HideAfterGifted,
      tooltip: I18n.Gmcm_Modules_Icons_Birthday_HideAfterGifted_Tooltip,
      getValue: () => Config.HideAfterGiftGiven,
      setValue: value => Config.HideAfterGiftGiven = value
    );
  }
#endregion
}
