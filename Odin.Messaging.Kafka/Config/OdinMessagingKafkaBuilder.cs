using Odin.Core.FlowControl;
using Odin.Messaging.Accessors;
using Odin.Messaging.Config;
using Odin.Messaging.Kafka.Consuming;
using Odin.Messaging.Kafka.Producing;
using Odin.Messaging.Kafka.Serialization;
using Odin.Messaging.Producing;
using Odin.Messaging.SerDes;

namespace Odin.Messaging.Kafka.Config;

public class OdinMessagingKafkaBuilder : OdinMessagingBuilder<OdinMessagingKafkaOptions>
{
	public OdinMessagingKafkaBuilder(ISiloBuilder siloBuilder, string? key)
		: base(siloBuilder, key)
	{
		key ??= OdinMessageBrokerNames.Default;

		ConfigureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<IProducerClient, ProducerClient>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<ProducerClient>(provider, key)
			);
			services.AddKeyedSingleton<IProducerResolver, ProducerResolver>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<ProducerResolver>(provider, key)
			);
			services.AddOptions<OdinMessagingKafkaOptions>(key).Configure(OptionsDelegate);
			services.AddKeyedSingleton<IMessagingRuntimeOptionsService, MessagingKafkaRuntimeOptionsService>(
				key,
				(sp, _) => ActivatorUtilities.CreateInstance<MessagingKafkaRuntimeOptionsService>(sp, key)
			);
			services.AddKeyedSingleton(
				key,
				(provider, _)
					=> (IMessagingKafkaRuntimeOptionsService)provider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(key)
			);
			services.AddKeyedSingleton<ITopicSerializerResolver, TopicSerializerResolver>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<TopicSerializerResolver>(provider)
			);

			services.AddKeyedSingleton<IOdinProducerAccessor, OdinKafkaProducerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<OdinKafkaProducerAccessor>(provider, key)
			);

			services.AddKeyedSingleton<IConsumerAccessor, OdinKafkaConsumerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<OdinKafkaConsumerAccessor>(provider, key)
			);
			services.AddKeyedSingleton<IQueueDirectory>(key, (sp, _) => sp.GetRequiredService<IOptions<OdinMessagingKafkaOptions>>().Value);
			services.AddGrainService<ConsumerGrainService>().AddSingleton<IConsumerGrainServiceClient, ConsumerGrainServiceClient>();
		};
	}

	public OdinMessagingKafkaBuilder WithOptions(Action<OdinMessagingKafkaOptions> configure)
	{
		OptionsDelegate += configure;
		return this;
	}
}

public class MessagingTopicConfigBuilder
{
	private readonly IServiceProvider _serviceProvider;
	private readonly string _serviceKey;
	private readonly OdinMessagingKafkaBuilder _kafkaBuilder;
	private readonly TopicConfig _topicConfig = new();

	public TopicConfig TopicConfig => _topicConfig;

	public MessagingTopicConfigBuilder(
		IServiceProvider serviceProvider,
		string topicName,
		string serviceKey
	)
	{
		_serviceProvider = serviceProvider;
		_serviceKey = serviceKey;
		_kafkaBuilder = serviceProvider.GetRequiredKeyedService<OdinMessagingKafkaBuilder>(serviceKey);
		_topicConfig.Name = topicName;

		serviceProvider.GetRequiredService<IOptionsMonitor<OdinMessagingKafkaOptions>>().Get(serviceKey).Topics.Add(_topicConfig);
	}

	// public MessagingTopicConfigBuilder IsProducer()
	// {
	// 	_topicConfig.IsProducer = true;
	// 	return this;
	// }

	public MessagingTopicConfigBuilder WithTopicType(TopicType type)
	{
		_topicConfig.Type = type;
		return this;
	}

	public MessagingTopicConfigBuilder WithPartitioning(bool isPartitioned = true)
	{
		_topicConfig.IsPartitioned = isPartitioned;
		return this;
	}

	public MessagingTopicConfigBuilder WithDelaySubscription(ScheduledThrottledActionOptions delayOpts)
	{
		_topicConfig.DelayOptions = delayOpts;
		return this;
	}

	public MessagingTopicConfigBuilder WithContract(Type contractType)
	{
		_topicConfig.ContractType = contractType;
		return this;
	}

	public MessagingTopicConfigBuilder WithContract<TMessage>()
		=> WithContract(typeof(TMessage));

