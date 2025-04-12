using System;
using UIInfoSuite2.Infrastructure.Models.Icons;

namespace UIInfoSuite2.Infrastructure.Models;

internal class TriggerField<T>(T value, Action trigger) where T : IComparable<T>
{
  private T _value = value;

  public T Value
  {
    get => _value;
    set
    {
      _value = value;
      trigger.Invoke();
    }
  }
}

internal class IconTriggerField<T>(ClickableIcon icon, T value)
  : TriggerField<T>(value, icon.MarkDirty) where T : IComparable<T>;
