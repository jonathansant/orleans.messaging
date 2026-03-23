using Odin.Messaging.Accessors;
using Odin.Messaging.Memory.Config;
using Orleans.Concurrency;

namespace Odin.Messaging.Memory.Producing;

public class OdinMemoryProducerAccessor(
	string serviceKey,
	IGrainFactory grainFactory,
	IServiceProvider serviceProvider
) : IOdinProducerAccessor
{
	private readonly OdinMessagingMemoryOptions _options = (OdinMessagingMemoryOptions)serviceProvider
		.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey)
		.GetOptions();

	public Task Produce<TMessage>(string queueName, string key, OdinMessage<TMessage> message)
	{
		var ringHashedKey = SimpleRingHash.Calculate(_options.MaxPartitionCount, key);
		var grain = grainFactory.GetMemoryProducerGrain<TMessage>(serviceKey, queueName, ringHashedKey.ToString());

		return grain.Produce(message.AsImmutable());
	}

	public Task ProduceToDlq<TMessage>(string queueName, string key, OdinMessage<TMessage> message)
		=> Task.CompletedTask;
}
