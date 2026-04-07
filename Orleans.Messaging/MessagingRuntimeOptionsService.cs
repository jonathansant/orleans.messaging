namespace Orleans.Messaging;

public interface IMessagingRuntimeOptionsService
{
	ValueTask<Type> GetSubscriptionGrainType(string queue);
	ValueTask<Type> GetProducerGrainType(string queue);
}

public abstract class MessagingRuntimeOptionsService : IMessagingRuntimeOptionsService
{
	public abstract ValueTask<Type> GetSubscriptionGrainType(string queue);

	public abstract ValueTask<Type> GetProducerGrainType(string queue);
}
