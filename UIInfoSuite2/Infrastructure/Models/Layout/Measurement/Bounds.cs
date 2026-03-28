using System;
using System.Diagnostics.CodeAnalysis;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
public interface IInsets : ICloneable
{
  int? Top { get; set; }
  int? Bottom { get; set; }
  int? Left { get; set; }
  int? Right { get; set; }

  void SetInsets(IInsets insets, bool quiet = false);
  void SetInsets(int? top, int? left, int? bottom, int? right, bool quiet = false);

  void SetHorizontal(int? horizontal, bool quiet = false);
  void SetHorizontal(int? top, int? bottom, bool quiet = false);

  void SetVertical(int? vertical, bool quiet = false);
  void SetVertical(int? left, int? right, bool quiet = false);
}

public static class InsetsExtensions
{
  public static int HorizontalTotal(this IInsets insets)
  {
    return insets.Left.OrZero() + insets.Right.OrZero();
  }

  public static int VerticalTotal(this IInsets insets)
  {
    return insets.Top.OrZero() + insets.Bottom.OrZero();
  }

  public static Dimensions ToDimensions(this IInsets insets)
  {
    return new Dimensions(insets.HorizontalTotal(), insets.VerticalTotal());
  }

  public static void SetAll(this IInsets insets, int? value, bool quiet = false)
  {
    insets.SetInsets(value, value, value, value, quiet);
  }

  public static IInsets Plus(this IInsets a, IInsets b)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(
      a.Top.OrZero() + b.Top.OrZero(),
      a.Left.OrZero() + b.Left.OrZero(),
      a.Bottom.OrZero() + b.Bottom.OrZero(),
      a.Right.OrZero() + b.Right.OrZero(),
      true
    );
    return clone;
  }

  public static IInsets Plus(this IInsets a, int b)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(a.Top.OrZero() + b, a.Left.OrZero() + b, a.Bottom.OrZero() + b, a.Right.OrZero() + b, true);
    return clone;
  }

  public static IInsets Minus(this IInsets a, IInsets b)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(
      a.Top.OrZero() - b.Top.OrZero(),
      a.Left.OrZero() - b.Left.OrZero(),
      a.Bottom.OrZero() - b.Bottom.OrZero(),
      a.Right.OrZero() - b.Right.OrZero(),
      true
    );
    return clone;
  }

  public static IInsets Minus(this IInsets a, int b)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(a.Top.OrZero() - b, a.Left.OrZero() - b, a.Bottom.OrZero() - b, a.Right.OrZero() - b, true);
    return clone;
  }

  public static IInsets Times(this IInsets a, IInsets b)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(
      a.Top.OrZero() * b.Top.OrZero(),
      a.Left.OrZero() * b.Left.OrZero(),
      a.Bottom.OrZero() * b.Bottom.OrZero(),
      a.Right.OrZero() * b.Right.OrZero(),
      true
    );
    return clone;
  }

  public static IInsets Times(this IInsets a, int b)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(a.Top.OrZero() * b, a.Left.OrZero() * b, a.Bottom.OrZero() * b, a.Right.OrZero() * b, true);
    return clone;
  }

  public static IInsets DividedBy(this IInsets a, IInsets b)
  {
    if (b.Top.OrZero() == 0 || b.Left.OrZero() == 0 || b.Bottom.OrZero() == 0 || b.Right.OrZero() == 0)
    {
      throw new DivideByZeroException("Cannot divide by zero in any inset value");
    }

    var clone = (IInsets)a.Clone();
    clone.SetInsets(
      a.Top.OrZero() / b.Top.OrZero(),
      a.Left.OrZero() / b.Left.OrZero(),
      a.Bottom.OrZero() / b.Bottom.OrZero(),
      a.Right.OrZero() / b.Right.OrZero(),
      true
    );
    return clone;
  }

  public static IInsets DividedBy(this IInsets a, int b)
  {
    if (b == 0)
    {
      throw new DivideByZeroException("Cannot divide by zero");
    }

    var clone = (IInsets)a.Clone();
    clone.SetInsets(a.Top.OrZero() / b, a.Left.OrZero() / b, a.Bottom.OrZero() / b, a.Right.OrZero() / b, true);
    return clone;
  }

  public static IInsets Negated(this IInsets a)
  {
    var clone = (IInsets)a.Clone();
    clone.SetInsets(-a.Top.OrZero(), -a.Left.OrZero(), -a.Bottom.OrZero(), -a.Right.OrZero(), true);
    return clone;
  }
}

