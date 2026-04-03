using Orleans.Messaging.Producing;
using Orleans.Messaging.Subscription;

namespace Orleans.Messaging;

public interface IMessagingClient
{
	Task<string> Subscribe<TMessage>(Action<SubscriptionBuilder<TMessage>> configure);
	Task<string> Subscribe<TMessage>(MessageSubscriptionInput<TMessage> input);
	Task Unsubscribe<TMessage>(string queueName, string subscriptionPattern, string subscriptionId); // todo: deprecate this
	Task Unsubscribe<TMessage>(TopicSubscription subscription);
	Task Unsubscribe<TMessage>(string subscriptionId);
	Task Produce<TMessage>(string queueName, string key, TMessage message);
	Task Produce<TMessage>(string queueName, Message<TMessage> message);
	internal Task SendToDlq<TMessage>(string queueName, Message<TMessage> message);
}

public class MessagingClient(
	IServiceProvider serviceProvider,
	string serviceKey
) : IMessagingClient
{
	private const char PipeSeparator = '|';
	private const char SlashSeparator = '/';
	private readonly IProducerClient _producerClient = serviceProvider.GetRequiredKeyedService<IProducerClient>(serviceKey);
	private readonly ISubscriptionClient _subscriptionClient = serviceProvider.GetRequiredKeyedService<ISubscriptionClient>(serviceKey);

	public Task<string> Subscribe<TMessage>(Action<SubscriptionBuilder<TMessage>> configure)
	{
		var builder = new SubscriptionBuilder<TMessage>();
		configure(builder);

		return Subscribe(builder.Build() with { ServiceKey = serviceKey });
	}

	public Task<string> Subscribe<TMessage>(MessageSubscriptionInput<TMessage> input)
		=> _subscriptionClient.Subscribe(input);

	public Task Unsubscribe<TMessage>(TopicSubscription subscription)
		=> Unsubscribe<TMessage>(
			subscription.TopicName,
			subscription.SubscriptionPattern,
			subscription.SubscriptionId
		);

	public Task Unsubscribe<TMessage>(string subscriptionId)
	{
		// todo: refactor this make it better and move it down the stack
		var idParts = subscriptionId.Split(PipeSeparator);
		var (grainId, subscriptionPart) = (idParts[0], idParts[1]);

		var grainParts = grainId.Split(SlashSeparator);
		var (_, _, queue, pattern) = (grainParts[0], grainParts[1], grainParts[2], grainParts[3]);

		return _subscriptionClient.Unsubscribe<TMessage>(serviceKey, queue, pattern, subscriptionId);
	}

	public Task Unsubscribe<TMessage>(string queueName, string subscriptionPattern, string subscriptionId)
		=> _subscriptionClient.Unsubscribe<TMessage>(serviceKey, queueName, subscriptionPattern, subscriptionId);

	public Task Produce<TMessage>(string queueName, string key, TMessage message)
		=> _producerClient.Produce(queueName, key, message);

	public Task Produce<TMessage>(string queueName, Message<TMessage> message)
		=> _producerClient.Produce(queueName, message);

	public Task SendToDlq<TMessage>(string queueName, Message<TMessage> message)
		=> _producerClient.SendToDlq(queueName, message);
}
