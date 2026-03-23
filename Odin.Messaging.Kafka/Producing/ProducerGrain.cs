using Confluent.Kafka;
using Odin.Core.FlowControl;
using Odin.Messaging.Kafka.Config;
using Odin.Messaging.Kafka.Serialization;
using Odin.Messaging.SerDes;
using Odin.Orleans.Core;
using Odin.Orleans.Core.Tenancy;
using Orleans.Concurrency;
using System.Text;
using System.Web;

namespace Odin.Messaging.Kafka.Producing;

public static partial class GrainFactoryExtensions
{
	public static IKafkaProducerGrain<TMessage> GetProducerGrain<TMessage>(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName,
		string partitioningKey
	) => grainFactory.GetGrain<IKafkaProducerGrain<TMessage>>(
		GenerateProducerGrainKey(serviceKey, queueName, partitioningKey, isDlq: false)
	);

	public static IKafkaProducerGrain<TMessage> GetDlqProducerGrain<TMessage>(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName,
		string partitioningKey
	) => grainFactory.GetGrain<IKafkaProducerGrain<TMessage>>(
		GenerateProducerGrainKey(serviceKey, queueName, partitioningKey, isDlq: true)
	);

	public static string GenerateProducerGrainKey(string serviceKey, string queueName, string partitioningKey, bool isDlq)
		=> $"odinMessagingKafkaProducer/{serviceKey}/{queueName}/{HttpUtility.UrlEncode(partitioningKey)}/{(isDlq ? "dlq" : "standard")}";
}

public interface IProducerGrain : IOdinGrainContract, IGrainWithStringKey
{
	Task Produce(Immutable<OdinMessage> message);
}

public interface IKafkaProducerGrain<TMessage> : IProducerGrain
{
	Task Produce(Immutable<OdinMessage<TMessage>> message);
}

[SharedTenant]
public class KafkaProducerGrain<TMessage> : OdinGrain, IKafkaProducerGrain<TMessage>
{
	private readonly ILogger<KafkaProducerGrain<TMessage>> _logger;
	private readonly OdinMessagingKafkaOptions _options;
	private readonly ITopicSerializerResolver _serializerResolver;
	private readonly IProducerResolver _producerResolver;
	private IOdinMessageSerializer<TMessage> _messageSerializer;
	private IProducer<byte[], byte[]> _producer;
	private TopicConfig _topicConfig;
	private KafkaProducerGrainKey _key;
	private static readonly Type MessageType = typeof(TMessage);
	private static readonly bool IsByte = typeof(TMessage) == typeof(byte[]);
	private readonly DebounceAction _debounceDeactivate;
	private byte[] _messageKey;
	private bool _isFirst = true;

	public KafkaProducerGrain(
		ILogger<KafkaProducerGrain<TMessage>> logger,
		ILoggingContext loggingContext,
		IOptionsMonitor<OdinMessagingKafkaOptions> optionsMonitor,
		IServiceProvider serviceProvider
	) : base(logger, loggingContext)
	{
		_key = this.ParseKey<KafkaProducerGrainKey>(KafkaProducerGrainKey.Template);
		_key.PartitioningKey = HttpUtility.UrlDecode(_key.PartitioningKey);
		_logger = logger;

		_options = optionsMonitor.Get(_key.ServiceKey);
		_serializerResolver = serviceProvider.GetRequiredKeyedService<ITopicSerializerResolver>(_key.ServiceKey);
		_producerResolver = serviceProvider.GetRequiredKeyedService<IProducerResolver>(_key.ServiceKey);

		_debounceDeactivate = new DebounceAction(
			() =>
			{
				DeactivateOnIdle();

				return Task.CompletedTask;
			},
			TimeSpan.FromMinutes(30)
		).OnError(ex =>
			{
				_logger.LogError(ex, "Error on debouncing producer grain deactivation");

				return Task.CompletedTask;
			}
		);
	}

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();

		if (_key.ProducerType == ProducerType.Standard)
		{
			_topicConfig = _options.Topics.FindFirst(x => _key.QueueName == x.Name);

			if (_topicConfig == null)
				throw new InvalidOperationException($"No topic configuration found for queue '{_key.QueueName}'");

			if (MessageType != _topicConfig?.ContractType)
				throw new InvalidOperationException(
					$"Producer grain for queue '{_key.QueueName}' is configured to produce messages of type "
					+ $"'{_topicConfig?.ContractType}' but was requested to produce messages of type '{MessageType}'"
				);
		}

		_messageSerializer = IsByte
			? null
			: _serializerResolver.Resolve<TMessage>(_key.QueueName);

