using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Extensions;

internal static class LayoutElementExtensions
{
  /// <summary>Sets uniform padding on all sides and returns the element for chaining.</summary>
  public static T WithPadding<T>(this T element, int all) where T : LayoutElement
  {
    element.Padding.SetAll(all);
    return element;
  }

  /// <summary>Sets padding per-side and returns the element for chaining.</summary>
  public static T WithPadding<T>(this T element, int top, int left, int bottom, int right) where T : LayoutElement
  {
    element.Padding.SetInsets(top, left, bottom, right);
    return element;
  }

  /// <summary>Sets uniform margin on all sides and returns the element for chaining.</summary>
  public static T WithMargin<T>(this T element, int all) where T : LayoutElement
  {
    element.Margin.SetAll(all);
    return element;
  }

  /// <summary>Sets margin per-side and returns the element for chaining.</summary>
  public static T WithMargin<T>(this T element, int top, int left, int bottom, int right) where T : LayoutElement
  {
    element.Margin.SetInsets(top, left, bottom, right);
    return element;
  }

  /// <summary>Sets only the top margin and returns the element for chaining.</summary>
  public static T WithMarginTop<T>(this T element, int top) where T : LayoutElement
  {
    element.Margin.Top = top;
    return element;
  }
}
