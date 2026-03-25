using System;
using System.Collections.Generic;
using System.Linq;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Layout.Strategy;

/// <summary>
///   A flex-box inspired layout strategy. Supports all four <see cref="FlexDirection" /> values,
///   all six <see cref="Enums.JustifyContent" /> modes, <see cref="Enums.AlignItems" /> per-container
///   and <see cref="LayoutElement.AlignSelf" /> per-child, flex grow/shrink space distribution,
///   per-child <see cref="LayoutElement.Order" /> sorting, and absolute positioning.
/// </summary>
internal class FlexLayoutStrategy : LayoutStrategy
{
  // ── Container-level properties ──────────────────────────────────────────

  /// <summary>Main axis direction. Supports Row, Column, and their reverse variants.</summary>
  public FlexDirection Direction { get; set; } = FlexDirection.Row;

  /// <summary>
  ///   How children are distributed along the main axis. Ignored when
  ///   <see cref="GridAlignment" /> is set — use <see cref="GridAlignment" /> for Start/Center/End
  ///   combinations, or set this directly for SpaceBetween/SpaceAround/SpaceEvenly.
  /// </summary>
  public JustifyContent JustifyContent { get; set; } = JustifyContent.Start;

  /// <summary>
  ///   Default cross-axis alignment applied to all children. Overridden per-child by
  ///   <see cref="LayoutElement.AlignSelf" />.
  /// </summary>
  public AlignItems AlignItems { get; set; } = AlignItems.Start;

  /// <summary>Minimum gap in pixels between adjacent flow children.</summary>
  public int Gap { get; set; } = 2;

  /// <summary>
  ///   When set, encodes both <see cref="JustifyContent" /> and <see cref="AlignItems" /> as a
  ///   9-grid value. Decoded at arrange-time so it remains correct regardless of the order in which
  ///   <see cref="Direction" /> and this property are set. Set to <c>null</c> to use
  ///   <see cref="JustifyContent" /> and <see cref="AlignItems" /> directly.
  /// </summary>
  public Alignment? GridAlignment { get; set; }

  // ── Private helpers ──────────────────────────────────────────────────────

  private bool IsRow => Direction is FlexDirection.Row or FlexDirection.RowReverse;
  private bool IsReverse => Direction is FlexDirection.RowReverse or FlexDirection.ColumnReverse;

  // ── LayoutStrategy implementation ────────────────────────────────────────

  /// <inheritdoc />
  public override Dimensions MeasureContent(IReadOnlyList<LayoutElement> allChildren)
  {
    bool isRow = IsRow;
    var flowMain = 0;
    var flowCross = 0;
    var visibleFlowCount = 0;
    var absW = 0;
    var absH = 0;

    foreach (LayoutElement child in allChildren)
    {
      child.Layout();
      Dimensions dims = child.Bounds.Size;
      if (dims == Dimensions.Empty) continue;

      if (child.IsAbsolute)
      {
        Insets pos = child.Bounds.Position;
        absW = Math.Max(absW, pos.Left.OrZero() + dims.Width + pos.Right.OrZero());
        absH = Math.Max(absH, pos.Top.OrZero() + dims.Height + pos.Bottom.OrZero());
        continue;
      }

      visibleFlowCount++;
      flowMain += isRow ? dims.Width : dims.Height;
      flowCross = Math.Max(flowCross, isRow ? dims.Height : dims.Width);
    }

    if (visibleFlowCount > 1)
    {
      flowMain += Gap * (visibleFlowCount - 1);
    }

    Dimensions flow = isRow
      ? new Dimensions(flowMain, flowCross)
      : new Dimensions(flowCross, flowMain);

    return new Dimensions(
      Math.Max(flow.Width, absW),
      Math.Max(flow.Height, absH)
    );
  }

