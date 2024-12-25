using UIInfoSuite2.Compatibility;

namespace UIInfoSuite2.Infrastructure.Interfaces;

public interface IConfigurable
{
  string? GetConfigSection();
  string GetSubHeader();
  void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi);
}
