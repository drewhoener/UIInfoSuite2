using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Events.Args;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts;

internal class MenuShortcutModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  EventsManager eventsManager,
  ApiManager apiManager
) : BaseModule(modEvents, logger, configManager)
{
  public const int PaddingAroundElements = 30;
  public const int SpaceAfterMenuBottom = 10;

  private readonly LayoutContainer _container = LayoutContainer.Row("MenuShortcuts").WithSpacing(PaddingAroundElements);

  private readonly List<MenuShortcutElement> _menuShortcuts = new();

  public override bool ShouldEnable()
  {
    return true;
  }

  public override void OnEnable()
  {
    if (apiManager.GetApi(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgmApi))
    {
      bgmApi.OnPageOverlayCreation(OnOverlayCreated);
    }
    else
    {
      Logger.Log("BetterGameMenu not detected, falling back to rendering menu content step.", LogLevel.Warn);
      eventsManager.OnRenderingMenuContentStep += OnRenderingMenu;
    }
  }

  public override void OnDisable()
  {
    if (apiManager.GetApi(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgmApi))
    {
      bgmApi.OffPageOverlayCreation(OnOverlayCreated);
    }
    else
    {
      eventsManager.OnRenderingMenuContentStep -= OnRenderingMenu;
    }
  }

  public void Register(IModHelper helper)
  {
    AddMenuShortcut(helper, new CalendarQuestMenuShortcut(80));
    AddMenuShortcut(helper, new MonsterSlayerShortcut(80));
    AddMenuShortcut(helper, new SpecialOrderMenuShortcut(80));
  }

  public void AddMenuShortcut(IModHelper helper, MenuShortcutElement shortcut)
  {
    _menuShortcuts.Add(shortcut);
    _container.AddChildren(shortcut);
    helper.Events.Input.ButtonPressed += shortcut.OnClick;
  }

  private void OnRenderingMenu(object? sender, RenderingMenuContentStepArgs stepArgs)
  {
    if (stepArgs.Menu is not GameMenu menu || menu.invisible)
    {
      return;
    }

    Draw(stepArgs.SpriteBatch, menu);
  }

  private void OnOverlayCreated(IPageOverlayCreationEvent evt)
  {
    if (!apiManager.GetApi(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgmApi))
    {
      return;
    }

    IBetterGameMenu? menu = bgmApi.AsMenu(evt.Menu);
    if (menu is null || menu.Invisible)
    {
      return;
    }

    evt.AddOverlay(new BetterGameMenuShortcutOverlay(this, evt.Menu));
  }

  private void Draw(SpriteBatch batch, IClickableMenu menu)
  {
    // Sync draw requirements before calling layout
    foreach (MenuShortcutElement shortcut in _menuShortcuts)
    {
      shortcut.IsHidden = !shortcut.ShouldDraw;
    }

    _container.Layout();

    if (_menuShortcuts.TrueForAll(s => s.IsHidden))
    {
      return;
    }

    int xStart = menu.xPositionOnScreen;
    int width = menu.width;
    int yStart = menu.yPositionOnScreen + menu.height - 20 + SpaceAfterMenuBottom;
    int height = _container.Bounds.Size.Height + PaddingAroundElements * 2;

    IClickableMenu.drawTextureBox(batch, xStart, yStart, width, height, Color.White);

    _container.Draw(batch, xStart + PaddingAroundElements, yStart + PaddingAroundElements);

    foreach (MenuShortcutElement shortcut in _menuShortcuts)
    {
      shortcut.DrawHoverText(batch);
    }
  }

  /// <summary>
  ///   Delegate class for when we have BetterGameMenu installed, we need a way to render our content.
  /// </summary>
  private class BetterGameMenuShortcutOverlay(MenuShortcutModule module, IClickableMenu menu) : IDisposable
  {
    public void Dispose() { }

    // ReSharper disable once UnusedMember.Local (Used by BetterGameMenu)
    public void Draw(SpriteBatch batch)
    {
      module.Draw(batch, menu);
    }
  }
}
