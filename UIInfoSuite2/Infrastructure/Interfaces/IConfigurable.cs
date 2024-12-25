using System;
using StardewModdingAPI;
using UIInfoSuite2.Compatibility;

namespace UIInfoSuite2.Infrastructure.Interfaces;

public interface IConfigurable : IComparable<IConfigurable>, IComparable
{
  int IComparable.CompareTo(object? obj)
  {
    if (obj is null)
    {
      return 1;
    }

    if (obj is not IConfigurable config)
    {
      throw new ArgumentException($"Object must be of type {nameof(IConfigurable)}");
    }

    return CompareTo(config);
  }

  int IComparable<IConfigurable>.CompareTo(IConfigurable? other)
  {
    if (other is null)
    {
      return 1;
    }

    // First compare by order
    int orderComparison = GetOrder().CompareTo(other.GetOrder());
    if (orderComparison != 0)
    {
      return orderComparison;
    }

    int sectionComparison = string.Compare(GetConfigSection(), other.GetConfigSection(), StringComparison.Ordinal);
    return sectionComparison != 0
      ? sectionComparison
      : string.Compare(GetSubHeader(), other.GetSubHeader(), StringComparison.Ordinal);
  }

  int GetOrder()
  {
    return 0;
  }

  string? GetConfigPage()
  {
    return null;
  }

  string GetConfigSection();

  string? GetSubHeader()
  {
    return null;
  }

  void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest);
}