  /// <inheritdoc />
  public override void ArrangeChildren(
    Dimensions contentSize,
    IReadOnlyList<LayoutElement> allChildren,
    List<LayoutElement> visibleChildren
  )
  {
    visibleChildren.Clear();

    bool isRow = IsRow;
    int containerMain = isRow ? contentSize.Width : contentSize.Height;
    int containerCross = isRow ? contentSize.Height : contentSize.Width;

    // Separate, sort, and optionally reverse flow children
    List<LayoutElement> flow = allChildren
      .Where(c => !c.IsAbsolute && !c.IsHidden)
      .OrderBy(c => c.Order)
      .ToList();

    if (IsReverse)
    {
      flow.Reverse();
    }

    List<LayoutElement> absolute = allChildren
      .Where(c => c.IsAbsolute && !c.IsHidden)
      .ToList();

    // ── Flex grow / shrink ──────────────────────────────────────────────────
    if (flow.Count > 0)
    {
      int naturalMain = flow.Sum(c => isRow ? c.Bounds.Width : c.Bounds.Height);
      int gapTotal = flow.Count > 1 ? Gap * (flow.Count - 1) : 0;
      int freeForFlex = containerMain - naturalMain - gapTotal;
      DistributeFlexSpace(flow, freeForFlex, isRow);
    }

    // ── Stretch cross-axis size before placement ────────────────────────────
    foreach (LayoutElement child in flow)
    {
      AlignItems effective = child.AlignSelf ?? AlignItems;
      if (effective != AlignItems.Stretch) continue;
      if (isRow) child.Bounds.Height = containerCross;
      else child.Bounds.Width = containerCross;
    }

    // ── Resolve justify-content ─────────────────────────────────────────────
    (JustifyContent justify, AlignItems crossAlign) = ResolveAlignment(isRow);

    int childrenMain = flow.Sum(c => isRow ? c.Bounds.Width : c.Bounds.Height);
    int gaps = flow.Count > 1 ? Gap * (flow.Count - 1) : 0;
    int freeSpace = Math.Max(0, containerMain - childrenMain - gaps);

    int mainOffset;
    int extraGap = 0;

    switch (justify)
    {
      case JustifyContent.End:
        mainOffset = freeSpace;
        break;
      case JustifyContent.Center:
        mainOffset = freeSpace / 2;
        break;
      case JustifyContent.SpaceBetween:
        mainOffset = 0;
        extraGap = flow.Count > 1 ? freeSpace / (flow.Count - 1) : 0;
        break;
      case JustifyContent.SpaceAround:
      {
        int around = flow.Count > 0 ? freeSpace / flow.Count : 0;
        mainOffset = around / 2;
        extraGap = around;
        break;
      }
      case JustifyContent.SpaceEvenly:
      {
        int evenly = flow.Count > 0 ? freeSpace / (flow.Count + 1) : 0;
        mainOffset = evenly;
        extraGap = evenly;
        break;
      }
      default: // Start
        mainOffset = 0;
        break;
    }

    // For reverse directions with Start/End justify, swap the edge placement
    if (IsReverse)
    {
      if (justify == JustifyContent.Start) mainOffset = freeSpace;
      else if (justify == JustifyContent.End) mainOffset = 0;
    }

    // ── Place flow children ─────────────────────────────────────────────────
    for (int i = 0; i < flow.Count; i++)
    {
      LayoutElement child = flow[i];
      visibleChildren.Add(child);

      int childMain = isRow ? child.Bounds.Width : child.Bounds.Height;
      int childCross = isRow ? child.Bounds.Height : child.Bounds.Width;

      // Per-child cross-axis alignment (AlignSelf overrides container AlignItems)
      AlignItems effectiveAlign = child.AlignSelf ?? crossAlign;
      int crossOffset = effectiveAlign switch
      {
        AlignItems.End => containerCross - childCross,
        AlignItems.Center => Math.Max(0, (containerCross - childCross) / 2),
        AlignItems.Stretch => 0, // size was already adjusted above
        // Baseline: not yet supported, falls through to Start
        _ => 0
      };

      child.Bounds.OffsetX = isRow ? mainOffset : crossOffset;
      child.Bounds.OffsetY = isRow ? crossOffset : mainOffset;

      mainOffset += childMain;
      if (i < flow.Count - 1)
      {
        mainOffset += Gap + extraGap;
      }
    }

    // ── Place absolute children ─────────────────────────────────────────────
    foreach (LayoutElement child in absolute)
    {
      visibleChildren.Add(child);
      (int? top, int? left, int? bottom, int? right) = child.Bounds.Position;

      int offsetX = left.OrZero();
      int offsetY = top.OrZero();

      if (bottom.HasValue && !top.HasValue)
      {
        offsetY = contentSize.Height - bottom.Value - child.Bounds.Height;
      }

      if (right.HasValue && !left.HasValue)
      {
        offsetX = contentSize.Width - right.Value - child.Bounds.Width;
      }

      child.Bounds.OffsetX = offsetX;
      child.Bounds.OffsetY = offsetY;
    }
  }

