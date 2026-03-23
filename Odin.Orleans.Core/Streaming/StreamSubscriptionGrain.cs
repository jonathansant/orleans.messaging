using Orleans.Streams;

namespace Odin.Orleans.Core.Streaming;

public interface IStreamSubscriptionGrain
{
	Task InitializeStreams();
	Task TearDownStreams();
	/// <summary>
	/// Adds a stream to be subscribed to. Handles resuming and error handling. Ensure to call <see cref="InitializeStreams"/> afterwards in order to subscribe.
	/// </summary>
	/// <remarks>If no Id is specified the grain primary key will be used as stream id.</remarks>
	/// <typeparam name="TMessage">The type of the message.</typeparam>
	/// <param name="streamIdentity">The stream identity.</param>
	/// <param name="handler">The handler.</param>
	void AddStream<TMessage>(StreamIdentity streamIdentity, Func<TMessage, StreamSequenceToken, Task> handler);
}

// todo: segregate an interface for SubscriptionGrain and implement method as extensions
// todo: merge with OdinGrain
/// <summary>
/// A grain that handles stream subscriptions and un-subscriptions.
/// </summary>
/// <typeparam name="T">GrainState Type</typeparam>
public abstract class StreamSubscriptionGrain<T> : OdinGrain<T>, IStreamSubscriptionGrain
	where T : new()
{
	private readonly IStreamService _streamService;

	protected StreamSubscriptionGrain(
		ILogger logger,
		ILoggingContext loggingContext,
		IStreamService streamService
	) : base(logger, loggingContext)
	{
		_streamService = streamService;
	}

	public async Task InitializeStreams() => await _streamService.Init();

	public async Task TearDownStreams() => await _streamService.TearDown();

	/// <inheritdoc />
	public void AddStream<TMessage>(
		StreamIdentity streamIdentity,
		Func<TMessage, StreamSequenceToken, Task> handler
	) => _streamService.Add(streamIdentity, this.GetStreamProvider, handler, this.GetPrimaryKeyString);

	protected Task RemoveStream(StreamIdentity streamIdentity) => _streamService.Remove(streamIdentity);
}

/// <summary>
/// A grain that handles stream subscriptions and un-subscriptions.
/// </summary>
public abstract class StreamSubscriptionGrain : OdinGrain, IStreamSubscriptionGrain
{
	protected IStreamService StreamService { get; init; }

	protected StreamSubscriptionGrain(
		ILogger logger,
		ILoggingContext loggingContext,
		IStreamService streamService
	) : base(logger, loggingContext)
	{
		StreamService = streamService;
	}

	public async Task InitializeStreams() => await StreamService.Init();

	public async Task TearDownStreams() => await StreamService.TearDown();

	/// <inheritdoc />
	public void AddStream<TMessage>(StreamIdentity streamIdentity, Func<TMessage, StreamSequenceToken, Task> handler)
		=> StreamService.Add(streamIdentity, this.GetStreamProvider, handler, this.GetPrimaryKeyString);

	protected Task RemoveStream(StreamIdentity streamIdentity) => StreamService.Remove(streamIdentity);
}
