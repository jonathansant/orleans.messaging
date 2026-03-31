using Orleans.Messaging.Tests.Kafka.Fixtures;

namespace Orleans.Messaging.Tests.Kafka.Tests;

public class UnsubscribeTests : IClassFixture<KafkaClusterFixture>
{
	private const string Topic = "test-messages";

	private readonly IMessagingClient _client;
	private readonly IGrainFactory _grains;

	public UnsubscribeTests(KafkaClusterFixture fixture)
	{
		_client = fixture.ServiceProvider
			.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
		_grains = fixture.GrainFactory;
	}

	[Fact]
	public async Task Unsubscribe_BySubscriptionId_StopsReceivingMessages()
	{
		var grainKey = $"unsub-{Guid.NewGuid():N}";
		var messageKey = $"key-{grainKey}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		var subscriptionId = await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(messageKey, o => o.PatternType = PatternType.Exact)
		);

		// Produce before unsubscribe - should be received
		await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage("before", "test"));
		var initialMessages = await WaitForMessages(receiver, 1);
		initialMessages.Should().HaveCount(1);

		await _client.Unsubscribe<TestMessage>(subscriptionId);

		// Produce after unsubscribe - should NOT be received
		await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage("after", "test"));
		await Task.Delay(3000);

		var finalMessages = await receiver.GetReceivedMessages();
		finalMessages.Should().HaveCount(1, "no messages should arrive after unsubscription");
		finalMessages[0].Payload.Value.Should().Be("before");
	}

	[Fact]
	public async Task Unsubscribe_ByTopicSubscription_StopsReceivingMessages()
	{
		var grainKey = $"unsub2-{Guid.NewGuid():N}";
		var messageKey = $"key2-{grainKey}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		var subscriptionId = await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(messageKey, o => o.PatternType = PatternType.Exact)
		);

		await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage("before", "test"));
		await WaitForMessages(receiver, 1);

		var topicSubscription = new TopicSubscription(
			MessageBrokerNames.DefaultBroker,
			subscriptionId,
			Topic,
			messageKey
		);
		await _client.Unsubscribe<TestMessage>(topicSubscription);

		await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage("after", "test"));
		await Task.Delay(3000);

		var messages = await receiver.GetReceivedMessages();
		messages.Should().HaveCount(1);
	}

	private static async Task<List<Message<TestMessage>>> WaitForMessages(
		ITestReceiverGrain grain,
		int expectedCount,
		TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(30));
		while (DateTime.UtcNow < deadline)
		{
			var msgs = await grain.GetReceivedMessages();
			if (msgs.Count >= expectedCount)
				return msgs;
			await Task.Delay(200);
		}
		return await grain.GetReceivedMessages();
	}
}