		_producer = await _producerResolver.GetProducer();
		_messageKey = Encoding.UTF8.GetBytes(_key.PartitioningKey!);
	}

	public Task Produce(Immutable<OdinMessage> message)
		=> Produce((message.Value as OdinMessage<TMessage>).AsImmutable());

	public async Task Produce(Immutable<OdinMessage<TMessage>> message)
	{
		try
		{
			var retryCount = 0;
			var retryOptions = _options.ProducerRetryOptions;

			do
			{
				var deliveryReport = await ProduceToKafka(message);
				LogProduceMessage(message, retryCount, deliveryReport);

				if (deliveryReport.Status is PersistenceStatus.Persisted or PersistenceStatus.PossiblyPersisted)
					break;

				await Task.Delay(retryOptions.RetryDelay);
			} while (retryOptions.MaxRetries > retryCount++);
		}
		catch (ProduceException<byte[], byte[]> ex)
		{
			_logger.LogError(
				ex,
				"Failed to produce message with key {key}, message {message} on queue {queue} - reason: {deliveryFailureReason} - message delivered to {deliveryPartitionOffset} with status {status}",
				_key.PartitioningKey,
				message.Value,
				_key.QueueName,
				ex.Error.Reason,
				ex.DeliveryResult.TopicPartitionOffset,
				ex.DeliveryResult.Status
			);

			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Failed to produce message with key {key}, message {message} on queue {queue}",
				_key.PartitioningKey,
				message.Value,
				_key.QueueName
			);

			throw;
		}
		finally
		{
			_debounceDeactivate.Execute();
			_isFirst = false;
		}
	}

	private void LogProduceMessage(Immutable<OdinMessage<TMessage>> message, int retryCount, DeliveryResult<byte[], byte[]> deliveryReport)
	{
		if (retryCount > 0)
			_logger.ProduceMessageRetryReportAsWarn(
				retryCount,
				_key.PartitioningKey,
				message.Value,
				_key.QueueName,
				deliveryReport.TopicPartitionOffset,
				deliveryReport.Status
			);
		else
			_logger.ProduceMessageRetryReport(
				retryCount,
				_key.PartitioningKey,
				message.Value,
				_key.QueueName,
				deliveryReport.TopicPartitionOffset,
				deliveryReport.Status
			);
	}

	private async Task<DeliveryResult<byte[], byte[]>> ProduceToKafka(Immutable<OdinMessage<TMessage>> message)
	{
		var cancellationTokenSource = new CancellationTokenSource(
			_isFirst ? (int)_options.ProducerTimeout.TotalMilliseconds : (int)_options.DesiredProducerTimeout.TotalMilliseconds
		);

		return await _producer.ProduceAsync(
			_key.QueueName,
			new()
			{
				Key = _messageKey,
				Value = IsByte
					? (byte[])(object)message.Value.Payload
					: await _messageSerializer.Serialize(_key.QueueName, message.Value.Payload),
				Headers = ToHeaders(message.Value.Headers)
			},
			cancellationTokenSource.Token
		);
	}

	public override async Task OnOdinDeactivate()
	{
		await base.OnOdinDeactivate();
		await _debounceDeactivate.DisposeAsync();
	}

	private static Headers ToHeaders(Dictionary<string, string> headers)
	{
		if (headers.IsNullOrEmpty())
			return null;

		var result = new Headers();
		foreach (var header in headers)
			result.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));

		return result;
	}
}

public struct KafkaProducerGrainKey
{
	public const string Template = "odinMessagingKafkaProducer/{serviceKey}/{queueName}/{partitioningKey}/{ProducerType}";

	public string ServiceKey { get; set; }
	public string QueueName { get; set; }
	public string PartitioningKey { get; set; }
	public ProducerType ProducerType { get; set; }
}

public enum ProducerType
{
	Standard,
	Dlq
}

internal static partial class LogExtensions
{
	[LoggerMessage(
		Level = LogLevel.Debug,
		Message =
			"Produced message after {retryCount} retries with key {key}, message value {message} on queue {queue} - message delivered to {deliveryPartitionOffset} with status {status}"
	)]
	internal static partial void ProduceMessageRetryReport(
		this ILogger logger,
		int retryCount,
		string key,
		OdinMessage message,
		string queue,
		TopicPartitionOffset deliveryPartitionOffset,
		PersistenceStatus status
	);

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message =
			"Produced message after {retryCount} retries with key {key}, message value {message} on queue {queue} - message delivered to {deliveryPartitionOffset} with status {status}"
	)]
	internal static partial void ProduceMessageRetryReportAsWarn(
		this ILogger logger,
		int retryCount,
		string key,
		OdinMessage message,
		string queue,
		TopicPartitionOffset deliveryPartitionOffset,
		PersistenceStatus status
	);
}
