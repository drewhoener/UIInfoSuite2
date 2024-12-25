using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Events.Args;

namespace UIInfoSuite2.Infrastructure.Events;

public class EventsManager
{
  public event EventHandler<EventArgs>? OnConfigChange;
  public event EventHandler<RenderingActiveMenuPostBackgroundArgs>? OnRenderingActiveMenuPostBackground;

  public void TriggerOnConfigChange()
  {
    OnConfigChange?.Invoke(this, EventArgs.Empty);
  }

  public void TriggerOnRenderingActiveMenuPostBackground(GameMenu menu, SpriteBatch spriteBatch)
  {
    OnRenderingActiveMenuPostBackground?.Invoke(this, new RenderingActiveMenuPostBackgroundArgs(menu, spriteBatch));
  }
}
