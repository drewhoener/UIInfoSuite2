using System.Collections.Generic;
using HarmonyLib;
using SimpleInjector;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.AdditionalFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;
using UIInfoSuite2.Infrastructure.Modules;
using UIInfoSuite2.UIElements;
using UIInfoSuite2.UIElements.MenuShortcuts.MenuShortcutDisplay;

namespace UIInfoSuite2;

internal class ModEntry : Mod
{
  private static SkipIntro _skipIntro; // Needed so GC won't throw away object with subscriptions

  // private static EventHandler<ButtonsChangedEventArgs> _calendarAndQuestKeyBindingsHandler;
  private readonly Container _container = new();

  public static ModEntry Instance { get; private set; } = null!;

  public static T GetSingleton<T>() where T : class
  {
    return Instance._container.GetInstance<T>();
  }

#region Entry
  public override void Entry(IModHelper helper)
  {
    Instance = this;
    I18n.Init(helper.Translation);

    _container.RegisterInstance(Helper);
    _container.RegisterInstance(ModManifest);
    _container.RegisterInstance(Monitor);
    _container.RegisterInstance(Helper.ConsoleCommands);
    _container.RegisterInstance(Helper.Data);
    _container.RegisterInstance(Helper.Events);
    _container.RegisterInstance(Helper.GameContent);
    _container.RegisterInstance(Helper.Input);
    _container.RegisterInstance(Helper.ModContent);
    _container.RegisterInstance(Helper.ModRegistry);
    _container.RegisterInstance(Helper.Reflection);
    _container.RegisterInstance(Helper.Translation);
    _container.RegisterInstance(new Harmony(Helper.ModContent.ModID));

    _container.RegisterSingleton<GameStateResolverCaches>();
    _container.RegisterSingleton<GameStateHelper>();
    _container.RegisterSingleton<BundleHelper>();
    _container.RegisterSingleton<DropsHelper>();
    _container.RegisterSingleton<SoundHelper>();

    _container.RegisterSingleton<ApiManager>();
    _container.RegisterSingleton<EventsManager>();
    _container.RegisterSingleton<ConfigManager>();

    _container.Collection.Register<BaseModule>(
      new[] { typeof(LuckOfDay), typeof(MenuShortcutDisplay) },
      Lifestyle.Singleton
    );

    _container.Register<MenuShortcutDisplay>(Lifestyle.Singleton);

    _container.Verify();

    _skipIntro = new SkipIntro(helper.Events);

    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.Rendering += IconHandler.Handler.Reset;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    _container.GetInstance<EventsManager>().OnConfigChange += (_, _) => OnConfigSave();
    _container.GetInstance<MenuShortcutDisplay>().Register(helper);

    IconHandler.Handler.IsQuestLogPermanent = helper.ModRegistry.IsLoaded(ModCompat.DeluxeJournal);
  }
#endregion

  public IEnumerable<BaseModule> GetAllModules()
  {
    return _container.GetAllInstances<BaseModule>();
  }

  private void OnConfigSave()
  {
    if (Game1.gameMode == Game1.titleScreenGameMode)
    {
      return;
    }

    foreach (BaseModule module in GetAllModules())
    {
      if (!module.Enabled && module.ShouldEnable())
      {
        module.Enable();
      }

      if (module.Enabled && !module.ShouldEnable())
      {
        module.Disable();
      }
    }
  }

  private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs eventArgs)
  {
    // Unload if the main player quits.
    if (Context.ScreenId != 0)
    {
      return;
    }

    foreach (BaseModule module in GetAllModules())
    {
      module.Disable();
    }

    _container.GetInstance<GameStateResolverCaches>().Clear();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    foreach (BaseModule module in GetAllModules())
    {
      if (module.ShouldEnable())
      {
        module.Enable();
      }
    }
  }

#region Generic mod config menu
  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    _container.GetInstance<SoundHelper>().Initialize(Helper);
    _container.GetInstance<ApiManager>().TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.2.1", true);
  }
#endregion

#region Event subscriptions
  // public static void RegisterCalendarAndQuestKeyBindings(IModHelper helper, bool subscribe)
  // {
  //   if (_calendarAndQuestKeyBindingsHandler == null)
  //   {
  //     _calendarAndQuestKeyBindingsHandler = (sender, e) => HandleCalendarAndQuestKeyBindings(helper);
  //   }
  //
  //   helper.Events.Input.ButtonsChanged -= _calendarAndQuestKeyBindingsHandler;
  //
  //   if (subscribe)
  //   {
  //     helper.Events.Input.ButtonsChanged += _calendarAndQuestKeyBindingsHandler;
  //   }
  // }

  // private static void HandleCalendarAndQuestKeyBindings(IModHelper helper)
  // {
  //   if (_modConfig != null)
  //   {
  //     if (Context.IsPlayerFree && _modConfig.OpenCalendarKeybind.JustPressed())
  //     {
  //       helper.Input.SuppressActiveKeybinds(_modConfig.OpenCalendarKeybind);
  //       Game1.activeClickableMenu = new Billboard();
  //     }
  //     else if (Context.IsPlayerFree && _modConfig.OpenQuestBoardKeybind.JustPressed())
  //     {
  //       helper.Input.SuppressActiveKeybinds(_modConfig.OpenQuestBoardKeybind);
  //       Game1.RefreshQuestOfTheDay();
  //       Game1.activeClickableMenu = new Billboard(true);
  //     }
  //   }
  // }
#endregion
}
