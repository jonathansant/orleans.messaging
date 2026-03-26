using Orleans.Messaging.FlowControl;
using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.Kafka.Consuming;
using Orleans.Messaging.Kafka.Producing;
using Orleans.Messaging.Kafka.Serialization;
using Orleans.Messaging.Producing;
using Orleans.Messaging.SerDes;

namespace Orleans.Messaging.Kafka.Config;

public class MessagingKafkaBuilder : MessagingBuilder<MessagingKafkaOptions>
{
	public MessagingKafkaBuilder(ISiloBuilder siloBuilder, string? key)
		: base(siloBuilder, key)
	{
		key ??= "defaultBroker";

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
			services.AddOptions<MessagingKafkaOptions>(key).Configure(OptionsDelegate);
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

			services.AddKeyedSingleton<IProducerAccessor, KafkaProducerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<KafkaProducerAccessor>(provider, key)
			);

			services.AddKeyedSingleton<IConsumerAccessor, KafkaConsumerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<KafkaConsumerAccessor>(provider, key)
			);
			services.AddKeyedSingleton<IQueueDirectory>(key, (sp, _) => sp.GetRequiredService<IOptions<MessagingKafkaOptions>>().Value);
			services.AddGrainService<ConsumerGrainService>().AddSingleton<IConsumerGrainServiceClient, ConsumerGrainServiceClient>();
		};
	}

	public MessagingKafkaBuilder WithOptions(Action<MessagingKafkaOptions> configure)
	{
		OptionsDelegate += configure;
		return this;
	}
}

public class MessagingTopicConfigBuilder
{
	private readonly IServiceProvider _serviceProvider;
	private readonly string _serviceKey;
	private readonly MessagingKafkaBuilder _kafkaBuilder;
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
		_kafkaBuilder = serviceProvider.GetRequiredKeyedService<MessagingKafkaBuilder>(serviceKey);
		_topicConfig.Name = topicName;

		serviceProvider.GetRequiredService<IOptionsMonitor<MessagingKafkaOptions>>().Get(serviceKey).Topics.Add(_topicConfig);
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
		where TSerializer : IMessageSerializer
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
			(IMessageSerializer)ActivatorUtilities.CreateInstance(
				_serviceProvider,
				serializerType.MakeGenericType(_topicConfig.ContractType),
				extraParams
			)
		);

		return this;
	}

	public MessagingTopicConfigBuilder WithConsumerMessageTransformer(Action<Message> transformer)
	{
		_topicConfig.MessageTransformer = transformer;
		return this;
	}

	public MessagingTopicConfigBuilder WithConsumerMessageTransformer(Func<IServiceProvider, Action<Message>> config)
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
		Action<IServiceProvider, MessagingKafkaBuilder>? configure = null
	)
	{
		var kafkaBuilder = provider.GetRequiredKeyedService<MessagingKafkaBuilder>(serviceKey);
		configure?.Invoke(provider, kafkaBuilder);

		return provider;
	}
}

// public static class HostBuilderExtensions
// {
// 	public static MessagingKafkaBuilder AddMessagingKafka(this ISiloBuilder hostBuilder)
// 		=> new(hostBuilder);
//
// 	public static ISiloBuilder AddMessagingKafka(this ISiloBuilder hostBuilder, Action<MessagingKafkaBuilder> cfg)
// 	{
// 		var builder = new MessagingKafkaBuilder(hostBuilder);
// 		cfg(builder);
// 		builder.Build();
//
// 		return hostBuilder;
// 	}
// }