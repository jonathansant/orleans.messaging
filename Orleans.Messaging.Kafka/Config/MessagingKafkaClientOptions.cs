using Orleans.Messaging.Config;

namespace Orleans.Messaging.Kafka.Config;

/// <summary>
///     Options for an Orleans messaging client connecting to Kafka from outside the silo.
///     Contains only producer and connection settings — consumer-specific options
///     (<c>ConsumerGroupId</c>, <c>ConsumeMode</c>, <c>PollRate</c>, <c>BatchSize</c>, etc.)
///     are omitted since consumption runs inside the silo.
/// </summary>
public record MessagingKafkaClientOptions : MessaginClientOptions;
