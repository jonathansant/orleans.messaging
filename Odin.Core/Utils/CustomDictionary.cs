using System.Collections;

namespace Odin.Core.Utils;

[GenerateSerializer]
public abstract class CustomDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
	where TKey : notnull
{
	[Id(0)]
	protected Dictionary<TKey, TValue> Data = new();

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		=> Data.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public virtual void Add(KeyValuePair<TKey, TValue> item)
		=> Data.Add(item.Key, item.Value);

	public virtual void Clear()
		=> Data.Clear();

	public virtual bool Contains(KeyValuePair<TKey, TValue> item)
		=> Data.Contains(item);

	public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		=> ((IDictionary<TKey, TValue>)Data).CopyTo(array, arrayIndex);

	public virtual bool Remove(KeyValuePair<TKey, TValue> item)
		=> Data.Remove(item.Key);

	public int Count => Data.Count;

	public bool IsReadOnly => false;

	public virtual void Add(TKey key, TValue value)
		=> Data.Add(key, value);

	public virtual bool ContainsKey(TKey key)
		=> Data.ContainsKey(key);

	public virtual bool Remove(TKey key)
		=> Data.Remove(key);

	public bool TryGetValue(TKey key, out TValue value)
		=> Data.TryGetValue(key, out value);

	public TValue this[TKey key]
	{
		get => Data[key];
		set => Data[key] = value;
	}

	public ICollection<TKey> Keys => Data.Keys;
	public ICollection<TValue> Values => Data.Values;

	IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

	IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;
}
