using System.Diagnostics.CodeAnalysis;

namespace UIInfoSuite2.Infrastructure.Extensions;

internal static class StringExtensions
{
  public static string Format(this string str, [NotNull] params object?[] args)
  {
    return string.Format(str, args);
  }
}