	public MessagingTopicConfigBuilder WithSerializer<TSerializer>(params object[] extraParams)
		where TSerializer : IOdinMessageSerializer
	{
		WithSerializer(typeof(TSerializer), extraParams);
		return this;
	}

	public MessagingTopicConfigBuilder WithPollRate(TimeSpan pollRate)
	{
		_topicConfig.PollRate = pollRate;
		return this;
	}

	public MessagingTopicConfigBuilder WithBatchSize(int batchSize)
	{
		_topicConfig.BatchSize = batchSize;
		return this;
	}

	public MessagingTopicConfigBuilder UseProcessingErrorHandlingMode(ProcessingFailedHandlingMode mode, string? dlqName = null)
	{
		if (mode is ProcessingFailedHandlingMode.Dlq)
			_topicConfig.DeadLetterQueueName = dlqName.IsNullOrEmpty() ? $"{_topicConfig.Name}_dlq" : dlqName;
		else if (!dlqName.IsNullOrEmpty())
			throw new ArgumentException("A dlq name was specified but `ProcessingFailedHandlingMode` is not set to Dlq", dlqName);

		_topicConfig.ProcessingFailedHandlingMode = mode;

		return this;
	}

	public MessagingTopicConfigBuilder WithSerializer(Type serializerType, params object[] extraParams)
	{
		if (!serializerType.IsGenericTypeDefinition)
			throw new ArgumentException("Serializer type must be a generic type definition", nameof(serializerType));

		var resolver = _serviceProvider.GetRequiredKeyedService<ITopicSerializerResolver>(_serviceKey);
		resolver.Register(
			_topicConfig.Name,
			(IOdinMessageSerializer)ActivatorUtilities.CreateInstance(
				_serviceProvider,
				serializerType.MakeGenericType(_topicConfig.ContractType),
				extraParams
			)
		);

		return this;
	}

	public MessagingTopicConfigBuilder WithConsumerMessageTransformer(Action<OdinMessage> transformer)
	{
		_topicConfig.MessageTransformer = transformer;
		return this;
	}

	public MessagingTopicConfigBuilder WithConsumerMessageTransformer(Func<IServiceProvider, Action<OdinMessage>> config)
	{
		_topicConfig.MessageTransformer = config(_serviceProvider);
		return this;
	}

	public MessagingTopicConfigBuilder WithCreationOptions(TopicCreationConfig creationConfig)
	{
		_topicConfig.AutoCreate = creationConfig.AutoCreate;
		_topicConfig.Partitions = creationConfig.Partitions;
		_topicConfig.ReplicationFactor = creationConfig.ReplicationFactor;
		_topicConfig.RetentionPeriodInMs = creationConfig.RetentionPeriodInMs;
		return this;
	}
}

public static partial class ServiceProviderExtensions
{
	public static MessagingTopicConfigBuilder AddTopic(this IServiceProvider provider, string topicName, string serviceKey)
	{
		var topicBuilder = ActivatorUtilities.CreateInstance<MessagingTopicConfigBuilder>(provider, topicName, serviceKey);
		return topicBuilder;
	}

	public static IServiceProvider AddTopic(
		this IServiceProvider provider,
		string topicName,
		string serviceKey,
		Action<MessagingTopicConfigBuilder> builder
	)
	{
		var topicBuilder = AddTopic(provider, topicName, serviceKey);
		builder(topicBuilder);

		return provider;
	}

	public static IServiceProvider ConfigureMessagingKafka(
		this IServiceProvider provider,
		string serviceKey,
		Action<IServiceProvider, OdinMessagingKafkaBuilder>? configure = null
	)
	{
		var kafkaBuilder = provider.GetRequiredKeyedService<OdinMessagingKafkaBuilder>(serviceKey);
		configure?.Invoke(provider, kafkaBuilder);

		return provider;
	}
}

// public static class HostBuilderExtensions
// {
// 	public static OdinMessagingKafkaBuilder AddOdinMessagingKafka(this ISiloBuilder hostBuilder)
// 		=> new(hostBuilder);
//
// 	public static ISiloBuilder AddOdinMessagingKafka(this ISiloBuilder hostBuilder, Action<OdinMessagingKafkaBuilder> cfg)
// 	{
// 		var builder = new OdinMessagingKafkaBuilder(hostBuilder);
// 		cfg(builder);
// 		builder.Build();
//
// 		return hostBuilder;
// 	}
// }