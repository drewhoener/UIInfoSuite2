using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.ExtendedItemInfo;

internal class BundleIconElement : LayoutElement
{
  private readonly ClickableIcon _icon;

  public BundleIconElement(Color color)
  {
    _icon = new ClickableIcon(Game1.mouseCursors, new Rectangle(331, 374, 15, 14), Game1.tileSize);
    _icon.AutoDrawDelegate = batch => { _icon.Draw(batch, color, 1f); };
  }

  protected override Dimensions MeasureContent()
  {
    return _icon.Dimensions.Bounds;
  }

  protected override void DrawSelf(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    _icon.MoveTo(positionX, positionY);
    _icon.AutoDrawDelegate.Invoke(spriteBatch);
  }
}

internal class BundleDisplayContainer : LayoutContainer
{
  public BundleDisplayContainer()
  {
    Direction = LayoutDirection.Row;
  }
}

internal class DescriptionContainer : LayoutContainer
{
  private readonly BundleHelper _bundleHelper;
  private readonly Dictionary<string, TooltipText> _bundles = new();

  public DescriptionContainer(BundleHelper bundleHelper)
  {
    Direction = LayoutDirection.Column;
    _bundleHelper = bundleHelper;
  }

  public void AddBundle(BundleRequiredItem bundleDisplayData)
  {
    if (_bundles.ContainsKey(bundleDisplayData.Name))
    {
      return;
    }

    // var bundleColor = _bundleHelper.GetRealColorFromIndex(bundleDisplayData.Id)?.Desaturate(0.35f);
    var newElement = new TooltipText(bundleDisplayData.Name);
    _bundles.Add(bundleDisplayData.Name, newElement);
    AddChildren(newElement);
  }

  public void ClearBundles()
  {
    if (_bundles.Count <= 0)
    {
      return;
    }

    foreach ((string? key, TooltipText? value) in _bundles)
    {
      RemoveChild(value);
    }

    _bundles.Clear();
  }
}

internal partial class ExtendedItemInfoModule : BaseModule, IConfigurable, IPatchable
{
  private readonly BundleHelper _bundleHelper;
  protected Profiler _containerProfiler;
  protected DescriptionContainer _descriptionContainer;
  protected LayoutContainer _footerContainer = new();

  public ExtendedItemInfoModule(
    IModEvents modEvents,
    IMonitor logger,
    ConfigManager configManager,
    BundleHelper bundleHelper
  ) : base(modEvents, logger, configManager)
  {
    _bundleHelper = bundleHelper;
    _descriptionContainer = new DescriptionContainer(bundleHelper);
    _containerProfiler = new Profiler("OnRenderingActiveMenu", 400, 100, str => ModEntry.DebugLog(str, LogLevel.Debug));
  }

  public override bool ShouldEnable()
  {
    return Config.ShowExtendedItemInfo;
  }

  public override void OnEnable()
  {
    ModEvents.Display.RenderingActiveMenu += OnRenderingActiveMenu;
    Stopwatch testWatch = new();
    testWatch.Start();
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingActiveMenu -= OnRenderingActiveMenu;
  }

  /// <summary>
  ///   Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered
  ///   to the screen.
  /// </summary>
  /// <param name="sender">The event sender.</param>
  /// <param name="e">The event arguments.</param>
  [EventPriority(EventPriority.Low)]
  private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
  {
    _containerProfiler.Start();
    _descriptionContainer.ClearBundles();
    if (Game1.activeClickableMenu == null)
    {
      return;
    }

    Item? hoveredItem = Tools.GetHoveredItem();
    if (hoveredItem == null)
    {
      return;
    }

    BundleRequiredItem? bundleDisplayData = _bundleHelper.GetBundleItemIfNotDonated(hoveredItem);
    if (bundleDisplayData == null)
    {
      return;
    }

    _descriptionContainer.AddBundle(bundleDisplayData);

    _descriptionContainer.Draw(e.SpriteBatch, 0, 0);
    _containerProfiler.Stop();

    Logger.Log("Rendering Active Menu");
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
