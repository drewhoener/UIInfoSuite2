using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using UIInfoSuite2.Infrastructure.Config.Configurable;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.Infrastructure.Modules.Hud;
using UIInfoSuite2.Infrastructure.Modules.MenuAdditions;
using UIInfoSuite2.Infrastructure.Patches;
using UIInfoSuite2.UIElements.MenuShortcuts.MenuShortcutDisplay;

namespace UIInfoSuite2;

internal class ModEntry : Mod
{
  private static SkipIntro _skipIntro; // Needed so GC won't throw away object with subscriptions

  private readonly Container _container = new();

  public static ModEntry Instance { get; private set; } = null!;

  public static T GetSingleton<T>() where T : class
  {
    return Instance._container.GetInstance<T>();
  }

  public static IEnumerable<BaseModule> GetAllModules()
  {
    return GetContainerCollection<BaseModule>();
  }

  public static IEnumerable<T> GetContainerCollection<T>() where T : class
  {
    return Instance._container.GetAllInstances<T>();
  }

#region Entry
  public override void Entry(IModHelper helper)
  {
    Instance = this;
    I18n.Init(helper.Translation);

    // Add Mod singletons to container
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

    // Set up UI Info Suite Helpers
    _container.RegisterSingleton<GameStateResolverCaches>();
    _container.RegisterSingleton<GameStateHelper>();
    _container.RegisterSingleton<BundleHelper>();
    _container.RegisterSingleton<DropsHelper>();
    _container.RegisterSingleton<SoundHelper>();

    // Set up Managers
    _container.RegisterSingleton<ApiManager>();
    _container.RegisterSingleton<EventsManager>();
    _container.RegisterSingleton<ConfigManager>();
    _container.RegisterSingleton<HudIconStorage>();

    // Set up empty registry sets
    _container.Collection.Register<BaseModule>(Enumerable.Empty<Type>(), Lifestyle.Singleton);
    _container.Collection.Register<HudIconModule>(Enumerable.Empty<Type>(), Lifestyle.Singleton);
    _container.Collection.Register<IPatchable>(Enumerable.Empty<Type>(), Lifestyle.Singleton);
    _container.Collection.Register<IConfigurable>(Enumerable.Empty<Type>(), Lifestyle.Singleton);

    // Register Modules
    RegisterConfigurable<ConfigurableHudIconPositioning>();
    RegisterPatchable<PatchRenderingMenuContentStep>();
    RegisterBaseModuleSingleton<MenuShortcutDisplay>();
    RegisterHudModuleSingleton<DailyLuckModule>();
    RegisterHudModuleSingleton<BirthdayReminderModule>();
    RegisterHudModuleSingleton<WeeklyRecipeModule>();
    RegisterHudModuleSingleton<ToolUpgradeReminderModule>();
    RegisterBaseModuleSingleton<ShopHarvestPriceModule>();
    RegisterBaseModuleSingleton<PartialHeartFillModule>();

    _container.Verify();

    _skipIntro = new SkipIntro(helper.Events);

    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.Display.Rendering += IconHandler.Handler.Reset;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    _container.GetInstance<EventsManager>().OnConfigChange += (_, _) => ReloadModules();
    _container.GetInstance<MenuShortcutDisplay>().Register(helper);

    IconHandler.Handler.IsQuestLogPermanent = helper.ModRegistry.IsLoaded(ModCompat.DeluxeJournal);

    var harmony = _container.GetInstance<Harmony>();
    foreach (IPatchable patchable in GetContainerCollection<IPatchable>())
    {
      patchable.Patch(harmony);
    }
  }
#endregion


  private void ReloadModules()
  {
    if (Game1.gameMode == Game1.titleScreenGameMode)
    {
      return;
    }

    // Recalculate the icon rows if necessary
    GetSingleton<HudIconStorage>().MarkRowsDirty();

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
    _container.GetInstance<HudIconStorage>().UnregisterEvents();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _container.GetInstance<HudIconStorage>().RegisterEvents();

    foreach (BaseModule module in GetAllModules())
    {
      if (module.ShouldEnable())
      {
        module.Enable();
      }
    }
  }

  private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
  {
    _container.GetInstance<SoundHelper>().Initialize(Helper);
    _container.GetInstance<ApiManager>().TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.2.1", true);
  }

#region Debug
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void DebugLog(string message, LogLevel level = LogLevel.Trace)
  {
#if DEBUG
    Instance.Monitor.Log(message, level);
#endif
  }
#endregion

#region Module Setup
  private void RegisterPatchable<T>() where T : class, IPatchable
  {
    _container.Collection.Append<IPatchable, T>();
  }

  private void RegisterConfigurable<T>() where T : class, IConfigurable
  {
    _container.Collection.Append<IConfigurable, T>();
  }

  private void RegisterBaseModuleSingleton<T>(bool registerConfigurable = true, bool registerPatchable = true)
    where T : BaseModule
  {
    _container.RegisterSingleton<T>();
    _container.Collection.Append<BaseModule, T>();

    // Check if T implements IPatchable using interface check
    if (registerPatchable && typeof(IPatchable).IsAssignableFrom(typeof(T)))
    {
      _container.Collection.Append(typeof(IPatchable), typeof(T));
    }

    // Check if T implements IConfigurable using interface check
    if (registerConfigurable && typeof(IConfigurable).IsAssignableFrom(typeof(T)))
    {
      _container.Collection.Append(typeof(IConfigurable), typeof(T));
    }
  }

  private void RegisterHudModuleSingleton<T>() where T : HudIconModule
  {
    RegisterBaseModuleSingleton<T>();
    _container.Collection.Append<HudIconModule, T>();
  }
#endregion
}
