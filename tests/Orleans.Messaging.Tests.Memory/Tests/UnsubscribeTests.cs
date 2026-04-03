using Orleans.Messaging.Tests.Memory.Fixtures;

namespace Orleans.Messaging.Tests.Memory.Tests;

public class UnsubscribeTests : IClassFixture<MemoryClusterFixture>
{
	private const string Topic = "test-messages";

	private readonly IMessagingClient _client;
	private readonly IGrainFactory _grains;

	public UnsubscribeTests(MemoryClusterFixture fixture)
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
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		// Produce before unsubscribe - should be received
		await _client.Produce<TestMessage>(Topic, messageKey, new("before", "test"));
		var initialMessages = await WaitForMessages(receiver, 1);
		initialMessages.Should().HaveCount(1);

		await _client.Unsubscribe<TestMessage>(subscriptionId);
		await Task.Delay(6000);

		// Produce after unsubscribe - should NOT be received
		await _client.Produce<TestMessage>(Topic, messageKey, new("after", "test"));
		await Task.Delay(1500);

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
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, messageKey, new("before", "test"));
		await WaitForMessages(receiver, 1);

		var topicSubscription = new TopicSubscription(
			MessageBrokerNames.DefaultBroker,
			subscriptionId,
			Topic,
			messageKey
		);
		await _client.Unsubscribe<TestMessage>(topicSubscription);
		await Task.Delay(6000);

		await _client.Produce<TestMessage>(Topic, messageKey, new("after", "test"));
		await Task.Delay(1500);

		var messages = await receiver.GetReceivedMessages();
		messages.Should().HaveCount(1);
	}

	private static async Task<List<Message<TestMessage>>> WaitForMessages(
		ITestReceiverGrain grain,
		int expectedCount,
		TimeSpan? timeout = null
	)
	{
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
		while (DateTime.UtcNow < deadline)
		{
			var msgs = await grain.GetReceivedMessages();

			if (msgs.Count >= expectedCount)
				return msgs;
			await Task.Delay(100);
		}

		return await grain.GetReceivedMessages();
	}
}
