using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.Memory.Config;
using Orleans.Concurrency;

namespace Orleans.Messaging.Memory.Producing;

public class MemoryProducerAccessor(
	string serviceKey,
	IGrainFactory grainFactory,
	IServiceProvider serviceProvider
) : IProducerAccessor
{
	private readonly IMessagingMemoryOptions _options = (IMessagingMemoryOptions)serviceProvider
		.GetRequiredKeyedService<IMessagingOptionsService>(serviceKey)
		.GetOptions();

	public Task Produce<TMessage>(string queueName, string key, Message<TMessage> message)
	{
		var ringHashedKey = SimpleRingHash.Calculate(_options.MaxPartitionCount, key);
		var grain = grainFactory.GetMemoryProducerGrain<TMessage>(serviceKey, queueName, ringHashedKey.ToString());

		return grain.Produce(message.AsImmutable());
	}

	public Task ProduceToDlq<TMessage>(string queueName, string key, Message<TMessage> message)
		=> Task.CompletedTask;
}
