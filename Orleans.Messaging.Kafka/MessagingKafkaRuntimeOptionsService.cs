using Orleans.Messaging.Kafka.Config;
using Orleans.Messaging.Kafka.Producing;
using Orleans.Messaging.Subscription;
using System.Collections.Concurrent;

namespace Orleans.Messaging.Kafka;

public interface IMessagingKafkaRuntimeOptionsService : IMessagingRuntimeOptionsService
{
	public Type ByteProducerGrainType { get; }
	public bool IsBytePayload(string topic);
	public Message GetEmptyMessage(string topic);
}

public class MessagingKafkaRuntimeOptionsService(
	IServiceProvider serviceProvider,
	string serviceKey
) : MessagingRuntimeOptionsService(serviceKey, serviceProvider), IMessagingKafkaRuntimeOptionsService
{
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ConcurrentDictionary<string, bool> _bytePayloadTopics = new();
	private readonly ConcurrentDictionary<string, Message> _emptyMessages = new();
	private readonly Type _subscriptionGrainType = typeof(ISubscriptionGrain<>);
	private readonly ConcurrentDictionary<string, Type> _subscriptionGrainTypes = new();

	public override Type OptionsType { get; } = typeof(MessagingKafkaOptions);

	private ConcurrentDictionary<string, TopicConfig> _topicConfigs;
	private readonly string _serviceKey = serviceKey;

	public Type ByteProducerGrainType { get; } = typeof(IKafkaProducerGrain<byte[]>);

	public bool IsBytePayload(string topic)
	{
		LoadConfigs();
		return _bytePayloadTopics.GetOrAdd(topic, x => _topicConfigs[x].ContractType == typeof(byte[]));
	}

	public Message GetEmptyMessage(string topic)
	{
		LoadConfigs();
		return _emptyMessages.GetOrAdd(
			topic,
			x => (Message)ActivatorUtilities.CreateInstance(
				_serviceProvider,
				typeof(Message<>).MakeGenericType(_topicConfigs[x].ContractType)
			)
		);
	}

	public override Type GetSubscriptionGrainType(string topic)
	{
		LoadConfigs();
		return _subscriptionGrainTypes.GetOrAdd(
			topic,
			x => _subscriptionGrainType.MakeGenericType(_topicConfigs[x].ContractType)
		);
	}

	public override Type GetProducerGrainType(string queue)
		=> null; // not needed

	private void LoadConfigs()
	{
		if (_topicConfigs is not null)
			return;

		_topicConfigs = (GetOptions() as MessagingKafkaOptions)!
			.Topics
			.Aggregate(
				new ConcurrentDictionary<string, TopicConfig>(),
				(dict, config) =>
				{
					dict.TryAdd(config.Name, config);
					return dict;
				}
			);
	}
}
