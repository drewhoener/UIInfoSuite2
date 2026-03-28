using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class FruitTreeTooltipContainer : LayoutContainer
{
  private readonly TooltipText _cropDaysRemainingElement = new(
    "UIIS2::UnknownTime",
    0.75f,
    identifier: "TreeGrowTimeRemaining"
  );

  private readonly TooltipText _cropDropsElement = new("UIIS2::UnknownDrops", 0.75f, identifier: "TreeDrops");

  private readonly TooltipIcon _cropIcon = new(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 40);

  private readonly TooltipText _cropNameElement = TooltipText.Bold("UIIS2::UnknownTree", identifier: "FruitTreeName");
  private readonly DropsHelper _dropsHelper;
  private FruitTree? _fruitTree;

  public FruitTreeTooltipContainer(FruitTree? fruitTree = null) : base("FruitTreeTooltip")
  {
    _dropsHelper = ModEntry.GetSingleton<DropsHelper>();

    ComponentSpacing = 10;
    AddChildren(Column(null, _cropNameElement, _cropDaysRemainingElement, _cropDropsElement), _cropIcon);
    IsHidden = true;

    FruitTree = fruitTree;
  }

  public FruitTree? FruitTree
  {
    get => _fruitTree;
    set => SetFruitTree(value);
  }

  private void SetFruitTree(FruitTree? fruitTree)
  {
    if (fruitTree == _fruitTree)
    {
      return;
    }

    _fruitTree = fruitTree;
    UpdateTree();
  }

  private void UpdateTree()
  {
    if (FruitTree is null)
    {
      ModEntry.LayoutDebug("Tree is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;

    FruitTreeInfo treeInfo = _dropsHelper.GetFruitTreeInfo(FruitTree);
    _cropNameElement.Text = treeInfo.TreeName;
    UpdateFruitTreeDays();
    _cropDropsElement.Text = string.Join("\n", treeInfo.Items.Select(item => GetInfoStringForDrop(item, true)));
  }

  private void UpdateFruitTreeDays()
  {
    if (FruitTree == null || FruitTree.daysUntilMature.Value <= 0)
    {
      _cropDaysRemainingElement.IsHidden = true;
      return;
    }

    _cropDaysRemainingElement.IsHidden = false;
    _cropDaysRemainingElement.Text = $"{FruitTree.daysUntilMature.Value} {I18n.DaysToMature()}";
  }

  private static string GetInfoStringForDrop(PossibleDroppedItem item, bool isReadyToday)
  {
    (ConditionFutureResult futureHarvestDates, ParsedItemData? parsedItemData, float chance, string? _) = item;

    WorldDate? nextDayToProduce = futureHarvestDates.GetNextDate(isReadyToday);
    if (nextDayToProduce == null)
    {
      return $"Unknown {I18n.Days()}";
    }

    string chanceStr = 1.0f.Equals(chance) ? "" : $" ({chance * 100:2F}%)";
    int daysUntilReady = nextDayToProduce.DayOfMonth - Game1.dayOfMonth;
    return daysUntilReady <= 0 || isReadyToday
      ? $"{parsedItemData.DisplayName}: {I18n.ReadyToHarvest()}"
      : $"{parsedItemData.DisplayName}: {daysUntilReady} {I18n.Days()}{chanceStr}";
  }
}
