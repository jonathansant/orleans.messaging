using Orleans.Streams;

namespace Odin.Orleans.Core.Streaming;

public interface IStreamService
{
	Task Init();
	Task TearDown();

	/// <summary>
	/// Adds a stream to be subscribed to. Handles resuming and error handling.
	/// </summary>
	/// <remarks>If no Id is specified the grain primary key will be used as [stream] id.</remarks>
	IAsyncStream<TMessage> Add<TMessage>(
		StreamIdentity streamIdentity,
		Func<string, IStreamProvider> streamProviderFactory,
		Func<TMessage, StreamSequenceToken, Task> handler,
		Func<string>? primaryKeyFactory = null,
		bool resume = true
	);

	/// <summary>
	/// Adds all streams in the container for subscription.
	/// </summary>
	void AddStreamPartitionContainer<TMessage>(
		StreamPartitionContainer<TMessage> streamPartitionContainer,
		Func<TMessage, StreamSequenceToken, Task> handler,
		bool resume = true
	);

	Task Subscribe<TMessage>(
		StreamIdentity streamIdentity,
		Func<string, IStreamProvider> streamProviderFactory,
		Func<TMessage, StreamSequenceToken, Task> handler,
		Func<string>? primaryKeyFactory = null,
		bool resume = true
	);

	Task Remove(string key);
	Task Remove(StreamIdentity streamIdentity);
	bool Has(string key);
	bool Has(StreamIdentity streamIdentity);
}

public class StreamService : IStreamService
{
	private readonly ILogger _logger;
	private readonly IDictionary<string, IStreamProvider> _providers;
	private readonly IDictionary<string, StreamWrapper> _streams;

	public StreamService(
		ILogger<StreamService> logger
	)
	{
		_logger = logger;
		_providers = new Dictionary<string, IStreamProvider>();
		_streams = new Dictionary<string, StreamWrapper>();
	}

	public async Task Init()
	{
		await Task.WhenAll(_streams.Values.Select(stream => stream.SubscribeAsync()));

		// clean-up we don't need to keep references since they were only useful while initializing the Grain
		_providers.Clear();
	}

	public async Task TearDown()
	{
		await Task.WhenAll(_streams.Values.Select(stream => stream.UnsubscribeAsync()));
		_streams.Clear();
	}

	/// <inheritdoc />
	public IAsyncStream<TMessage> Add<TMessage>(
		StreamIdentity streamIdentity,
		Func<string, IStreamProvider> streamProviderFactory,
		Func<TMessage, StreamSequenceToken, Task> handler,
		Func<string>? primaryKeyFactory = null,
		bool resume = true
	) => AddStream(streamIdentity, streamProviderFactory, handler, primaryKeyFactory, resume).Stream;

	public void AddStreamPartitionContainer<TMessage>(
		StreamPartitionContainer<TMessage> streamPartitionContainer,
		Func<TMessage, StreamSequenceToken, Task> handler,
		bool resume = true
	)
	{
		foreach (var stream in streamPartitionContainer.Streams)
		{
			var streamIdentity = new StreamIdentity
			{
				Id = streamPartitionContainer.StreamId,
				ProviderName = streamPartitionContainer.ProviderName,
				Namespace = streamPartitionContainer.StreamNamespace
			};

			var wrapper = new StreamWrapper<TMessage>(_logger)
			{
				Stream = stream,
				Handler = (message, sequence) => MessageHandler(handler, streamIdentity.Id, message, sequence),
				Resume = resume
			};

			_streams.Add(streamIdentity.ToString(), wrapper);
		}
	}

	public async Task Subscribe<TMessage>(
		StreamIdentity streamIdentity,
		Func<string, IStreamProvider> streamProviderFactory,
		Func<TMessage, StreamSequenceToken, Task> handler,
		Func<string>? primaryKeyFactory = null,
		bool resume = true
	)
	{
		var stream = AddStream(streamIdentity, streamProviderFactory, handler, primaryKeyFactory, resume);
		await stream.SubscribeAsync();
	}

	public async Task Remove(string key)
	{
		if (!_streams.TryGetValue(key, out var stream))
			return;

		await stream.UnsubscribeAsync();
		_streams.Remove(key);
	}

	public Task Remove(StreamIdentity streamIdentity)
		=> Remove(streamIdentity.ToString());

	public bool Has(string key)
		=> _streams.ContainsKey(key);

	public bool Has(StreamIdentity streamIdentity)
		=> Has(streamIdentity.ToString());

	private StreamWrapper<TMessage> AddStream<TMessage>(
		StreamIdentity streamIdentity,
		Func<string, IStreamProvider> streamProviderFactory,
		Func<TMessage, StreamSequenceToken, Task> handler,
		Func<string>? primaryKeyFactory = null,
		bool resume = true
	)
	{
		if (!_providers.TryGetValue(streamIdentity.Namespace, out var provider))
		{
			provider = streamProviderFactory(streamIdentity.ProviderName);
			_providers.Add(streamIdentity.Namespace, provider);
		}

		var streamId = streamIdentity.Id;
		if (streamId.IsNullOrEmpty())
		{
			streamId = primaryKeyFactory?.Invoke();
			if (streamId.IsNullOrEmpty())
				throw new InvalidOperationException("StreamId must be provided or primaryKeyFactory must be provided.");
		}

		var stream = new StreamWrapper<TMessage>(_logger)
		{
			Stream = provider.GetStream<TMessage>(streamIdentity.Namespace, streamId),
			Handler = (message, sequence) => MessageHandler(handler, streamId, message, sequence),
			Resume = resume
		};

		_streams.Add(streamIdentity.ToString(), stream);

		return stream;
	}

	private async Task MessageHandler<TMessage>(
		Func<TMessage, StreamSequenceToken, Task> handler,
		string streamId,
		TMessage message,
		StreamSequenceToken? sequenceToken = null
	)
	{
		try
		{
			// We need to await to catch any errors.
			await handler(message, sequenceToken);
		}
		catch (Exception ex)
		{
			// todo: retry logic. Currently if a message fails it will be discarded and we lose it.
			_logger.Error(
				ex,
				"Failed to update: {Id}. Message type: {messageType} due to: {exceptionMessage} stack trace: {stack}",
				streamId,
				typeof(TMessage).Name,
				ex.Message,
				ex.StackTrace
			);
		}
	}
}
