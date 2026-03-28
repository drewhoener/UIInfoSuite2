using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Layout;

[Flags]
internal enum LayoutDirtyFlags
{
  None = 0,
  Initial = 1 << 0,
  Layout = 1 << 1,
  Margin = 1 << 2,
  Padding = 1 << 3,
  Position = 1 << 4,
  Constraints = 1 << 5,
  Direction = 1 << 6,
  Value = 1 << 7,
  Visibility = 1 << 8
}

internal static class LayoutDirtyFlagsExtensions
{
  public static bool HasFlagFast(this LayoutDirtyFlags value, LayoutDirtyFlags flag)
  {
    return (value & flag) != 0;
  }
}

internal abstract class LayoutElement : ITrackable, IDisposable
{
  private static readonly Dictionary<string, int> Identifiers = new();
  private readonly TrackableValue<bool> _hidden;
  protected internal readonly string Id;
  protected readonly TrackedInsets MarginTracked;
  protected readonly TrackedInsets PaddingTracked;
  private AlignItems? _alignSelf;
  private int? _fixedHeight;
  private int? _fixedWidth;

  // ── Flex item properties (read by the parent container's layout strategy) ──
  private float _flexGrow;
  private float _flexShrink = 1f;
  private int _order;
  private LayoutElement? _parent;
  protected internal LayoutBounds Bounds = new();
  internal Dimensions ContentSize = Dimensions.Empty;

  protected LayoutElement(string? identifier)
  {
    Id = NormalizeIdentifier(identifier);

    MarginTracked = new TrackedInsets(0, MarkMarginDirty, "Margin");
    PaddingTracked = new TrackedInsets(0, MarkPaddingDirty, "Padding");
    _hidden = new TrackableValue<bool>(false, MarkVisibilityDirty, "Hidden");

    MarkLayoutDirty("Init");
  }

  protected LayoutElement() : this(null) { }

  protected LayoutDirtyFlags DirtyFlags { get; set; } = LayoutDirtyFlags.Initial;

  public IInsets Margin
  {
    get => MarginTracked;
    set => MarginTracked.SetInsets(value);
  }

  public IInsets Padding
  {
    get => PaddingTracked;
    set => PaddingTracked.SetInsets(value);
  }

  public bool IsHidden
  {
    get => _hidden.Value;
    set => _hidden.Value = value;
  }

  public bool IsAbsolute
  {
    get => Bounds.IsAbsolute;
    set
    {
      if (Bounds.IsAbsolute == value)
      {
        return;
      }

      Bounds.IsAbsolute = value;
      MarkFlagDirty(LayoutDirtyFlags.Position);
    }
  }

  // ── Flex item properties ─────────────────────────────────────────────────

  /// <summary>
  ///   How much this element grows relative to siblings when positive free space is available
  ///   along the main axis. <c>0</c> (default) means the element does not grow.
  /// </summary>
  public float FlexGrow
  {
    get => _flexGrow;
    set
    {
      if (Math.Abs(_flexGrow - value) < 0.001f)
      {
        return;
      }

      _flexGrow = value;
      MarkLayoutDirty(Id);
    }
  }

  /// <summary>
  ///   How much this element shrinks relative to siblings when the children overflow the container.
  ///   Default is <c>1</c> (proportional shrink). Set to <c>0</c> to prevent shrinking.
  /// </summary>
  public float FlexShrink
  {
    get => _flexShrink;
    set
    {
      if (Math.Abs(_flexShrink - value) < 0.001f)
      {
        return;
      }

      _flexShrink = value;
      MarkLayoutDirty(Id);
    }
  }

  /// <summary>
  ///   Controls the placement order within the parent container, independent of DOM order.
  ///   Lower values are placed first. Default is <c>0</c>.
  /// </summary>
  public int Order
  {
    get => _order;
    set
    {
      if (_order == value)
      {
        return;
      }

      _order = value;
      MarkFlagDirty(LayoutDirtyFlags.Layout);
    }
  }

