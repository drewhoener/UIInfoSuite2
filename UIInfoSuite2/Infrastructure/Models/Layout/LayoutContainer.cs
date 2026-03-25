using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Layout.Strategy;

namespace UIInfoSuite2.Infrastructure.Models.Layout;

internal class LayoutContainer : LayoutElement, IDisposable
{
  /// <summary>
  ///   Row/Column shorthand for the <see cref="Direction" /> property. Subset of
  ///   <see cref="FlexDirection" /> kept for backward compatibility. Use
  ///   <see cref="FlexDirection" /> when reverse directions are needed.
  /// </summary>
  public enum LayoutDirection
  {
    Row,
    Column
  }

  private readonly List<LayoutElement> _children = [];
  private readonly List<LayoutElement> _visibleChildren = [];
  private readonly FlexLayoutStrategy _flexStrategy = new();
  private LayoutStrategy _strategy = null!; // set in every constructor path

  /// <summary>
  ///   When true, the container automatically hides itself whenever all of its children are hidden,
  ///   and shows itself again as soon as any child becomes visible. Applied during the layout pass.
  /// </summary>
  public bool AutoHideWhenEmpty { get; set; }

  public LayoutContainer(string? identifier, params LayoutElement[] children) : base(identifier)
  {
    _strategy = _flexStrategy;
    AddChildren(children);
  }

  public LayoutContainer(params LayoutElement[] children) : this(null, children) { }

  // ── Strategy ─────────────────────────────────────────────────────────────

  /// <summary>
  ///   The active layout strategy. Defaults to <see cref="FlexLayoutStrategy" />.
  ///   Replacing the strategy does not transfer any previously-configured flex properties;
  ///   configure the new strategy before assigning it.
  /// </summary>
  public LayoutStrategy Strategy
  {
    get => _strategy;
    set
    {
      _strategy = value ?? _flexStrategy;
      MarkLayoutDirty(Id);
    }
  }

  // ── Convenience properties (configure the default FlexLayoutStrategy) ─────
  //
  // These always update _flexStrategy regardless of what Strategy is currently
  // set to. If Strategy has been replaced with a custom strategy these setters
  // have no visual effect until Strategy is reset to _flexStrategy.

