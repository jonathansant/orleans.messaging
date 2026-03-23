namespace Odin.Core.Tasks;

public static class TaskExtensions
{
	public static async Task<T> TryExecute<T>(
		this Task<T> task,
		ILogger logger,
		string errorMessage
	)
	{
		try
		{
			return await task;
		}
		catch (Exception ex)
		{
			logger.Error(ex, errorMessage);
		}
		return default;
	}

	public static async Task TryExecute(
		this Task task,
		ILogger logger,
		string errorMessage
	)
	{
		try
		{
			await task;
		}
		catch (Exception ex)
		{
			logger.Error(ex, errorMessage);
		}
	}
}
