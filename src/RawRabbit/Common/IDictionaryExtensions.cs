using System.Collections.Generic;

namespace RawRabbit.Common;

public static class DictionaryExtensions
{
	public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
	{
		return dictionary.TryGetValue(key, out TValue value) ? value : default;
	}
}
