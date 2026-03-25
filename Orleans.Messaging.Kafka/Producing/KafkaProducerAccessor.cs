using Orleans.Messaging.Accessors;
using Orleans.Concurrency;

namespace Orleans.Messaging.Kafka.Producing;

public class KafkaProducerAccessor(
	string serviceKey,
	IGrainFactory grainFactory
) : IProducerAccessor
{
	public Task Produce<TMessage>(string queueName, string key, Message<TMessage> message)
	{
		var grain = grainFactory.GetProducerGrain<TMessage>(serviceKey, queueName, key);
		return grain.Produce(message.AsImmutable());
	}

	public Task ProduceToDlq<TMessage>(string queueName, string key, Message<TMessage> message)
	{
		var grain = grainFactory.GetDlqProducerGrain<TMessage>(serviceKey, queueName, message.Key);
		return grain.Produce(message.AsImmutable());
	}
}
