using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace UIInfoSuite2.Infrastructure.Modules;

public abstract class BaseModule : IDisposable
{
  protected readonly IMonitor Logger;
  protected readonly IModEvents ModEvents;

  public BaseModule(IModEvents modEvents, IMonitor logger)
  {
    ModEvents = modEvents;
    Logger = logger;
  }

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
