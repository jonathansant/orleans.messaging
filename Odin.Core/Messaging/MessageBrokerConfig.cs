using Odin.Core.FlowControl;
using Odin.Core.Json;

namespace Odin.Core;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public record MessageBrokerConfig
{
	protected string DebuggerDisplay => $"Type: '{Type}', Queues: {Queues}, Kafka: {Kafka}";

	/// <summary>
	/// Gets or sets the message broker type.
	/// </summary>
	public MessageBrokerType Type { get; set; }

	/// <summary>
	/// Gets or sets the message broker queue settings.
	/// </summary>
	public QueueConfig Queues { get; set; }

	/// <summary>
	/// Gets or sets the kafka settings.
	/// </summary>
	public KafkaConfig Kafka { get; set; }

	/// <summary>
	/// Enable/disable all consumption
	/// </summary>
	public bool IsConsumeEnabled { get; set; } = true;

	/// <summary>
	/// Enable/disable all producing functionality
	/// </summary>
	public bool IsProduceEnabled { get; set; } = true;
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public record KafkaConfig
{
	protected string DebuggerDisplay
		=> $"Brokers: {Brokers.ToDebugString()}, UserName: '{UserName}', Password: '{Password}', SslCaLocation: '{SslCaLocation}'";

	public SchemaConfig Schema { get; set; }

	public List<string> Brokers { get; set; }
	public string SslCaLocation { get; set; } = "/usr/local/etc/openssl/cert.pem";
	public string UserName { get; set; }
	public string Password { get; set; }
	public double? PollTimeoutMs { get; set; }
	public double? PollRate { get; set; }
	public int? BatchSize { get; set; }
	public MessageConsumeMode? ConsumeMode { get; set; }
	public TimeSpan? StartUpDelay { get; set; }
	public string? ConsumerGroupId { get; set; }
	public TimeSpan? DesiredProducerTimeout { get; set; }
	public TimeSpan? ProducerTimeout { get; set; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class SchemaConfig
{
	protected string DebuggerDisplay => $"Url: '{Url}'";

	public string Url { get; set; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class QueueConfig
{
	protected string DebuggerDisplay => $"Template: '{Template}', NameSeparator: '{NameSeparator}', Items: {Items.ToDebugString()}";

	/// <summary>
	/// Gets or sets the template.
	/// </summary>
	public string Template { get; set; }

	/// <summary>
	/// Gets or sets the publisher template, if not set it will use Template.
	/// </summary>
	public string PublisherTemplate { get; set; }

	/// <summary>
	/// Gets or sets the Queue Name Separator.
	/// </summary>
	public string NameSeparator { get; set; }

	/// <summary>
	/// Gets or sets the queue meta data.
	/// </summary>
	/// <remarks>A map that identifies the relationship between queue "alias" and the queue version.</remarks>
	public IDictionary<string, QueueMetaConfig> Items { get; set; }
}

public sealed class QueueMetaConfig
{
	public string? Template { get; set; }
	public bool AutoCreate { get; set; }

	public bool IsEnabled { get; set; }

	// public bool IsPublisher { get; set; }
	public QueueType QueueType { get; set; }
	public string Version { get; set; }
	public string ContractAlias { get; set; }
	public string ServiceName { get; set; }
	public string TopicName { get; set; }
	public string AvroUrl { get; set; }
	public string Pattern { get; set; }
	public double? PollRate { get; set; }
	public int? BatchSize { get; set; }
	public HashSet<string> SplitByHeaders { get; set; }
	public bool? IsPartitioned { get; set; }
	public int Partitions { get; set; }
	public ScheduledThrottledActionOptions DelayOptions { get; set; }
	public Action<OdinMessage> MessageTransformer { get; set; }
	public SerializerType? SerializerType { get; set; }
	public JsonSerializerConfiguration? JsonSerializerSettings { get; set; }
}

[GenerateSerializer]
public record JsonSerializerConfiguration
{
	[Id(0)]
	public bool? IsIgnoreCase { get; set; }

	[Id(1)]
	public OdinJsonNamingPolicy? NamingPolicyType { get; set; }
}

public enum SerializerType
{
	Json = 0,
	String,
	Avro
}

public enum MessageBrokerType
{
	Kafka = 0,
	Memory
}

public enum MessageConsumeMode
{
	Beginning = 0,
	LastCommittedMessage = 1,
	Last = 2
}

public enum QueueType
{
	Consumer = 0,
	Publisher,
	InOut
}