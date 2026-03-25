using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
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
  EventsManager eventsManager
) : BaseModule(modEvents, logger, configManager)
{
  public const int PaddingAroundElements = 30;
  public const int SpaceAfterMenuBottom = 10;

  private readonly List<MenuShortcutElement> _menuShortcuts = new();
  private readonly LayoutContainer _container = LayoutContainer.Row("MenuShortcuts")
    .WithSpacing(PaddingAroundElements);

  public override bool ShouldEnable()
  {
    return true;
  }

  public override void OnEnable()
  {
    eventsManager.OnRenderingMenuContentStep += Draw;
  }

  public override void OnDisable()
  {
    eventsManager.OnRenderingMenuContentStep -= Draw;
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

  public void Draw(object? sender, RenderingMenuContentStepArgs stepArgs)
  {
    if (stepArgs.Menu is not GameMenu menu || menu.invisible)
    {
      return;
    }

    // Sync per-frame game-state conditions into layout visibility before measuring
    foreach (MenuShortcutElement shortcut in _menuShortcuts)
    {
      shortcut.IsHidden = !shortcut.ShouldDraw;
    }

    _container.Layout();

    if (_menuShortcuts.TrueForAll(s => s.IsHidden))
    {
      return;
    }

    SpriteBatch batch = stepArgs.SpriteBatch;
    int xStart = menu.xPositionOnScreen;
    int width = menu.pages[menu.currentTab].width;
    int yStart = menu.yPositionOnScreen + menu.pages[menu.currentTab].height - 20 + SpaceAfterMenuBottom;
    int height = _container.Bounds.Size.Height + PaddingAroundElements * 2;

    IClickableMenu.drawTextureBox(batch, xStart, yStart, width, height, Color.White);

    _container.Draw(batch, xStart + PaddingAroundElements, yStart + PaddingAroundElements);

    foreach (MenuShortcutElement shortcut in _menuShortcuts)
    {
      shortcut.DrawHoverText(batch);
    }
  }
}
