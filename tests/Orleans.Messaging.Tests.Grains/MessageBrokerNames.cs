namespace Orleans.Messaging.Tests.Grains;

/// <summary>Well-known service keys for registered messaging broker instances.</summary>
public static class MessageBrokerNames
{
	public const string DefaultBroker = "messageBroker";
	public const string Conduit = "conduitMessageBroker";
	public const string IronwoodRelay = "ironwoodRelayMessageBroker";
}
