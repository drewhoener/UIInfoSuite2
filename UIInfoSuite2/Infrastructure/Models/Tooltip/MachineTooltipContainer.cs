using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class MachineTooltipContainer : LayoutContainer
{
  private static readonly Lazy<ParsedItemData> BatteryItem = new(() => ItemRegistry.GetDataOrErrorItem("787"));
  private readonly TooltipText _machineName = TooltipText.Bold("UIIS2::UnknownMachine", identifier: "MachineName");
  private readonly TooltipText _timeRemaining = new("UIIS2::UnknownTime", 0.75f, identifier: "MachineTimeRemaining");
  private SObject? _machine;

  public MachineTooltipContainer(SObject? machine = null) : base("MachineTooltip")
  {
    Direction = LayoutDirection.Column;
    ComponentSpacing = 0;
    Margin.SetAll(0);
    AddChildren(_machineName, _timeRemaining);
    IsHidden = true;

    Machine = machine;
  }

  public SObject? Machine
  {
    get => _machine;
    set => SetMachine(value);
  }

  private static bool IsTrackableMachine(SObject? machine)
  {
    if (machine == null)
    {
      return false;
    }

    bool isValidMachineType = machine.Name != "Heater";

    return machine.IsWorking() && isValidMachineType;
  }

  [SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible")]
  private void SetMachine(SObject? newMachine)
  {
    if (ReferenceEquals(_machine, newMachine))
    {
      return;
    }

    if (!IsTrackableMachine(newMachine))
    {
      newMachine = null;
    }

    if (_machine != null)
    {
      _machine.minutesUntilReady.fieldChangeEvent -= OnMachineTimeUpdated;
    }

    _machine = newMachine;
    if (_machine != null)
    {
      _machine.minutesUntilReady.fieldChangeEvent += OnMachineTimeUpdated;
    }


    UpdateMachineTime();
  }

  private void OnMachineTimeUpdated(NetIntDelta field, int oldVal, int newVal)
  {
    if (_machine == null)
    {
      ModEntry.Instance.Monitor.LogOnce(
        "Got a machine time updated message, but we have no machine! This is probably a bug",
        LogLevel.Error
      );
      return;
    }

    if (_machine != null && !IsTrackableMachine(_machine))
    {
      SetMachine(null);
      return;
    }

    ModEntry.DebugLog($"Got time update for machine {_machine?.DisplayName}");
    UpdateMachineTime();
  }

  private void UpdateMachineTime()
  {
    if (Machine is null)
    {
      ModEntry.LayoutDebug("Machine is null, skipping render");
      IsHidden = true;
      return;
    }

    IsHidden = false;
    _machineName.Text = GetOutputForMachine(Machine);
    _timeRemaining.Text = GetTimeStringForMachine(Machine);
  }

  private static string GetOutputForMachine(SObject machine)
  {
    if (machine.IsSolarPanel() && machine.IsWorking() && machine.MinutesUntilReady == 0)
    {
      return BatteryItem.Value.DisplayName;
    }

    return machine.heldObject.Value.DisplayName;
  }

  private static string GetTimeStringForMachine(SObject machine)
  {
    if (machine is Cask cask)
    {
      return $"{(int)(cask.daysToMature.Value / cask.agingRate.Value)} {I18n.DaysToMature()}";
    }

    if (machine.IsSolarPanel() && machine.IsWorking())
    {
      // If working but time == 0, then it must have reset. Mark 7 days, it'll update overnight
      return machine.MinutesUntilReady == 0
        ? $"7 {I18n.Days()}"
        // We can't do the same assumption as we do below because it's only ever increments of days.
        : $"{machine.MinutesUntilReady / 60 / 20} {I18n.Days()}";
    }

    int timeLeft = machine.MinutesUntilReady;
    int longTime = timeLeft / 60;
    string longText = I18n.Hours();
    int shortTime = timeLeft % 60;
    string shortText = I18n.Minutes();

    // 1600 minutes per day if you go to bed at 2am, more if you sleep early.
    if (timeLeft >= 1600)
    {
      // Unlike crops and casks, this is only an approximate number of days
      // because of how time works while sleeping. It's close enough though.
      longText = I18n.Days();
      longTime = timeLeft / 1600;

      shortText = I18n.Hours();
      shortTime = timeLeft % 1600;

      // Hours below 1200 are 60 minutes per hour. Overnight it's 100 minutes per hour.
      // We could just divide by 60 here, but then you could see strange times like
      // "2 days, 25 hours".
      // This is a bit of a fudge since depending on the current time of day and when the
      // farmer goes to bed, the night might happen earlier or last longer, but it's just
      // an approximation; regardless the processing won't finish before tomorrow.
      if (shortTime <= 1200)
      {
        shortTime /= 60;
      }
      else
      {
        shortTime = 20 + (shortTime - 1200) / 100;
      }
    }

    StringBuilder builder = new();

    if (longTime > 0)
    {
      builder.Append($"{longTime} {longText}, ");
    }

    builder.Append($"{shortTime} {shortText}");

    return builder.ToString();
  }
}
