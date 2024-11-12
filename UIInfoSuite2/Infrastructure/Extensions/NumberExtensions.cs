using System;

namespace UIInfoSuite2.Infrastructure.Extensions;

public static class NumberExtensions
{
  public static bool NearlyEqual(this double a, double b, double epsilon = double.Epsilon)
  {
    const double
      minNormal = 2.2250738585072014E-308; // equivalent to BitConverter.Int64BitsToDouble(0x0010000000000000L)

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
    const float minNormal = 1.17549435E-38f; // equivalent to BitConverter.Int32BitsToSingle(0x00800000)

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
