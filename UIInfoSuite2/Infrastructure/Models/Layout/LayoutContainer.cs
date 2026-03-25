using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Layout;

internal class LayoutContainer : LayoutElement, IDisposable
{
  /// <summary>
  ///   Defines the layout direction for tooltip components.
  /// </summary>
  public enum LayoutDirection
  {
    Row,

    Column
    // TODO: Row Reverse, Column Reverse
  }

  private readonly List<LayoutElement> _children = [];
  private readonly List<LayoutElement> _visibleChildren = [];
  private Alignment _alignment = Alignment.TopLeft;
  private int _componentSpacing = 2;
  private Dimensions _componentSpacingSize = new(0, 2);
  private LayoutDirection _layoutDirection = LayoutDirection.Column;

  /// <summary>
  ///   When true, the container automatically hides itself whenever all of its children are hidden,
  ///   and shows itself again as soon as any child becomes visible. Applied during the layout pass.
  /// </summary>
  public bool AutoHideWhenEmpty { get; set; }

  public LayoutContainer(string? identifier, params LayoutElement[] children) : base(identifier)
  {
    AddChildren(children);
  }

  public LayoutContainer(params LayoutElement[] children) : this(null, children) { }

  /// <summary>
  ///   Gets or sets the spacing between elements in the container.
  /// </summary>
  public int ComponentSpacing
  {
    get => _componentSpacing;
    set
    {
      _componentSpacing = value;
      UpdateLayoutSpacing();
      MarkLayoutDirty(Id);
    }
  }