  /// <summary>
  ///   Overrides the parent container's <c>AlignItems</c> for this element specifically.
  ///   <c>null</c> (default) defers to the container.
  /// </summary>
  public AlignItems? AlignSelf
  {
    get => _alignSelf;
    set
    {
      if (_alignSelf == value)
      {
        return;
      }

      _alignSelf = value;
      MarkFlagDirty(LayoutDirtyFlags.Layout);
    }
  }

  /// <summary>
  ///   Forces a fixed content-box width in pixels, bypassing the measured content width.
  ///   The total bounds will be <c>FixedWidth + margin + padding</c>. Set to <c>null</c> to
  ///   let content drive the width.
  /// </summary>
  public int? FixedWidth
  {
    get => _fixedWidth;
    set
    {
      if (_fixedWidth == value)
      {
        return;
      }

      _fixedWidth = value;
      MarkLayoutDirty(Id);
    }
  }

  /// <summary>
  ///   Forces a fixed content-box height in pixels, bypassing the measured content height.
  ///   The total bounds will be <c>FixedHeight + margin + padding</c>. Set to <c>null</c> to
  ///   let content drive the height.
  /// </summary>
  public int? FixedHeight
  {
    get => _fixedHeight;
    set
    {
      if (_fixedHeight == value)
      {
        return;
      }

      _fixedHeight = value;
      MarkLayoutDirty(Id);
    }
  }

  public Insets Position
  {
    get => Bounds.Position;
    set
    {
      if (Bounds.Position.Equals(value))
      {
        return;
      }

      Bounds.SetPosition(value);
      MarkFlagDirty(LayoutDirtyFlags.Position);
    }
  }

  /// <summary>
  ///   Gets or sets the parent tooltip element. Can only be set once.
  /// </summary>
  /// <exception cref="ArgumentException">Thrown when trying to create a circular dependency or change an existing parent.</exception>
  public LayoutElement? Parent
  {
    get => _parent;
    set
    {
      if (value == this)
      {
        // ??????????
        throw new ArgumentException("Parent cannot be a circular dependency.");
      }

      if (_parent is LayoutContainer layoutContainer)
      {
        layoutContainer.RemoveChild(this);
      }

      _parent = value;
    }
  }

  protected virtual bool NeedsLayout => IsDirty;

  public virtual void Dispose() { }

  public virtual bool IsDirty => DirtyFlags != LayoutDirtyFlags.None;


  public virtual void ResetDirty()
  {
    DirtyFlags = LayoutDirtyFlags.None;
    _hidden.ResetDirty();
    MarginTracked.ResetDirty();
    PaddingTracked.ResetDirty();
  }

  private static string NormalizeIdentifier(string? identifier)
  {
    if (string.IsNullOrWhiteSpace(identifier))
    {
      return Guid.NewGuid().ToString();
    }

    string lookup = identifier.ToLower();
    int result = Identifiers.GetOrCreate(lookup, () => 0);
    Identifiers[lookup] = result + 1;

    return $"{lookup}-{result}";
  }

  /// <summary>
  ///   Removes the parent reference from this element.
  /// </summary>
  protected internal void UnsetParent()
  {
    _parent = null;
  }

  protected void MarkPaddingDirty(string? identifier)
  {
    MarkFlagDirty(LayoutDirtyFlags.Padding, identifier);
  }

  protected void MarkMarginDirty(string? identifier)
  {
    MarkFlagDirty(LayoutDirtyFlags.Margin, identifier);
  }

  protected internal void MarkLayoutDirty(string? identifier)
  {
    MarkFlagDirty(LayoutDirtyFlags.Layout, identifier);
  }

  protected internal void MarkVisibilityDirty(string? identifier)
  {
    MarkFlagDirty(LayoutDirtyFlags.Visibility | LayoutDirtyFlags.Layout, identifier);
  }

