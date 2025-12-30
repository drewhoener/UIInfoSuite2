using System;
using System.Collections.Generic;
using System.Linq;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Layout.Strategy;

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

  private static readonly HashSet<string> SidedProps =
  [
    Margin,
    Padding
  ];

  public static bool IsSided(string prop)
  {
    return SidedProps.Contains(prop);
  }

  public static (string, string, string, string) GetSidedProps(string prop)
  {
    return ($"{prop}Top", $"{prop}Right", $"{prop}Bottom", $"{prop}Left");
  }
}

internal class PropertyCache : Dictionary<string, TrackableValue<object>>
{
  public Action<string?>? CallbackAction { get; set; }

  public string DefaultString { get; set; } = "";
  public int DefaultInt { get; set; } = 0;
  public bool DefaultBool { get; set; } = false;

  // Convenience methods for common insets
  public IInsets Margin => GetAsInsets(Property.Margin);
  public IInsets Padding => GetAsInsets(Property.Padding);
  public int MarginVertical => Margin.VerticalTotal();
  public int MarginHorizontal => Margin.HorizontalTotal();

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
    if (TryGetValue(key, out TrackableValue<object>? existingValue))
    {
      existingValue.SetAndMark(value);
    }
    else
    {
      var newVal = new TrackableValue<object>(value, CallbackAction, $"prop-{key}");
      Add(key, newVal);
      newVal.Mark();
    }
  }

  public string GetAsString(string propertyKey)
  {
    return Get(propertyKey, DefaultString);
  }

  public int GetAsInt(string propertyKey)
  {
    return Get(propertyKey, DefaultInt);
  }

  public bool GetAsBool(string propertyKey)
  {
    return Get(propertyKey, DefaultBool);
  }

  public TEnum GetAsEnum<TEnum>(string propertyKey, TEnum defaultValue) where TEnum : struct, Enum
  {
    object value = Get<object>(propertyKey, defaultValue);
    return value switch
    {
      TEnum enumValue => enumValue,
      string str when Enum.TryParse(str, true, out TEnum parsed) => parsed,
      int intValue when Enum.IsDefined(typeof(TEnum), intValue) => (TEnum)(object)intValue,
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

internal abstract class LayoutStrategy
{
  public PropertyCache Properties { get; } = new();

  public abstract void ArrangeChildren(LayoutContainer container, List<LayoutElement> visibleChildren);
  public abstract Dimensions MeasureContent(LayoutContainer container, List<LayoutElement> visibleChildren);
}

internal class FlexLayoutStrategy : LayoutStrategy
{
  public override void ArrangeChildren(LayoutContainer container, List<LayoutElement> visibleChildren)
  {
    if (visibleChildren.IsEmpty())
    {
      return;
    }

    FlexDirection direction = Properties.GetAsEnum(Property.FlexDirection, FlexDirection.Row);
    int gap = Properties.GetAsInt(Property.Gap);

    bool isRow = direction is FlexDirection.Row or FlexDirection.RowReverse;

    int totalChildrenSize = visibleChildren.Sum(child => isRow ? child.Bounds.Width : child.Bounds.Height);
    int totalGaps = gap * (visibleChildren.Count - 1);
    int availableSpace = (isRow ? container.ContentSize.Width : container.ContentSize.Height) -
                         totalChildrenSize -
                         totalGaps;
  }

  public override Dimensions MeasureContent(LayoutContainer container, List<LayoutElement> visibleChildren)
  {
    throw new NotImplementedException();
  }

  private List<int> CalculateMainAxisPositions(
    JustifyContent justify,
    int childCount,
    int totalSize,
    int containerSize,
    int gap
  )
  {
    var positions = new List<int>(childCount);
    int availableSpace = containerSize - totalSize;
  }
}

// using System;
// using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
//
// namespace UIInfoSuite2.Infrastructure.Models.Layout.Strategy;
//
// internal abstract class LayoutStrategy(LayoutElement owner)
// {
//   protected readonly LayoutElement Owner = owner;
//
//   // For measuring the element's own content (text, sprites, etc)
//   public abstract Dimensions MeasureOwnContent(int? widthConstraint = null);
//
//   // For arranging the element's own content within its bounds
//   public abstract void ArrangeOwnContent();
//
//   // For measuring child elements (only used by container strategies)
//   public virtual Dimensions MeasureChildren(int? widthConstraint = null) => Dimensions.Empty;
//
//   // For arranging child elements (only used by container strategies)
//   public virtual void ArrangeChildren() { }
//
//   // Combines own content and children measurements
//   public Dimensions MeasureContent()
//   {
//
//     // First pass - measure without constraints to get natural sizes
//     var initialSize = new Dimensions(
//       Math.Max(MeasureOwnContent().Width, MeasureChildren().Width),
//       Math.Max(MeasureOwnContent().Height, MeasureChildren().Height)
//     );
//
//     // Second pass - measure with width constraint
//     var parentWidth = Owner.Parent?.ContentSize.Width;
//     return new Dimensions(
//       Math.Max(MeasureOwnContent(parentWidth).Width, MeasureChildren(parentWidth).Width),
//       Math.Max(MeasureOwnContent(parentWidth).Height, MeasureChildren(parentWidth).Height)
//     );
//   }
// }
//
// // Base strategy for elements with just content
// internal abstract class ElementLayoutStrategy(LayoutElement owner) : LayoutStrategy(owner)
// {
//   // Override with NotSupportedException or return empty to make it clear
//   // these strategies don't handle children
//   public sealed override Dimensions MeasureChildren() =>
//     throw new NotSupportedException("Element strategies do not support child measurement");
//
//   public sealed override void ArrangeChildren() =>
//     throw new NotSupportedException("Element strategies do not support child arrangement");
// }
//
// // Base strategy for containers
// internal abstract class ContainerLayoutStrategy : LayoutStrategy
// {
//   protected ContainerLayoutStrategy(LayoutContainer owner) : base(owner) { }
//
//   // Make it clear these must be implemented for containers
//   public abstract override Dimensions MeasureChildren();
//   public abstract override void ArrangeChildren();
//
//   // Most containers don't have their own content
//   public override Dimensions MeasureOwnContent() => Dimensions.Empty;
//   public override void ArrangeOwnContent() { }
// }
