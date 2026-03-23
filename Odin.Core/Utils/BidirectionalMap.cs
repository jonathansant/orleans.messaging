using AutoMapper;

namespace Odin.Core.Utils;

public static class BidirectionalMap
{
	public static BidirectionalMap<T1, T2> Create<T1, T2>(
		IDictionary<T1, T2> dictionary,
		IEqualityComparer<string>? t1Comparer = null,
		IEqualityComparer<string>? t2Comparer = null
	) => new(dictionary, t1Comparer, t2Comparer);

	public static void CreateMap<T1, T2>(this Profile profile, BidirectionalMap<T1, T2> biMap)
	{
		profile.CreateMap<T1, T2>()
			.ConvertUsing(arg => biMap.Get(arg));

		profile.CreateMap<T2, T1>()
			.ConvertUsing(arg => biMap.Get(arg));
	}

	public static void CreateMap<T1, T2>(this IMapperConfigurationExpression mapperConfig, BidirectionalMap<T1, T2> biMap)
	{
		mapperConfig.CreateMap<T1, T2>()
			.ConvertUsing(arg => biMap.Get(arg));

		mapperConfig.CreateMap<T2, T1>()
			.ConvertUsing(arg => biMap.Get(arg));
	}
}

public class BidirectionalMap<T1, T2>
{
	private readonly Dictionary<string, T2> _stateAMap;
	private readonly Dictionary<string, T1> _stateBMap;

	public BidirectionalMap(
		Dictionary<T1, T2> dictionary,
		IEqualityComparer<string>? t1Comparer = null,
		IEqualityComparer<string>? t2Comparer = null
	) : this((IDictionary<T1, T2>)dictionary, t1Comparer, t2Comparer)
	{
	}

	public BidirectionalMap(
		IDictionary<T1, T2> dictionary,
		IEqualityComparer<string>? t1Comparer = null,
		IEqualityComparer<string>? t2Comparer = null
	)
	{
		_stateAMap = new(t1Comparer);
		_stateBMap = new(t2Comparer);
		AddRange(dictionary);
	}

	public static BidirectionalMap<T1, T2> Create(
		IDictionary<T1, T2> dictionary,
		IEqualityComparer<string>? t1Comparer = null,
		IEqualityComparer<string>? t2Comparer = null
	) => new(dictionary, t1Comparer, t2Comparer);

	public BidirectionalMap<T1, T2> AddRange(IDictionary<T1, T2> dictionary)
	{
		foreach (var item in dictionary)
			Add(item.Key, item.Value);

		return this;
	}

	/// <summary>
	/// Adds key/value both indexed. They must be unique.
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	public BidirectionalMap<T1, T2> Add(T1 key, T2 value)
	{
		_stateAMap.Add(key?.ToString() ?? string.Empty, value);
		_stateBMap.Add(value?.ToString() ?? string.Empty, key);

		return this;
	}

	/// <summary>
	/// Adds mapping to the same value when not already exists.
	/// <example>
	/// X.MeleeAssassin => LoLHeroRoleType.Assassin
	/// X.RangedAssassin => LoLHeroRoleType.Assassin
	/// </example>
	/// </summary>
	/// <param name="key">Key to map (must be unique)</param>
	/// <param name="value">Value to map (no required to be unique).</param>
	public BidirectionalMap<T1, T2> AddSame(T1 key, T2 value)
	{
		_stateAMap.Add(key.ToString(), value);

		var valueStr = value.ToString();
		_stateBMap.TryAdd(valueStr, key);

		return this;
	}

	/// <summary>
	/// Get match by source (left).
	/// </summary>
	/// <param name="key">Key to get.</param>
	public T2 GetBySource(T1 key)
	{
		if (!TryGetBySource(key, out var value))
			throw new OdinKeyNotFoundException(key.ToString());
		return value;
	}

	/// <summary>
	/// Get match by source (left).
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="value">When this method returns, contains the value specified with the key.</param>
	public bool TryGetBySource(T1 key, out T2 value)
		=> _stateAMap.TryGetValue(key.ToString(), out value);

	/// <summary>
	/// Get match by source (left) or default.
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="defaultValue">Default value to use.</param>
	public T2 GetBySourceOrDefault(T1 key, T2 defaultValue)
		=> TryGetBySource(key, out var value) ? value : defaultValue;

	/// <summary>
	/// Get match by mapped (right).
	/// </summary>
	/// <param name="key">Key to get.</param>
	public T1 GetByMap(T2 key)
	{
		if (!TryGetByMap(key, out var value))
			throw new OdinKeyNotFoundException(key.ToString());
		return value;
	}