  /// <summary>
  ///   Gets or sets the layout direction for child components.
  /// </summary>
  public LayoutDirection Direction
  {
    get => _layoutDirection;
    set
    {
      _layoutDirection = value;
      UpdateLayoutSpacing();
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  /// <summary>
  ///   Gets or sets the 9-grid alignment for children within this container.
  ///   The horizontal axis controls main-axis justify (start/center/end) and the vertical axis
  ///   controls cross-axis alignment (start/center/end), both relative to the layout direction.
  ///   For Row: Left/Center/Right = horizontal justify, Top/Middle/Bottom = vertical align.
  ///   For Column: Top/Center/Bottom = vertical justify, Left/Center/Right = horizontal align.
  /// </summary>
  public Alignment Alignment
  {
    get => _alignment;
    set
    {
      if (_alignment == value)
      {
        return;
      }

      _alignment = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  /// <summary>Sets the spacing between children and returns this container for chaining.</summary>
  public LayoutContainer WithSpacing(int spacing)
  {
    ComponentSpacing = spacing;
    return this;
  }

  /// <summary>Sets the 9-grid alignment and returns this container for chaining.</summary>
  public LayoutContainer WithAlignment(Alignment alignment)
  {
    Alignment = alignment;
    return this;
  }

  public override void Dispose()
  {
    if (Parent is LayoutContainer parentContainer)
    {
      parentContainer.RemoveChild(this);
    }

    UnsetParent();
    foreach (LayoutElement child in _children)
    {
      child.UnsetParent();
    }

    _children.Clear();
  }

  public static LayoutContainer Row(string? identifier, params LayoutElement[] children)
  {
    var newContainer = new LayoutContainer(identifier);
    newContainer.Direction = LayoutDirection.Row;
    newContainer.AddChildren(children);
    return newContainer;
  }

  public static LayoutContainer Row(string? identifier, int spacing, params LayoutElement[] children)
  {
    var newContainer = new LayoutContainer(identifier);
    newContainer.Direction = LayoutDirection.Row;
    newContainer.ComponentSpacing = spacing;
    newContainer.AddChildren(children);
    return newContainer;
  }

  public static LayoutContainer Column(string? identifier, params LayoutElement[] children)
  {
    var newContainer = new LayoutContainer(identifier);
    newContainer.AddChildren(children);
    return newContainer;
  }

  public static LayoutContainer Column(string? identifier, int spacing, params LayoutElement[] children)
  {
    var newContainer = new LayoutContainer(identifier);
    newContainer.ComponentSpacing = spacing;
    newContainer.AddChildren(children);
    return newContainer;
  }

  /// <summary>
  ///   Adds multiple components to the container.
  /// </summary>
  /// <param name="components">The components to add.</param>
  public void AddChildren(params LayoutElement[] components)
  {
    _children.EnsureCapacity(_children.Count + components.Length);
    foreach (LayoutElement component in components)
    {
      component.Parent = this;
      _children.Add(component);
    }

    MarkLayoutDirty(Id);
  }

  /// <summary>
  ///   Removes a element from the container.
  /// </summary>
  /// <param name="element">The element to remove.</param>
  public void RemoveChild(LayoutElement element)
  {
    element.UnsetParent();
    _children.Remove(element);
    MarkLayoutDirty(Id);
  }

  /// <summary>
  ///   Updates the spacing vector based on the current layout direction.
  /// </summary>
  private void UpdateLayoutSpacing()
  {
    _componentSpacingSize = _layoutDirection switch
    {
      LayoutDirection.Row => new Dimensions(_componentSpacing, 0),
      LayoutDirection.Column => new Dimensions(0, _componentSpacing),
      _ => Dimensions.Empty
    };
    MarkLayoutDirty(Id);
  }

  protected bool AllChildrenHidden()
  {
    return _children.TrueForAll(e => e.IsHidden);
  }

  protected internal override void PropagateLayoutChange(LayoutElement? caller = null)
  {
    // If this was triggered by a child and we're already dirty, our parent already knows.
    if (caller is not null && IsDirty)
    {
      return;
    }

    // A child changed: mark our own layout dirty so NeedsLayout is true without a separate set.
    if (caller is not null)
    {
      DirtyFlags |= LayoutDirtyFlags.Layout;
    }

    ModEntry.LayoutDebug($"{GetType().Name}::{caller?.Id} Propagated layout change");
    Parent?.PropagateLayoutChange(this);
  }

  public override void Draw(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    if (IsHidden && !NeedsLayout)
    {
      return;
    }

    Layout();

    if (_visibleChildren.Count == 0)
    {
      return;
    }

    DrawSelf(spriteBatch, positionX, positionY);
    if (ModEntry.Config.DrawDebugBounds)
    {
      DrawDebugBounds(spriteBatch, positionX, positionY);
    }

    int baseX = positionX + Margin.Left.OrZero() + Padding.Left.OrZero();
    int baseY = positionY + Margin.Top.OrZero() + Padding.Top.OrZero();

    foreach (LayoutElement component in _visibleChildren)
    {
      component.Draw(spriteBatch, baseX + component.Bounds.OffsetX, baseY + component.Bounds.OffsetY);
    }
  }

  protected internal override void Layout()
  {
    if (!NeedsLayout)
    {
      return;
    }

    // First measure ourselves (which includes measuring children)
    UpdateBounds();

    // Auto-hide when all children are hidden. Re-measure if visibility changed so
    // our bounds are correct before the parent reads them.
    if (AutoHideWhenEmpty)
    {
      bool allHidden = AllChildrenHidden();
      if (allHidden != IsHidden)
      {
        IsHidden = allHidden;
        UpdateBounds();
      }
    }

    // Always do full child positioning since any child could affect layout
    ArrangeChildren();

    ResetDirty();
  }

  protected override Dimensions MeasureContent()
  {
    if (_children.Count == 0)
    {
      return Dimensions.Empty;
    }

    var maxWidth = 0;
    var maxHeight = 0;
    var totalWidth = 0;
    var totalHeight = 0;
    var visibleFlowCount = 0;

    // Handle flow layout children
    foreach (LayoutElement child in _children.Where(c => !c.IsAbsolute))
    {
      child.Layout();
      Dimensions dims = child.Bounds.Size; // Will be Empty if hidden
      if (dims == Dimensions.Empty)
      {
        continue;
      }

      visibleFlowCount++;
      maxWidth = Math.Max(maxWidth, dims.Width);
      maxHeight = Math.Max(maxHeight, dims.Height);
      totalWidth += dims.Width;
      totalHeight += dims.Height;
    }

    // Add spacing between visible flow components
    if (visibleFlowCount > 1)
    {
      totalWidth += _componentSpacingSize.Width * (visibleFlowCount - 1);
      totalHeight += _componentSpacingSize.Height * (visibleFlowCount - 1);
    }

    // Calculate flow layout bounds
    Dimensions flowBounds = Direction switch
    {
      LayoutDirection.Row => new Dimensions(totalWidth, maxHeight),
      LayoutDirection.Column => new Dimensions(maxWidth, totalHeight),
      _ => Dimensions.Empty
    };

    // Handle absolute positioned children
    var absoluteBounds = new Dimensions();
    foreach (LayoutElement child in _children.Where(c => c.IsAbsolute))
    {
      child.Layout();
      Dimensions dims = child.Bounds.Size; // Will be Empty if hidden
      if (dims == Dimensions.Empty)
      {
        continue;
      }

      Insets position = child.Bounds.Position;
      int requiredWidth = position.Left.OrZero() + dims.Width + position.Right.OrZero();
      int requiredHeight = position.Top.OrZero() + dims.Height + position.Bottom.OrZero();

      absoluteBounds.Width = Math.Max(absoluteBounds.Width, requiredWidth);
      absoluteBounds.Height = Math.Max(absoluteBounds.Height, requiredHeight);
    }

    return new Dimensions(
      Math.Max(flowBounds.Width, absoluteBounds.Width),
      Math.Max(flowBounds.Height, absoluteBounds.Height)
    );
  }

  private void ArrangeChildren()
  {
    _visibleChildren.Clear();

    List<LayoutElement> normalChildren = _children.Where(c => c is { IsAbsolute: false, IsHidden: false }).ToList();
    IEnumerable<LayoutElement> absoluteChildren = _children.Where(c => c is { IsAbsolute: true, IsHidden: false });

    // Determine layout axes
    bool isRow = _layoutDirection == LayoutDirection.Row;
    int containerCrossSize = isRow ? ContentSize.Height : ContentSize.Width;
    int containerMainSize = isRow ? ContentSize.Width : ContentSize.Height;

    // Decode 9-grid alignment into horizontal and vertical intent
    bool alignHCenter = _alignment is Alignment.TopCenter or Alignment.Center or Alignment.BottomCenter;
    bool alignHEnd = _alignment is Alignment.TopRight or Alignment.MiddleRight or Alignment.BottomRight;
    bool alignVCenter = _alignment is Alignment.MiddleLeft or Alignment.Center or Alignment.MiddleRight;
    bool alignVEnd = _alignment is Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight;

    // Map horizontal/vertical intent to main/cross axis based on direction
    bool mainCenter = isRow ? alignHCenter : alignVCenter;
    bool mainEnd = isRow ? alignHEnd : alignVEnd;
    bool crossCenter = isRow ? alignVCenter : alignHCenter;
    bool crossEnd = isRow ? alignVEnd : alignHEnd;

    // Calculate total main-axis size (children + spacing between them)
    var totalMainSize = 0;
    foreach (LayoutElement child in normalChildren)
    {
      totalMainSize += isRow ? child.Bounds.Width : child.Bounds.Height;
    }

    if (normalChildren.Count > 1)
    {
      totalMainSize += _componentSpacing * (normalChildren.Count - 1);
    }

    // Calculate starting offset along the main axis (justify-content)
    int freeSpace = Math.Max(0, containerMainSize - totalMainSize);
    int mainOffset = mainCenter ? freeSpace / 2 : mainEnd ? freeSpace : 0;

    // Place flow children
    for (var i = 0; i < normalChildren.Count; i++)
    {
      LayoutElement child = normalChildren[i];
      _visibleChildren.Add(child);
      Dimensions childSize = child.Bounds.Size;

      int childMainSize = isRow ? childSize.Width : childSize.Height;
      int childCrossSize = isRow ? childSize.Height : childSize.Width;

      // Calculate cross-axis offset per child (align-items)
      int crossOffset = crossCenter
        ? (containerCrossSize - childCrossSize) / 2
        : crossEnd
          ? containerCrossSize - childCrossSize
          : 0;

      child.Bounds.OffsetX = isRow ? mainOffset : crossOffset;
      child.Bounds.OffsetY = isRow ? crossOffset : mainOffset;

      mainOffset += childMainSize;
      if (i < normalChildren.Count - 1)
      {
        mainOffset += _componentSpacing;
      }
    }

    // Handle absolute children
    foreach (LayoutElement child in absoluteChildren)
    {
      _visibleChildren.Add(child);
      (int? top, int? left, int? bottom, int? right) = child.Bounds.Position;

      // Default to Top=0, Left=0 if not specified
      int offsetX = left.OrZero();
      int offsetY = top.OrZero();

      // If bottom is specified but top isn't, position from bottom
      if (bottom.HasValue && !top.HasValue)
      {
        offsetY = Bounds.Height - bottom.Value - child.Bounds.Height;
      }

      // If right is specified but left isn't, position from right
      if (right.HasValue && !left.HasValue)
      {
        offsetX = Bounds.Width - right.Value - child.Bounds.Width;
      }

      child.Bounds.OffsetX = offsetX;
      child.Bounds.OffsetY = offsetY;
    }
  }

  protected void DrawContainerBox(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    int x = positionX + Margin.Left.OrZero();
    int y = positionY + Margin.Top.OrZero();
    int finalWidth = ContentSize.Width + Padding.HorizontalTotal();
    int finalHeight = ContentSize.Height + Padding.VerticalTotal();
    IClickableMenu.drawTextureBox(
      spriteBatch,
      Game1.menuTexture,
      TextureHelper.OutlinedTextureBox,
      x,
      y,
      finalWidth,
      finalHeight,
      Color.White
    );
  }
}
