﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace UIInfoSuite2.Infrastructure.Extensions;

public record GetOrCreateResult<T>(T Result, bool WasCreated);

public static class CollectionExtensions
{

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

  /// <summary>
  ///   Get a value from a dictionary, or return a default if it isn't present in the collection
  /// </summary>
  /// <param name="dictionary">This Dictionary</param>
  /// <param name="key">The key to look up</param>
  /// <param name="defaultValue">The value to return if the key is not present</param>
  /// <typeparam name="TKey">Dictionary Key Type</typeparam>
  /// <typeparam name="TValue">Dictionary Value Type</typeparam>
  /// <returns></returns>
  public static TValue GetOrDefault<TKey, TValue>(
    this IDictionary<TKey, TValue> dictionary,
    TKey key,
    TValue defaultValue
  )
  {
    return dictionary.TryGetValue(key, out TValue? foundDictValue) ? foundDictValue : defaultValue;
  }

  public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    where TValue : unmanaged
  {
    return dictionary.GetOrDefault(key, default);
  }

  public static void AddIfNotNull<TValue>(this IList<TValue> list, TValue? value, bool allowEmptyStrings = false)
  {
    if (value == null)
    {
      return;
    }

    if (!allowEmptyStrings && value is string str && string.IsNullOrEmpty(str))
    {
      return;
    }

    list.Add(value);
  }

  public static bool IsEmpty<T>(this ICollection<T> list)
  {
    return list.Count == 0;
  }
}
