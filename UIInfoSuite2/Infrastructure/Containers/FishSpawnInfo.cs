using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;

namespace UIInfoSuite2.Infrastructure.Containers;

// Rider lies to me when I debug a HashSet, so back it with a List in debug builds
#if DEBUG
using SpawnBlockersContainer = List<FishSpawnBlockedReason>;

#else
using SpawnBlockersContainer = HashSet<FishSpawnBlockedReason>;
#endif

public enum FishSpawnBlockedReason
{
  WrongFishingArea,
  WrongPlayerPos,
  WrongBobberPos,
  WrongGameState,
  OverCatchLimit,
  WrongSeason,
  WrongTime,
  RequiresRain,
  RequiresSun,
  PlayerLevelTooLow,
  PlayerRodTooWeak,
  WaterTooShallow,
  WaterTooDeep,
  RequiresMagicBait,
  InvalidFormat,
  TutorialCatch,
  Unknown
}

public abstract class FishSpawnInfo
{
  protected static readonly Dictionary<string, Item> CommonFishLookupCache = new();
  private readonly MovingAverage _entryPickedChanceAverage = new();
  private readonly MovingAverage _spawnProbabilityAverage = new();
  protected readonly Dictionary<string, Item> LookupItems = new();

  protected bool ResetItemCacheNextLookup;

  protected FishSpawnInfo()
  {
    ResetSpawnChecks();
    ResetSpawnProbability();
    ResetEntryPickedChance();
  }

  public ICollection<FishSpawnBlockedReason> SpawnBlockReasons { get; } = new SpawnBlockersContainer();

  public abstract string Id { get; }
  public abstract int Precedence { get; }
  public abstract SpawnFishData? SpawnData { get; }

  public virtual bool CouldSpawn => SpawnBlockReasons.Count == 0;
  public virtual bool IsSpawnConditionUnknown => SpawnBlockReasons.Contains(FishSpawnBlockedReason.Unknown);
  public virtual bool IsSpawnConditionOnlyUnknown => SpawnBlockReasons.Count == 1 && IsSpawnConditionUnknown;

  public bool IsOnlyNonFishItems { get; set; } = true;

  public double SpawnProbability
  {
    get => _spawnProbabilityAverage.Avg;
    set => _spawnProbabilityAverage.AddValue(value);
  }

  public double EntryPickedChance
  {
    get => _entryPickedChanceAverage.Avg;
    set => _entryPickedChanceAverage.AddValue(value);
  }

  public double ChanceToSpawn
  {
    get
    {
      if (IsOnlyNonFishItems)
      {
        return EntryPickedChance;
      }

      return EntryPickedChance * SpawnProbability;
    }
  }

  public double ChanceToNotSpawn => 1.0 - ChanceToSpawn;

  public void ResetResolvedItems()
  {
    ResetItemCacheNextLookup = true;
  }

  public void ResetSpawnChecks()
  {
    SpawnBlockReasons.Clear();
    SpawnBlockReasons.Add(FishSpawnBlockedReason.Unknown);
  }

  public void ResetSpawnProbability()
  {
    _spawnProbabilityAverage.Reset();
  }

  public void ResetEntryPickedChance()
  {
    _entryPickedChanceAverage.Reset();
  }

  public void AddBlockedReason(FishSpawnBlockedReason reason)
  {
    // Again, the debug type for HashSet in Rider doesn't seem to work properly right now
    // so just remove it if it already exists.
#if DEBUG
    SpawnBlockReasons.Remove(reason);
#endif

    SpawnBlockReasons.Add(reason);
    // ModEntry.LogExDebug_2($"Adding Blocker {reason}");

    if (reason != FishSpawnBlockedReason.Unknown && SpawnBlockReasons.Remove(FishSpawnBlockedReason.Unknown))
    {
      // ModEntry.LogExDebug_2($"\tRemoved unknown state from list. Count: {SpawnBlockReasons.Count}");
    }
  }

  /// <summary>
  ///   Separate out the logic for creating items via resolver vs just a standard create.
  ///   ItemQueryResolver.TryResolve is a much more expensive method than just creating a single item, so try
  ///   to do that if we can.
  /// </summary>
  /// <param name="queryContext">The query context used to resolve the items</param>
  /// <param name="bobberTile">The tile the bobber is on</param>
  /// <param name="waterDepth">Tiles away from land</param>
  /// <param name="forceReloadCache">If we need to reload the cache (shouldn't happen but who knows)</param>
  public abstract void PopulateItemsForTile(
    ItemQueryContext queryContext,
    Vector2 bobberTile,
    int waterDepth,
    bool forceReloadCache = false
  );

  public virtual string GetDisplayName()
  {
    return string.Join(", ", GetItems().Select(item => item.DisplayName));
  }


  public virtual void SetSpawnAllowed()
  {
    SpawnBlockReasons.Clear();
  }

  public IEnumerable<Item> GetItems()
  {
    return LookupItems.Values;
  }

