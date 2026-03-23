namespace Odin.Orleans.Core.BackgroundWorkload;

public enum BackgroundWorkloadState
{
	NotStarted,
	Started,
	Completed,
	Exception
}

[GenerateSerializer]
public record BackgroundWorkload<TResponse>
{
	[Id(0)]
	public BackgroundWorkloadState State { get; internal set; } = BackgroundWorkloadState.NotStarted;
	[Id(1)]
	public TResponse? Data { get; internal set; }
	[Id(2)]
	public Exception? Exception { get; internal set; }
}

public abstract class OdinLongRunningGrain<TRequest, TResponse, TInterface> : Grain, IOdinLongRunningGrain<TRequest, TResponse>
	where TInterface : IOdinLongRunningGrain<TRequest, TResponse>
{
	private readonly BackgroundWorkload<TResponse> _result = new();
	private Task? _task;

	public Task<bool> StartAsync(TRequest request)
		=> StartAsync(request, CancellationToken.None);

	public Task<bool> StartAsync(TRequest request, GrainCancellationToken cancellationToken)
		=> StartAsync(request, cancellationToken.CancellationToken);

	public Task CompleteAsync(TResponse response)
	{
		if (_result.State is not BackgroundWorkloadState.Started)
			return Task.CompletedTask;

		_task = null;

		_result.State = BackgroundWorkloadState.Completed;
		_result.Data = response;
		return Task.CompletedTask;
	}

	public Task FailedAsync(Exception exception)
	{
		if (_result.State is not BackgroundWorkloadState.Started)
			return Task.CompletedTask;

		_task = null;
		_result.State = BackgroundWorkloadState.Exception;
		_result.Exception = exception;

		return Task.CompletedTask;
	}

	public Task<BackgroundWorkload<TResponse>> GetResultAsync()
		=> Task.FromResult(_result);

	protected abstract Task<TResponse> ProcessAsync(TRequest request, CancellationToken cancellationToken);

	private Task<bool> StartAsync(TRequest request, CancellationToken cancellationToken)
	{
		if (_task != null)
			return Task.FromResult(false);

		_result.State = BackgroundWorkloadState.Started;
		_task = CreateTask(request, cancellationToken, TaskScheduler.Current);

		return Task.FromResult(true);
	}

	private Task CreateTask(TRequest request, CancellationToken cancellationToken, TaskScheduler orleansTaskScheduler)
		=> Task.Run(async () =>
		{
			try
			{
				var response = await ProcessAsync(request, cancellationToken);
				await InvokeGrainAsync(orleansTaskScheduler, grain => grain.CompleteAsync(response));
			}
			catch (Exception exception)
			{
				await InvokeGrainAsync(orleansTaskScheduler, grain => grain.FailedAsync(exception));
			}
		});

	private Task InvokeGrainAsync(TaskScheduler orleansTaskScheduler, Func<TInterface, Task> action) =>
		Task.Factory.StartNew(async () =>
		{
			var grain = GrainFactory.GetGrain<TInterface>(this.GetPrimaryKeyString());
			await action(grain);
		}, CancellationToken.None, TaskCreationOptions.None, orleansTaskScheduler);
}
