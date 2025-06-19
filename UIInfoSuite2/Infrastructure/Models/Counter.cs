using System.Collections;
using System.Collections.Generic;

namespace UIInfoSuite2.Infrastructure.Models;

public class Counter<T> : IEnumerable<T> where T : notnull
{
  private readonly Dictionary<T, int> _counterMap = new();

  public IEnumerable<T> Keys => _counterMap.Keys;

  public IEnumerable<KeyValuePair<T, int>> Pairs => _counterMap;

  public IEnumerator<T> GetEnumerator()
  {
    return _counterMap.Keys.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public void Add(T value)
  {
    if (!_counterMap.ContainsKey(value))
    {
      _counterMap.Add(value, 0);
    }

    _counterMap[value]++;
  }

  public void Add(IEnumerable<T> values)
  {
    foreach (T value in values)
    {
      Add(value);
    }
  }

  public void AddWithoutIncrement(T value)
  {
    if (_counterMap.ContainsKey(value))
    {
      return;
    }

    _counterMap[value] = 1;
  }

  public void AddWithoutIncrement(IEnumerable<T> values)
  {
    foreach (T value in values)
    {
      AddWithoutIncrement(value);
    }
  }

  public void Remove(T value)
  {
    if (!_counterMap.ContainsKey(value))
    {
      return;
    }

    if (_counterMap[value] == 1)
    {
      _counterMap.Remove(value);
    }
    else
    {
      _counterMap[value]--;
    }
  }

  public void Clear(T value)
  {
    _counterMap.Remove(value);
  }

  public void Clear()
  {
    _counterMap.Clear();
  }

  public int Count(T key)
  {
    return _counterMap.GetValueOrDefault(key, 0);
  }
}
