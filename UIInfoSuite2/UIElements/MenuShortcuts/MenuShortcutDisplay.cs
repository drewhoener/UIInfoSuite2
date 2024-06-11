using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley.Menus;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

internal class MenuShortcutDisplay
{
  private static readonly Lazy<MenuShortcutDisplay> LazyInstance = new(() => new MenuShortcutDisplay());

  private readonly List<BaseMenuShortcut> _menuShortcuts = new();
  private int _maxElementHeight = 100;
  public int PaddingAroundElements => 30;
  public int SpaceAfterMenuBottom => 10;

  public static MenuShortcutDisplay Instance => LazyInstance.Value;

  public void Register(IModHelper helper)
  {
    Harmony harmony = new(helper.ModContent.ModID);

    MethodInfo? patchingMethod = AccessTools.DeclaredMethod(
      typeof(GameMenu),
      nameof(GameMenu.draw),
      new[] { typeof(SpriteBatch) }
    );
    var transpilerMethod = new HarmonyMethod(
      AccessTools.DeclaredMethod(typeof(MenuShortcutDisplay), nameof(TranspileGameMenuDraw))
    );

    harmony.Patch(patchingMethod, transpiler: transpilerMethod);

    AddMenuShortcut(helper, new CalendarQuestMenuShortcut(80));
    AddMenuShortcut(helper, new MonsterSlayerShortcut(80));
  }

  private static void InstanceDraw(GameMenu menu, SpriteBatch b)
  {
    Instance.Draw(menu, b);
  }

  // TODO: Move harmony patch to its own registry
  private static IEnumerable<CodeInstruction> TranspileGameMenuDraw(
    IEnumerable<CodeInstruction> instructions,
    ILGenerator generator
  )
  {
    CodeMatcher matcher = new(instructions, generator);

    matcher.MatchStartForward(
             new CodeMatch(OpCodes.Ldarg_0),
             new CodeMatch(i => i.opcode == OpCodes.Ldfld),
             new CodeMatch(OpCodes.Ldarg_0),
             new CodeMatch(i => i.opcode == OpCodes.Ldfld)
           )
           .ThrowIfNotMatch("Unable to find insertion point for drawing menu shortcuts");
    matcher.InsertAndAdvance(
      new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(matcher.Instruction),
      new CodeInstruction(OpCodes.Ldarg_1),
      new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MenuShortcutDisplay), nameof(InstanceDraw)))
    );

    return matcher.InstructionEnumeration();
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
