using Confluent.Kafka;
using Odin.Messaging.Consuming;
using Odin.Messaging.Kafka.Config;
using Odin.Messaging.Kafka.Producing;
using Odin.Messaging.Kafka.Serialization;
using Odin.Messaging.SerDes;
using Odin.Messaging.Subscription;
using Odin.Orleans.Core;
using Odin.Orleans.Core.Tenancy;
using Orleans.Concurrency;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;

namespace Odin.Messaging.Kafka.Consuming;

public static partial class GrainFactoryExtensions
{
	public static IConsumerGrain GetConsumerGrain(this IGrainFactory grainFactory, string serviceKey, string topic, string partition)
		=> grainFactory.GetGrain<IConsumerGrain>($"odinKafkaConsumer/{serviceKey}/{topic}/{partition}");
}

public interface IConsumerGrain : IGrainWithStringKey
{
	[AlwaysInterleave]
	ValueTask RefreshSubscriptionList(Dictionary<string, PatternOptions> subscriptions);

	Task Initialize();
	Task ConsumeAndPublish();
	ValueTask Rebalance();
}

[SharedTenant]
public sealed class ConsumerGrain : OdinGrain, IConsumerGrain
{
	private Type _dlqProducerGrainByteType;
	private readonly ConsumerGrainKey _keyData;
	private IConsumer<byte[], byte[]> _consumer;

	private TopicConfig _topicConfig;

	private IOdinMessageSerializer _messageSerializer;
	private bool _shouldKeepBytePayload;
	private OdinMessage _emptyMessage;
	private readonly OdinMessage<byte[]> _emptyByteMessage = new();

	private readonly IPersistentState<ConsumerGrainState> _store;

	private readonly ILogger<ConsumerGrain> _logger;
	private readonly IOdinDigestingUtilityService _digestingService;
	private readonly OdinMessagingKafkaOptions _options;
	private readonly ITopicSerializerResolver _serializerResolver;
	private readonly IMessagingKafkaRuntimeOptionsService _runtimeOptionsService;

	public ConsumerGrain(
		ILogger<ConsumerGrain> logger,
		ILoggingContext loggingContext,
		IPersistentStateFactory persistentStateFactory,
		IGrainContext grainContext,
		IOptionsMonitor<OdinMessagingKafkaOptions> optionsMonitor,
		IOdinDigestingUtilityServiceFactory digestingServiceFactory,
		IServiceProvider serviceProvider
	) : base(logger, loggingContext)
	{
		_logger = logger;
		_keyData = this.ParseKey<ConsumerGrainKey>(ConsumerGrainKey.Template);
		_digestingService = digestingServiceFactory.Create(_keyData.ServiceKey, _keyData.TopicId);
		_options = optionsMonitor.Get(_keyData.ServiceKey);
		_serializerResolver = serviceProvider.GetRequiredKeyedService<ITopicSerializerResolver>(_keyData.ServiceKey);
		_runtimeOptionsService = serviceProvider.GetRequiredKeyedService<IMessagingKafkaRuntimeOptionsService>(_keyData.ServiceKey);
		_store = persistentStateFactory.Create<ConsumerGrainState>(
			grainContext,
			new PersistentStateAttribute("consumer", _options.StoreName)
		);
	}

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();
		_topicConfig = _options.Topics.FindFirst(x => _keyData.TopicId == x.Name);
		_messageSerializer = _serializerResolver.Resolve(_keyData.TopicId);
		_shouldKeepBytePayload = _runtimeOptionsService.IsBytePayload(_topicConfig.Name);
		_dlqProducerGrainByteType = _runtimeOptionsService.ByteProducerGrainType;
		_emptyMessage = _runtimeOptionsService.GetEmptyOdinMessage(_topicConfig.Name);

		await ReloadSubscriptions();
		await AssignToPartition(_store.State.IsRebalancing ? ConsumeMode.LastCommittedMessage : null);

