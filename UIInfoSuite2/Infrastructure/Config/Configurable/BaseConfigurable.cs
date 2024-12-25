using StardewModdingAPI;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Config.Configurable;

public abstract class BaseConfigurable(ConfigManager configManager) : IConfigurable
{
  protected ModConfig Config => configManager.Config;

  public virtual int GetOrder()
  {
    return 0;
  }

  public virtual string? GetConfigPage()
  {
    return null;
  }

  public virtual string? GetSubHeader()
  {
    return null;
  }

  public abstract string GetConfigSection();
  public abstract void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest);
}
