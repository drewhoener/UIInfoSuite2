using System.Collections.Generic;
using System.Linq;
using Netcode;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class CropStatusContainer : LayoutContainer
{
  private readonly List<TooltipIcon> _fertilizerIcons = [];
  private readonly TooltipIcon _treeFertilizerIcon;
  private readonly TooltipIcon _wateringCanIcon;
  private HoeDirt? _hoeDirt;
  private Tree? _tree;

  public CropStatusContainer(HoeDirt? dirt = null) : base("CropStatus")
  {
    ComponentSpacing = 5;
    AutoHideWhenEmpty = true;

    _treeFertilizerIcon = CreateItemIcon("805");
    _wateringCanIcon = CreateItemIcon("(T)IridiumWateringCan");

    AddChildren(_treeFertilizerIcon, _wateringCanIcon);
    IsHidden = true;
    _treeFertilizerIcon.IsHidden = true;
    _wateringCanIcon.IsHidden = true;

    HoeDirt = dirt;
  }

  public HoeDirt? HoeDirt
  {
    get => _hoeDirt;
    set => SetHoeDirt(value);
  }

  public Tree? Tree
  {
    get => _tree;
    set => SetTree(value);
  }

  private static TooltipIcon CreateItemIcon(
    string itemId,
    int finalSize = 30,
    PrimaryDimension dimension = PrimaryDimension.Width,
    string? identifier = null
  )
  {
    ParsedItemData thing = ItemRegistry.GetDataOrErrorItem(itemId);
    return new TooltipIcon(thing.GetTexture(), thing.GetSourceRect(), finalSize, dimension, identifier);
  }

  private static List<TooltipIcon> GetFertilizerIcons(HoeDirt dirtTile)
  {
    return dirtTile.fertilizer.Value == null
      ? []
      : dirtTile.fertilizer.Value.Split("|").Select(fertilizerStr => CreateItemIcon(fertilizerStr)).ToList();
  }

  private void WatchFertilizerField(NetString field, string oldValue, string newValue)
  {
    UpdateFertilizerIcons();
  }

  private void WatchWateredField(NetInt field, int oldValue, int newValue)
  {
    UpdateWateredIcon();
  }

  private void WatchTreeFertilizerField(NetBool field, bool oldValue, bool newValue)
  {
    UpdateTreeFertilizerIcon();
  }

  private void SetTree(Tree? tree)
  {
    if (tree == _tree)
    {
      return;
    }

    if (_tree != null)
    {
      _tree.fertilized.fieldChangeEvent -= WatchTreeFertilizerField;
    }

    _tree = tree;

    if (_tree != null)
    {
      _tree.fertilized.fieldChangeEvent += WatchTreeFertilizerField;
    }

    UpdateTreeFertilizerIcon();
  }

  private void SetHoeDirt(HoeDirt? hoeDirt)
  {
    if (hoeDirt == _hoeDirt)
    {
      return;
    }

    // ModEntry.LayoutDebug($"Updated Tooltip Crop: {crop.GetCropString()}");

    if (_hoeDirt != null)
    {
      _hoeDirt.fertilizer.fieldChangeEvent -= WatchFertilizerField;
      _hoeDirt.state.fieldChangeEvent -= WatchWateredField;
    }

    _hoeDirt = hoeDirt;

    if (_hoeDirt != null)
    {
      _hoeDirt.fertilizer.fieldChangeEvent += WatchFertilizerField;
      _hoeDirt.state.fieldChangeEvent += WatchWateredField;
    }

    UpdateDirt();
  }

  private void UpdateDirt()
  {
    if (HoeDirt is null)
    {
      _wateringCanIcon.IsHidden = true;
      foreach (TooltipIcon fertilizerIcon in _fertilizerIcons)
      {
        fertilizerIcon.IsHidden = true;
      }

      return;
    }

    UpdateFertilizerIcons();
    UpdateWateredIcon();
  }

  private void UpdateTreeFertilizerIcon()
  {
    _treeFertilizerIcon.IsHidden = !(Tree?.fertilized.Value ?? false);
  }

  private void UpdateFertilizerIcons()
  {
    foreach (TooltipIcon fertilizerIcon in _fertilizerIcons)
    {
      RemoveChild(fertilizerIcon);
    }

    _fertilizerIcons.Clear();

    if (HoeDirt == null)
    {
      return;
    }

    _fertilizerIcons.AddRange(GetFertilizerIcons(HoeDirt));
    AddChildren(_fertilizerIcons.ToArray<LayoutElement>());
  }

  private void UpdateWateredIcon()
  {
    _wateringCanIcon.IsHidden = !(HoeDirt?.isWatered() ?? false);
  }
}
