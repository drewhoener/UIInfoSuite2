using System;
using System.Collections.Generic;
using System.Linq;

namespace UIInfoSuite2.Infrastructure.Extensions;

public record GetOrCreateResult<T>(T Result, bool WasCreated);

public static class CollectionExtensions
{
  public static TValue SafeGet<TKey, TValue>(
    this IDictionary<TKey, TValue> dictionary,
    TKey key,
    TValue defaultValue = default
  )
  {
    TValue value = defaultValue;

    if (dictionary != null)
    {
      if (!dictionary.TryGetValue(key, out value))
      {
        value = defaultValue;
      }
    }

    return value;
  }

  public static GetOrCreateResult<TValue> GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    where TValue : new()
  {
    if (dictionary.TryGetValue(key, out TValue? value))
    {
      return new GetOrCreateResult<TValue>(value, false);
    }

    dictionary[key] = new TValue();

    return new GetOrCreateResult<TValue>(dictionary[key], true);
  }

  public static GetOrCreateResult<TValue> GetOrCreate<TKey, TValue>(
    this IDictionary<TKey, TValue> dictionary,
    TKey key,
    Func<TValue> defaultCreate
  )
  {
    if (dictionary.TryGetValue(key, out TValue? value))
    {
      return new GetOrCreateResult<TValue>(value, false);
    }

    dictionary[key] = defaultCreate();

    return new GetOrCreateResult<TValue>(dictionary[key], true);
  }

  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
  {
    var random = new Random();
    List<T> shuffledList = source.ToList();
    int n = shuffledList.Count;
    while (n > 1)
    {
      n--;
      int k = random.Next(n + 1);
      (shuffledList[k], shuffledList[n]) = (shuffledList[n], shuffledList[k]);
    }

    return shuffledList;
  }
}
