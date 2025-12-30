using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
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

  private readonly HashSet<string> _childIds = [];
  private readonly List<LayoutElement> _children = [];
  private readonly HashSet<string> _dirtyChildren = [];
  private readonly List<LayoutElement> _visibleChildren = [];
  private int _componentSpacing = 2;
  private Dimensions _componentSpacingSize = new(0, 2);
  private LayoutDirection _layoutDirection = LayoutDirection.Column;

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

  protected override bool NeedsLayout => IsDirty || _dirtyChildren.Count != 0;

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
    _childIds.Clear();
  }

  public static LayoutContainer Row(string? identifier, params LayoutElement[] children)
  {
    var newContainer = new LayoutContainer(identifier);
    newContainer.Direction = LayoutDirection.Row;
    newContainer.AddChildren(children);
    return newContainer;
  }

  public static LayoutContainer Column(string? identifier, params LayoutElement[] children)
  {
    var newContainer = new LayoutContainer(identifier);
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
      _childIds.Add(component.Id);
      if (component.IsDirty)
      {
        _dirtyChildren.Add(component.Id);
      }
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
    _childIds.Remove(element.Id);
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

  public override void ResetDirty()
  {
    base.ResetDirty();
    MarginTracked.ResetDirty();
    PaddingTracked.ResetDirty();
    _dirtyChildren.Clear();
  }

  protected internal override void PropagateLayoutChange(LayoutElement? caller = null)
  {
    // Only prevent propagation if the calling element is already dirty
    if (caller is not null && _dirtyChildren.Contains(caller.Id))
    {
      return;
    }

    if (caller is not null && _childIds.Contains(caller.Id))
    {
      _dirtyChildren.Add(caller.Id);
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
    // Split children into absolute and normal flow
    IEnumerable<LayoutElement> normalChildren = _children.Where(c => c is { IsAbsolute: false, IsHidden: false });
    IEnumerable<LayoutElement> absoluteChildren = _children.Where(c => c is { IsAbsolute: true, IsHidden: false });

    // Handle normal flow children first
    var offsetX = 0;
    var offsetY = 0;
    foreach (LayoutElement child in normalChildren)
    {
      _visibleChildren.Add(child);
      child.Bounds.OffsetX = offsetX;
      child.Bounds.OffsetY = offsetY;
      Dimensions childSize = child.Bounds.Size;
      switch (_layoutDirection)
      {
        case LayoutDirection.Row:
          offsetX += childSize.Width;
          break;
        case LayoutDirection.Column:
          offsetY += childSize.Height;
          break;
      }

      offsetX += _componentSpacingSize.Width;
      offsetY += _componentSpacingSize.Height;
    }

    // Handle absolute children
    foreach (LayoutElement child in absoluteChildren)
    {
      _visibleChildren.Add(child);
      (int? top, int? left, int? bottom, int? right) = child.Bounds.Position;

      // Default to Top=0, Left=0 if not specified
      offsetX = left.OrZero();
      offsetY = top.OrZero();

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
