﻿using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.AdditionalFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Containers;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Helpers.FishHelper;
using UIInfoSuite2.Options;
using UIInfoSuite2.Patches;

namespace UIInfoSuite2;

public class ModEntry : Mod
{
  private static SkipIntro _skipIntro; // Needed so GC won't throw away object with subscriptions
  private static ModConfig _modConfig;
  private static Harmony Harmony = null!;

  private static EventHandler<ButtonsChangedEventArgs> _calendarAndQuestKeyBindingsHandler;

  private ModOptions _modOptions;
  private ModOptionsPageHandler _modOptionsPageHandler;


  public static IMonitor MonitorObject { get; private set; }
  public static DynamicGameAssetsEntry DGA { get; private set; }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void LogExDebug(string message)
  {
#if EX_LOG_1
    MonitorObject.Log(message);
#endif
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void LogExDebug_2(string message)
  {
#if EX_LOG_2
    MonitorObject.Log(message);
#endif
  }

#region Entry
  public override void Entry(IModHelper helper)
  {
    MonitorObject = Monitor;
    DGA = new DynamicGameAssetsEntry(Helper, Monitor);
    I18n.Init(helper.Translation);

    _skipIntro = new SkipIntro(helper.Events);
    _modConfig = Helper.ReadConfig<ModConfig>();

    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    helper.Events.GameLoop.Saved += OnSaved;
    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.Rendering += IconHandler.Handler.Reset;

    EventInvoker.RegisterEvents(helper);

    helper.Events.Display.RenderingHud += (sender, e) =>
    {
      Vector2 fontDimensions = Game1.smallFont.MeasureString("OOO");
      Vector2 startingPos = new(15, 15);
      var yOffset = 5;

      IClickableMenu.drawTextureBox(
        e.SpriteBatch,
        Game1.menuTexture,
        new Rectangle(0, 256, 60, 60),
        0,
        0,
        1200,
        300,
        Color.White * 1.0f,
        drawShadow: false
      );

      double cumulativeChances = FishHelper.SimulateActualFishChance();
      FishingInformationCache fishingCache = FishHelper.GetOrCreateFishingCache(Game1.player.currentLocation);

      foreach (FishSpawnInfo fish in FishHelper.GetCatchableFishDisplayOrder(Game1.player.currentLocation))
      {
        var renderStr =
          $"{fish.DisplayName} - {Math.Round(fish.ActualHookChance * 100, fishingCache.FishingChancesConverged() ? 1 : 0)}%{(fish.IsOnlyNonFishItems ? " - Only Item" : "")}";
        Utility.drawTextWithShadow(e.SpriteBatch, renderStr, Game1.smallFont, startingPos, Color.Black);
        startingPos.Y += fontDimensions.Y + yOffset;
      }

      Utility.drawTextWithShadow(
        e.SpriteBatch,
        $"Cumulative: {cumulativeChances * 100:F1}",
        Game1.smallFont,
        startingPos,
        Color.Black
      );
    };

    helper.Events.Player.Warped += (sender, args) =>
    {
      FishHelper.PopulateWaterCacheForLocation(args.NewLocation);
      FishHelper.SimulateFishingAtGameLocation(args.NewLocation, args.Player);
    };
  }
#endregion

#region Generic mod config menu
  private void InitModConfigMenu()
  {
    // get Generic Mod Config Menu's API (if it's installed)
    ISemanticVersion? modVersion = Helper.ModRegistry.Get("spacechase0.GenericModConfigMenu")?.Manifest?.Version;
    var minModVersion = "1.6.0";
    if (modVersion?.IsOlderThan(minModVersion) == true)
    {
      Monitor.Log(
        $"Detected Generic Mod Config Menu {modVersion} but expected {minModVersion} or newer. Disabling integration with that mod.",
        LogLevel.Warn
      );
      return;
    }

    var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
    if (configMenu is null)
    {
      return;
    }

    // register mod
    configMenu.Register(ModManifest, () => _modConfig = new ModConfig(), () => Helper.WriteConfig(_modConfig));

    // add some config options
    configMenu.AddBoolOption(
      ModManifest,
      name: () => "Show options in in-game menu",
      tooltip: () => "Enables an extra tab in the in-game menu where you can configure every options for this mod.",
      getValue: () => _modConfig.ShowOptionsTabInMenu,
      setValue: value => _modConfig.ShowOptionsTabInMenu = value
    );
    configMenu.AddTextOption(
      ModManifest,
      name: () => "Apply default settings from this save",
      tooltip: () => "New characters will inherit the settings for the mod from this save file.",
      getValue: () => _modConfig.ApplyDefaultSettingsFromThisSave,
      setValue: value => _modConfig.ApplyDefaultSettingsFromThisSave = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => "Open calendar keybind",
      tooltip: () => "Opens the calendar tab.",
      getValue: () => _modConfig.OpenCalendarKeybind,
      setValue: value => _modConfig.OpenCalendarKeybind = value
    );
    configMenu.AddKeybindList(
      ModManifest,
      name: () => "Open quest board keybind",
      tooltip: () => "Opens the quest board.",
      getValue: () => _modConfig.OpenQuestBoardKeybind,
      setValue: value => _modConfig.OpenQuestBoardKeybind = value
    );
  }
#endregion


#region Event subscriptions
  private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
  {
    Harmony = new Harmony(ModManifest.UniqueID);
    InitModConfigMenu();

    PatchManager.Apply(Harmony);
  }

  private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
  {
    // Unload if the main player quits.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptionsPageHandler?.Dispose();
    _modOptionsPageHandler = null;
  }

  private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _modOptions = Helper.Data.ReadJsonFile<ModOptions>($"data/{Constants.SaveFolderName}.json") ??
                  Helper.Data.ReadJsonFile<ModOptions>($"data/{_modConfig.ApplyDefaultSettingsFromThisSave}.json") ??
                  new ModOptions();

    _modOptionsPageHandler = new ModOptionsPageHandler(Helper, _modOptions, _modConfig.ShowOptionsTabInMenu);
  }

  private void OnSaved(object sender, EventArgs e)
  {
    // Only save for the main player.
    if (Context.ScreenId != 0)
    {
      return;
    }

    Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", _modOptions);
  }

  public static void RegisterCalendarAndQuestKeyBindings(IModHelper helper, bool subscribe)
  {
    if (_calendarAndQuestKeyBindingsHandler == null)
    {
      _calendarAndQuestKeyBindingsHandler = (sender, e) => HandleCalendarAndQuestKeyBindings(helper);
    }

    helper.Events.Input.ButtonsChanged -= _calendarAndQuestKeyBindingsHandler;

    if (subscribe)
    {
      helper.Events.Input.ButtonsChanged += _calendarAndQuestKeyBindingsHandler;
    }
  }

  private static void HandleCalendarAndQuestKeyBindings(IModHelper helper)
  {
    if (_modConfig != null)
    {
      if (Context.IsPlayerFree && _modConfig.OpenCalendarKeybind.JustPressed())
      {
        helper.Input.SuppressActiveKeybinds(_modConfig.OpenCalendarKeybind);
        Game1.activeClickableMenu = new Billboard();
      }
      else if (Context.IsPlayerFree && _modConfig.OpenQuestBoardKeybind.JustPressed())
      {
        helper.Input.SuppressActiveKeybinds(_modConfig.OpenQuestBoardKeybind);
        Game1.RefreshQuestOfTheDay();
        Game1.activeClickableMenu = new Billboard(true);
      }
    }
  }
#endregion
}
