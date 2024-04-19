using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using UIInfoSuite2.Infrastructure.Helpers.FishHelper;

namespace UIInfoSuite2.Infrastructure.Events;

public class EventInvoker
{
  private static readonly Lazy<EventInvoker> Singleton = new(() => new EventInvoker());

  private readonly PerScreen<Item?> _lastPlayerHeldItem = new(() => null);
  private bool _registered;
  public static EventInvoker Instance => Singleton.Value;

  public static void RegisterEvents(IModHelper helper)
  {
    if (Instance._registered)
    {
      return;
    }

    EventManager.PlayerToolUnequip += Instance.OnPlayerRodUnequip;
    EventManager.PlayerToolEquip += Instance.OnPlayerRodEquip;
    Instance._registered = true;
  }

#region OnTick
  private void OnPlayerRodEquip(object? sender, PlayerToolChangeEventArgs args)
  {
    if (!args.IsLocalPlayer || args.Item is not FishingRod)
    {
      return;
    }

    ModEntry.MonitorObject.Log($"Equipping rod in slot {args.Who.CurrentToolIndex}");
    FishHelper.ResetStatistics(args.Who.currentLocation);
    FishHelper.SimulateNFishingOperations(args.Who.currentLocation, 1, 500, args.Who);
  }

  private void OnPlayerRodUnequip(object? sender, PlayerToolChangeEventArgs args)
  {
    if (!args.IsLocalPlayer || args.Item is not FishingRod)
    {
      return;
    }

    ModEntry.MonitorObject.Log($"Unequipped rod in slot {args.Who.CurrentToolIndex}");
    FishHelper.ResetStatistics(args.Who.currentLocation);
    FishHelper.SimulateNFishingOperations(args.Who.currentLocation, 1, 500, args.Who);
  }
#endregion
}
