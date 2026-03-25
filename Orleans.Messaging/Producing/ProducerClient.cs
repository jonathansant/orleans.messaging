using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;

namespace Orleans.Messaging.Producing;

public interface IProducerClient
{
	Task Produce<TMessage>(string queueName, string key, TMessage message);
	Task Produce<TPayload>(string queueName, Message<TPayload> message);
	Task SendToDlq<TMessage>(string queueName, Message<TMessage> message);
}

public class ProducerClient(
	IServiceProvider serviceProvider,
	string serviceKey
) : IProducerClient
{
	private readonly MessagingOptions _options = serviceProvider
		.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey)
		.GetOptions();

	private readonly IProducerAccessor _producerAccessor = serviceProvider.GetRequiredKeyedService<IProducerAccessor>(serviceKey);

	public Task Produce<TMessage>(string queueName, string key, TMessage message)
		=> !_options.IsProduceEnabled
			? Task.CompletedTask
			: _producerAccessor.Produce(queueName, key, message.AsMessage(key));

	public Task Produce<TPayload>(string queueName, Message<TPayload> message)
		=> !_options.IsProduceEnabled
			? Task.CompletedTask
			: _producerAccessor.Produce(queueName, message.Key, message);

	public Task SendToDlq<TMessage>(string queueName, Message<TMessage> message)
		=> _producerAccessor.ProduceToDlq(queueName, message.Key, message);
}
