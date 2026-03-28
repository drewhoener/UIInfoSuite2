using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Models.Layout;

namespace UIInfoSuite2.Infrastructure.Models;

internal abstract class TooltipExtensionContainer : LayoutContainer
{
  private Item? _item;

  public Item? Item
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

  protected abstract void OnItemChange(Item? item);
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

  public static readonly ContainerPatchPoint[] AllPoints = Enum.GetValues<ContainerPatchPoint>();

  private static readonly Dictionary<ContainerPatchPoint, List<TooltipExtensionContainer>> Containers = new();

  /// <summary>
  ///   Helper field for what item we're hovering over in our inventory while in a ShopMenu.
  ///   ShopMenu#draw only passes along an item to IClickableMenu#drawHoverText if it's for sale,
  ///   not if the player is the one selling.
  ///   Hold this while we're hovering over a valid item in our inventory.
  /// </summary>
  private static Item? _shopMenuHoverItem;

  public static Item? ShopMenuHoverItem
  {
    get => _shopMenuHoverItem;
    set
    {
      Item? oldValue = _shopMenuHoverItem;
      _shopMenuHoverItem = value;
      // ModEntry.Instance.Monitor.Log($"Setting ShopThing to {value?.DisplayName ?? "null"}");
      if (oldValue != value)
      {
        UpdateHoveredItem(value);
      }
    }
  }

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

  public static void UpdateHoveredItem(Item? item)
  {
    Item? updateItem = item;
    if (item == null && ShopMenuHoverItem != null)
    {
      updateItem = ShopMenuHoverItem;
    }

    foreach (TooltipExtensionContainer container in Containers.Values.SelectMany(c => c))
    {
      container.Item = updateItem;
    }
  }

  public static IEnumerable<TooltipExtensionContainer> GetContainers(
    ContainerPatchPoint point,
    bool includeHidden = false
  )
  {
    List<TooltipExtensionContainer> containers = Containers.GetValueOrDefault(point) ?? [];
    return containers.Where(c => includeHidden || !c.IsHidden);
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

  public static int MaxWidth(IEnumerable<ContainerPatchPoint> points)
  {
    return points.Max(p =>
      {
        IEnumerable<TooltipExtensionContainer> containers = GetContainers(p);
        return containers.Select(c => c.Bounds.Width).DefaultIfEmpty(0).Max();
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
      num1 = Math.Max(num1, MaxWidth(AllPoints) + 30);
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
      c.Draw(b, x + 16, drawY + 12);
      drawY += c.Bounds.Height;
    }
  }
}
