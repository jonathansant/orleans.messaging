using Odin.Core.FlowControl;
using Odin.Orleans.Core.Tenancy;

namespace Odin.Orleans.Core.GrainReplication;

public interface IGrainReplicaSessionGrain : IGrainWithStringKey
{
	Task<ReplicaSession> AcquireLock();
	Task ReleaseLock();
}

[SharedTenant]
public class GrainReplicaSessionGrain : OdinGrain, IGrainReplicaSessionGrain
{
	private bool _isLocked;
	private readonly Cooldown _cooldown;

	public GrainReplicaSessionGrain(
		ILogger<IGrainReplicaSessionGrain> logger,
		ILoggingContext loggingContext
	) : base(logger, loggingContext)
	{
		_cooldown = new Cooldown(TimeSpan.FromMilliseconds(100));
	}

	public Task<ReplicaSession> AcquireLock()
	{
		if (_cooldown.IsOnCooldown)
			return Task.FromResult(new ReplicaSession(this, false));

		var lockAcquired = !_isLocked;
		_isLocked = true;

		return Task.FromResult(new ReplicaSession(this, lockAcquired));
	}

	public Task ReleaseLock()
	{
		_isLocked = false;
		_cooldown.Set();
		return Task.CompletedTask;
	}
}

[GenerateSerializer]
public class ReplicaSession : IAsyncDisposable
{
	[Id(0)]
	private readonly IGrainReplicaSessionGrain _sessionGrain;

	[Id(1)]
	public bool IsResponsible { get; }

	public ReplicaSession(
		IGrainReplicaSessionGrain sessionGrain,
		bool isResponsible
	)
	{
		_sessionGrain = sessionGrain;
		IsResponsible = isResponsible;
	}

	public async ValueTask DisposeAsync()
	{
		if (IsResponsible)
			await _sessionGrain.ReleaseLock();
	}
}
