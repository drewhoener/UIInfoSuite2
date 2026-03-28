using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.ExtendedItemInfo;

internal class ExtendedItemInfoModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  BundleHelper bundleHelper
) : BaseModule(modEvents, logger, configManager), IConfigurable
{
  private DescriptionContainer _descriptionContainer = new(bundleHelper);

  public override bool ShouldEnable()
  {
    return Config.ShowExtendedItemInfo;
  }

  public override void OnEnable()
  {
    DescriptionContainer.ContainerCache.Clear();
    bundleHelper.SyncBundleInformation();
    TooltipExtensionRegistry.Register(ContainerPatchPoint.AfterDescription, _descriptionContainer);
    ModEvents.Player.Warped += SyncBundleEventHandler;
    ModEvents.GameLoop.SaveLoaded += SyncBundleEventHandler;
#if DEBUG
    HotReloadService.UpdateApplicationEvent += OnHotReload;
#endif
  }

  public override void OnDisable()
  {
    ModEvents.Player.Warped -= SyncBundleEventHandler;
    ModEvents.GameLoop.SaveLoaded -= SyncBundleEventHandler;
    TooltipExtensionRegistry.Unregister(ContainerPatchPoint.AfterDescription, _descriptionContainer);
#if DEBUG
    HotReloadService.UpdateApplicationEvent -= OnHotReload;
#endif
  }

  private void OnHotReload(Type[]? changedTypes)
  {
    Logger.Log("Reloading hot-reload objects");
    TooltipExtensionRegistry.Register(ContainerPatchPoint.AfterDescription, _descriptionContainer);
    DescriptionContainer.ContainerCache.Clear();
    _descriptionContainer = new DescriptionContainer(bundleHelper);
    TooltipExtensionRegistry.Register(ContainerPatchPoint.AfterDescription, _descriptionContainer);
  }

  private void SyncBundleEventHandler(object? sender, EventArgs _)
  {
    bundleHelper.SyncBundleInformation();
  }

#region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.MenuFeatures;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_ExtendedItemInfo();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Menus_ExtendedItemInfo_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_ExtendedItemInfo_Enable_Tooltip,
      getValue: () => Config.ShowExtendedItemInfo,
      setValue: value => Config.ShowExtendedItemInfo = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Menus_ExtendedItemInfo_Bundles_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_ExtendedItemInfo_Bundles_Enable_Tooltip,
      getValue: () => Config.ShowItemsRequiredForBundles,
      setValue: value => Config.ShowItemsRequiredForBundles = value
    );
  }
#endregion
}
