using System.Collections.Generic;
using SimpleInjector;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.UIElements;
using UIInfoSuite2.UIElements.MenuShortcuts.MenuShortcutDisplay;

namespace UIInfoSuite2.Infrastructure.Modules;

public class ModuleManager
{
  private readonly Container _container;

  public ModuleManager(Container container)
  {
    _container = container;

    container.Collection.Register<BaseModule>(
      new[] { typeof(LuckOfDay), typeof(MenuShortcutDisplay) },
      Lifestyle.Singleton
    );
  }

  public IEnumerable<BaseModule> GetAllModules()
  {
    return _container.GetAllInstances<BaseModule>();
  }

  public void OnConfigSave()
  {
    foreach (BaseModule module in GetAllModules())
    {
      if (!module.Enabled && module.ShouldEnable())
      {
        module.Enable();
      }

      if (module.Enabled && !module.ShouldEnable())
      {
        module.Disable();
      }
    }
  }

  public void OnReturnToMenu()
  {
    foreach (BaseModule module in GetAllModules())
    {
      module.Disable();
    }
  }

  public void OnGameLoaded()
  {
    foreach (BaseModule module in GetAllModules())
    {
      if (module.ShouldEnable())
      {
        module.Enable();
      }
    }
  }
}
