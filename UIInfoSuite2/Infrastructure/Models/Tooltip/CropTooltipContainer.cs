using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class CropTooltipContainer : LayoutContainer
{
  private readonly TooltipText _cropDaysRemainingElement = new(
    "UIIS2::UnknownTime",
    0.75f,
    identifier: "CropTimeRemaining"
  );

  private readonly TooltipIcon _cropIcon = new(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 20);

  private readonly TooltipText _cropNameElement = TooltipText.Bold(
    "UIIS2::UnknownCrop",
    identifier: "CropName",
    scale: 0.75f
  );

  private readonly DropsHelper _dropsHelper;
  private Crop? _crop;

  public CropTooltipContainer(Crop? crop = null) : base("CropTooltip")
  {
    _dropsHelper = ModEntry.GetSingleton<DropsHelper>();
    _cropIcon.Padding.SetInsets(10, 10, 10, 10);
    _cropIcon.Margin.Top = 10;
    _cropDaysRemainingElement.Margin.Top = 10;
    Direction = LayoutDirection.Row;

    ComponentSpacing = 10;
    AddChildren(Column(null, _cropNameElement, _cropDaysRemainingElement), _cropIcon);
    IsHidden = true;

    Crop = crop;
  }

  public Crop? Crop
  {
    get => _crop;
    set => SetCrop(value);
  }

  private void SetCrop(Crop? crop)
  {
    if (crop == _crop)
    {
      return;
    }

    ModEntry.LayoutDebug($"Updated Tooltip Crop: {crop.GetCropString()}");

    _crop = crop;
    UpdateCrop();
  }

  private void UpdateCrop()
  {
    if (Crop is null)
    {
      ModEntry.LayoutDebug("Crop is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;

    ComponentSpacing = 10;

    var daysLeft = 0;
    if (Crop.fullyGrown.Value && Crop.dayOfCurrentPhase.Value > 0)
    {
      daysLeft = Crop.dayOfCurrentPhase.Value;
    }
    else
    {
      for (int i = Crop.currentPhase.Value; i < Crop.phaseDays.Count - 1; i++)
      {
        daysLeft += Crop.phaseDays[i];
      }

      daysLeft -= Crop.dayOfCurrentPhase.Value;
    }

    string cropName = _dropsHelper.GetCropHarvestName(Crop);
    string daysLeftStr = daysLeft <= 0 ? I18n.ReadyToHarvest() : $"{daysLeft} {I18n.Days()}";

    _cropNameElement.Text = cropName;
    _cropDaysRemainingElement.Text = daysLeftStr;

    Item cropOutput = ItemRegistry.Create(Crop.indexOfHarvest.Value);
    ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(cropOutput.QualifiedItemId);
    _cropIcon.SetIcon(dataOrErrorItem.GetTexture(), dataOrErrorItem.GetSourceRect(), 50, PrimaryDimension.Height);
  }
}
