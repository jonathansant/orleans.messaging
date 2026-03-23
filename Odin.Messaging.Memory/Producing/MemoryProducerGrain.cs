using Odin.Messaging.Consuming;
using Odin.Messaging.Memory.Config;
using Odin.Messaging.Memory.Utilities;
using Odin.Messaging.Subscription;
using Odin.Orleans.Core;
using Odin.Orleans.Core.Tenancy;
using Orleans.Concurrency;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Web;

namespace Odin.Messaging.Memory.Producing;

public static partial class GrainFactoryExtensions
{
	public static IMemoryProducerGrain<TMessage> GetMemoryProducerGrain<TMessage>(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName,
		string partitioningKey
	) => grainFactory.GetGrain<IMemoryProducerGrain<TMessage>>(
		GenerateProducerGrainKey(serviceKey, queueName, partitioningKey)
	);

	public static string GenerateProducerGrainKey(string serviceKey, string queueName, string partitioningKey)
		=> $"odinMessagingMemoryProducer/{serviceKey}/{queueName}/{HttpUtility.UrlEncode(partitioningKey)}";
}

public interface IMemoryProducerGrain : IOdinGrainContract, IGrainWithStringKey
{
	Task Produce(Immutable<OdinMessage> message);

	ValueTask RefreshSubscriptionList(Immutable<Dictionary<string, PatternOptions>> subscriptionTable);
}

public interface IMemoryProducerGrain<TMessage> : IMemoryProducerGrain
{
	Task Produce(Immutable<OdinMessage<TMessage>> message);
}

[SharedTenant]
public class MemoryProducerGrain<TMessage> : OdinGrain, IMemoryProducerGrain<TMessage>
{
	private readonly ILogger<MemoryProducerGrain<TMessage>> _logger;
	private readonly IGrainFactory _grainFactory;
	private readonly IOdinDigestingUtilityService _digestingUtilityService;
	private readonly OdinMessagingMemoryOptions _options;

	private readonly MemoryProducerGrainKey _key;

	private static readonly Type MessageType = typeof(TMessage);
	private readonly IPersistentState<MemoryProducerGrainState> _store;
	private IDisposable _timer;
	private readonly IMessagingMemoryRuntimeOptionsService _runtimeOptionsService;

	public MemoryProducerGrain(
		ILogger<MemoryProducerGrain<TMessage>> logger,
		ILoggingContext loggingContext,
		IOptionsMonitor<OdinMessagingMemoryOptions> optionsMonitor,
		IServiceProvider serviceProvider,
		IGrainContext grainContext,
		IPersistentStateFactory persistentStateFactory,
		IGrainFactory grainFactory,
		IOdinDigestingUtilityServiceFactory digestingUtilityServiceFactory
	) : base(logger, loggingContext)
	{
		_key = this.ParseKey<MemoryProducerGrainKey>(MemoryProducerGrainKey.Template);
		_key.PartitioningKey = HttpUtility.UrlDecode(_key.PartitioningKey);
		_logger = logger;
		_grainFactory = grainFactory;
		_digestingUtilityService = digestingUtilityServiceFactory.Create(_key.ServiceKey, _key.QueueName);
		_runtimeOptionsService =
			serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(_key.ServiceKey) as
				IMessagingMemoryRuntimeOptionsService;

		_options = optionsMonitor.Get(_key.ServiceKey);

		_store = persistentStateFactory.Create<MemoryProducerGrainState>(
			grainContext,
			new PersistentStateAttribute("memoryProducer", _options.StoreName)
		);
	}

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();

		_runtimeOptionsService.RegisterQueueType(_key.QueueName, MessageType);

		var subscriptionIndexGrain = GrainFactory.GetSubscriptionIndexGrain(_key.ServiceKey, _key.QueueName);
		var subscriptions = await subscriptionIndexGrain.RegisterConsumer(_key.QueueName, _key.PartitioningKey);

		_digestingUtilityService.UpdateCache(subscriptions);

