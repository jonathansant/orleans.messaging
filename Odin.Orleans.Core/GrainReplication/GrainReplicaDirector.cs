using Orleans.Streams;

namespace Odin.Orleans.Core.GrainReplication;

public interface IGrainReplicaDirector
{
	/// <summary>
	/// Get/builds the key including the replica index.
	/// </summary>
	/// <param name="grainKey">Grain key to manipulate.</param>
	/// <returns></returns>
	string GetKey(string grainKey);

	/// <summary>
	/// Makes sure that it produces one and only one of the same message at a given time.
	/// </summary>
	/// <typeparam name="TMessage">The type of the message.</typeparam>
	/// <param name="stream">The stream.</param>
	/// <param name="grainKey">The grain primary key.</param>
	/// <param name="message">The message.</param>
	/// <returns></returns>
	Task ProduceMessage<TMessage>(IAsyncStream<TMessage> stream, string grainKey, TMessage message);

	/// <summary>
	/// Makes sure that it executes once.
	/// </summary>
	/// <param name="task"> Action to execute once.</param>
	/// <param name="uniqueKey">A Unique key per action.</param>
	/// <param name="grainKey">The grain primary key.</param>
	/// <returns></returns>
	Task ExecuteOnce(Func<Task> func, string uniqueKey, string grainKey);
}

public interface IGrainReplicaDirector<TGrain>
	: IGrainReplicaDirector where TGrain : IGrainWithStringKey
{
}

public class GrainReplicaDirector<TGrain> : IGrainReplicaDirector<TGrain>
	where TGrain : IGrainWithStringKey
{
	private readonly IGrainFactory _grainFactory;
	private readonly GrainReplicaStrategy _strategy;
	private readonly ILogger _logger;

	public GrainReplicaDirector(
		IGrainFactory grainFactory,
		IServiceProvider serviceProvider,
		ILogger<GrainReplicaDirector<TGrain>> logger
	)
	{
		_grainFactory = grainFactory;
		_logger = logger;
		_strategy = GrainReplicaStrategy.Create<TGrain>(serviceProvider);
	}

	public string GetKey(string grainKey)
		=> $"{grainKey}:{_strategy.CalculateResponsible(grainKey)}";

	public async Task ProduceMessage<TMessage>(IAsyncStream<TMessage> stream, string grainKey, TMessage message)
	{
		var hash = await message.ComputeHash();
		var uniqueKey = $"{stream}:{hash}";
		await ExecuteOnce(() => stream.OnNextAsync(message), uniqueKey, grainKey);
	}

	public async Task ExecuteOnce(Func<Task> func, string uniqueKey, string grainKey)
	{
		var sessionKey = $"{grainKey}:{uniqueKey}";
		var sessionGrain = _grainFactory.GetReplicaSessionGrain(sessionKey);

		await using var streamLock = await sessionGrain.AcquireLock();
		_logger.Debug($"Executing {func.Method.Name} with sessionKey {sessionKey} and hasAcquiredLock = {streamLock.IsResponsible}");
		if (streamLock.IsResponsible)
			await func();
	}
}
