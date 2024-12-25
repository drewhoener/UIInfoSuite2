using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Config;

namespace UIInfoSuite2.Infrastructure.Modules.Base;

public abstract class BaseModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager) : IDisposable
{
  protected readonly ConfigManager ConfigManager = configManager;
  protected readonly IMonitor Logger = logger;
  protected readonly IModEvents ModEvents = modEvents;

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