  protected void MarkFlagDirty(LayoutDirtyFlags flags, string? identifier = null)
  {
    if (DirtyFlags.HasFlagFast(flags) && !flags.HasFlagFast(LayoutDirtyFlags.Visibility))
    {
      ModEntry.LayoutDebug($"{GetType().Name}::{identifier} Updated existing flag {flags}");
      return;
    }

    ModEntry.LayoutDebug($"{GetType().Name}::{identifier} Updated layout flag {flags}");
    DirtyFlags |= flags;
    PropagateLayoutChange();
  }

  protected internal virtual void PropagateLayoutChange(LayoutElement? caller = null)
  {
    if (caller is not null && IsDirty)
    {
      return;
    }

    ModEntry.LayoutDebug($"{GetType().Name}::{caller?.Id} Propagated layout change");
    Parent?.PropagateLayoutChange(this);
  }

  protected virtual void DrawContent(SpriteBatch spriteBatch, int positionX, int positionY) { }

  /// <summary>
  ///   Draws the element's own visual elements, if any.
  /// </summary>
  /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
  /// <param name="positionX"></param>
  /// <param name="positionY"></param>
  protected virtual void DrawSelf(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    DrawContent(
      spriteBatch,
      positionX + Margin.Left.OrZero() + Padding.Left.OrZero(),
      positionY + Margin.Top.OrZero() + Padding.Top.OrZero()
    );
  }

  public virtual void Draw(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    if (IsHidden && !IsDirty)
    {
      return;
    }

    Layout();

    if (IsHidden)
    {
      return;
    }

    DrawSelf(spriteBatch, positionX, positionY);
    if (ModEntry.Config.DrawDebugBounds)
    {
      DrawDebugBounds(spriteBatch, positionX, positionY);
    }
  }

  protected internal virtual void Layout()
  {
    if (!NeedsLayout)
    {
      return;
    }

    UpdateBounds(); // This handles dimensions, content size, etc.
    ResetDirty();
  }

  protected abstract Dimensions MeasureContent();

  protected internal void UpdateBounds()
  {
    if (IsHidden)
    {
      // Keep content sixe but zero out bounds so that when we reappear it forces a layout change.
      Bounds.Size = Dimensions.Empty;
      return;
    }

    Dimensions newContentSize = MeasureContent();

    // Apply fixed content-box overrides (content size only; bounds = content + margin + padding)
    if (_fixedWidth.HasValue)
    {
      newContentSize = new Dimensions(_fixedWidth.Value, newContentSize.Height);
    }

    if (_fixedHeight.HasValue)
    {
      newContentSize = new Dimensions(newContentSize.Width, _fixedHeight.Value);
    }

    ContentSize = newContentSize;
    Bounds.Size = ContentSize + Margin.ToDimensions() + Padding.ToDimensions();
  }

  /// <summary>
  ///   Draws debug visualization of the element's bounds.
  /// </summary>
  /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
  /// <param name="positionX">The x position to draw at</param>
  /// <param name="positionY">The y position to draw at</param>
  protected virtual void DrawDebugBounds(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    if (Bounds.Width <= 0 || Bounds.Height <= 0)
    {
      return;
    }

    // Draw full bounds
    Tools.DrawBoxOutline(spriteBatch, positionX, positionY, Bounds.Width, Bounds.Height, Color.Red, 2);

    positionX += Margin.Left.OrZero();
    positionY += Margin.Top.OrZero();


    // Draw padded bounds
    Tools.DrawBoxOutline(
      spriteBatch,
      positionX,
      positionY,
      Bounds.Width - Margin.HorizontalTotal(),
      Bounds.Height - Margin.VerticalTotal(),
      Color.Blue,
      2
    );

    if (ContentSize.Width <= 0 || ContentSize.Height <= 0)
    {
      return;
    }

    // Draw content bounds
    positionX += Padding.Left.OrZero();
    positionY += Padding.Top.OrZero();

    // Draw content bounds
    Tools.DrawBoxOutline(spriteBatch, positionX, positionY, ContentSize.Width, ContentSize.Height, Color.Green, 2);
  }
}
