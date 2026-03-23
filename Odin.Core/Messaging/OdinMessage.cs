namespace Odin.Core;

[Immutable]
[GenerateSerializer]
public abstract record OdinMessage
{
	[Id(0)]
	public string MessageId { get; set; } = Ulid.NewUlid().ToString();

	[Id(1)]
	public string Key { get; set; }

	[Id(2)]
	public ConsumerQueueIdentity QueueIdentity { get; set; }

	[Id(3)]
	public Dictionary<string, string> Headers { get; set; } = new();

	[Id(4)]
	public object Payload { get; set; }

	[Id(5)]
	public DateTimeOffset? ConsumedTimestamp { get; set; }
}

[Immutable]
[GenerateSerializer]
public record OdinMessage<TPayload> : OdinMessage
{
	[Id(0)]
	public new TPayload Payload
	{
		get => (TPayload)base.Payload;
		set => base.Payload = value;
	}

	public OdinMessage<TPayload> AddHeader(string key, string value)
	{
		Headers.Add(key, value);
		return this;
	}
}

[Immutable]
[GenerateSerializer]
public record ConsumerQueueIdentity
{
	[Id(0)]
	public string ConsumerQueue { get; set; }

	[Id(1)]
	public string ConsumerPartition { get; set; }

	[Id(2)]
	public string SequenceKey { get; set; }

	[Id(3)]
	public Dictionary<string, string> Metadata { get; set; } = new();
}

public static partial class MessagingObjectExtensions
{
	public static OdinMessage<TMessage> AsOdinMessage<TMessage>(
		this TMessage payload,
		string key,
		Dictionary<string, string>? headers = null
	)
	{
		var message = new OdinMessage<TMessage>
		{
			Key = key,
			Payload = payload,
			Headers = headers ?? new()
		};

		return message;
	}

	public static TimeSpan ProcessingTimeUtc(this OdinMessage message, DateTime? endTimestamp = null)
		=> (endTimestamp ?? DateTime.UtcNow) - message.ConsumedTimestamp.GetValueOrDefault(DateTime.UtcNow);
}