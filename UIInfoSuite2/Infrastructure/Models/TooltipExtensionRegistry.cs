using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models;

internal abstract class TooltipExtensionContainer : LayoutContainer
{
  private Item _item;

  public Item Item
  {
    get => _item;
    set
    {
      if (_item == value)
      {
        return;
      }

      _item = value;
      OnItemChange(value);
      Layout();
    }
  }

  protected abstract void OnItemChange(Item item);
}

internal class BundleElement : LayoutContainer
{
  private BundleRequiredItem _bundle;

  private readonly TooltipIcon _icon;
  private readonly TooltipText _text;

  public BundleElement(BundleRequiredItem bundle)
  {
    _bundle = bundle;
    _icon = new TooltipIcon(Game1.mouseCursors, new Rectangle(331, 374, 15, 14), 64);
    _text = new TooltipText(bundle.Name);

    AddChildren(_icon, _text);
  }
}

internal class BundleContainer : TooltipExtensionContainer
{
  public BundleContainer()
  {
    AutoHideWhenEmpty = true;
    Direction = LayoutDirection.Column;
  }

  protected override void OnItemChange(Item item)
  {
    throw new NotImplementedException();
  }
}

internal enum ContainerPatchPoint
{
  BeforeTitle,
  AfterTitle,
  AfterCategory,
  BeforeDescription,
  AfterDescription,
  AfterBuffs,
  BeforeFooter,
  AfterFooter
}

internal class TooltipExtensionRegistry
{
  // Patch points that live inside the title sub-box
  private static readonly ContainerPatchPoint[] TitleBoxPoints =
  [
    ContainerPatchPoint.BeforeTitle,
    ContainerPatchPoint.AfterTitle,
    ContainerPatchPoint.AfterCategory
  ];

  private static readonly ContainerPatchPoint[] AllPoints = Enum.GetValues<ContainerPatchPoint>();

  private static readonly Dictionary<ContainerPatchPoint, List<TooltipExtensionContainer>> Containers = new();

  public static void Register(ContainerPatchPoint point, TooltipExtensionContainer container)
  {
    if (!Containers.TryGetValue(point, out List<TooltipExtensionContainer>? list))
    {
      Containers[point] = list = [];
    }

    list.Add(container);
  }

  public static void Unregister(ContainerPatchPoint point, TooltipExtensionContainer container)
  {
    Containers.GetValueOrDefault(point)?.Remove(container);
  }

  public static void UpdateHoveredItem(Item item)
  {
    foreach (TooltipExtensionContainer container in Containers.Values.SelectMany(c => c))
    {
      container.Item = item;
    }
  }

  public static IEnumerable<TooltipExtensionContainer> GetContainers(ContainerPatchPoint point)
  {
    List<TooltipExtensionContainer> containers = Containers.GetValueOrDefault(point) ?? [];
    return containers.Where(c => !c.IsHidden);
  }

  // ── Layout helpers ─────────────────────────────────────────────────────────

  private static int SumHeight(IEnumerable<ContainerPatchPoint> points)
  {
    return points.Sum(p =>
      {
        IEnumerable<TooltipExtensionContainer> containers = GetContainers(p);
        return containers.Sum(c => c.Bounds.Height);
      }
    );
  }

  private static int MaxWidth(IEnumerable<ContainerPatchPoint> points)
  {
    return points.Max(p =>
      {
        IEnumerable<TooltipExtensionContainer> containers = GetContainers(p);
        return containers.Select(c => c.Bounds.Height).DefaultIfEmpty(0).Max();
      }
    );
  }

  // ── Transpiler-invoked ─────────────────────────────────────────────────────

  /// Called just before position calculation.
  /// Respects overrides: if a dimension is externally fixed we leave it alone.
  public static void AdjustLayout(ref int startingHeight, ref int num1, int boxWidthOverride, int boxHeightOverride)
  {
    if (boxHeightOverride == -1)
    {
      startingHeight += SumHeight(AllPoints);
    }

    if (boxWidthOverride == -1)
    {
      num1 = Math.Max(num1, MaxWidth(AllPoints));
    }
  }

  /// Adjusts the title sub-box height, used for BOTH:
  /// • the sub-box drawTextureBox height argument
  /// • the divider Rectangle y-offset  (y1 + titleBoxHeight)
  public static int AdjustTitleBoxHeight(int h)
  {
    return h + SumHeight(TitleBoxPoints);
  }

  /// Draw-time entry point called at each patch point.
  public static void Draw(
    SpriteBatch b,
    int x,
    ref int drawY,
    int innerWidth,
    float alpha,
    Item hoveredItem,
    ContainerPatchPoint point
  )
  {
    foreach (TooltipExtensionContainer c in GetContainers(point))
    {
      // c.Draw(b, x, ref drawY, innerWidth, alpha, hoveredItem);
      c.Draw(b, x, drawY);
    }
  }
}
