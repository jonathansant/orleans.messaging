using Odin.Messaging.Accessors;
using Orleans.Concurrency;

namespace Odin.Messaging.Kafka.Producing;

public class OdinKafkaProducerAccessor(
	string serviceKey,
	IGrainFactory grainFactory
) : IOdinProducerAccessor
{
	public Task Produce<TMessage>(string queueName, string key, OdinMessage<TMessage> message)
	{
		var grain = grainFactory.GetProducerGrain<TMessage>(serviceKey, queueName, key);
		return grain.Produce(message.AsImmutable());
	}

	public Task ProduceToDlq<TMessage>(string queueName, string key, OdinMessage<TMessage> message)
	{
		var grain = grainFactory.GetDlqProducerGrain<TMessage>(serviceKey, queueName, message.Key);
		return grain.Produce(message.AsImmutable());
	}
}
