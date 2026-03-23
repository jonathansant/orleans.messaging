using Microsoft.Extensions.Hosting;
using Odin.Core.FlowControl;
using Odin.Core.Paging;
using Orleans.Concurrency;
using OrleansDashboard;
using System.Collections;

namespace Odin.Orleans.Core.Reactive;

[GenerateSerializer]
public record ReactiveResult<TModel>(
	TModel? Model,
	TimeSpan? TimeToLive,
	Guid VersionToken,
	bool IsFaulted = false
);

public interface IOdinReactiveGrain<TState>
{
	Task<TState?> Get();

	Task<ReactiveResult<TState>?> TryWait(Guid versionToken);
}

// todo: add OdinReactiveIncrementalGrain that only sends diffs
[Reentrant]
public abstract class OdinReactiveGrain<TState>(
	ILogger<OdinReactiveGrain<TState>> logger,
	ILoggingContext loggingContext,
	IHostApplicationLifetime lifetime
) : OdinGrain(logger, loggingContext), IOdinReactiveGrain<TState>
	where TState : new()
{
	private Guid _versionToken = Guid.Empty;
	private TaskCompletionSource<ReactiveResult<TState>> _completion = new();
	private TState _state = new();
	protected TimeSpan TimeToLive = TimeSpan.FromMinutes(10);

	protected ScheduledThrottledAction? WriteThrottledAction;

	protected IPersistentState<TState>? Store = null;

	public override async Task OnOdinActivate()
	{
		if (Store != null)
		{
			WriteThrottledAction ??= this.CreateScheduledThrottledAction(
					_ => Store.WriteStateAsync(),
					opts =>
					{
						opts.ThrottleTime = TimeSpan.FromSeconds(5);
						opts.FlushScheduledOnDispose = true;
						opts.HasLeadingDelay = true;
					}
				);
		}

		await base.OnOdinActivate();
	}

	public virtual async Task<TState?> Get()
	{
		try
		{
			return State = State.IsNullOrEmpty() ? await Load() : State;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to refresh Reactive Grain state {@PrimaryKey}, deactivating", PrimaryKey);
			_completion.SetResult(new(State, TimeToLive, _versionToken, true));
			_completion = new();
			State = default;
			return State;
		}
	}

	protected virtual Task<TState?> Load() => Task.FromResult(State);

	/// <summary>
	/// Fulfill reactive requests
	/// </summary>
	protected async Task TriggerReactiveCache()
	{
		_versionToken = Guid.NewGuid();
		_completion.SetResult(new(State, TimeSpan.Zero, _versionToken));
		_completion = new();

		if (WriteThrottledAction is not null)
			await WriteThrottledAction.Trigger();
	}

	// ignore dashboard profiling for this method as it is long-running
	[NoProfiling]
	public async Task<ReactiveResult<TState>?> TryWait(Guid versionToken)
	{
		// resolve the request immediately if the caller has a different version
		if (versionToken != _versionToken)
			return new(State, TimeSpan.Zero, _versionToken);

		// pin the completion to avoid reentrancy issues
		var completion = _completion;

		if (completion.Task.IsCompleted)
			return completion.Task.Result;

		// wait for the completion to resolve or a timeout
		if (await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(20), lifetime.ApplicationStopping)) == completion.Task)
			// fulfill the request with the new data version
			return await completion.Task;

		// this means we don't have anything new
		return null;
	}

	public override async Task OnOdinDeactivate()
	{
		await base.OnOdinDeactivate();
		if (WriteThrottledAction != null)
			await WriteThrottledAction.DisposeAsync();
	}

	protected TState? State
	{
		get => GetState();
		private set => SetState(value);
	}

	private TState GetState()
		=> Store == null ? _state : Store!.State;

	private void SetState(TState? value)
	{
		if (Store != null)
			Store.State = value;
		else
			_state = value;
	}
}

public static class StorageStateExtensions
{
	public static bool IsNullOrEmpty<TState>(this TState state)
	{
		if (state == null)
			return true;

		return state switch
		{
			ICollection collection => collection.Count == 0,
			IPageContext pageContext => pageContext.TotalItems == 0,
			_ => false
		};
	}
}
