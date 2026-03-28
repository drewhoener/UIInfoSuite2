using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.Inventories;
using StardewValley.Objects;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip.BuildingContainers;

internal class BuildingChestTooltipContainer : LayoutContainer
{
  private readonly TooltipText _buildingInputs = new("UIIS2::UnknownContent", 0.75f, identifier: "BuildingInputs");
  private readonly TooltipText _buildingOutputs = new("UIIS2::UnknownContent", 0.75f, identifier: "BuildingOutputs");
  private Building? _building;
  private List<Chest> _inputChests = [];
  private List<Chest> _outputChests = [];

  public BuildingChestTooltipContainer(Building? building = null) : base("BuildingContainersTooltip")
  {
    Direction = LayoutDirection.Column;
    ComponentSpacing = 0;
    Margin.SetAll(0);
    AddChildren(_buildingInputs, _buildingOutputs);
    IsHidden = true;

    Building = building;
  }

  public Building? Building
  {
    get => _building;
    set => SetBuilding(value);
  }

  private void ClearChests(BuildingChestType chestType)
  {
    if (chestType == BuildingChestType.Chest)
    {
      return;
    }

    List<Chest> chests = chestType == BuildingChestType.Load ? _inputChests : _outputChests;
    foreach (Chest chest in chests)
    {
      chest.Items.OnInventoryReplaced -= OnItemsReplaced;
      chest.Items.OnSlotChanged -= OnSlotChanged;
    }

    chests.Clear();
  }

  private void SetChests(IEnumerable<Chest> newChests, BuildingChestType chestType)
  {
    if (chestType == BuildingChestType.Chest)
    {
      return;
    }

    ClearChests(chestType);
    List<Chest> chests;
    if (chestType == BuildingChestType.Load)
    {
      _inputChests = new List<Chest>(newChests);
      chests = _inputChests;
    }
    else
    {
      _outputChests = new List<Chest>(newChests);
      chests = _outputChests;
    }

    foreach (Chest chest in chests)
    {
      chest.Items.OnInventoryReplaced += OnItemsReplaced;
      chest.Items.OnSlotChanged += OnSlotChanged;
    }
  }

  private void OnItemsReplaced(Inventory inventory, IList<Item> before, IList<Item> after)
  {
    UpdateBuildingChestData();
  }

  private void OnSlotChanged(Inventory inventory, int index, Item before, Item after)
  {
    UpdateBuildingChestData();
  }

  [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible")]
  private void SetBuilding(Building? newBuilding)
  {
    if (ReferenceEquals(_building, newBuilding))
    {
      return;
    }

    if (newBuilding?.buildingChests.Count <= 0)
    {
      newBuilding = null;
    }

    _building = newBuilding;
    if (Building is null)
    {
      ModEntry.LayoutDebug("Building is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;

    SetChests(MachineHelper.GetBuildingChestsFromType(Building, BuildingChestType.Load), BuildingChestType.Load);
    SetChests(MachineHelper.GetBuildingChestsFromType(Building, BuildingChestType.Collect), BuildingChestType.Collect);
    UpdateBuildingChestData();
  }

  private static Dictionary<string, int> GetItemCountMap(IEnumerable<Item?> items)
  {
    Dictionary<string, int> itemCounter = new();
    foreach (Item? item in items)
    {
      if (item is null)
      {
        continue;
      }

      int count = itemCounter.GetOrDefault(item.DisplayName, 0) + item.Stack;
      itemCounter[item.DisplayName] = count;
    }

    return itemCounter;
  }


  private void UpdateBuildingChestData()
  {
    Dictionary<string, int> inputItems = GetItemCountMap(_inputChests.SelectMany(chest => chest.Items));
    Dictionary<string, int> outputItems = GetItemCountMap(_outputChests.SelectMany(chest => chest.Items));

    List<string> entries = [];
    _buildingInputs.IsHidden = true;
    _buildingOutputs.IsHidden = true;

    if (inputItems.Count > 0)
    {
      _buildingInputs.IsHidden = false;
      entries.Add($"{I18n.MachineProcessing()}:");
      foreach ((string displayName, int count) in inputItems)
      {
        entries.Add($"{displayName} x{count}");
      }

      _buildingInputs.Text = string.Join("\n", entries);
    }

    if (outputItems.Count <= 0)
    {
      return;
    }

    entries.Clear();
    _buildingOutputs.IsHidden = false;
    entries.Add($"{I18n.MachineDone()}:");
    foreach ((string displayName, int count) in outputItems)
    {
      entries.Add($"{displayName} x{count}");
    }

    _buildingOutputs.Text = string.Join("\n", entries);
  }
}