	/// <summary>
	/// Get match by mapped (right).
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="value">When this method returns, contains the value specified with the key.</param>
	public bool TryGetByMap(T2 key, out T1 value)
		=> _stateBMap.TryGetValue(key.ToString(), out value);

	/// <summary>
	/// Get match by mapped (right) or default.
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="defaultValue">Default value to use.</param>
	public T1 GetByMapOrDefault(T2 key, T1 defaultValue)
		=> TryGetByMap(key, out var value) ? value : defaultValue;

	/// <summary>
	/// Get match by source (left). Same as <see cref="GetBySource"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="key">Key to get.</param>
	public T2 Get(T1 key) => GetBySource(key);

	/// <summary>
	/// Get match by mapped (right). Same as <see cref="GetByMap"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="key">Key to get.</param>
	public T1 Get(T2 key) => GetByMap(key);

	/// <summary>
	/// Get match by source (left). Same as <see cref="GetBySource"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="defaultValue">Default value to use.</param>
	public T2 GetOrDefault(T1 key, T2 defaultValue) => GetBySourceOrDefault(key, defaultValue);

	/// <summary>
	/// Get match by mapped (right). Same as <see cref="GetByMap"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="defaultValue">Default value to use.</param>
	public T1 GetOrDefault(T2 key, T1 defaultValue) => GetByMapOrDefault(key, defaultValue);

	/// <summary>
	/// Get match by source (left). Same as <see cref="GetBySource"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="value">When this method returns, contains the value specified with the key.</param>
	public bool TryGet(T1 key, out T2 value) => TryGetBySource(key, out value);

	/// <summary>
	/// Get match by mapped (right). Same as <see cref="GetByMap"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="key">Key to get.</param>
	/// <param name="value">When this method returns, contains the value specified with the key.</param>
	public bool TryGet(T2 key, out T1 value) => TryGetByMap(key, out value);

	/// <summary>
	/// Get many matches by source value (left).
	/// </summary>
	/// <param name="keys">Keys to get.</param>
	/// <param name="isOptional">Determines whether all should match or else throw not found.</param>
	public IEnumerable<T2> GetManyBySource(IEnumerable<T1> keys, bool isOptional = false)
	{
		foreach (var key in keys)
		{
			if (!isOptional)
				yield return Get(key);

			if (_stateAMap.TryGetValue(key.ToString(), out var value))
				yield return value;
		}
	}

	/// <summary>
	/// Get many mapped matches by mapped values (right).
	/// </summary>
	/// <param name="keys">Keys to get.</param>
	/// <param name="isOptional">Determines whether all should match or else throw not found.</param>
	/// <returns></returns>
	public IEnumerable<T1> GetManyByMap(IEnumerable<T2> keys, bool isOptional = false)
	{
		foreach (var key in keys)
		{
			if (!isOptional)
				yield return Get(key);

			if (_stateBMap.TryGetValue(key.ToString(), out var value))
				yield return value;
		}
	}

	/// <summary>
	/// Get many matches by source value (left). Same as <see cref="GetManyBySource"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="keys">Keys to get.</param>
	/// <param name="isOptional">Determines whether all should match or else throw not found.</param>
	public IEnumerable<T2> GetMany(IEnumerable<T1> keys, bool isOptional = false)
		=> GetManyBySource(keys, isOptional);

	/// <summary>
	/// Get many mapped matches by mapped values (right). Same as <see cref="GetManyByMap"/> however this is not usable when map types are similar due to ambiguity.
	/// </summary>
	/// <param name="keys">Keys to get.</param>
	/// <param name="isOptional">Determines whether all should match or else throw not found.</param>
	/// <returns></returns>
	public IEnumerable<T1> GetMany(IEnumerable<T2> keys, bool isOptional = false)
		=> GetManyByMap(keys, isOptional);

	/// <summary>
	/// Get all mapped values (left).
	/// </summary>
	public IEnumerable<T1> GetAllSource()
		=> _stateBMap.Values;

	/// <summary>
	/// Returns the &lt;left, right&gt; dictionary
	/// </summary>
	public IDictionary<string, T2> GetSourceDictionary()
		=> _stateAMap;

	/// <summary>
	/// Get all source values (right).
	/// </summary>
	public IEnumerable<T2> GetAllMapped()
		=> _stateAMap.Values;

	/// <summary>
	/// Returns the &lt;right, left&gt; dictionary
	/// </summary>
	public IDictionary<string, T1> GetMapDictionary()
		=> _stateBMap;
}