		if (!_store.State.IsRebalancing)
		{
			_store.State.IsRebalancing = true;
			await _store.WriteStateAsync();
		}
	}

	public async Task Initialize()
	{
		var subscriptionList = await GrainFactory.GetSubscriptionIndexGrain(_keyData.ServiceKey, _keyData.TopicId)
			.RegisterConsumer(_keyData.TopicId, _keyData.Partition);

		UpdateSubscriptions(subscriptionList);
	}

	public Task ConsumeAndPublish()
		=> Consume();

	public ValueTask RefreshSubscriptionList(Dictionary<string, PatternOptions> subscriptions)
	{
		UpdateSubscriptions(subscriptions);
		return ValueTask.CompletedTask;
	}

	public ValueTask Rebalance()
	{
		DeactivateOnIdle();
		return ValueTask.CompletedTask;
	}

	private async Task Consume()
	{
		if (!_digestingService.HasSubscriptions)
			return;

		var subscriptionBatches = new Dictionary<string, List<KafkaBatchResult>>();
		var topicConfigBatchSize = _topicConfig.BatchSize ?? _options.BatchSize;

		for (var i = 0; i < topicConfigBatchSize; i++)
		{
			var consumeResult = _consumer.Consume(_options.PollTimeout);
			if (consumeResult == null)
				break;

			var (message, byteMessage) = await ToOdinMessageWithFallback(consumeResult);
			_digestingService.PopulateSubscriptionBatch(
				ref subscriptionBatches,
				message,
				m => new() { Message = m, RawMessage = byteMessage }
			);
		}

		if (subscriptionBatches.IsNullOrEmpty())
			return;

		// todo: consider IAsyncEnumerable from digesting service
		var failedMessages = new OrderedDictionary();
		await subscriptionBatches.ForEachAsync(async subscriptionMessages =>
			{
				var messages = subscriptionMessages.Value.Select(x => x.Message).ToImmutableList();

				try
				{
					LogMessagePush(subscriptionMessages);

					var subscriptionGrain = _digestingService.GetSubscriptionGrain(_keyData.TopicId, subscriptionMessages.Key);
					await subscriptionGrain.Push(messages);

					if (_topicConfig.ProcessingFailedHandlingMode is ProcessingFailedHandlingMode.AckOnComplete)
						await Acknowledge(messages);
				}
				catch (SubscriptionMessageProcessingException ex)
				{
					foreach (var meta in ex.SubscriberFailureMetas)
					{
						_logger.LogError(
							meta.Exception,
							"Failed to process message batch for subscriber {subscriber} on queue with name: {queueName} due to: '{exceptionMessage}' messageIds: {messageIds}",
							meta.SubscriberKey,
							_keyData.TopicId,
							meta.Exception.Message,
							meta.MessageIds
						);
					}

					subscriptionMessages.Value.ForEach(x => failedMessages.TryAdd(x.Message.MessageId, x.RawMessage));
				}
				catch (Exception ex)
				{
					_logger.LogError(
						ex,
						"Failed to process message batch on queue with name: {queueName} due to: '{exceptionMessage}' messageIds: {messageIds}",
						_keyData.TopicId,
						ex.Message,
						subscriptionMessages.Value
					);

					subscriptionMessages.Value.ForEach(x => failedMessages.TryAdd(x.Message.MessageId, x.RawMessage));
				}
			}
		);

		if (failedMessages.Count > 0)
			await HandleProcessingError(failedMessages.Values.Cast<OdinMessage<byte[]>>().ToList());
	}

	public override Task OnOdinDeactivate()
	{
		_consumer.Close();
		_consumer.Dispose();

		return base.OnOdinDeactivate();
	}

	private async Task ReloadSubscriptions()
	{
		var subscriptionList = await GrainFactory.GetSubscriptionIndexGrain(_keyData.ServiceKey, _keyData.TopicId).Get();
		UpdateSubscriptions(subscriptionList);
	}

	private async Task AssignToPartition(ConsumeMode? consumeMode = null)
	{
		var consumerConfig = _options.ToConsumerProperties(_topicConfig);
		var offsetMode = (consumeMode ?? _options.ConsumeMode) switch
		{
			ConsumeMode.LastCommittedMessage => Offset.Stored,
			ConsumeMode.Last => Offset.End,
			ConsumeMode.Beginning => Offset.Beginning
		};

		var builder = new ConsumerBuilder<byte[], byte[]>(consumerConfig)
				.SetErrorHandler((sender, errorEvent) =>
					_logger.LogError(
						"Consume error reason: {reason}, code: {code}, is broker error: {errorType}",
						errorEvent.Reason,
						errorEvent.Code,
						errorEvent.IsBrokerError
					)
				)
			;

		_consumer = builder.Build();
		if (_topicConfig.IsPartitioned)
			_consumer.Assign(new TopicPartitionOffset(_keyData.TopicId, int.Parse(_keyData.Partition), offsetMode));
		else
		{
			if (_store.State.PartitionIds.IsNullOrEmpty())
			{
				var topics = await GrainFactory.GetTopicGrain(_keyData.ServiceKey).GetBrokerTopics();
				var partitionIds = topics
					.Where(x => x.Topic == _keyData.TopicId)
					.SelectMany(x => x.Partitions)
					.Select(x => x.PartitionId)
					.ToList();
				_store.State.PartitionIds = partitionIds;
				await _store.WriteStateAsync();
			}

			var partitionOffsets = _store.State.PartitionIds
				.Select(partitionId => new TopicPartitionOffset(_keyData.TopicId, partitionId, offsetMode))
				.ToList();

			_consumer.Assign(partitionOffsets);
		}
	}

	private void UpdateSubscriptions(Dictionary<string, PatternOptions> subscriptions)
		=> _digestingService.UpdateCache(subscriptions);

	private async Task<(OdinMessage Message, OdinMessage<byte[]> RawMessage)> ToOdinMessageWithFallback(
		ConsumeResult<byte[], byte[]> consumeResult
	)
	{
		try
		{
			var message = await ToOdinMessage(consumeResult, shouldDeserialize: true, skipMessageTransformation: false);
			OdinMessage<byte[]> byteMessage = null;
			if (_topicConfig.ProcessingFailedHandlingMode is ProcessingFailedHandlingMode.Dlq)
				byteMessage = (OdinMessage<byte[]>)(await ToOdinMessage(consumeResult, false, skipMessageTransformation: true));

			_logger.PulledMessage(message.MessageId, message.Key, string.Join(',', message.Headers));

			message.ConsumedTimestamp = DateTime.UtcNow;

			return (message, byteMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Failed to process message queue with name: {queueName} with key: {messageKey}. Message handling: {errorHandlingMode}.",
				_keyData.TopicId,
				Encoding.UTF8.GetString(consumeResult.Message.Key),
				_topicConfig.ProcessingFailedHandlingMode
			);

			var dlqMessage = (OdinMessage<byte[]>)(await ToOdinMessage(
				consumeResult,
				shouldDeserialize: false,
				skipMessageTransformation: true
			));
			await HandleProcessingError(dlqMessage.ToSingleList());
			throw;
		}
	}

	private async Task<OdinMessage> ToOdinMessage(
		ConsumeResult<byte[], byte[]> consumeResult,
		bool shouldDeserialize,
		bool skipMessageTransformation
	)
	{
		var messageValue = !_shouldKeepBytePayload && shouldDeserialize
			? await _messageSerializer.Deserialize(_keyData.TopicId, consumeResult.Message.Value)
			: consumeResult.Message.Value;

		var message = (shouldDeserialize ? _emptyMessage : _emptyByteMessage) with
		{
			MessageId = Ulid.NewUlid().ToString(),
			Key = Encoding.UTF8.GetString(consumeResult.Message.Key),
			Payload = messageValue,
			QueueIdentity = new()
			{
				ConsumerPartition = _keyData.Partition,
				ConsumerQueue = _keyData.TopicId,
				SequenceKey = consumeResult.Offset.Value.ToString(),
				Metadata = new(2)
				{
					[nameof(consumeResult.LeaderEpoch)] = consumeResult.LeaderEpoch.ToString(),
					[nameof(consumeResult.Partition)] = consumeResult.Partition.Value.ToString()
				}
			},
			Headers = consumeResult.Message.Headers.ToDictionary(x => x.Key, x => Encoding.UTF8.GetString(x.GetValueBytes()))
		};

		if (!skipMessageTransformation)
			_topicConfig.MessageTransformer?.Invoke(message);
		return message;
	}

	private void LogMessagePush(KeyValuePair<string, List<KafkaBatchResult>> consumeResults)
	{
		if (!_logger.IsEnabled(LogLevel.Information))
			return;

		var messageIds = new StringBuilder();
		var headers = new StringBuilder();
		var i = 0;
		foreach (var results in consumeResults.Value)
		{
			messageIds.Append(results.Message.MessageId);
			var notLast = i < consumeResults.Value.Count - 1;
			if (notLast)
				messageIds.Append(' ');

			// todo: log message
			headers.Append(string.Join(',', results.Message.Headers.Select(x => $"[{x.Key.ToString()}: {x.Value.ToString()}]")));
			if (notLast)
				headers.Append(' ');

			i++;
		}

		_logger.PushingBatch(consumeResults.Key, messageIds.ToString(), headers.ToString());
	}

	private async Task SendToDlq(List<OdinMessage> batch)
		=> await batch.ForEachAsync(SendOneToDlq);

	private Task SendOneToDlq(OdinMessage message)
	{
		var isByteBody = _shouldKeepBytePayload || message.Payload is byte[];
		var grainKey = Producing.GrainFactoryExtensions.GenerateProducerGrainKey(
			_keyData.ServiceKey,
			_topicConfig.DeadLetterQueueName,
			message.Key,
			isDlq: isByteBody
		);

		var producerGrain = (IProducerGrain)GrainFactory.GetGrain(_dlqProducerGrainByteType, grainKey);
		return producerGrain.Produce(message.AsImmutable());
	}

	private async Task Acknowledge(ImmutableList<OdinMessage> batch)
		=> await batch.GroupBy(x => new { x.QueueIdentity.ConsumerQueue, x.QueueIdentity.ConsumerPartition })
			.ForEachAsync(async batchMessage => await GrainFactory.GetAcknowledgerGrain(
					_keyData.ServiceKey,
					batchMessage.Key.ConsumerQueue,
					batchMessage.Key.ConsumerPartition
				)
				.Acknowledge(batchMessage.Select(x => x.ToAck()).ToImmutableList())
			);

	private async Task HandleProcessingError(List<OdinMessage<byte[]>> messages)
	{
		foreach (var message in messages)
		{
			switch (_topicConfig.ProcessingFailedHandlingMode)
			{
				case ProcessingFailedHandlingMode.AckOnComplete or ProcessingFailedHandlingMode.Ignore:
					_logger.LogError(
						"Failed to process message {messageId} on queue with name: {queueName} with key: {messageKey}. Message handling: {errorHandlingMode}.",
						message.MessageId,
						_keyData.TopicId,
						message.Key,
						_topicConfig.ProcessingFailedHandlingMode
					);
					break;
				case ProcessingFailedHandlingMode.Dlq:
					{
						_logger.LogError(
							"Failed to process message {messageId} queue with name: {queueName} with key: {messageKey}. Message handling: {errorHandlingMode}.",
							message.MessageId,
							_keyData.TopicId,
							message.Key,
							_topicConfig.ProcessingFailedHandlingMode
						);

						await SendOneToDlq(message);
						break;
					}
			}
		}
	}
}

internal record KafkaBatchResult : BatchResult
{
	public OdinMessage<byte[]> RawMessage { get; init; }
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public struct ConsumerGrainKey
{
	private string DebuggerDisplay => $"ServiceKey: '{ServiceKey}', TopicId: '{TopicId}', Partition: '{Partition}'";

	public static string Template = "odinKafkaConsumer/{serviceKey}/{topicId}/{partition}";
	public string ServiceKey { get; set; }
	public string TopicId { get; set; }
	public string Partition { get; set; }
}

[GenerateSerializer]
public record ConsumerGrainState
{
	[Id(0)]
	public List<int> PartitionIds { get; set; } = new();

	[Id(1)]
	public bool IsRebalancing { get; set; } = false;
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