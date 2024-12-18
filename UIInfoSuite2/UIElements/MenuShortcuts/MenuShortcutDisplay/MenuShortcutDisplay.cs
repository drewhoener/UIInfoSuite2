﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Modules;

namespace UIInfoSuite2.UIElements.MenuShortcuts.MenuShortcutDisplay;

internal partial class MenuShortcutDisplay : BaseModule, IPatchable
{
  private readonly List<BaseMenuShortcut> _menuShortcuts = new();
  private int _maxElementHeight = 100;

  public MenuShortcutDisplay(IModEvents modEvents, IMonitor logger, ConfigManager configManager) : base(
    modEvents,
    logger,
    configManager
  ) { }

  public int PaddingAroundElements => 30;
  public int SpaceAfterMenuBottom => 10;

  public override bool ShouldEnable()
  {
    return true;
  }

  public override void OnEnable() { }

  public override void OnDisable() { }

  public void Register(IModHelper helper)
  {
    AddMenuShortcut(helper, new CalendarQuestMenuShortcut(80));
    AddMenuShortcut(helper, new MonsterSlayerShortcut(80));
  }

  private static void InstanceDraw(GameMenu menu, SpriteBatch b)
  {
    ModEntry.GetSingleton<MenuShortcutDisplay>().Draw(menu, b);
  }

  public void AddMenuShortcut(IModHelper helper, BaseMenuShortcut shortcut)
  {
    if (shortcut.RenderedHeight > _maxElementHeight)
    {
      _maxElementHeight = shortcut.RenderedHeight;
    }

    _menuShortcuts.Add(shortcut);
    helper.Events.Input.ButtonPressed += shortcut.OnClick;
  }

  public void Draw(GameMenu menu, SpriteBatch batch)
  {
    BaseMenuShortcut[] drawableElements = _menuShortcuts.Where(e => e.ShouldDraw).ToArray();

    if (menu.invisible || !drawableElements.Any())
    {
      return;
    }

    _maxElementHeight = _menuShortcuts.Select(e => e.RenderedHeight).Max();

    int xStart = menu.xPositionOnScreen;
    int width = menu.pages[menu.currentTab].width;
    int yStart = menu.yPositionOnScreen + menu.pages[menu.currentTab].height - 20 + SpaceAfterMenuBottom;
    int height = _maxElementHeight + PaddingAroundElements * 2;

    IClickableMenu.drawTextureBox(batch, xStart, yStart, width, height, Color.White);

    int halfPadding = PaddingAroundElements / 2;
    int elementXStart = halfPadding;

    foreach (BaseMenuShortcut menuShortcut in drawableElements)
    {
      elementXStart += halfPadding;
      menuShortcut.Draw(batch, xStart + elementXStart, yStart + PaddingAroundElements);
      elementXStart += menuShortcut.RenderedWidth + halfPadding;
    }

    foreach (BaseMenuShortcut menuShortcut in drawableElements)
    {
      menuShortcut.DrawHoverText(batch);
    }
  }
}
