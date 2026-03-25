using System;
using System.Collections.Generic;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Layout.Strategy;

/// <summary>
///   String constants for well-known layout properties. Used by custom strategies that choose to
///   implement dynamic property storage via <see cref="PropertyCache" />.
/// </summary>
public static class Property
{
  public const string Gap = "gap";
  public const string FlexDirection = "flexDirection";
  public const string FlexWrap = "flexWrap";
  public const string FlexGrow = "flexGrow";
  public const string FlexShrink = "flexShrink";
  public const string FlexBasis = "flexBasis";
  public const string AlignItems = "alignItems";
  public const string AlignSelf = "alignSelf";
  public const string JustifyContent = "justifyContent";
  public const string Order = "order";
  public const string Margin = "margin";
  public const string MarginTop = $"{Margin}Top";
  public const string MarginBottom = $"{Margin}Bottom";
  public const string MarginLeft = $"{Margin}Left";
  public const string MarginRight = $"{Margin}Right";
  public const string Padding = "padding";
  public const string PaddingTop = $"{Padding}Top";
  public const string PaddingBottom = $"{Padding}Bottom";
  public const string PaddingLeft = $"{Padding}Left";
  public const string PaddingRight = $"{Padding}Right";
  public const string Width = "width";
  public const string Height = "height";
  public const string MinWidth = "minWidth";

  private static readonly HashSet<string> SidedProps = [Margin, Padding];

  public static bool IsSided(string prop) => SidedProps.Contains(prop);

  public static (string top, string right, string bottom, string left) GetSidedProps(string prop) =>
    ($"{prop}Top", $"{prop}Right", $"{prop}Bottom", $"{prop}Left");
}

/// <summary>
///   Optional dynamic property bag for custom <see cref="LayoutStrategy" /> implementations that prefer
///   a string-keyed property system over typed C# properties.
/// </summary>
internal class PropertyCache : Dictionary<string, TrackableValue<object>>
{
  public string DefaultString { get; set; } = "";
  public int DefaultInt { get; set; } = 0;
  public bool DefaultBool { get; set; } = false;

  private T Get<T>(string key, T defaultValue)
  {
    if (!TryGetValue(key, out TrackableValue<object>? value))
    {
      return defaultValue;
    }

    return value.Value is T typedValue ? typedValue : defaultValue;
  }

  public void Set(string key, object value)
  {
    if (TryGetValue(key, out TrackableValue<object>? existing))
    {
      existing.SetAndMark(value);
    }
    else
    {
      var newVal = new TrackableValue<object>(value, debugIdentifier: $"prop-{key}");
      Add(key, newVal);
      newVal.Mark();
    }
  }

  public string GetAsString(string key) => Get(key, DefaultString);
  public int GetAsInt(string key) => Get(key, DefaultInt);
  public bool GetAsBool(string key) => Get(key, DefaultBool);

  public TEnum GetAsEnum<TEnum>(string key, TEnum defaultValue) where TEnum : struct, Enum
  {
    object raw = Get<object>(key, defaultValue);
    return raw switch
    {
      TEnum e => e,
      string s when Enum.TryParse(s, true, out TEnum parsed) => parsed,
      int i when Enum.IsDefined(typeof(TEnum), i) => (TEnum)(object)i,
      _ => defaultValue
    };
  }

  public IInsets GetAsInsets(string propertyKey)
  {
    (string top, string right, string bottom, string left) = Property.GetSidedProps(propertyKey);
    return new Insets
    {
      Top = GetAsInt(top), Right = GetAsInt(right), Bottom = GetAsInt(bottom), Left = GetAsInt(left)
    };
  }
}

/// <summary>
///   Base class for all container layout strategies. A strategy is responsible for two phases:
///   <list type="bullet">
///     <item><description><see cref="MeasureContent"/> — computes how much space the children collectively require.</description></item>
///     <item><description><see cref="ArrangeChildren"/> — positions each child within the measured space and populates the visible-children list for the draw pass.</description></item>
///   </list>
///   The container calls each phase in order during its own layout cycle.
/// </summary>
internal abstract class LayoutStrategy
{
  /// <summary>
  ///   Measures the space required by <paramref name="allChildren" />. Called during the
  ///   <c>UpdateBounds</c> phase. Implementations should call <c>child.Layout()</c> on each
  ///   child so they measure themselves before reading <c>child.Bounds.Size</c>.
  /// </summary>
  public abstract Dimensions MeasureContent(IReadOnlyList<LayoutElement> allChildren);

  /// <summary>
  ///   Positions children within <paramref name="contentSize" /> and populates
  ///   <paramref name="visibleChildren" /> with every child that should be drawn this frame.
  ///   Implementations must call <c>visibleChildren.Clear()</c> at the start.
  /// </summary>
  public abstract void ArrangeChildren(
    Dimensions contentSize,
    IReadOnlyList<LayoutElement> allChildren,
    List<LayoutElement> visibleChildren
  );
}
