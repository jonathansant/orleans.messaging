using Confluent.Kafka;
using Odin.Messaging.Kafka.Config;

namespace Odin.Messaging.Kafka.Producing;

public interface IProducerResolver
{
	ValueTask<IProducer<byte[], byte[]>> GetProducer();
}

public sealed class ProducerResolver : IProducerResolver
{
	private readonly IProducer<byte[], byte[]> _producer;

	public ProducerResolver(IServiceProvider serviceProvider, string serviceKey)
	{
		var producerProperties = serviceProvider
			.GetRequiredService<IOptionsMonitor<OdinMessagingKafkaOptions>>()
			.Get(serviceKey)
			.ToProducerProperties();

		_producer = new ProducerBuilder<byte[], byte[]>(producerProperties).Build();
	}

	public ValueTask<IProducer<byte[], byte[]>> GetProducer()
		=> ValueTask.FromResult(_producer);
}
