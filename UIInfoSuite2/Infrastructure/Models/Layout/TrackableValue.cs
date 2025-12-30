using System;
using UIInfoSuite2.Infrastructure.Interfaces;

namespace UIInfoSuite2.Infrastructure.Models.Layout;

/// <summary>
///   Class to track modifiable values and their changes.
/// </summary>
/// <remarks>
///   Blatantly adapted from focustense's wonderful StardewUI library.
///   https://github.com/focustense/StardewUI/blob/master/Core/Layout/DirtyTracker.cs
/// </remarks>
/// <param name="initialValue">Initial value</param>
/// <typeparam name="T">The type to track</typeparam>
internal class TrackableValue<T>(
  T initialValue,
  Action<string?>? onChangeNotifier = null,
  string debugIdentifier = "nil"
) : ITrackable
{
  private T _value = initialValue;

  public T Value
  {
    get => _value;
    set => SetAndMark(value);
  }

  public bool IsDirty { get; private set; }

  public void ResetDirty()
  {
    IsDirty = false;
  }

  public bool CheckAndClearDirtyState()
  {
    bool dirty = IsDirty;
    IsDirty = false;
    return dirty;
  }

  public void Mark()
  {
    IsDirty = true;
    onChangeNotifier?.Invoke(debugIdentifier);
  }

  public bool SetAndMark(T newValue, bool forceMark = false, bool runCallback = true)
  {
    if (!forceMark && Equals(newValue, _value))
    {
      return false;
    }

    _value = newValue;
    if (runCallback)
    {
      Mark();
    }

    return true;
  }
}
