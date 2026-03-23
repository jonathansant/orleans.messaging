
namespace Odin.Core;

public static class SemaphoreExtensions
{
	/// <summary>
	/// Execute async based on state data, when state is available avoid execution or else invoke <paramref name="execute"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="semaphore">Semaphore to use.</param>
	/// <param name="stateData">State data to check.</param>
	/// <param name="execute">Function to execute locked.</param>
	/// <returns></returns>
	public static async Task<T> ExecuteAsync<T>(this SemaphoreSlim semaphore, Func<T> stateData, Func<Task<T>> execute)
	{
		try
		{
			var data = stateData();
			if (data != null)
				return data;

			await semaphore.WaitAsync();

			data = stateData();
			if (data != null)
				return data;

			return await execute();
		}
		finally
		{
			semaphore.Release();
		}
	}

	/// <summary>
	/// Execute async based on state data, when state is available avoid execution or else invoke <paramref name="execute"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="semaphore">Semaphore to use.</param>
	/// <param name="stateData">State data to check.</param>
	/// <param name="execute">Function to execute locked.</param>
	/// <returns></returns>
	public static async Task ExecuteAsync<T>(this SemaphoreSlim semaphore, Func<T> stateData, Func<Task> execute)
	{
		try
		{
			var data = stateData();
			if (data != null)
				return;

			await semaphore.WaitAsync();

			data = stateData();
			if (data != null)
				return;

			await execute();
		}
		finally
		{
			semaphore.Release();
		}
	}
}