  // ── Private helpers ──────────────────────────────────────────────────────

  /// <summary>
  ///   Resolves the effective <see cref="Enums.JustifyContent" /> and <see cref="Enums.AlignItems" />
  ///   for the current frame. When <see cref="GridAlignment" /> is set it is decoded direction-aware
  ///   at call-time, ensuring correctness regardless of property-set order.
  /// </summary>
  private (JustifyContent justify, AlignItems crossAlign) ResolveAlignment(bool isRow)
  {
    if (GridAlignment is null)
    {
      return (JustifyContent, AlignItems);
    }

    Alignment grid = GridAlignment.Value;

    bool hCenter = grid is Alignment.TopCenter or Alignment.Center or Alignment.BottomCenter;
    bool hEnd = grid is Alignment.TopRight or Alignment.MiddleRight or Alignment.BottomRight;
    bool vCenter = grid is Alignment.MiddleLeft or Alignment.Center or Alignment.MiddleRight;
    bool vEnd = grid is Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight;

    // For Row: horizontal → justify (main), vertical → align (cross)
    // For Column: vertical → justify (main), horizontal → align (cross)
    JustifyContent justify = isRow
      ? (hCenter ? JustifyContent.Center : hEnd ? JustifyContent.End : JustifyContent.Start)
      : (vCenter ? JustifyContent.Center : vEnd ? JustifyContent.End : JustifyContent.Start);

    AlignItems crossAlign = isRow
      ? (vCenter ? AlignItems.Center : vEnd ? AlignItems.End : AlignItems.Start)
      : (hCenter ? AlignItems.Center : hEnd ? AlignItems.End : AlignItems.Start);

    return (justify, crossAlign);
  }

  /// <summary>
  ///   Distributes free space among children via <see cref="LayoutElement.FlexGrow" /> (positive
  ///   free space) or <see cref="LayoutElement.FlexShrink" /> (overflow). Mutates child bounds
  ///   along the main axis in-place.
  /// </summary>
  private static void DistributeFlexSpace(List<LayoutElement> children, int freeSpace, bool isRow)
  {
    if (freeSpace > 0)
    {
      float totalGrow = children.Sum(c => c.FlexGrow);
      if (totalGrow <= 0) return;

      float perUnit = freeSpace / totalGrow;
      foreach (LayoutElement child in children)
      {
        if (child.FlexGrow <= 0) continue;
        int extra = (int)(child.FlexGrow * perUnit);
        if (isRow) child.Bounds.Width += extra;
        else child.Bounds.Height += extra;
      }
    }
    else if (freeSpace < 0)
    {
      // Weighted shrink: each child shrinks proportional to (shrink-factor × current size)
      float totalFactor = children.Sum(c => c.FlexShrink * (isRow ? c.Bounds.Width : c.Bounds.Height));
      if (totalFactor <= 0) return;

      int overflow = -freeSpace;
      foreach (LayoutElement child in children)
      {
        if (child.FlexShrink <= 0) continue;
        float weight = child.FlexShrink * (isRow ? child.Bounds.Width : child.Bounds.Height) / totalFactor;
        int reduction = (int)(overflow * weight);
        if (isRow) child.Bounds.Width = Math.Max(0, child.Bounds.Width - reduction);
        else child.Bounds.Height = Math.Max(0, child.Bounds.Height - reduction);
      }
    }
  }
}
