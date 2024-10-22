using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Config;

namespace UIInfoSuite2.Infrastructure.Modules;

public abstract class BaseModule : IDisposable
{
  protected readonly ConfigManager ConfigManager;
  protected readonly IMonitor Logger;
  protected readonly IModEvents ModEvents;

  public BaseModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  {
    ModEvents = modEvents;
    Logger = logger;
    ConfigManager = configManager;
  }

  protected ModConfig Config => ConfigManager.Config;

  public bool Enabled { get; protected set; }

  public void Dispose()
  {
    Disable();
    GC.SuppressFinalize(this);
  }

  public abstract bool ShouldEnable();

  public void Enable()
  {
    OnEnable();
    Enabled = true;
  }

  public void Disable()
  {
    OnDisable();
    Enabled = false;
  }

  public abstract void OnEnable();

  public abstract void OnDisable();
}