internal class Insets : IInsets, IEquatable<Insets>
{
  private int? _bottom;
  private int? _cachedHashCode;
  private int? _left;
  private int? _right;

  private int? _top;

  public bool Equals(Insets? other)
  {
    if (other is null)
    {
      return false;
    }

    return Top == other.Top && Left == other.Left && Bottom == other.Bottom && Right == other.Right;
  }

  public int? Top
  {
    get => _top;
    set
    {
      _top = value;
      _cachedHashCode = null;
    }
  }

  public int? Bottom
  {
    get => _bottom;
    set
    {
      _bottom = value;
      _cachedHashCode = null;
    }
  }

  public int? Left
  {
    get => _left;
    set
    {
      _left = value;
      _cachedHashCode = null;
    }
  }

  public int? Right
  {
    get => _right;
    set
    {
      _right = value;
      _cachedHashCode = null;
    }
  }

  public object Clone()
  {
    return new Insets { Top = Top, Left = Left, Bottom = Bottom, Right = Right };
  }

  public void SetInsets(IInsets insets, bool quiet = false)
  {
    Top = insets.Top;
    Left = insets.Left;
    Bottom = insets.Bottom;
    Right = insets.Right;
  }

  public void SetInsets(int? top, int? left, int? bottom, int? right, bool quiet = false)
  {
    Top = top;
    Left = left;
    Bottom = bottom;
    Right = right;
  }

  public void SetHorizontal(int? horizontal, bool quiet = false)
  {
    Left = horizontal;
    Right = horizontal;
  }

  public void SetHorizontal(int? left, int? right, bool quiet = false)
  {
    Left = left;
    Right = right;
  }

  public void SetVertical(int? vertical, bool quiet = false)
  {
    Top = vertical;
    Bottom = vertical;
  }

  public void SetVertical(int? top, int? bottom, bool quiet = false)
  {
    Top = top;
    Bottom = bottom;
  }

  public void Deconstruct(out int? top, out int? left, out int? bottom, out int? right)
  {
    top = Top;
    left = Left;
    bottom = Bottom;
    right = Right;
  }

  public override bool Equals(object? obj)
  {
    return obj is Insets insets && Equals(insets);
  }

