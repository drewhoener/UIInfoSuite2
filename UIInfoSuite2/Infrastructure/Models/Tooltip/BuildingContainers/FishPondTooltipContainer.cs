using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Netcode;
using StardewValley.Buildings;
using StardewValley.GameData.FishPonds;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip.BuildingContainers;

internal class FishPondTooltipContainer : LayoutContainer
{
  private readonly TooltipText _capacityText = new("UIIS2::UnknownContent", 0.75f, identifier: "BuildingInputs");
  private readonly TooltipText _daysUntilReady = new("UIIS2::UnknownContent", 0.75f, identifier: "BuildingInputs");
  private FishPond? _pond;

  public FishPondTooltipContainer(FishPond? pond = null) : base("BuildingTooltip")
  {
    Direction = LayoutDirection.Column;
    ComponentSpacing = 0;
    Margin.SetAll(0);
    AddChildren(_capacityText, _daysUntilReady);
    IsHidden = true;

    Pond = pond;
  }

  public FishPond? Pond
  {
    get => _pond;
    set => SetPond(value);
  }

  private bool IsPondValid(FishPond? pond)
  {
    return pond?.fishType.Value != null;
  }

  [MemberNotNullWhen(true, nameof(Pond))]
  private bool IsPondValid()
  {
    return IsPondValid(Pond);
  }

  private void SetPond(FishPond? newPond)
  {
    if (ReferenceEquals(_pond, newPond))
    {
      return;
    }

    if (Pond is not null)
    {
      Pond.fishType.fieldChangeVisibleEvent -= OnPondFieldChanged;
      Pond.currentOccupants.fieldChangeVisibleEvent -= OnPondFieldChanged;
    }

    _pond = newPond;
    if (!IsPondValid())
    {
      ModEntry.LayoutDebug("Pond has no data");
      IsHidden = true;
      return;
    }

    Pond.fishType.fieldChangeVisibleEvent += OnPondFieldChanged;
    Pond.currentOccupants.fieldChangeVisibleEvent += OnPondFieldChanged;

    UpdateFishText();
  }

  private void OnPondFieldChanged<T, TNet>(NetField<T, TNet> field, T oldValue, T newValue)
    where TNet : NetField<T, TNet>
  {
    UpdateFishText();
  }

  private void UpdateFishText()
  {
    UpdateFishCount();
    UpdateFishDays();
    IsHidden = false;
  }

  private void UpdateFishCount()
  {
    if (!IsPondValid())
    {
      return;
    }

    string fishName = Pond.GetFishObject().DisplayName;
    int count = Pond.currentOccupants.Value;
    int capacity = Pond.maxOccupants.Value;
    _capacityText.Text = $"{fishName}: {count}/{capacity}";
  }

  private void UpdateFishDays()
  {
    if (!IsPondValid())
    {
      return;
    }

    int count = Pond.currentOccupants.Value;
    int days = Pond.daysSinceSpawn.Value;
    int spawnTime = Pond.GetFishPondData().SpawnTime;
    int totalPondCapacity = GetMaxPopulation(Pond);
    int daysToSpawn = days == 0 ? spawnTime : spawnTime - days;

    _daysUntilReady.IsHidden = count >= totalPondCapacity || (daysToSpawn <= 0 && count >= Pond.maxOccupants.Value);
    _daysUntilReady.Text = $"Next Spawn: {daysToSpawn} {I18n.Days()}";
  }

  private static int GetMaxPopulation(FishPond pond)
  {
    FishPondData? data = pond.GetFishPondData();
    if (data == null)
    {
      return 10;
    }

    if (data.MaxPopulation > 0)
    {
      return data.MaxPopulation;
    }

    return data.PopulationGates.Count <= 0 ? 10 : data.PopulationGates.Keys.Max();
  }
}
