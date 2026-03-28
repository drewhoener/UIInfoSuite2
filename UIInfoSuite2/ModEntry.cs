// #define LAYOUT_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SimpleInjector;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.AdditionalFeatures;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Config.Configurable;
using UIInfoSuite2.Infrastructure.DebugMenu;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Managers;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.Infrastructure.Modules.Hud;
using UIInfoSuite2.Infrastructure.Modules.MenuAdditions;
using UIInfoSuite2.Infrastructure.Modules.MenuAdditions.ExtendedItemInfo;
using UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts;
using UIInfoSuite2.Infrastructure.Modules.Overlay;
using UIInfoSuite2.Infrastructure.Patches;
using UIInfoSuite2.Infrastructure.Patches.ExtensibleItemTooltips;

#if DEBUG
[assembly: MetadataUpdateHandler(typeof(HotReloadService))]
#endif

namespace UIInfoSuite2;

internal class ModEntry : Mod
{
  private static readonly Type[] BucketTypes =
  [
    typeof(BaseModule), typeof(HudIconModule), typeof(IPatchable), typeof(IConfigurable)
  ];

  private static SkipIntro _skipIntro; // Needed so GC won't throw away object with subscriptions

  private readonly Container _container = new();

  public static ModEntry Instance { get; private set; } = null!;

  public static ModConfig Config => GetSingleton<ConfigManager>().Config;

  public static T GetSingleton<T>() where T : class
  {
    return Instance._container.GetInstance<T>();
  }

  public static Lazy<T> LazyGetSingleton<T>() where T : class
  {
    return new Lazy<T>(() => Instance._container.GetInstance<T>());
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
#if DEBUG
    Harmony.DEBUG = true;
#endif

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
    _container.RegisterSingleton<FloatingTextManager>();

    // Set up empty registry sets
    foreach (Type bucketType in BucketTypes)
    {
      _container.Collection.Register(bucketType, Enumerable.Empty<Type>(), Lifestyle.Singleton);
    }

    // Register Modules
    Register<ConfigurableHudIconPositioning>();
    Register<ConfigurableDebugOptions>();
    Register<ExtensibleItemTooltips>();
    Register<PatchMasteryXpGainEvent>();
    Register<PatchBushShakeItemEvent>();
    Register<PatchRenderingMenuContentStep>();
    Register<MenuShortcutModule>();
    // RegisterBaseModuleSingleton<ShowCropAndBarrelTime>();
    Register<ArtifactTrackerModule>();
    Register<BirthdayReminderModule>();
    Register<ConstructionTrackerModule>();
    Register<DailyLuckModule>();
    Register<DailyWeatherModule>();
    Register<SeasonalForageDisplayModule>();
    Register<WeeklyRecipeModule>();
    Register<ToolUpgradeReminderModule>();
    Register<MerchantReminderModule>();
    Register<ExtendedItemInfoModule>();
    Register<GiftLockModule>();
    Register<PartialHeartFillModule>();
    Register<ShopHarvestPriceModule>();
    Register<SocialPageFilterModule>();
    Register<AnimalInteractModule>();
    Register<ObjectEffectRangeModule>();
    Register<ObjectInfoModule>();
    Register<ExperienceModule>();

    _container.Verify();

    _skipIntro = new SkipIntro(helper.Events);

    helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
    helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
    _container.GetInstance<EventsManager>().OnConfigChange += (_, _) => ReloadModules();
    _container.GetInstance<MenuShortcutModule>().Register(helper);

    var harmony = _container.GetInstance<Harmony>();
    foreach (IPatchable patchable in GetContainerCollection<IPatchable>())
    {
      patchable.Patch(harmony);
    }

    helper.ConsoleCommands.Add(
      "uiis2_layout_test",
      "Opens the layout system test menu.",
      (_, _) =>
      {
        if (!Context.IsWorldReady)
        {
          Monitor.Log("Load a save first.", LogLevel.Warn);
          return;
        }

        Game1.activeClickableMenu = new LayoutTestMenu();
      }
    );
  }
#endregion


  private void ReloadModules()
  {
    if (Game1.gameMode == Game1.titleScreenGameMode)
    {
      return;
    }

    // Recalculate the icon rows if necessary
    _container.GetInstance<HudIconStorage>().MarkRowsDirty();

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
    _container.GetInstance<FloatingTextManager>().UnregisterEvents();
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    // Only load once for split screen.
    if (Context.ScreenId != 0)
    {
      return;
    }

    _container.GetInstance<HudIconStorage>().RegisterEvents();
    _container.GetInstance<FloatingTextManager>().RegisterEvents();

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
    _container.GetInstance<ApiManager>().TryRegisterApi<ICustomBushApi>(Helper, ModCompat.CustomBush, "1.5.0", true);
    _container.GetInstance<ApiManager>().TryRegisterApi<IBetterGameMenuApi>(Helper, ModCompat.BetterGameMenu, "1.0.1");
    _container.GetInstance<ApiManager>().TryRegisterApi<ICloudySkiesApi>(Helper, ModCompat.CloudySkies, "1.9.0");
  }

#region Module Setup
  private void Register<T>() where T : class
  {
    _container.RegisterSingleton<T>();

    foreach (Type bucketType in BucketTypes)
    {
      if (bucketType.IsAssignableFrom(typeof(T)))
      {
        _container.Collection.Append(bucketType, typeof(T));
      }
    }
  }
#endregion

#region Debug
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void DebugLog(string message, LogLevel level = LogLevel.Trace)
  {
#if DEBUG
    Instance.Monitor.Log(message, level);
#endif
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void LayoutDebug(string message, LogLevel level = LogLevel.Trace)
  {
#if LAYOUT_DEBUG
    Instance.Monitor.Log(message, level);
#endif
  }
#endregion
}
