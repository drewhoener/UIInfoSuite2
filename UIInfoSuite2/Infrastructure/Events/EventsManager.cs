using System;

namespace UIInfoSuite2.Infrastructure.Events;

public class EventsManager
{
  public event EventHandler<EventArgs>? OnConfigChange;

  public void TriggerOnConfigChange()
  {
    OnConfigChange?.Invoke(this, EventArgs.Empty);
  }
}
