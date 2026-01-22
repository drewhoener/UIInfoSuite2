using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Compatibility.CustomBush;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Helpers.GameStateHelpers;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class BushTooltipContainer : LayoutContainer
{
  private readonly TooltipText _bushDaysRemainingElement = new(
    "UIIS2::UnknownTime",
    0.75f,
    identifier: "BushTimeRemaining"
  );

  private readonly TooltipIcon _bushIcon = new(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 40);

  private readonly TooltipText _bushNameElement = TooltipText.Bold("UIIS2::UnknownBush", identifier: "BushName");

  private readonly TooltipText _doesNotProduceElement = new(
    I18n.DoesNotProduceThisSeason(),
    0.75f,
    identifier: "BushDoesNotProduce"
  );

  private readonly DropsHelper _dropsHelper;

  private readonly TooltipText _dropsText = new("UIIS2::UnknownDrops", identifier: "BushDrops", scale: 0.75f);
  private Bush? _bush;

  public BushTooltipContainer(Bush? crop = null) : base("BushTooltip")
  {
    _dropsHelper = ModEntry.GetSingleton<DropsHelper>();
    _bushIcon.Padding.SetInsets(10, 10, 10, 10);
    _bushIcon.Margin.Top = 10;
    _bushDaysRemainingElement.Margin.Top = 10;
    Direction = LayoutDirection.Row;

    ComponentSpacing = 10;
    AddChildren(
      Column(null, _bushNameElement, _bushDaysRemainingElement, _dropsText, _doesNotProduceElement),
      _bushIcon
    );

    _doesNotProduceElement.IsHidden = true;
    IsHidden = true;
  }

  public Bush? Bush
  {
    get => _bush;
    set => SetBush(value);
  }

  private void SetBush(Bush? crop)
  {
    if (crop == _bush)
    {
      return;
    }

    _bush = crop;
    UpdateBush();
  }

  private void UpdateBush()
  {
    if (Bush is null)
    {
      ModEntry.LayoutDebug("Bush is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;
    ComponentSpacing = 10;
    _dropsText.Text = "";
    _doesNotProduceElement.IsHidden = true;
    _bushDaysRemainingElement.IsHidden = true;

    int currentDay = Game1.dayOfMonth;
    Season currentSeason = Game1.season;

    int ageToMature;
    bool isReadyToday = false;
    bool willProduceThisSeason;
    bool inProductionPeriod;
    int daysUntilProductionPeriod;
    string bushName;

    List<PossibleDroppedItem> droppedItems = [];

    if (ModEntry.GetSingleton<ApiManager>().GetApi(ModCompat.CustomBush, out ICustomBushApi? customBushApi)
      && customBushApi.TryGetBush(Bush, out ICustomBushData? bushData, out string? bushID))
    {
      ageToMature = bushData.AgeToProduce;
      willProduceThisSeason = bushData.Seasons.Contains(currentSeason);
      bushName = ItemRegistry.GetData(bushID).DisplayName;
      inProductionPeriod = currentDay >= bushData.DayToBeginProducing;
      daysUntilProductionPeriod = inProductionPeriod ? 0 : bushData.DayToBeginProducing - currentDay;

      if (willProduceThisSeason && customBushApi.TryGetDrops(bushID, out IList<ICustomBushDrop>? drops))
      {
        foreach (var drop in drops)
        {
          if (Bush.tileSheetOffset.Value == 1)
          {
            droppedItems.Add(new PossibleDroppedItem(ConditionFutureResult.Today(), ItemRegistry.GetData(drop.ItemId), drop.Chance));
            isReadyToday = true;
            continue;
          }

          bool dayFound = false;
          int daysAhead = 0;

          while (daysAhead < 28 - currentDay && !dayFound)
          {
            daysAhead++;
            Game1.dayOfMonth = currentDay + daysAhead;
            dayFound = GameStateQuery.CheckConditions(drop.Condition);
          }

          Game1.dayOfMonth = currentDay;
          if (dayFound)
          {
            WorldDate whenThisHappens = new(Game1.Date);
            whenThisHappens.TotalDays += daysAhead;

            droppedItems.Add(new PossibleDroppedItem(ConditionFutureResult.FromDays(whenThisHappens), ItemRegistry.GetData(drop.ItemId), drop.Chance));
          }
        }
      }
    } else
    {
      ageToMature = 20;
      willProduceThisSeason = Game1.season != Season.Winter;
      bushName = ItemRegistry.GetData("(O)251").DisplayName;
      inProductionPeriod = Game1.dayOfMonth >= 22;
      daysUntilProductionPeriod = inProductionPeriod ? 0 : 22 - Game1.dayOfMonth;

      if (Bush.tileSheetOffset.Value == 1)
      {
        droppedItems.Add(new PossibleDroppedItem(ConditionFutureResult.Today(), ItemRegistry.GetData("(O)815"), 1.0f));
        isReadyToday = true;
      }
      else if (Game1.dayOfMonth >= 21 && Game1.dayOfMonth < 28)
      {
        droppedItems.Add(new PossibleDroppedItem(ConditionFutureResult.Tomorrow(), ItemRegistry.GetData("(O)815"), 1.0f));
      }
    }

    

    _bushNameElement.Text = bushName;

    // Too young to start producing
    bool isMature = Bush.getAge() >= ageToMature;

    if (!isMature || !willProduceThisSeason)
    {
      if (!isMature)
      {
        _bushDaysRemainingElement.Text = $"{ageToMature - Bush.getAge()} {I18n.DaysToMature()}";
        _bushDaysRemainingElement.IsHidden = false;
      }
      if (!willProduceThisSeason)
      {
        _doesNotProduceElement.IsHidden = false;
      }
      return;
    }

    // Too early in the season to produce
    if (!inProductionPeriod)
    {
      _bushDaysRemainingElement.Text = $"{daysUntilProductionPeriod} {I18n.Days()}";
      _bushDaysRemainingElement.IsHidden = false;
      return;
    }

    _dropsText.Text = string.Join(", ", droppedItems.Select(item => GetInfoStringForDrop(item, isReadyToday)));
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
