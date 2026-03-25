using Orleans.Messaging.Subscription;

namespace Orleans.Messaging.Accessors;

public interface IConsumerAccessor
{
	Task RefreshSubscriptionList(string queue, string partition, Dictionary<string, PatternOptions> patterns);
}

public interface IProducerAccessor
{
	Task Produce<TMessage>(string queueName, string key, Message<TMessage> message);
	Task ProduceToDlq<TMessage>(string queueName, string key, Message<TMessage> message);
}
