using System;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Events;

public enum ToolChangeAction
{
  Equip,
  Unequip,
  Unknown
}

public class PlayerToolChangeEventArgs : PlayerItemChangeEventArgs<Tool>
{
  public PlayerToolChangeEventArgs(Farmer who, Tool? item, ToolChangeAction action) : base(who, item, action) { }
}

public class PlayerItemChangeEventArgs : PlayerItemChangeEventArgs<Item>
{
  public PlayerItemChangeEventArgs(Farmer who, Item? item, ToolChangeAction action) : base(who, item, action) { }
}

public class PlayerItemChangeEventArgs<T> : EventArgs where T : Item
{
  public PlayerItemChangeEventArgs(Farmer who, T? item, ToolChangeAction action)
  {
    Who = who;
    Item = item;
    Action = action;
  }

  public Farmer Who { get; }
  public T? Item { get; init; }
  public ToolChangeAction Action { get; init; }
  public bool IsLocalPlayer => Who.IsLocalPlayer;
}
