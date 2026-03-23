using Nito.AsyncEx;
using System.Collections.Immutable;
using EnumerableExtensions = Odin.Core.EnumerableExtensions;

// ReSharper disable once CheckNamespace
namespace System.Threading.Tasks;

public static class TaskExtensions
{
	extension(Task task)
	{
		public Task OrTimeout(int timeoutSeconds = 5)
			=> task.OrTimeout(TimeSpan.FromSeconds(timeoutSeconds));

		public async Task OrTimeout(TimeSpan timeout)
		{
			if (task == await Task.WhenAny(task, Task.Delay(timeout)))
			{
				await task;
				return;
			}

			throw new TimeoutException();
		}

		public IEnumerable<Task> AppendTask(Task task1)
			=> [task, task1];
	}

	extension<TResponse>(Task<TResponse> task)
	{
		public Task<TResponse> OrTimeout(int timeoutSeconds = 5)
			=> task.OrTimeout(TimeSpan.FromSeconds(timeoutSeconds));

		public async Task<TResponse> OrTimeout(TimeSpan timeout)
		{
			if (task == await Task.WhenAny(task, Task.Delay(timeout)))
				return await task;

			throw new TimeoutException();
		}

		public async Task<TResponse> OrTimeout(TimeSpan timeout, string message)
		{
			if (task == await Task.WhenAny(task, Task.Delay(timeout)))
				return await task;

			throw new TimeoutException(message);
		}
	}

	public static async Task<ImmutableList<TResponse>> AsImmutableList<TResponse>(this Task<List<TResponse>> task)
	{
		var response = await task;
		return [.. response];
	}

	public static async IAsyncEnumerable<T> WhenEach<T>(this IEnumerable<Task<T>> tasks)
	{
		foreach (var task in tasks.OrderByCompletion())
			yield return await task;
	}

	public static async Task<T?> SingleOrDefault<T>(this Task<List<T>> collectionTask)
	{
		var collection = await collectionTask;
		return collection.SingleOrDefault();
	}

	public static async Task<T> Single<T>(this Task<List<T>> collectionTask)
	{
		var collection = await collectionTask;
		return collection.Single();
	}

	extension<T>(Task<List<T>> collectionTask)
	{
		public async Task<T> First()
		{
			var collection = await collectionTask;
			return collection.First();
		}

		public async Task<T?> FirstOrDefault()
		{
			var collection = await collectionTask;
			return collection.FirstOrDefault();
		}
	}

	extension<T>(Task<List<T>> source)
	{
		public async Task<T?> FindFirst(Func<T, bool> predicate)
			=> (await source).Where(predicate).FirstOrDefault();

		public async Task<List<TResult>> Select<TResult>(Func<T, TResult> selector)
		{
			var collection = await source;
			return collection.Select(selector).ToList();
		}
	}

	extension<T>(Task<List<T>> collectionTask)
	{
		public async Task<List<T>> Distinct(Func<T, object> keySelector)
		{
			var collection = await collectionTask;
			return EnumerableExtensions.Distinct(collection, keySelector).ToList();
		}

		public async Task<List<T>> Where(Func<T, bool> keySelector)
		{
			var collection = await collectionTask;
			return collection.Where(keySelector).ToList();
		}
	}

	public static async Task<Dictionary<TKey, TValue>> Where<TKey, TValue>(
		this Task<Dictionary<TKey, TValue>> collectionTask,
		Func<KeyValuePair<TKey, TValue>, bool> keySelector
	)
		where TKey : notnull
	{
		var collection = await collectionTask;
		return collection.Where(keySelector).ToDictionary(x => x.Key, x => x.Value);
	}

	extension<T>(Task<IEnumerable<T>> collectionTask)
	{
		public async Task<IEnumerable<T>> Where(Func<T, bool> keySelector)
		{
			var collection = await collectionTask;
			return collection.Where(keySelector);
		}

		public async Task<IEnumerable<T[]>> Chunk(int size)
		{
			var collection = await collectionTask;
			return collection.Chunk(size);
		}

		public async Task<IEnumerable<TResult>> Select<TResult>(Func<T, TResult> selector)
		{
			var collection = await collectionTask;
			return collection.Select(selector);
		}

		public async Task<T> First()
		{
			var collection = await collectionTask;
			return collection.First();
		}

		public async Task<IEnumerable<T>> Distinct(Func<T, object> keySelector)
		{
			var collection = await collectionTask;
			return EnumerableExtensions.Distinct(collection, keySelector);
		}

		public async Task<T?> SingleOrDefault()
		{
			var collection = await collectionTask;
			return collection.SingleOrDefault();
		}

		public async Task<Dictionary<TKey, T>> ToDictionaryAsync<TKey>(Func<T, TKey> keySelector)
			where TKey : notnull
		{
			var collection = await collectionTask;
			return collection.ToDictionary(keySelector);
		}

		public async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
			Func<T, TKey> keySelector,
			Func<T, TValue> valueSelector
		)
			where TKey : notnull
		{
			var collection = await collectionTask;
			return collection.ToDictionary(keySelector, valueSelector);
		}

		public async Task<T> MaxBy<TKey>(Func<T, TKey> keySelector)
		{
			var collection = await collectionTask;
			return collection.MaxBy(keySelector)!;
		}

		public async Task<IEnumerable<IGrouping<TKey, T>>> GroupBy<TKey>(Func<T, TKey> keySelector)
		{
			var collection = await collectionTask;
			return collection.GroupBy(keySelector);
		}
	}

	extension<T>(Task<List<T>> collectionTask)
	{
		public async Task<Dictionary<TKey, T>> ToDictionaryAsync<TKey>(Func<T, TKey> keySelector)
			where TKey : notnull
		{
			var collection = await collectionTask;
			return collection.ToDictionary(keySelector);
		}

		public async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TKey, TValue>(
			Func<T, TKey> keySelector,
			Func<T, TValue> valueSelector
		)
			where TKey : notnull
		{
			var collection = await collectionTask;
			return collection.ToDictionary(keySelector, valueSelector);
		}

		public async Task<T> MaxBy<TKey>(Func<T, TKey> keySelector)
		{
			var collection = await collectionTask;
			return collection.MaxBy(keySelector)!;
		}

		public async Task<IEnumerable<IGrouping<TKey, T>>> GroupBy<TKey>(Func<T, TKey> keySelector)
		{
			var collection = await collectionTask;
			return collection.GroupBy(keySelector);
		}
	}

	public static IEnumerable<Task> AppendTask<T>(this IEnumerable<Task> collection, Task task)
		=> collection.Append(task);
}