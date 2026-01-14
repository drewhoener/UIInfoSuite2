using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Events.Args;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts.ShortcutModules;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.MenuShortcuts;

internal class MenuShortcutModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  EventsManager eventsManager
) : BaseModule(modEvents, logger, configManager)
{
  private readonly List<BaseMenuShortcut> _menuShortcuts = new();
  private int _maxElementHeight = 100;

  public int PaddingAroundElements => 30;
  public int SpaceAfterMenuBottom => 10;

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

  public void AddMenuShortcut(IModHelper helper, BaseMenuShortcut shortcut)
  {
    if (shortcut.RenderedHeight > _maxElementHeight)
    {
      _maxElementHeight = shortcut.RenderedHeight;
    }

    _menuShortcuts.Add(shortcut);
    helper.Events.Input.ButtonPressed += shortcut.OnClick;
  }

  public void Draw(object? sender, RenderingMenuContentStepArgs stepArgs)
  {
    SpriteBatch batch = stepArgs.SpriteBatch;

    BaseMenuShortcut[] drawableElements = _menuShortcuts.Where(e => e.ShouldDraw).ToArray();
    if (!drawableElements.Any() || Tools.GetCurrentMenuPage() is not InventoryPage page)
    {
      return;
    }

    _maxElementHeight = drawableElements.Max(e => e.RenderedHeight);
    int xStart = page.xPositionOnScreen;
    int width = page.width;
    int yStart = page.yPositionOnScreen + page.height - 20 + SpaceAfterMenuBottom;
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