		_timer = (this as IOdinGrain).RegisterTimer(
			async _ => await ProduceToSubscriptions(),
			null,
			TimeSpan.FromMilliseconds(_options.ProduceInitDelayMs),
			TimeSpan.FromMilliseconds(_options.ProducePollRateMs),
			isInterleave: false
		);
	}

	public async Task Produce(Immutable<OdinMessage> message)
	{
		var subscriptionBatches = new Dictionary<string, List<BatchResult>>();
		_digestingUtilityService.PopulateSubscriptionBatch(ref subscriptionBatches, message.Value);

		foreach (var batch in subscriptionBatches)
		{
			_store.State.MessageQueue
				.GetOrAdd(batch.Key, _ => ([], 0))
				.Batch
				.AddRange(batch.Value.Select(x => x.Message));
		}

		await _store.WriteStateAsync();
	}

	public Task Produce(Immutable<OdinMessage<TMessage>> message)
		=> Produce((message.Value as OdinMessage).AsImmutable());

	public ValueTask RefreshSubscriptionList(Immutable<Dictionary<string, PatternOptions>> subscriptionTable)
	{
		_digestingUtilityService.UpdateCache(subscriptionTable.Value);
		return ValueTask.CompletedTask;
	}

	public override async Task OnOdinDeactivate()
	{
		await base.OnOdinDeactivate();
		_timer.Dispose();

		if (!_store.State.MessageQueue.IsNullOrEmpty())
			await ProduceToSubscriptions();
	}

	private async Task ProduceToSubscriptions()
	{
		Dictionary<string, (List<OdinMessage> Batch, int RetryCount)> failedBatchKeys = new();
		await _store.State.MessageQueue.ForEachAsync(async subBatch =>
			{
				try
				{
					var subscriptionGrain = _digestingUtilityService.GetSubscriptionGrain(_key.QueueName, subBatch.Key);
					await subscriptionGrain.Push(subBatch.Value.Batch.ToImmutableList());

					LogMessagePush(subBatch);
				}
				catch (Exception ex)
				{
					_logger.LogError(
						ex,
						"Failed to produce message with key {key}, subscription batch key: {batchKey} on queue {queue}",
						_key.PartitioningKey,
						subBatch.Key,
						_key.QueueName
					);

					failedBatchKeys.Add(
						subBatch.Key,
						subBatch.Value with { RetryCount = subBatch.Value.RetryCount + 1 }
					);
				}
			}
		);

		ClearQueue(failedBatchKeys);
		await _store.WriteStateAsync();
	}

	private void ClearQueue(Dictionary<string, (List<OdinMessage> Batch, int RetryCount)> failedBatches)
	{
		_store.State.MessageQueue.Clear();

		if (!_options.EnsureHandlerDeliveryOnFailure)
			return;

		var validFailedBatches = failedBatches
			.Where(failedBatch => _options.ProducerRetryOptions.MaxRetries > failedBatch.Value.RetryCount);

		foreach (var failedBatch in validFailedBatches)
		{
			_store.State.MessageQueue.TryAdd(failedBatch.Key, failedBatch.Value);
		}
	}

	private void LogMessagePush(KeyValuePair<string, (List<OdinMessage> Batch, int RetryCount)> messages)
	{
		if (!_logger.IsEnabled(LogLevel.Information))
			return;

		var messageIds = new StringBuilder();
		var headers = new StringBuilder();
		var i = 0;
		foreach (var message in messages.Value.Batch)
		{
			messageIds.Append(message.MessageId);
			var notLast = i < messages.Value.Batch.Count - 1;
			if (notLast)
				messageIds.Append(' ');

			// todo: log message
			headers.Append(string.Join(',', message.Headers.Select(x => $"[{x.Key.ToString()}: {x.Value.ToString()}]")));
			if (notLast)
				headers.Append(' ');

			i++;
		}

		_logger.PushingBatch(messages.Key, messageIds.ToString(), headers.ToString());
	}
}

internal static partial class LogExtensions
{
	[LoggerMessage(
		Level = LogLevel.Debug,
		Message = "Pulled message with messageId {messageId} key {messageKey} headers {headers}"
	)]
	internal static partial void PulledMessage(this ILogger logger, string messageId, string messageKey, string headers);

	[LoggerMessage(
		Level = LogLevel.Debug,
		Message = "Pushing batch to pattern {pattern}, messageIds {messageIds}, headers {headers}"
	)]
	internal static partial void PushingBatch(this ILogger logger, string pattern, string messageIds, string headers);
}

public struct MemoryProducerGrainKey
{
	public const string Template = "odinMessagingMemoryProducer/{serviceKey}/{queueName}/{partitioningKey}";

	public string ServiceKey { get; set; }
	public string QueueName { get; set; }
	public string PartitioningKey { get; set; }
}

[GenerateSerializer]
public record MemoryProducerGrainState
{
	[Id(0)]
	public ConcurrentDictionary<string, (List<OdinMessage> Batch, int RetryCount)> MessageQueue { get; set; } = new();
}
