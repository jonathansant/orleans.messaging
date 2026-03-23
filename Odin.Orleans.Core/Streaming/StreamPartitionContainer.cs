using Orleans.Streams;
using System.Collections.ObjectModel;

namespace Odin.Orleans.Core.Streaming;

// todo: move to Stream Utils?
public static class StreamPartitionExtensions
{
	public static string BuildPartitionStreamName(string streamName, int partitionIndex)
		=> $"{streamName}:{partitionIndex}";

	public static string BuildPartitionStreamNameRandom(string streamName, int partitions)
		=> BuildPartitionStreamName(streamName, Randomizer.Next(0, partitions));

	private static readonly Random Randomizer = new();

	/// <summary>
	/// Get stream partition either randomly according to the size given OR consistent based on the <paramref name="consistentId"/> (and partitioned based on its hash).
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="streamProvider"></param>
	/// <param name="streamId"></param>
	/// <param name="streamNamespace"></param>
	/// <param name="partitions">Max partitions to obtain stream from.</param>
	/// <param name="consistentId">Consistent value to generate partition id for. e.g. primary key.</param>
	/// <returns></returns>
	public static IAsyncStream<T> GetStreamPartition<T>(
		this IStreamProvider streamProvider,
		Guid streamId,
		string streamNamespace,
		int partitions,
		string? consistentId = null
	)
	{
		var ns = string.IsNullOrEmpty(consistentId)
				? BuildPartitionStreamNameRandom(streamNamespace, partitions)
				: BuildPartitionStreamName(streamNamespace, consistentId.ToPartitionIndex((uint)partitions));
		return streamProvider.GetStream<T>(ns, streamId.ToString());
	}

	public static async Task ResumeAllSubscriptionHandlers<T>(this IAsyncStream<T> stream, Func<T, StreamSequenceToken, Task> onNextAsync)
	{
		var subscriptions = await stream.GetAllSubscriptionHandles();
		if (subscriptions?.Count > 0)
		{
			var tasks = subscriptions.Select(x => x.ResumeAsync(onNextAsync));
			await Task.WhenAll(tasks);
		}
	}

	public static async Task UnsubscribeAllSubscriptionHandlers<T>(this IAsyncStream<T> stream)
	{
		var subscriptions = await stream.GetAllSubscriptionHandles();
		if (subscriptions?.Count > 0)
		{
			var tasks = subscriptions.Select(x => x.UnsubscribeAsync());
			await Task.WhenAll(tasks);
		}
	}

	/// <summary>
	/// Add partition to the namespace.
	/// </summary>
	/// <param name="streamIdentity">StreamIdentity to add partition index to.</param>
	/// <param name="partitions">Max available partitions.</param>
	/// <param name="consistentId">Consistent value to generate partition id for. e.g. primary key.</param>
	/// <returns></returns>
	public static StreamIdentity AddPartitionName(this StreamIdentity streamIdentity, int partitions, string? consistentId = null)
	{
		streamIdentity.Namespace = consistentId.IsNullOrEmpty()
			? BuildPartitionStreamNameRandom(streamIdentity.Namespace, partitions)
			: BuildPartitionStreamName(streamIdentity.Namespace, consistentId.ToPartitionIndex((uint)partitions))
			;
		return streamIdentity;
	}
}

/// <summary>
/// Contains streams partitioned for listening and managing multiple partitions easier.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class StreamPartitionContainer<T>
{
	protected string DebuggerDisplay => $"StreamId: '{StreamId}', StreamNamespace: '{StreamNamespace}', MaxPartitions: {MaxPartitions}";

	public string StreamId { get; init; }
	public string StreamNamespace { get; init; }
	public string ProviderName { get; init; }
	public int MaxPartitions { get; init; }
	public ReadOnlyCollection<IAsyncStream<T>> Streams { get; init; }

	private readonly List<IAsyncStream<T>> _streams = new();

	public StreamPartitionContainer(Func<string, IStreamProvider> streamProviderFactory, StreamIdentity streamIdentity, int maxPartitions)
		: this(streamProviderFactory(streamIdentity.ProviderName), streamIdentity.Id, streamIdentity.Namespace, maxPartitions)
	{
	}

	/// <summary>
	/// Create a new instance of stream with partitions.
	/// </summary>
	/// <param name="streamProvider"></param>
	/// <param name="streamId"></param>
	/// <param name="streamNamespace"></param>
	/// <param name="maxPartitions">Max partitions to create.</param>
	public StreamPartitionContainer(IStreamProvider streamProvider, string streamId, string streamNamespace, int maxPartitions)
	{
		Streams = _streams.AsReadOnly();
		StreamId = streamId;
		StreamNamespace = streamNamespace;
		MaxPartitions = maxPartitions;
		ProviderName = streamProvider.Name;

		for (var i = 0; i < maxPartitions; i++)
		{
			var namespacePartition = StreamPartitionExtensions.BuildPartitionStreamName(streamNamespace, i);
			_streams.Add(streamProvider.GetStream<T>(namespacePartition, streamId));
		}
	}

	public StreamPartitionContainer(
		IStreamProvider streamProvider,
		Guid streamId,
		string streamNamespace,
		int maxPartitions
	) : this(streamProvider, streamId.ToString(), streamNamespace, maxPartitions)
	{
	}

	public async Task SubscribeAsync(Func<T, StreamSequenceToken, Task> onNextAsync)
		=> await _streams.ForEachAsync(stream => stream.SubscribeAsync(onNextAsync));

	public async Task<IList<StreamSubscriptionHandle<T>>> GetAllSubscriptionHandles()
	{
		var handles = await _streams.SelectAsync(stream => stream.GetAllSubscriptionHandles());
		return handles.SelectMany(x => x).ToList();
	}

	/// <summary>
	/// Publishes message in all partitions.
	/// </summary>
	/// <param name="item"></param>
	/// <param name="token"></param>
	public async Task OnNextAsync(T item, StreamSequenceToken? token = null)
		=> await _streams.ForEachAsync(stream => stream.OnNextAsync(item, token));
}
