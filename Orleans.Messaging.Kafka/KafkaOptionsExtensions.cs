using Confluent.Kafka;
using Orleans.Messaging.Kafka.Config;
using SaslMechanism = Confluent.Kafka.SaslMechanism;
using SecurityProtocol = Confluent.Kafka.SecurityProtocol;

namespace Orleans.Messaging.Kafka;

internal static class KafkaOptionsExtensions
{
	extension(MessagingKafkaOptions options)
	{
		public ProducerConfig ToProducerProperties()
		{
			var config = CreateCommonProperties<ProducerConfig>(options);
			config.MessageTimeoutMs = (int)options.ProducerTimeout.TotalMilliseconds;

			return config;
		}

		public ConsumerConfig ToConsumerProperties(TopicConfig? topicConfig = null)
		{
			var config = CreateCommonProperties<ConsumerConfig>(options);

			config.GroupId = options.ConsumerGroupId;
			config.EnableAutoCommit =
				topicConfig?.ProcessingFailedHandlingMode
					is ProcessingFailedHandlingMode.Dlq
					or ProcessingFailedHandlingMode.Ignore; // if using deadletter queues auto commit is enabled
			config.ConnectionsMaxIdleMs = options.IdleTimeout;

			return config;
		}

		public AdminClientConfig ToAdminProperties()
			=> CreateCommonProperties<AdminClientConfig>(options);
	}

	private static TClientConfig CreateCommonProperties<TClientConfig>(MessagingKafkaOptions options)
		where TClientConfig : ClientConfig, new()
	{
		var config = new TClientConfig()
		{
			BootstrapServers = string.Join(",", options.BrokerList),
			ApiVersionRequestTimeoutMs = options.ApiVersionFallbackMs
		};

		if (options.SecurityProtocol is null)
			return config;

		config.SaslMechanism = (SaslMechanism)(int)(options.SaslMechanism ?? Config.SaslMechanism.Plain);
		config.SecurityProtocol = (SecurityProtocol)(int)options.SecurityProtocol;
		config.SslCaLocation = options.SslCaLocation;
		config.SaslUsername = options.SaslUserName;
		config.SaslPassword = options.SaslPassword;

		return config;
	}
}
