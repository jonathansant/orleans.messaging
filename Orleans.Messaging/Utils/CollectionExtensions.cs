using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Orleans.Messaging.Utils;

public static class CollectionExtensions
{
	/// <summary>
	/// Indicates whether the collection is null or empty.
	/// </summary>
	public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? collection)
		=> !collection?.Any() ?? true;

	/// <summary>
	/// Indicates whether the string is null or empty.
	/// </summary>
	public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
		=> string.IsNullOrEmpty(value);

	public static void Each<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var obj in source)
			action(obj);
	}

	public static Task ForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> transform)
		=> Task.WhenAll(collection.Select(transform));

	public static T? FindFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is List<T> list)
			return list.Find(x => predicate(x));

		return source.Where(predicate).FirstOrDefault();
	}

	public static T? FindSingle<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		=> source.Where(predicate).SingleOrDefault();

	public static List<T> ToSingleList<T>(this T item)
		=> new(1) { item };

	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key, Func<TKey, TValue> valueFactory)
		where TKey : notnull
	{
		if (map.TryGetValue(key, out var value))
			return value;
		value = valueFactory(key);
		map.Add(key, value);
		return value;
	}

	public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
		this IDictionary<TKey, TValue> map,
		TKey key,
		Func<TKey, Task<TValue>> asyncValueFactory
	)
		where TKey : notnull
	{
		if (map.TryGetValue(key, out var value))
			return value;
		value = await asyncValueFactory(key);
		map[key] = value;
		return value;
	}

	public static bool TryAdd(this OrderedDictionary dict, object key, object value)
	{
		if (dict.Contains(key))
			return false;

		dict.Add(key, value);
		return true;
	}

	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var item in source)
			action(item);
	}
}
