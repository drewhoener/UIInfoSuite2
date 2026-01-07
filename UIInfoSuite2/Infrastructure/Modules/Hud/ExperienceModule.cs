using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Events.Args;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models.Experience;
using UIInfoSuite2.Infrastructure.Models.Managers;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

internal record XpThreshold(int TotalXp, int LevelXp, int NextLevelXp);

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ExperienceModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  SoundHelper soundHelper,
  FloatingTextManager floatingTextManager,
  EventsManager eventsManager
) : BaseModule(modEvents, logger, configManager), IConfigurable
{
  private const string FloatingTextKey = "ExperienceGain";
  private const int XpVisibleTicks = 100;
  private static readonly XpThreshold EmptyThreshold = new(0, 0, 0);

  private readonly PerScreen<ExperienceBarModel> _displayedExperienceBar = new(() => new ExperienceBarModel());

  private readonly PerScreen<FieldChange<NetInt, int>?[]> _experienceCallbacks =
    new(() => new FieldChange<NetInt, int>[Game1.player.experiencePoints.Length]);

  private readonly PerScreen<Item?> _previousItem = new();

  public override bool ShouldEnable()
  {
    return Config.ShowExperienceBar || Config.ShowExperienceGain || Config.ShowLevelUpAnimation;
  }

  public override void OnEnable()
  {
    ModEvents.Display.RenderingHud += OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked_HandleTimers;
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked_CheckTools;
    ModEvents.Player.LevelChanged += OnLevelChanged;
    eventsManager.OnMasteryXpGain += OnMasteryXpChange;

    _displayedExperienceBar.Value.UpdatePosition();

    AddXpValueListeners();
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingHud -= OnRenderingHud;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked_HandleTimers;
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked_CheckTools;
    ModEvents.Player.LevelChanged -= OnLevelChanged;
    eventsManager.OnMasteryXpGain -= OnMasteryXpChange;

    if (Context.IsWorldReady)
    {
      RemoveXpValueListeners();
    }
  }

  private void OnMasteryXpChange(object? sender, MasteryXpGainArgs args)
  {
    UpdateExperienceBar(args.SkillType, args.OldXp);
  }

  private void ShowExperienceBar(int skillIndex)
  {
    XpThreshold threshold = GetXpThreshold(skillIndex);
    _displayedExperienceBar.Value.SetTrackedSkill(skillIndex, threshold);
  }

  private void UpdateExperienceBar(int skillIndex, int prevXp)
  {
    XpThreshold threshold = GetXpThreshold(skillIndex);
    int experienceGain = threshold.TotalXp - prevXp;

    if (threshold.NextLevelXp > 0 && experienceGain > 0)
    {
      AddFloatingXpText(experienceGain);
    }

    _displayedExperienceBar.Value.SetTrackedSkill(skillIndex, threshold);
  }

  private void OnLevelChanged(object? sender, LevelChangedEventArgs e)
  {
    if (!Config.ShowLevelUpAnimation || !e.IsLocalPlayer)
    {
      return;
    }

    floatingTextManager.Add(new LevelUpMessage(TextureHelper.SkillIconRectangles[(int)e.Skill]));
    soundHelper.Play(Sounds.LevelUp);
  }

  private void OnUpdateTicked_CheckTools(object? sender, UpdateTickedEventArgs e)
  {
    Item? currentItem = Game1.player.CurrentItem;
    if (!e.IsMultipleOf(15) || currentItem == _previousItem.Value)
    {
      return;
    }

    _previousItem.Value = currentItem;
    ShowExperienceBar(GetSkillIndexFromTool(currentItem));
  }

  private void OnUpdateTicked_HandleTimers(object? sender, UpdateTickedEventArgs e)
  {
    _displayedExperienceBar.Value.Update();
  }

  private void OnRenderingHud(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally())
    {
      return;
    }

    _displayedExperienceBar.Value.Draw(e.SpriteBatch);
  }

  private static int GetSkillIndexFromTool(Item? currentItem)
  {
    return currentItem switch
    {
      FishingRod => (int)SkillType.Fishing,
      Pickaxe => (int)SkillType.Mining,
      MeleeWeapon weapon when weapon.Name != "Scythe" => (int)SkillType.Combat,
      _ when Game1.currentLocation is Farm && currentItem is not Axe => (int)SkillType.Farming,
      _ => (int)SkillType.Foraging
    };
  }

  private void AddFloatingXpText(int experienceGain)
  {
    if (!Config.ShowExperienceGain)
    {
      return;
    }

    floatingTextManager.Add(
      new FloatingText(
        $"Exp: {experienceGain}",
        XpVisibleTicks,
        new Vector2(0, -0.5f),
        true,
        FloatingTextKey,
        fullAlphaTicks: 20
      )
    );
  }

