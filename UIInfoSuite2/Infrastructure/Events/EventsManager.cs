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