  public override string ToString()
  {
    return
      $"DisplayName: {GetDisplayName()}, {nameof(EntryPickedChance)}: {EntryPickedChance}, {nameof(SpawnProbability)}: {SpawnProbability}, Spawnable: {CouldSpawn}, {nameof(SpawnBlockReasons)}: [{string.Join(", ", SpawnBlockReasons)}]";
  }
}

public class FishSpawnInfoFromItem : FishSpawnInfo
{
  private readonly Item _item;

  public FishSpawnInfoFromItem(Item item, int precedence)
  {
    _item = item;
    Precedence = precedence;
  }

  public override string Id => _item.QualifiedItemId;
  public override int Precedence { get; }
  public override SpawnFishData? SpawnData => null;

  public override void PopulateItemsForTile(
    ItemQueryContext queryContext,
    Vector2 bobberTile,
    int waterDepth,
    bool forceReloadCache = false
  )
  {
    if (LookupItems.Count != 0 && !forceReloadCache && !ResetItemCacheNextLookup)
    {
      return;
    }

    LookupItems.Clear();
    ResetItemCacheNextLookup = false;

    LookupItems.Add(Id, _item);
  }
}

public class FishSpawnInfoFromData : FishSpawnInfo
{
  public FishSpawnInfoFromData(SpawnFishData spawnFishData)
  {
    SpawnData = spawnFishData;
  }

  public override string Id => SpawnData.Id;
  public override int Precedence => SpawnData.Precedence;
  public override SpawnFishData SpawnData { get; }

  public override void PopulateItemsForTile(
    ItemQueryContext queryContext,
    Vector2 bobberTile,
    int waterDepth,
    bool forceReloadCache = false
  )
  {
    if (LookupItems.Count != 0 && !forceReloadCache && !ResetItemCacheNextLookup)
    {
      return;
    }

    LookupItems.Clear();
    ResetItemCacheNextLookup = false;

    var resolved = false;

    if (SpawnData.RandomItemId != null)
    {
      foreach (string itemId in SpawnData.RandomItemId)
      {
        // Try and use the cache to avoid a ton of CreateItem calls when iterating
        if (CommonFishLookupCache.TryGetValue(itemId, out Item? cachedItem))
        {
          LookupItems.TryAdd(itemId, cachedItem);
          continue;
        }

        ParsedItemData? itemData = ItemRegistry.GetData(itemId);
        Item? itemInstance = itemData?.ItemType.CreateItem(itemData);
        if (itemInstance == null)
        {
          continue;
        }

        // Add the item to the cache
        CommonFishLookupCache.TryAdd(itemId, itemInstance);
        LookupItems.TryAdd(itemId, itemInstance);
      }

      if (LookupItems.Count == 0)
      {
        ModEntry.MonitorObject.Log(
          $"SpawnData {Id} was supposed to output multiple items, but did nothing.",
          LogLevel.Error
        );
      }
      else
      {
        resolved = true;
      }
    }
    else if (ItemRegistry.IsQualifiedItemId(Id) && ItemRegistry.IsQualifiedItemId(SpawnData.ItemId))
    {
      // Try and use the cache to avoid a ton of CreateItem calls when iterating
      if (CommonFishLookupCache.TryGetValue(SpawnData.ItemId, out Item? cachedItem))
      {
        LookupItems.TryAdd(SpawnData.ItemId, cachedItem);
      }
      else
      {
        ParsedItemData? itemData = ItemRegistry.GetData(SpawnData.ItemId);
        if (itemData != null)
        {
          Item? itemInstance = itemData.ItemType.CreateItem(itemData);
          if (itemInstance == null)
          {
            ModEntry.MonitorObject.Log(
              $"SpawnData {Id} is supposed to have a defined item, but might be an item query.",
              LogLevel.Error
            );
          }
          else
          {
            resolved = true;
            // Add to the cache
            CommonFishLookupCache.TryAdd(SpawnData.ItemId, itemInstance);
            LookupItems.TryAdd(SpawnData.ItemId, itemInstance);
          }
        }
      }
    }

    if (resolved)
    {
      return;
    }

    // If we really have to, fall back to the item resolver.
    // Try and avoid if we can, this is an expensive operation
    IList<ItemQueryResult>? items = ItemQueryResolver.TryResolve(SpawnData, queryContext, formatItemId: FormatItemId);
    foreach (ItemQueryResult itemQueryResult in items)
    {
      if (itemQueryResult.Item is not Item item)
      {
        continue;
      }

      CommonFishLookupCache.TryAdd(item.QualifiedItemId, item);
      LookupItems.TryAdd(item.QualifiedItemId, item);
    }

    return;


    string FormatItemId(string query)
    {
      return query.Replace("BOBBER_X", ((int)bobberTile.X).ToString())
                  .Replace("BOBBER_Y", ((int)bobberTile.Y).ToString())
                  .Replace("WATER_DEPTH", waterDepth.ToString());
    }
  }
}