  [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
  public override int GetHashCode()
  {
    _cachedHashCode ??= HashCode.Combine(Top, Left, Bottom, Right);
    return _cachedHashCode.Value;
  }

  public static Insets operator +(Insets a, Insets b)
  {
    return (Insets)a.Plus(b);
  }

  public static Insets operator +(Insets a, int b)
  {
    return (Insets)a.Plus(b);
  }

  public static Insets operator +(int a, Insets b)
  {
    return (Insets)b.Plus(a);
  }

  public static Insets operator -(Insets a, Insets b)
  {
    return (Insets)a.Minus(b);
  }

  public static Insets operator -(Insets a, int b)
  {
    return (Insets)a.Minus(b);
  }

  public static Insets operator *(Insets a, Insets b)
  {
    return (Insets)a.Times(b);
  }

  public static Insets operator *(Insets a, int b)
  {
    return (Insets)a.Times(b);
  }

  public static Insets operator *(int a, Insets b)
  {
    return (Insets)b.Times(a);
  }

  public static Insets operator /(Insets a, Insets b)
  {
    return (Insets)a.DividedBy(b);
  }

  public static Insets operator /(Insets a, int b)
  {
    return (Insets)a.DividedBy(b);
  }

  public static Insets operator -(Insets a)
  {
    return (Insets)a.Negated();
  }

  public override string ToString()
  {
    return $"{nameof(Top)}: {Top}, {nameof(Bottom)}: {Bottom}, {nameof(Left)}: {Left}, {nameof(Right)}: {Right}";
  }
}

internal class TrackedInsets(
  int? top = null,
  int? left = null,
  int? bottom = null,
  int? right = null,
  Action<string?>? callbackAction = null,
  string callbackIdentifier = "nil"
) : IInsets, ITrackable
{
  private readonly TrackableValue<int?> _bottom = new(bottom, callbackAction, "right");
  private readonly TrackableValue<int?> _left = new(left, callbackAction, "left");
  private readonly TrackableValue<int?> _right = new(right, callbackAction, "top");
  private readonly TrackableValue<int?> _top = new(top, callbackAction, "bottom");

  public TrackedInsets(int allValue = 0, Action<string?>? callbackAction = null, string callbackIdentifier = "nil") :
    this(allValue, allValue, allValue, allValue, callbackAction, callbackIdentifier) { }

  public bool ExecuteCallback { get; set; } = true;

  public int? Top
  {
    get => _top.Value;
    set => _top.Value = value;
  }

  public int? Left
  {
    get => _left.Value;
    set => _left.Value = value;
  }

  public int? Bottom
  {
    get => _bottom.Value;
    set => _bottom.Value = value;
  }

  public int? Right
  {
    get => _right.Value;
    set => _right.Value = value;
  }

  public object Clone()
  {
    return new TrackedInsets(Top, Left, Bottom, Right, callbackAction, callbackIdentifier)
    {
      ExecuteCallback = ExecuteCallback
    };
  }

  public void SetInsets(IInsets insets, bool quiet = false)
  {
    ArgumentNullException.ThrowIfNull(insets);
    SetInsets(insets.Top, insets.Left, insets.Bottom, insets.Right, quiet);
  }

  public void SetInsets(int? top, int? left, int? bottom, int? right, bool quiet = false)
  {
    _top.SetAndMark(top, runCallback: false);
    _left.SetAndMark(left, runCallback: false);
    _bottom.SetAndMark(bottom, runCallback: false);
    _right.SetAndMark(right, runCallback: false);
    if (!quiet)
    {
      Callback("all");
    }
  }

  public void SetHorizontal(int? horizontal, bool quiet = false)
  {
    SetHorizontal(horizontal, horizontal, quiet);
  }

  public void SetHorizontal(int? left, int? right, bool quiet = false)
  {
    _left.SetAndMark(left, runCallback: false);
    _right.SetAndMark(right, runCallback: false);
    if (!quiet)
    {
      Callback("all");
    }
  }

  public void SetVertical(int? vertical, bool quiet = false)
  {
    SetVertical(vertical, vertical, quiet);
  }

  public void SetVertical(int? top, int? bottom, bool quiet = false)
  {
    _top.SetAndMark(top, runCallback: false);
    _bottom.SetAndMark(bottom, runCallback: false);
    if (!quiet)
    {
      Callback("all");
    }
  }


  public bool IsDirty => _top.IsDirty || _left.IsDirty || _bottom.IsDirty || _right.IsDirty;

  public void ResetDirty()
  {
    _top.ResetDirty();
    _left.ResetDirty();
    _bottom.ResetDirty();
    _right.ResetDirty();
  }

  private void Callback(string identifier)
  {
    if (callbackAction == null || !ExecuteCallback)
    {
      return;
    }

    var raisedIdentifier = $"{callbackIdentifier}::{identifier}";
    callbackAction.Invoke(raisedIdentifier);
  }

  public static TrackedInsets operator +(TrackedInsets a, TrackedInsets b)
  {
    return (TrackedInsets)a.Plus(b);
  }

  public static TrackedInsets operator +(TrackedInsets a, int b)
  {
    return (TrackedInsets)a.Plus(b);
  }

  public static TrackedInsets operator +(int a, TrackedInsets b)
  {
    return (TrackedInsets)b.Plus(a);
  }

  public static TrackedInsets operator -(TrackedInsets a, TrackedInsets b)
  {
    return (TrackedInsets)a.Minus(b);
  }

  public static TrackedInsets operator -(TrackedInsets a, int b)
  {
    return (TrackedInsets)a.Minus(b);
  }

  public static TrackedInsets operator *(TrackedInsets a, TrackedInsets b)
  {
    return (TrackedInsets)a.Times(b);
  }

  public static TrackedInsets operator *(TrackedInsets a, int b)
  {
    return (TrackedInsets)a.Times(b);
  }

  public static TrackedInsets operator *(int a, TrackedInsets b)
  {
    return (TrackedInsets)b.Times(a);
  }

  public static TrackedInsets operator /(TrackedInsets a, TrackedInsets b)
  {
    return (TrackedInsets)a.DividedBy(b);
  }

  public static TrackedInsets operator /(TrackedInsets a, int b)
  {
    return (TrackedInsets)a.DividedBy(b);
  }

  public static TrackedInsets operator -(TrackedInsets a)
  {
    return (TrackedInsets)a.Negated();
  }

  public override string ToString()
  {
    return
      $"{nameof(Top)}: {Top}, {nameof(Left)}: {Left}, {nameof(Bottom)}: {Bottom}, {nameof(Right)}: {Right}, {nameof(IsDirty)}: {IsDirty}";
  }
}
