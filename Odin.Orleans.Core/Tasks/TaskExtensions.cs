using Orleans.Concurrency;

namespace Odin.Orleans.Core.Tasks;

public static class TaskExtensions
{
	public static async Task<Immutable<TResponse>> AsImmutable<TResponse>(this Task<TResponse> task)
	{
		var response = await task;
		return response.AsImmutable();
	}
}
