using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Events;

public class EventManager
{
  public static event EventHandler<PlayerItemChangeEventArgs>? PlayerItemEquip;
  public static event EventHandler<PlayerItemChangeEventArgs>? PlayerItemUnequip;
  public static event EventHandler<PlayerToolChangeEventArgs>? PlayerToolEquip;
  public static event EventHandler<PlayerToolChangeEventArgs>? PlayerToolUnequip;

  public static void InvokePlayerItemChange(Farmer who, Item? item, ToolChangeAction action)
  {
    if (item is Tool tool)
    {
      InvokePlayerToolChange(who, tool, action);
      return;
    }

    PlayerItemChangeEventArgs itemChangeEventArgs;
    if (action == ToolChangeAction.Equip && PlayerItemEquip != null)
    {
      itemChangeEventArgs = new PlayerItemChangeEventArgs(who, item, action);
      InvokeEvent("PlayerItemChangeEvent", PlayerItemEquip.GetInvocationList(), null, itemChangeEventArgs);
      return;
    }

    if (action != ToolChangeAction.Unequip || PlayerItemUnequip == null)
    {
      return;
    }

    itemChangeEventArgs = new PlayerItemChangeEventArgs(who, item, action);
    InvokeEvent("PlayerItemChangeEvent", PlayerItemUnequip.GetInvocationList(), null, itemChangeEventArgs);
  }

  public static void InvokePlayerToolChange(Farmer who, Tool? item, ToolChangeAction action)
  {
    PlayerToolChangeEventArgs toolChangeEventArgs;
    if (action == ToolChangeAction.Equip && PlayerToolEquip != null)
    {
      toolChangeEventArgs = new PlayerToolChangeEventArgs(who, item, action);
      InvokeEvent("PlayerToolChangeEvent", PlayerToolEquip.GetInvocationList(), null, toolChangeEventArgs);
      return;
    }

    if (action != ToolChangeAction.Unequip || PlayerToolUnequip == null)
    {
      return;
    }

    toolChangeEventArgs = new PlayerToolChangeEventArgs(who, item, action);
    InvokeEvent("PlayerToolChangeEvent", PlayerToolUnequip.GetInvocationList(), null, toolChangeEventArgs);
  }


  // Stolen from SpaceCore, which stole it from SMAPI
  public static void InvokeEvent(string name, IEnumerable<Delegate> handlers, object? sender)
  {
    var args = EventArgs.Empty;
    foreach (EventHandler handler in handlers.Cast<EventHandler>())
    {
      try
      {
        handler.Invoke(sender, args);
      }
      catch (Exception e)
      {
        ModEntry.MonitorObject.Log($"Exception while handling event {name}:\n{e}", LogLevel.Error);
      }
    }
  }

  public static void InvokeEvent<T>(string name, IEnumerable<Delegate> handlers, object? sender, T args)
  {
    foreach (EventHandler<T> handler in handlers.Cast<EventHandler<T>>())
    {
      try
      {
        handler.Invoke(sender, args);
      }
      catch (Exception e)
      {
        ModEntry.MonitorObject.Log($"Exception while handling event {name}:\n{e}", LogLevel.Error);
      }
    }
  }
}
