using Odin.Core.FlowControl;
using Odin.Messaging.Config;

namespace Odin.Messaging.Kafka.Config;

public record OdinMessagingKafkaOptions : OdinMessagingOptions, IQueueDirectory
{
	public IList<TopicConfig> Topics { get; set; } = new List<TopicConfig>();
	public IList<string> BrokerList { get; set; }
	public string ConsumerGroupId { get; set; } = "odin-messaging-kafka";
	public TimeSpan PollTimeout { get; set; } = TimeSpan.FromMilliseconds(50);
	public TimeSpan AdminRequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
	public ConsumeMode ConsumeMode { get; set; } = ConsumeMode.LastCommittedMessage;
	public TimeSpan ProducerTimeout { get; set; } = TimeSpan.FromMilliseconds(5000);
	public int? ApiVersionFallbackMs { get; set; }
	public SecurityProtocol? SecurityProtocol { get; set; } = null;
	public string SslCaLocation { get; set; }
	public string SaslUserName { get; set; }
	public string SaslPassword { get; set; }
	public SaslMechanism? SaslMechanism { get; set; } = null;
	public TimeSpan PollRate { get; set; } = TimeSpan.FromMilliseconds(17);
	public int BatchSize { get; set; } = 10;
	public int IdleTimeout { get; set; } = 0;
	public bool IsConsumeEnabled { get; set; } = true;
	public string? AvroUrl { get; set; }

	public List<string> GetAllQueues()
		=> Topics.Select(t => t.Name).ToList();
}

public record TopicConfig
{
	public string Name { get; set; }

	/// <summary>
	/// The expected Payload that is expected on this topic
	/// </summary>
	public Type ContractType { get; set; }

	/// <summary>
	/// Determines whether the topic will be auto created
	/// </summary>
	/// <remarks><c>false</c> by default.</remarks>
	public bool AutoCreate { get; set; }

	/// <summary>
	/// Use a single partition for this topic if false
	/// </summary>
	public bool IsPartitioned { get; set; } = true;

	/// <summary>
	/// If <see cref="AutoCreate"/> is true the topic will
	/// be created with set number of topics
	/// </summary>
	/// <remarks>-1 by default</remarks>
	public int Partitions { get; set; } = -1;

	/// <summary>
	/// If <see cref="AutoCreate"/> is true the topic will
	/// be created with the Replication Factor defined
	/// </summary>
	/// <remarks>1 by default</remarks>
	public short ReplicationFactor { get; set; } = 1;

	/// <summary>
	/// If <see cref="RetentionPeriodInMs"/> is set the topic will
	/// be created with only retain data for this much duration.
	/// If not set, it'll take default configuration of broker which is 7 days.
	/// </summary>
	/// <remarks>7 days by default at broker level if not set</remarks>
	public ulong? RetentionPeriodInMs { get; set; }

	/// <summary>
	/// Is the topic a consumer or producer or both
	/// </summary>
	public TopicType Type { get; set; }

	public ProcessingFailedHandlingMode ProcessingFailedHandlingMode { get; set; } = ProcessingFailedHandlingMode.AckOnComplete;

	public Action<OdinMessage> MessageTransformer { get; set; }

	public string DeadLetterQueueName { get; set; }

	public TimeSpan? PollRate { get; set; }
	public int? BatchSize { get; set; }
	public ScheduledThrottledActionOptions DelayOptions { get; set; }
}

public record TopicCreationConfig
{
	/// <summary>
	/// Determines whether the topic will be auto created
	/// </summary>
	/// <remarks><c>false</c> by default.</remarks>
	public bool AutoCreate { get; set; }

	/// <summary>
	/// If <see cref="AutoCreate"/> is true the topic will
	/// be created with set number of topics
	/// </summary>
	/// <remarks>-1 by default</remarks>
	public int Partitions { get; set; } = -1;

	/// <summary>
	/// If <see cref="AutoCreate"/> is true the topic will
	/// be created with the Replication Factor defined
	/// </summary>
	/// <remarks>1 by default</remarks>
	public short ReplicationFactor { get; set; } = 1;

	/// <summary>
	/// If <see cref="RetentionPeriodInMs"/> is set the topic will
	/// be created with only retain data for this much duration in milliseconds.
	/// If not set, it'll take default configuration of broker which is 7 days.
	/// </summary>
	/// <remarks>7 days by default</remarks>
	public ulong? RetentionPeriodInMs { get; set; }
}

public record struct ProducerRetryOptions(
	int MaxRetries = 2
)
{
	public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(10);
}

public enum ConsumeMode
{
	Beginning = 0,
	LastCommittedMessage = 1,
	Last = 2
}

public enum SaslMechanism
{
	Gssapi,
	Plain,
	ScramSha256,
	ScramSha512,
}

public enum SecurityProtocol
{
	Plaintext,
	Ssl,
	SaslPlaintext,
	SaslSsl,
}

public enum ProcessingFailedHandlingMode
{
	AckOnComplete,
	Dlq,
	Ignore
}

public enum TopicType
{
	Consumer = 0,
	Producer,
	InOut
}