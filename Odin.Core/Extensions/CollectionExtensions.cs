using Humanizer;
using Newtonsoft.Json.Linq;
using Odin.Core.Error;
using Odin.Core.FlowControl;
using Odin.Core.Querying.Filtering;
using System.Collections.Frozen;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Odin.Core;

public static class CollectionExtensions
{
	/// <summary>
	/// Indicates whether the collection is null or empty.
	/// </summary>
	/// <param name="collection">Collection to test.</param>
	/// <returns></returns>
	public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? collection)
		=> !collection?.Any() ?? true;

	/// <summary>
	/// Indicates whether the collection is null or empty or contains the expected value.
	/// </summary>
	/// <param name="collection">Collection to test.</param>
	/// <param name="expectedValue">Expected value to be present in collection.</param>
	/// <returns></returns>
	public static bool IsNullOrEmptyOrContains<T>(
		[NotNullWhen(false)] this ICollection<T>? collection,
		T expectedValue
	) => collection.IsNullOrEmpty() || collection.Contains(expectedValue);

	/// <summary>
	/// If the collection is null return an empty IEnumerable
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="source"></param>
	/// <returns></returns>
	public static IEnumerable<T> SetEmptyIfNull<T>(this ICollection<T>? source)
		=> source ?? Enumerable.Empty<T>();

	/// <summary>
	/// Adds range of values to Collection. e.g. <see cref="HashSet{T}"/>.
	/// </summary>
	/// <param name="collection">Collection to test.</param>
	/// <param name="values">Values to add.</param>
	/// <returns></returns>
	public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> values)
		=> values.Each(collection.Add);

	/// <summary>
	/// Get value or return key as the default.
	/// </summary>
	/// <param name="dic">Dictionary to get value from.</param>
	/// <param name="key">key to retrieve.</param>
	/// <returns></returns>
	public static T GetValueOrKeyAsDefault<T>(this IDictionary<T, T> dic, T key)
		=> dic.TryGetValue(key, out var value) ? value : key;

	/// <summary>
	/// Adds dictionary together and throws if key is already added, elements are added at the end of the source dictionary.
	/// </summary>
	/// <param name="dic">Source dictionary.</param>
	/// <param name="dicToAdd">Dictionary to add.</param>
	public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
		=> dicToAdd.Each(x => dic.TryAdd(x.Key, x.Value));

	/// <summary>
	/// Adds dictionary together if key has not already been added, elements are added at the end of the source dictionary.
	/// </summary>
	/// <param name="dic">Source dictionary.</param>
	/// <param name="dicToAdd">Dictionary to add.</param>
	public static void TryAddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
		=> dicToAdd.Each(x => dic.TryAdd(x.Key, x.Value));

	/// <summary>
	/// Adds dictionary together and replaces if key is already added, elements are added at the end of the source dictionary.
	/// </summary>
	/// <param name="dic">Source dictionary.</param>
	/// <param name="dicToAdd">Dictionary to add.</param>
	public static void AddRangeOverride<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
		=> dicToAdd.Each(x => dic[x.Key] = x.Value);

	/// <summary>
	/// Adds dictionary together and skip if key was already added, elements are added at the end of the source dictionary.
	/// </summary>
	/// <param name="dic">Source dictionary.</param>
	/// <param name="dicToAdd">Dictionary to add.</param>
	public static void AddRangeNewOnly<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
		=> dicToAdd.Each(
			x =>
			{
				if (!dic.ContainsKey(x.Key)) dic.Add(x.Key, x.Value);
			}
		);

	/// <summary>
	/// Adds/Update key and union its values if entry already exists.
	/// </summary>
	/// <typeparam name="TKey">Lookup key type.</typeparam>
	/// <typeparam name="TValue">Lookup value type.</typeparam>
	/// <param name="lookup">Lookup collection to union with.</param>
	/// <param name="key">Key to add/update</param>
	/// <param name="entries">Entries to add (union).</param>
	public static void UnionLookup<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> lookup, TKey key, HashSet<TValue> entries)
	{
		if (!lookup.TryGetValue(key, out var existingEntries))
		{
			lookup[key] = entries;
			return;
		}

		existingEntries.UnionWith(entries);
	}

	/// <summary>
	/// Adds/Update keys and union their values if entry already exists.
	/// </summary>
	/// <typeparam name="TKey">Lookup key type.</typeparam>
	/// <typeparam name="TValue">Lookup value type.</typeparam>
	/// <param name="lookup">Lookup collection to union with.</param>
	/// <param name="other">Other Lookup collection to union.</param>
	public static void UnionWith<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>> lookup, IDictionary<TKey, HashSet<TValue>>? other)
	{
		if (other == null)
			return;
		foreach (var (key, value) in other)
			lookup.UnionLookup(key, value);
	}

	/// <summary>
	/// Determines whether the dictionary contains any keys specified.
	/// </summary>
	/// <param name="dic">Source dictionary.</param>
	/// <param name="keys">Keys to locate in the dictionary.</param>
	/// <returns></returns>
	public static bool ContainsKeys<TKey, TValue>(this IDictionary<TKey, TValue> dic, IEnumerable<TKey> keys)
	{
		var result = false;
		keys.EachOrBreak(
			x =>
			{
				result = dic.ContainsKey(x);
				return result;
			}
		);
		return result;
	}

	public static ValueTask DisposeAllAsync<TKey>(this IDictionary<TKey, ScheduledThrottledAction> dic)
		=> dic.ForEachValueTaskAsync(x => x.Value.DisposeAsync());

	/// <summary>
	/// Converts dictionary to generic type.
	/// </summary>
	/// <typeparam name="TValue">Dictionary value type.</typeparam>
	/// <typeparam name="TResult">Generic type to convert dictionary to.</typeparam>
	/// <param name="source">Data source to populate object with.</param>
	/// <param name="throwIfNotFound">Determine whether to throw an exception when property not found or not.</param>
	/// <param name="keyTransform">Key transform function to match property name (defaults: to pascalize).</param>
	/// <returns></returns>
	public static TResult ToObject<TResult, TValue>(
		this IDictionary<string, TValue> source,
		bool throwIfNotFound = false,
		Func<string, string>? keyTransform = null
	) where TResult : new()
	{
		object obj = new TResult();
		var objType = obj.GetType();
		keyTransform ??= ToPascalCase;

		foreach (var item in source)
		{
			var propName = keyTransform(item.Key);
			var prop = objType.GetProperty(propName);

			if (prop == null)
			{
				if (throwIfNotFound)
					throw new MissingMemberException(objType.GetDemystifiedName(), item.Key);
				continue;
			}

			if (prop.PropertyType.IsEnum)
			{
				var enumValue = item.Value is string strValue
					? Enum.Parse(prop.PropertyType, strValue, ignoreCase: true)
					: Enum.ToObject(prop.PropertyType, item.Value);
				prop.SetValue(obj, enumValue, null);
			}
			else if (prop.PropertyType.IsNullableEnum())
			{
				var enumValue = item.Value is string strValue
					? Enum.Parse(Nullable.GetUnderlyingType(prop.PropertyType), strValue, ignoreCase: true)
					: Enum.ToObject(Nullable.GetUnderlyingType(prop.PropertyType), item.Value);
				prop.SetValue(obj, enumValue, null);
			}
			else if (typeof(List<string>).IsAssignableFrom(prop.PropertyType) && item.Value is string valueStr)
			{
				if (valueStr.IsNullOrEmpty())
					continue;

				var value = valueStr.Split(',').ToList();
				prop.SetValue(obj, value, null);
			}
			else if (typeof(Dictionary<string, string>).IsAssignableFrom(prop.PropertyType) && item.Value is string valueDicStr)
			{
				if (valueDicStr.IsNullOrEmpty())
					continue;

				var value = valueDicStr.Split(',')
					.Select(
						x =>
						{
							var arr = x.Split('=');
							return new
							{
								Key = arr[0],
								Value = arr[1]
							};
						}
					)
					.ToDictionary(x => x.Key, x => x.Value);
				prop.SetValue(obj, value, null);
			}
			else if (item.Value is JArray array)
			{
				var listType = prop.PropertyType;
				var value = array.ToObject(listType);
				prop.SetValue(obj, value, null);
			}
			else if (item.Value is not null
					 && prop.PropertyType.IsNumericType()
					 && (item.Value.GetType().IsNumericType() || item.Value is string))
			{
				var targetType = prop.PropertyType.GetCardinalType();
				var value = Convert.ChangeType(item.Value, targetType);
				prop.SetValue(obj, value, null);
			}
			else if (prop.GetSetMethod() != null)
			{
				prop.SetValue(obj, item.Value, null);
			}
		}

		return (TResult)obj;
	}

	/// <summary>
	/// Converts dictionary to generic type.
	/// </summary>
	/// <typeparam name="T">Generic type to convert dictionary to.</typeparam>
	/// <param name="source">Data source to populate object with.</param>
	/// <param name="throwIfNotFound">Determine whether to throw an exception when property not found or not.</param>
	/// <param name="keyTransform">Key transform function to match property name (defaults: to pascalize).</param>
	public static T ToObject<T>(
		this IDictionary<string, string> source,
		bool throwIfNotFound = true,
		Func<string, string>? keyTransform = null
	) where T : new()
		=> ToObject<T, string>(source, throwIfNotFound, keyTransform);

	/// <summary>
	/// Converts dictionary to generic type.
	/// </summary>
	/// <typeparam name="T">Generic type to convert dictionary to.</typeparam>
	/// <param name="source">Data source to populate object with.</param>
	/// <param name="throwIfNotFound">Determine whether to throw an exception when property not found or not.</param>
	/// <param name="keyTransform">Key transform function to match property name (defaults: to pascalize).</param>
	public static T ToObject<T>(
		this IDictionary<string, object> source,
		bool throwIfNotFound = true,
		Func<string, string>? keyTransform = null
	) where T : new()
		=> ToObject<T, object>(source, throwIfNotFound, keyTransform);

	private static string ToPascalCase(string str) => str.Pascalize();

	/// <summary>
	/// Converts the key and value of the dictionary to string.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="dictionary">Dictionary which consists of a value of List.</param>
	public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary)
		=> $"{string.Join(", ", dictionary.Select(kv => $"{kv.Key}: {ToDebugString(kv.Value)}"))}";

	public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, IEnumerable<TValue>> dictionary)
		=> $"{string.Join(", ", dictionary.Select(kv => $"{kv.Key}: {ToDebugString(kv.Value)}"))}";

	public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, HashSet<TValue>?> dictionary)
		=> $"{string.Join(", ", dictionary.Select(kv => $"{kv.Key}: {ToDebugString(kv.Value)}"))}";

	/// <summary>
	/// Converts the collection into a string.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	public static string ToDebugString<TKey>(this IEnumerable<TKey>? collection)
		=> collection == null ? "[]" : $"[{string.Join(", ", collection)}]";

	/// <summary>
	/// Converts the collection into a string.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	public static string ToDebugString<TKey>(this IEnumerable<TKey>? collection, Func<TKey, string> itemSelector)
		=> collection == null ? "[]" : $"[{string.Join(", ", collection.Select(itemSelector))}]";

	/// <summary>
	/// Converts the key and value of the dictionary to string.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="dictionary">Dictionary to convert.</param>
	public static string ToStringify<TKey, TValue>(this IDictionary<TKey, TValue>? dictionary)
		=> dictionary == null
			? string.Empty
			: $"{string.Join(",", dictionary.Select(kv => $"{kv.Key}={kv.Value}"))}";

	/// <summary>
	/// Converts the collection into a string.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	public static string ToStringify<TKey>(this IEnumerable<TKey>? collection)
		=> collection == null ? string.Empty : $"{string.Join(",", collection)}";

	/// <summary>
	/// Will add the kvp if the key does not exist in the map.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <returns>True if added and False if not.</returns>
	public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key, TValue value)
		where TKey : notnull
	{
		var isNotContained = !map.ContainsKey(key);

		if (isNotContained)
			map.Add(key, value);

		return isNotContained;
	}

	/// <summary>
	/// Get or Add if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <returns>Returns the current, or the newly created instance.</returns>
	public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> map, TKey key, Func<TKey, TValue> valueFactory)
		where TKey : notnull
	{
		if (map.TryGetValue(key, out var value))
			return value;
		value = valueFactory(key);
		map.Add(key, value);

		return value;
	}

	/// <summary>
	/// Get or Add if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <returns>Returns the current, or the newly created instance.</returns>
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

	/// <summary>
	/// Adds an item in a set in the dictionary, or creates the set if it does not exist.
	/// </summary>
	/// <typeparam name="TItem">The type of the items in the set</typeparam>
	/// <typeparam name="TKey">The key type</typeparam>
	/// <typeparam name="TDictionary">The dictionary type, e.g. Dictionary, ConcurrentDictionary</typeparam>
	/// <param name="map">The dictionary of sets</param>
	/// <param name="key">The key of the set</param>
	/// <param name="value">The item to add in the set</param>
	/// <returns></returns>
	public static TDictionary AddValue<TDictionary, TKey, TItem>(this TDictionary map, TKey key, TItem value)
		where TDictionary : IDictionary<TKey, HashSet<TItem>>
		where TKey : notnull
	{
		if (!map.TryGetValue(key, out var set))
		{
			set = [];
			map[key] = set;
		}

		set.Add(value);

		return map;
	}

	/// <summary>
	/// Loop through batch of items and delay invocation per every x of seconds.
	/// </summary>
	/// <typeparam name="TItem">The type of the item</typeparam>
	/// <param name="list">Collection to loop</param>
	/// <param name="func">The function to invoke</param>
	/// <param name="takeLimit">batch limit per invocation</param>
	/// <param name="throttleDelay">The time span to throttle invocations</param>
	/// <returns></returns>
	public static async Task ForEachBatchedThrottle<TItem>(
		this ICollection<TItem> list,
		Func<TItem, Task> func,
		int takeLimit = 70,
		TimeSpan? throttleDelay = null
	)
	{
		if (list.IsNullOrEmpty())
			return;

		throttleDelay ??= TimeSpan.FromSeconds(2.5);

		var skip = 0;
		while (skip < list.Count)
		{
			await list.Skip(skip).Take(takeLimit).ForEachAsync(func);

			await Task.Delay(throttleDelay.Value);
			skip += takeLimit;
		}
	}

	/// <summary>
	/// Helper method to add a Dictionary like collection of items to a NameValueCollection
	/// </summary>
	/// <param name="nvCollection"></param>
	/// <param name="items"></param>
	public static void AddRange(
		this NameValueCollection nvCollection,
		IEnumerable<KeyValuePair<string, string>> items
	)
	{
		foreach (var item in items)
			nvCollection.Add(item.Key, item.Value);
	}

	/// <summary>
	/// Dictionary deconstruct.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <param name="kvp"></param>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
	{
		key = kvp.Key;
		value = kvp.Value;
	}

	/// <summary>
	/// Converts Lookup to Dictionary.
	/// </summary>
	public static Dictionary<TKey, List<TElement>> ToDictionary<TKey, TElement>(this ILookup<TKey, TElement> lookup)
		where TKey : notnull
		=> lookup.ToDictionary(x => x.Key, x => x.ToList());

	/// <summary>
	/// Converts Lookup to FrozenDictionary.
	/// </summary>
	public static FrozenDictionary<TKey, List<TElement>> ToFrozenDictionary<TKey, TElement>(this ILookup<TKey, TElement> lookup)
		where TKey : notnull
		=> lookup.ToFrozenDictionary(x => x.Key, x => x.ToList());

	/// <summary>
	/// Converts Dictionary with list value to Lookup.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	public static ILookup<TKey, TElement> ToLookup<TKey, TElement>(this IDictionary<TKey, List<TElement>> dic)
		=> dic.SelectMany(kvp => kvp.Value.Select(x => new { kvp.Key, Value = x }))
			.ToLookup(pair => pair.Key, pair => pair.Value);

	/// <summary>
	/// Checks for duplicates
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection"></param>
	/// <param name="stringComparer"></param>
	/// <returns></returns>
	public static bool HasDuplicates<T>(this IEnumerable<T> collection, StringComparer? stringComparer = null)
	{
		if (typeof(T) == typeof(string))
		{
			var stringHashSet = new HashSet<string>(stringComparer);
			return collection.Any(value => !stringHashSet.Add(value?.ToString()));
		}

		var hash = new HashSet<T>();
		return collection.Any(value => !hash.Add(value));
	}

	public static void Deconstruct<TKey, TElement>(
		this IGrouping<TKey, TElement> grouping,
		out TKey key,
		out IEnumerable<TElement> elements
	)
	{
		key = grouping.Key;
		elements = grouping;
	}

	// todo: move Filter Input/op extensions to a separate file in filtering
	public static OdinFilterInput ToOrFilter(
		this IEnumerable<string> ids,
		string index,
		FilterOperator filterOperator = FilterOperator.Equals
	) => new()
	{
		Or = ids.ToFilterOperator(index, filterOperator)
	};

	public static OdinFilterInput ToAndFilter(this string id, string index, FilterOperator filterOperator = FilterOperator.Equals)
		=> id.ToSingleList().ToAndFilter(index, filterOperator);

	public static OdinFilterInput ToAndFilter(
		this IEnumerable<string> ids,
		string index,
		FilterOperator filterOperator = FilterOperator.Equals
	) => new()
	{
		And = ids.ToFilterOperator(index, filterOperator)
	};

	public static KeyValuePair<string, OdinFilterOperatorInput> ToFilterOperatorInput(
		this IEnumerable<string?> ids,
		string index,
		FilterOperator filterOperator = FilterOperator.Equals
	) => ids.ToFilterOperatorInput<string>(index, filterOperator);

	public static KeyValuePair<string, OdinFilterOperatorInput> ToFilterOperatorInput<T>(
		this IEnumerable<T?> values,
		string index,
		FilterOperator filterOperator = FilterOperator.Equals
	) => new(
		index,
		new()
		{
			Operator = filterOperator,
			Items = values.Cast<object>().ToList()
		}
	);

	public static KeyValuePair<string, OdinFilterOperatorInput> ToFilterOperatorInput(
		this string id,
		string index,
		FilterOperator filterOperator = FilterOperator.Equals
	) => id.ToSingleList().ToFilterOperatorInput(index, filterOperator);

	public static List<TItem> Flatten<TItem>(this IEnumerable<IEnumerable<TItem>> listOfLists)
		=> listOfLists.SelectMany(x => x).ToList();

	public static LinkedList<T> ToLinkedList<T>(this List<T> collection)
		=> new(collection);

	public static LinkedList<T> AddBeforeIndex<T>(this LinkedList<T> collection, int index, T value)
	{
		var currentNode = collection.First;
		var currentIndex = 0;

		while (currentNode != null)
		{
			if (currentIndex++ == index)
			{
				collection.AddBefore(currentNode, value);
				return collection;
			}

			currentNode = currentNode.Next;
		}

		return collection;
	}

	public static HashSet<string> CartesianProduct(this ISet<string> setA, ISet<string> setB, Func<string, string, string> transformFunc)
		=> setA.Select(a => setB.Select(b => transformFunc(a, b))).Flatten().ToHashSet();

	public static TItem Single<TItem>(this IEnumerable<TItem> items, string fieldName)
	{
		var itemsList = items as IReadOnlyCollection<TItem> ?? items.ToList();

		if (itemsList.Count != 1)
			throw ErrorResult.AsValidationError()
				.AddField(fieldName, x => x.AsInvalidItemsAmount(itemsList.Count, minItems: 1, maxItems: 1))
				.AsApiErrorException();

		return itemsList.Single();
	}

	public static TItem OnlyOne<TItem>(this IEnumerable<TItem>? items, string fieldName)
	{
		var itemsList = items?.ToList();

		return itemsList.OnlyOne(
			() => throw ErrorResult.AsValidationError()
				.AddField(fieldName, x => x.AsInvalidItemsAmount(itemsList?.Count ?? 0, minItems: 1, maxItems: 1))
				.AsApiErrorException()
		);
	}

	public static TItem OnlyOne<TItem>(this IEnumerable<TItem>? items, Func<ApiErrorException> exceptionCreation)
	{
		var itemsList = items?.ToList();

		if (itemsList?.Count != 1)
			throw exceptionCreation();

		return itemsList.Single();
	}

	public static OdinFilterOperator ToFilterOperator(
		this IEnumerable<string> ids,
		string index,
		FilterOperator filterOperator = FilterOperator.Equals
	) => new(
		new List<KeyValuePair<string, OdinFilterOperatorInput>>
		{
			ids.ToFilterOperatorInput(index, filterOperator)
		}
	);

	public static bool TryAdd(this OrderedDictionary dict, object key, object value)
	{
		if (dict.Contains(key))
			return false;

		dict.Add(key, value);
		return true;
	}

	public static Dictionary<TKey, TValue> RemoveRange<TKey, TValue>(this Dictionary<TKey, TValue> source, IEnumerable<TKey> keys)
		where TKey : notnull
	{
		foreach (var key in keys)
			source.Remove(key);

		return source;
	}

	public static Dictionary<string, TValue> ToCaseInsensitive<TValue>(this IDictionary<string, TValue> dict)
		=> new(dict, StringComparer.OrdinalIgnoreCase);
}
