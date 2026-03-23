namespace Odin.Messaging.Subscription;

[GenerateSerializer]
public class SubscriptionMessageProcessingException(
	string message,
	List<MessageFailedMeta> subscriberFailureMetas
) : Exception(message)
{
	[Id(0)]
	public List<MessageFailedMeta> SubscriberFailureMetas { get; } = subscriberFailureMetas;
}

[GenerateSerializer]
public record struct MessageFailedMeta
{
	[Id(0)]
	public Exception Exception { get; set; }

	[Id(1)]
	public string SubscriberKey { get; set; }

	[Id(2)]
	public List<string> MessageIds { get; set; }
}
