using System;

namespace UIInfoSuite2.Infrastructure.Extensions;

public static class NumberExtensions
{
  /// <summary>
  /// Safe comparison for floating point numbers.
  /// </summary>
  /// <see href="https://stackoverflow.com/a/3875619"/>
  /// <param name="a">First Number</param>
  /// <param name="b">Second Number</param>
  /// <param name="epsilon">Epsilon value for comparison, defaults to double.Epsilon</param>
  /// <returns>If the numbers are equal within the confines of the data type's min-normal value.</returns>
  public static bool NearlyEqual(this double a, double b, double epsilon = double.Epsilon)
  {
    const double minNormal = double.Epsilon;
    double absA = Math.Abs(a);
    double absB = Math.Abs(b);
    double diff = Math.Abs(a - b);

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    if (a == b) // shortcut, handles infinities
    {
      return true;
    }

    if (a == 0 || b == 0 || absA + absB < minNormal)
    {
      // a or b is zero or both are extremely close to it
      // relative error is less meaningful here
      return diff < epsilon * minNormal;
    }

    // use relative error
    return diff / Math.Min(absA + absB, double.MaxValue) < epsilon;
  }

  public static bool NearlyEqual(this float a, float b, float epsilon = float.Epsilon)
  {
    const float minNormal = float.Epsilon;
    float absA = Math.Abs(a);
    float absB = Math.Abs(b);
    float diff = Math.Abs(a - b);

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    if (a == b) // shortcut, handles infinities
    {
      return true;
    }

    if (a == 0 || b == 0 || absA + absB < minNormal)
    {
      // a or b is zero or both are extremely close to it
      // relative error is less meaningful here
      return diff < epsilon * minNormal;
    }

    // use relative error
    return diff / Math.Min(absA + absB, float.MaxValue) < epsilon;
  }
}