#region NetField listeners
  private void AddXpValueListeners()
  {
    for (var i = 0; i < Game1.player.experiencePoints.Length; i++)
    {
      int skillIndex = i;
      FieldChange<NetInt, int>? oldCallback = _experienceCallbacks.Value[i];
      FieldChange<NetInt, int> newCallback = (_, prev, _) =>
      {
        // These callbacks won't work when we're in mastery range since they still provide an accurate count of xp
        // for that specific skill, but not the overall mastery count.
        if (Tools.IsMasteryLevel())
        {
          return;
        }

        UpdateExperienceBar(skillIndex, prev);
      };

      if (oldCallback != null)
      {
        Game1.player.experiencePoints.Fields[i].fieldChangeVisibleEvent -= oldCallback;
      }

      _experienceCallbacks.Value[i] = newCallback;
      Game1.player.experiencePoints.Fields[i].fieldChangeVisibleEvent += newCallback;
    }
  }

  private void RemoveXpValueListeners()
  {
    for (var i = 0; i < Game1.player.experiencePoints.Length; i++)
    {
      FieldChange<NetInt, int>? oldCallback = _experienceCallbacks.Value[i];
      if (oldCallback != null)
      {
        Game1.player.experiencePoints.Fields[i].fieldChangeVisibleEvent -= oldCallback;
      }
    }

    Array.Clear(_experienceCallbacks.Value);
  }
#endregion

#region Static Helpers
  private static XpThreshold GetMasteryThreshold()
  {
    int currentMasteryLevel = MasteryTrackerMenu.getCurrentMasteryLevel(); // 1
    var totalMasteryXp = (int)Game1.stats.Get("MasteryExp"); // 15,000
    int xpToReachCurLevel = MasteryTrackerMenu.getMasteryExpNeededForLevel(currentMasteryLevel); // 10,000
    int xpThisLevel = totalMasteryXp - xpToReachCurLevel; // 15,000 - 10,000 = 5,000
    int xpForNextLevel = MasteryTrackerMenu.getMasteryExpNeededForLevel(currentMasteryLevel + 1); // 25,000
    return new XpThreshold(totalMasteryXp, xpThisLevel, xpForNextLevel - xpToReachCurLevel);
  }

  private static XpThreshold GetXpThreshold(int skillType)
  {
    // Player is on mastery track
    if (Tools.IsMasteryLevel())
    {
      return GetMasteryThreshold();
    }

    int skillLevel = Game1.player.GetUnmodifiedSkillLevel(skillType);
    int totalSkillXp = Game1.player.experiencePoints[skillType];
    int xpToGetToLevel = skillLevel == 0 ? 0 : Farmer.getBaseExperienceForLevel(skillLevel);
    if (skillLevel == 10 || xpToGetToLevel == -1)
    {
      return EmptyThreshold;
    }

    int xpThisLevel = totalSkillXp - xpToGetToLevel;
    int xpForNextLevel = Farmer.getBaseExperienceForLevel(skillLevel + 1) - xpToGetToLevel;

    return new XpThreshold(totalSkillXp, xpThisLevel, xpForNextLevel);
  }
#endregion

#region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.HudGlobal;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_XpBar();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Enable,
      tooltip: I18n.Gmcm_Modules_Xpbar_Enable_Tooltip,
      getValue: () => Config.ShowExperienceBar,
      setValue: value => Config.ShowExperienceBar = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Fadeout,
      tooltip: I18n.Gmcm_Modules_Xpbar_Fadeout_Tooltip,
      getValue: () => Config.AllowExperienceBarToFadeOut,
      setValue: value => Config.AllowExperienceBarToFadeOut = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Gain,
      tooltip: I18n.Gmcm_Modules_Xpbar_Gain_Tooltip,
      getValue: () => Config.ShowExperienceGain,
      setValue: value =>
      {
        if (!value)
        {
          floatingTextManager.ClearId(FloatingTextKey);
        }

        Config.ShowExperienceGain = value;
      }
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Xpbar_Levelup,
      tooltip: I18n.Gmcm_Modules_Xpbar_Levelup_Tooltip,
      getValue: () => Config.ShowLevelUpAnimation,
      setValue: value => Config.ShowLevelUpAnimation = value
    );
  }
#endregion
}
