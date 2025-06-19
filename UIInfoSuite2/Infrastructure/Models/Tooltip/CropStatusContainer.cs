using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
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
  private readonly TooltipText _cropDaysRemainingElement = new(
    "UIIS2::UnknownTime",
    0.75f,
    identifier: "CropTimeRemaining"
  );

  private readonly TooltipIcon _cropIcon = new(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 40);
  private readonly List<TooltipIcon> _fertilizerIcons = [];
  private readonly TooltipIcon _treeFertilizerIcon;
  private readonly TooltipIcon _wateringCanIcon;
  private HoeDirt? _hoeDirt;
  private Tree? _tree;

  public CropStatusContainer(HoeDirt? dirt = null) : base("CropStatus")
  {
    Direction = LayoutDirection.Row;
    ComponentSpacing = 5;

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
    int finalSize = 40,
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
    UpdateHidden();
  }

  private void WatchWateredField(NetInt field, int oldValue, int newValue)
  {
    UpdateWateredIcon();
    UpdateHidden();
  }

  private void WatchTreeFertilizerField(NetBool field, bool oldValue, bool newValue)
  {
    UpdateTreeFertilizerIcon();
    UpdateHidden();
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
    UpdateHidden();
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

      UpdateHidden();
      return;
    }

    UpdateFertilizerIcons();
    UpdateWateredIcon();
    UpdateHidden();
  }

  private void UpdateHidden()
  {
    IsHidden = AllChildrenHidden();
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