  /// <summary>Gets or sets the layout direction (Row or Column).</summary>
  public LayoutDirection Direction
  {
    get => _flexStrategy.Direction is FlexDirection.Row or FlexDirection.RowReverse
      ? LayoutDirection.Row
      : LayoutDirection.Column;
    set
    {
      FlexDirection mapped = value == LayoutDirection.Row ? FlexDirection.Row : FlexDirection.Column;
      if (_flexStrategy.Direction == mapped) return;
      _flexStrategy.Direction = mapped;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  /// <summary>
  ///   Full flex direction including reverse variants. Use instead of <see cref="Direction" />
  ///   when RowReverse or ColumnReverse is needed.
  /// </summary>
  public FlexDirection FlexDirection
  {
    get => _flexStrategy.Direction;
    set
    {
      if (_flexStrategy.Direction == value) return;
      _flexStrategy.Direction = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  /// <summary>Gets or sets the spacing between children in pixels.</summary>
  public int ComponentSpacing
  {
    get => _flexStrategy.Gap;
    set
    {
      if (_flexStrategy.Gap == value) return;
      _flexStrategy.Gap = value;
      MarkLayoutDirty(Id);
    }
  }

  /// <summary>
  ///   9-grid alignment shorthand. Decoded direction-aware at arrange-time so it remains
  ///   correct regardless of whether <see cref="Direction" /> or <see cref="FlexDirection" />
  ///   is set before or after this property. Setting this overrides any direct
  ///   <see cref="JustifyContent" /> / <see cref="AlignItems" /> values previously set.
  ///   Set <see cref="JustifyContent" /> directly for SpaceBetween/SpaceAround/SpaceEvenly.
  /// </summary>
  public Alignment Alignment
  {
    get => _flexStrategy.GridAlignment ?? Alignment.TopLeft;
    set
    {
      if (_flexStrategy.GridAlignment == value) return;
      _flexStrategy.GridAlignment = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  /// <summary>Controls how children are distributed along the main axis.</summary>
  public JustifyContent JustifyContent
  {
    get => _flexStrategy.JustifyContent;
    set
    {
      if (_flexStrategy.JustifyContent == value) return;
      // Clear GridAlignment so it does not override the explicit setting
      _flexStrategy.GridAlignment = null;
      _flexStrategy.JustifyContent = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  /// <summary>Default cross-axis alignment for all children.</summary>
  public AlignItems AlignItems
  {
    get => _flexStrategy.AlignItems;
    set
    {
      if (_flexStrategy.AlignItems == value) return;
      // Clear GridAlignment so it does not override the explicit setting
      _flexStrategy.GridAlignment = null;
      _flexStrategy.AlignItems = value;
      MarkFlagDirty(LayoutDirtyFlags.Direction);
    }
  }

  // ── Fluent API ────────────────────────────────────────────────────────────

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

  /// <summary>Sets the full flex direction (including reverse variants) and returns this container for chaining.</summary>
  public LayoutContainer WithFlexDirection(FlexDirection direction)
  {
    FlexDirection = direction;
    return this;
  }

  /// <summary>Sets the justify-content mode and returns this container for chaining.</summary>
  public LayoutContainer WithJustifyContent(JustifyContent justify)
  {
    JustifyContent = justify;
    return this;
  }

  /// <summary>Sets the default align-items mode and returns this container for chaining.</summary>
  public LayoutContainer WithAlignItems(AlignItems align)
  {
    AlignItems = align;
    return this;
  }

  // ── Factory methods ───────────────────────────────────────────────────────

  public static LayoutContainer Row(string? identifier, params LayoutElement[] children)
  {
    var c = new LayoutContainer(identifier);
    c.Direction = LayoutDirection.Row;
    c.AddChildren(children);
    return c;
  }

  public static LayoutContainer Row(string? identifier, int spacing, params LayoutElement[] children)
  {
    var c = new LayoutContainer(identifier);
    c.Direction = LayoutDirection.Row;
    c.ComponentSpacing = spacing;
    c.AddChildren(children);
    return c;
  }

  public static LayoutContainer Column(string? identifier, params LayoutElement[] children)
  {
    var c = new LayoutContainer(identifier);
    c.AddChildren(children);
    return c;
  }

  public static LayoutContainer Column(string? identifier, int spacing, params LayoutElement[] children)
  {
    var c = new LayoutContainer(identifier);
    c.ComponentSpacing = spacing;
    c.AddChildren(children);
    return c;
  }

  // ── Child management ──────────────────────────────────────────────────────

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

  public void RemoveChild(LayoutElement element)
  {
    element.UnsetParent();
    _children.Remove(element);
    MarkLayoutDirty(Id);
  }

  protected bool AllChildrenHidden() => _children.TrueForAll(e => e.IsHidden);

  // ── Layout ────────────────────────────────────────────────────────────────

  protected internal override void PropagateLayoutChange(LayoutElement? caller = null)
  {
    if (caller is not null && IsDirty)
    {
      return;
    }

    if (caller is not null)
    {
      DirtyFlags |= LayoutDirtyFlags.Layout;
    }

    ModEntry.LayoutDebug($"{GetType().Name}::{caller?.Id} Propagated layout change");
    Parent?.PropagateLayoutChange(this);
  }

  protected internal override void Layout()
  {
    if (!NeedsLayout)
    {
      return;
    }

    UpdateBounds();

    if (AutoHideWhenEmpty)
    {
      bool allHidden = AllChildrenHidden();
      if (allHidden != IsHidden)
      {
        IsHidden = allHidden;
        UpdateBounds();
      }
    }

    _strategy.ArrangeChildren(ContentSize, _children, _visibleChildren);

    ResetDirty();
  }

  protected override Dimensions MeasureContent()
  {
    if (_children.Count == 0)
    {
      return Dimensions.Empty;
    }

    return _strategy.MeasureContent(_children);
  }

  // ── Drawing ───────────────────────────────────────────────────────────────

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

  // ── IDisposable ───────────────────────────────────────────────────────────

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
}
