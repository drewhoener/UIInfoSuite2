using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Events.Args;

namespace UIInfoSuite2.Infrastructure.Events;

public class EventsManager
{
  public event EventHandler<EventArgs>? OnConfigChange;
  public event EventHandler<RenderingMenuContentStepArgs>? OnRenderingMenuContentStep;

  public void TriggerOnConfigChange()
  {
    OnConfigChange?.Invoke(this, EventArgs.Empty);
  }

  public void TriggerOnRenderingMenuContentStep(IClickableMenu menu, SpriteBatch spriteBatch)
  {
    OnRenderingMenuContentStep?.Invoke(this, new RenderingMenuContentStepArgs(menu, spriteBatch));
  }
}

#if DEBUG
public static class HotReloadService
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
  public static event Action<Type[]?>? UpdateApplicationEvent;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

  internal static void ClearCache(Type[]? types) { }

  internal static void UpdateApplication(Type[]? types)
  {
    UpdateApplicationEvent?.Invoke(types);
  }
}

#endif
