using Odin.Core.Utils;
using System.Reflection;

namespace Odin.Core;

public static class EnumerableExtensions
{
	public static IEnumerable<int> Range(Range range)
		=> Enumerable.Range(range.Start.Value, range.End.Value);

	/// <summary>
	/// Exclude any nulls within a collection.
	/// </summary>
	/// <param name="collection">Collection to exclude nulls for.</param>
	/// <returns>Collection with all null values removed, guaranteeing non-null elements.</returns>
	public static IEnumerable<T> ExcludeNulls<T>(this IEnumerable<T?> collection)
		where T : notnull
		=> collection.Where(x => x is not null)!;

	public static void Each<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var obj in source)
			action(obj);
	}

	public static void Each<T>(this IEnumerable<T> source, Action<T, int> action)
	{
		var num = 0;
		foreach (var obj in source)
			action(obj, num++);
	}

	public static void EachOrBreak<T>(this IEnumerable<T> source, Func<T, bool> func)
	{
		foreach (var item in source)
		{
			var result = func(item);
			if (result)
				break;
		}
	}

	public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, object> uniqueCheckerMethod)
		=> source.Distinct(new GenericComparer<T>(uniqueCheckerMethod));

	public static Task ForEachAsync<T>(this IEnumerable<T> collection, Func<T, Task> transform)
		=> Task.WhenAll(collection.Select(transform));

	public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> collection)
		=> (await collection).ToList();

	public static async ValueTask ForEachValueTaskAsync<T>(this IEnumerable<T> collection, Func<T, ValueTask> transform)
	{
		var valueList = collection.Select(transform).ToList();
		foreach (var valueTask in valueList)
			await valueTask;
	}

	public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TInput, TKey, TValue>(
		this IEnumerable<TInput> collection,
		Func<TInput, TKey> keySelector,
		Func<TInput, Task<TValue>> elementSelector
	) where TKey : notnull
	{
		var kvps = await collection.SelectAsync(async x =>
			{
				var value = await elementSelector(x);
				return new KeyValuePair<TKey, TValue>(keySelector(x), value);
			}
		);

		return kvps.ToDictionary(x => x.Key, x => x.Value);
	}

	public static async Task<List<TResult>> SelectAsync<TInput, TResult>(
		this IEnumerable<TInput> collection,
		Func<TInput, Task<TResult>> transform
	) => (await Task.WhenAll(collection.Select(transform))).ToList();

	public static async Task<List<TResult>> SelectManyAsync<TInput, TResult>(
		this IEnumerable<TInput> collection,
		Func<TInput, Task<List<TResult>>> transform
	) => (await collection.SelectAsync(transform)).Flatten();

	public static async Task<List<TResult>> SelectManyAsync<TInput, TResult>(
		this IEnumerable<TInput> collection,
		Func<TInput, Task<IEnumerable<TResult>>> transform
	) => (await collection.SelectAsync(transform)).Flatten();

	public static Task WhenAll<TInput>(this IEnumerable<TInput> collection, Func<TInput, Task> transform)
		=> Task.WhenAll(collection.Select(transform));

	public static async Task<List<Task>> WhenAll(this IEnumerable<Task> collection)
	{
		var tasks = collection.ToList();
		await Task.WhenAll(tasks);

		return tasks;
	}

	public static async Task<List<Task<object>>> WhenAll(this IEnumerable<Task<object>> collection)
	{
		var tasks = collection.ToList();
		await Task.WhenAll(tasks);

		return tasks;
	}

	public static async ValueTask<List<TResult>> SelectValueTaskAsync<TInput, TResult>(
		this IEnumerable<TInput> collection,
		Func<TInput, ValueTask<TResult>> transform
	)
	{
		var valueList = collection.Select(transform).ToList();
		var resultList = new List<TResult>();

		foreach (var valueTask in valueList)
			resultList.Add(await valueTask);

		return resultList;
	}

	public static List<T> ToSingleList<T>(this T item)
		=> new(1)
		{
			item
		};

	public static HashSet<T> ToSingleHashSet<T>(this T item)
		=> new(1)
		{
			item
		};

	public static T GetRandom<T>(this IEnumerable<T> source)
	{
		var index = RandomUtils.GenerateNumber(0, source.Count() - 1);
		return source.ElementAt(index);
	}

	public static T? FindFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
	{
		if (source is List<T> list)
			return list.Find(x => predicate(x));

		return source.Where(predicate).FirstOrDefault();
	}

	public static T? FindSingle<T>(this IEnumerable<T> source, Func<T, bool> predicate)
		=> source.Where(predicate).SingleOrDefault();

	public static IEnumerable<TSource> IntersectBy<TSource, TIntersect, TKey>(
		this IEnumerable<TSource> left,
		IEnumerable<TIntersect> right,
		Func<TSource, TKey> leftSelector,
		Func<TIntersect, TKey> rightSelector,
		Predicate<TSource>? orByPredicate = null,
		IEqualityComparer<TKey>? keyComparer = null
	)
	{
		ArgumentNullException.ThrowIfNull(left, nameof(left));
		ArgumentNullException.ThrowIfNull(right, nameof(right));
		ArgumentNullException.ThrowIfNull(leftSelector, nameof(leftSelector));
		ArgumentNullException.ThrowIfNull(rightSelector, nameof(rightSelector));

		keyComparer ??= EqualityComparer<TKey>.Default;

		var keys = new HashSet<TKey>(right.Select(rightSelector), keyComparer);

		return left.Where(x => keys.Contains(leftSelector(x)) || (orByPredicate != null && orByPredicate(x)));
	}

	/// <summary>
	/// Calculates and order number that must be given to an item provided the index that the user wishes to place the item at
	/// </summary>
	/// <param name="collection"></param>
	/// <param name="index"></param>
	/// <param name="factor"></param>
	/// <param name="isOrdered"></param>
	/// <returns></returns>
	public static decimal CalculateSortOrder(this IEnumerable<decimal> collection, int index, int factor = 1, bool isOrdered = false)
	{
		var sortOrderDictionary = CalculateSortOrder(collection, (index as int?).ToSingleList(), factor, isOrdered);
		return sortOrderDictionary.First().Value[0];
	}

	public static Dictionary<int, List<decimal>> CalculateSortOrder(
		this IEnumerable<decimal> collection,
		List<int?> indices,
		int factor = 1,
		bool isOrdered = false
	)
	{
		var collectionLength = collection.Count();
		var orderedList = isOrdered ? collection.ToList() : collection.OrderBy(x => x).ToList();

		var linkedList = orderedList.ToLinkedList();
		var nCollectionLength = collectionLength;
		var sortOrderDictionary = new Dictionary<int, List<decimal>>();

		foreach (var index in indices)
		{
			if (nCollectionLength == 0)
			{
				linkedList.AddLast(factor);
				AddToSortOrderDictionary(index, factor);
				nCollectionLength++;
			}
			else
			{
				if (!index.HasValue || index >= nCollectionLength)
				{
					var sortOrderAtEnd = linkedList.Last() + factor;
					linkedList.AddLast(sortOrderAtEnd);
					AddToSortOrderDictionary(index, sortOrderAtEnd);
					continue;
				}

				var orderAtIndex = linkedList.ElementAt(index.Value);
				var orderBeforeIndex = index.Value == 0 ? linkedList.ElementAt(0) - factor : linkedList.ElementAt(index.Value - 1);
				var sortOrder = (orderAtIndex == orderBeforeIndex)
					? orderBeforeIndex
					: CalculateOrderPlacement(orderBeforeIndex, orderAtIndex, factor);

				linkedList.AddBeforeIndex(index.Value, sortOrder);
				AddToSortOrderDictionary(index, sortOrder);
			}
		}

		return sortOrderDictionary;

		void AddToSortOrderDictionary(int? index, decimal sortOrder)
		{
			var indexValue = index ?? -1;
			if (!sortOrderDictionary.TryGetValue(indexValue, out var list))
			{
				list = new();
				sortOrderDictionary[indexValue] = list;
			}

			list.Add(sortOrder);
		}
	}

	public static List<T> Add<T>(this List<T> list, T item)
	{
		list.Add(item);
		return list;
	}

	/// <summary>
	/// Batches the collection in chunks according to the batch size..
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="sequence"></param>
	/// <param name="batchSize">Batch size to chunk with.</param>
	/// <returns></returns>
	public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> sequence, int batchSize)
	{
		var batch = new List<T>(batchSize);
		foreach (var item in sequence)
		{
			batch.Add(item);

			if (batch.Count >= batchSize)
			{
				yield return batch;
				batch = new(batchSize);
			}
		}

		if (batch.Count > 0)
			yield return batch;
	}

	/// <summary>
	/// Returns the left anti join of two sets.
	/// Example: Left anti join of {1, 2, 3} and {1, 2} is {3}
	/// </summary>
	public static IEnumerable<TResult> LeftAntiJoin<TLeft, TRight, TKey, TResult>(
		this IReadOnlyCollection<TLeft>? left,
		IReadOnlyCollection<TRight>? right,
		Func<TLeft, TKey> leftKeySelector,
		Func<TRight, TKey> rightKeySelector,
		Func<TLeft, TResult> resultFactory
	)
	{
		if (left.IsNullOrEmpty())
			return [];

		if (right.IsNullOrEmpty())
			return left.Select(resultFactory);

		return left.GroupJoin(
				right,
				leftKeySelector,
				rightKeySelector,
				(leftItem, rightGroup) => rightGroup.IsNullOrEmpty()
					? resultFactory(leftItem)
					: (TResult)default
			)
			.Where(x => x != null);
	}

	/// <summary>
	/// Returns the right anti join of two sets.
	/// Example: Right anti join of {1, 2} and {1, 2, 3} is {3}
	/// </summary>
	public static IEnumerable<TResult> RightAntiJoin<TLeft, TRight, TKey, TResult>(
		this IReadOnlyCollection<TLeft>? left,
		IReadOnlyCollection<TRight>? right,
		Func<TLeft, TKey> leftKeySelector,
		Func<TRight, TKey> rightKeySelector,
		Func<TRight, TResult> resultFactory
	)
	{
		if (right.IsNullOrEmpty())
			return [];

		if (left.IsNullOrEmpty())
			return right.Select(resultFactory);

		return right.GroupJoin(
				left,
				rightKeySelector,
				leftKeySelector,
				(rightItem, leftGroup) => leftGroup.IsNullOrEmpty()
					? resultFactory(rightItem)
					: (TResult)default
			)
			.Where(x => x != null);
	}

	/// <summary>
	/// Returns the inner join of two sets.
	/// Example: Inner join of {1, 2, 3} and {1, 2} is {1, 2}
	/// </summary>
	public static IEnumerable<TResult> InnerJoin<TLeft, TRight, TKey, TResult>(
		this IReadOnlyCollection<TLeft>? left,
		IReadOnlyCollection<TRight>? right,
		Func<TLeft, TKey> leftKeySelector,
		Func<TRight, TKey> rightKeySelector,
		Func<TLeft, TRight, TResult> resultFactory
	)
	{
		if (left.IsNullOrEmpty() || right.IsNullOrEmpty())
			return [];

		return left.Join(right, leftKeySelector, rightKeySelector, resultFactory);
	}

	private static decimal CalculateOrderPlacement(decimal order, decimal nextNumber, int factor)
	{
		var currentLevel = 0;
		while (true)
		{
			var result = order + ((decimal)Math.Pow(10, -currentLevel) * factor);
			if (result < nextNumber)
				return result;

			currentLevel++;
		}
	}

	/// <summary>
	/// Returns true if all items in b are found in a
	/// </summary>
	public static bool ContainsAllFrom<T>(this IEnumerable<T> a, IEnumerable<T> b)
		=> !b.Except(a).Any();

	private sealed class GenericComparer<T> : IEqualityComparer<T>
	{
		private readonly Func<T, object> _uniqueCheckerMethod;

		public GenericComparer(Func<T, object> uniqueCheckerMethod)
		{
			_uniqueCheckerMethod = uniqueCheckerMethod;
		}

		bool IEqualityComparer<T>.Equals(T? x, T? y) => _uniqueCheckerMethod(x!).Equals(_uniqueCheckerMethod(y!));

		int IEqualityComparer<T>.GetHashCode(T obj)
		{
			var model = _uniqueCheckerMethod(obj)
						?? throw new ArgumentNullException(nameof(obj), $"unique checker returned null for type: {typeof(T)}");
			return model.GetHashCode();
		}
	}

	private static readonly Lazy<MethodInfo> CastMethod = new(() => typeof(Enumerable).GetCachedMethod("Cast"));
	private static readonly Lazy<MethodInfo> ToListMethod = new(() => typeof(Enumerable).GetCachedMethod("ToList"));

	public static T CastToList<T>(this List<object> list, Type targetType)
		=> (T)CastToList(list, targetType);

	public static object CastToList(this List<object> list, Type targetType)
	{
		var castMethod = CastMethod.Value.MakeGenericMethod(targetType);
		var toListMethod = ToListMethod.Value.MakeGenericMethod(targetType);

		var castedList = castMethod.Invoke(null, [list]);
		return toListMethod.Invoke(null, [castedList]);
	}

}
