using Odin.Messaging.Accessors;
using Odin.Messaging.Config;

namespace Odin.Messaging.Producing;

public interface IProducerClient
{
	Task Produce<TMessage>(string queueName, string key, TMessage message);
	Task Produce<TPayload>(string queueName, OdinMessage<TPayload> message);
	Task SendToDlq<TMessage>(string queueName, OdinMessage<TMessage> message);
}

public class ProducerClient(
	IServiceProvider serviceProvider,
	string serviceKey
) : IProducerClient
{
	private readonly OdinMessagingOptions _options = serviceProvider
		.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey)
		.GetOptions();

	private readonly IOdinProducerAccessor _producerAccessor = serviceProvider.GetRequiredKeyedService<IOdinProducerAccessor>(serviceKey);

	public Task Produce<TMessage>(string queueName, string key, TMessage message)
		=> !_options.IsProduceEnabled
			? Task.CompletedTask
			: _producerAccessor.Produce(queueName, key, message.AsOdinMessage(key));

	public Task Produce<TPayload>(string queueName, OdinMessage<TPayload> message)
		=> !_options.IsProduceEnabled
			? Task.CompletedTask
			: _producerAccessor.Produce(queueName, message.Key, message);

	public Task SendToDlq<TMessage>(string queueName, OdinMessage<TMessage> message)
		=> _producerAccessor.ProduceToDlq(queueName, message.Key, message);
}
