using Microsoft.Extensions.Hosting;
using Orleans.Concurrency;

namespace Odin.Orleans.Core.Reactive;

public interface IOdinReactiveReplicaGrain;

[Reentrant]
public abstract class OdinReactiveReplicaGrain<TState>(
	ILogger<OdinReactiveReplicaGrain<TState>> logger,
	ILoggingContext loggingContext,
	IHostApplicationLifetime lifetime
) : OdinGrain(logger, loggingContext), IOdinReactiveReplicaGrain
	where TState : new()
{
	private IDisposable? _pollTimer;
	private Guid _versionToken = Guid.Empty;
	private TimeSpan? _timeToLive = TimeSpan.Zero;

	protected TState State = new();

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();
		_pollTimer = RegisterTimer();
	}

	public virtual Task<TState> Get()
		=> Task.FromResult(State);

	// throttled polling
	private async Task Poll()
	{
		if (lifetime.ApplicationStopping.IsCancellationRequested)
			return;

		try
		{
			var reactiveGrain = GetReactiveGrain();
			var result = await reactiveGrain.TryWait(_versionToken);

			if (result is null)
				return;

			_versionToken = result.VersionToken;
			_timeToLive = result.TimeToLive;
			State = result.Model ?? new();

			if (result.IsFaulted)
			{
				DeactivateOnIdle();
				logger.LogWarning("Reactive grain {grainId} reported faulted state, deactivating replica", PrimaryKey);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to poll reactive grain {grainId}", PrimaryKey);
			DeactivateOnIdle();
		}
	}

	protected abstract IOdinReactiveGrain<TState> GetReactiveGrain();

	protected virtual IGrainTimer RegisterTimer(TimeSpan? dueTime = null, TimeSpan? interval = null)
		=> this.RegisterGrainTimer(Poll, new() { DueTime = dueTime ?? TimeSpan.FromMinutes(1), Period = interval ?? TimeSpan.FromSeconds(30) });

	public override async Task OnOdinDeactivate()
	{
		await base.OnOdinDeactivate();
		_pollTimer?.Dispose();
	}
}
