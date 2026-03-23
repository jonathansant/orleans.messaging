using Odin.Messaging.Subscription;

namespace Odin.Messaging.Accessors;

public interface IConsumerAccessor
{
	Task RefreshSubscriptionList(string queue, string partition, Dictionary<string, PatternOptions> patterns);
}

public interface IOdinProducerAccessor
{
	Task Produce<TMessage>(string queueName, string key, OdinMessage<TMessage> message);
	Task ProduceToDlq<TMessage>(string queueName, string key, OdinMessage<TMessage> message);
}
