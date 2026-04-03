using Orleans.Messaging.Tests.Kafka.Fixtures;

namespace Orleans.Messaging.Tests.Kafka.Tests;

[Collection("Kafka")]
public class SubscribeTests
{
	private const string Topic = "test-messages";

	private readonly IMessagingClient _client;
	private readonly IGrainFactory _grains;

	public SubscribeTests(KafkaClusterFixture fixture)
	{
		_client = fixture.ServiceProvider
			.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
		_grains = fixture.GrainFactory;
	}

	[Fact]
	public async Task Subscribe_ExactPattern_ReceivesOnlyMatchingMessages()
	{
		var grainKey = $"exact-{Guid.NewGuid():N}";
		var messageKey = $"key-{grainKey}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(messageKey, o => o.PatternType = PatternType.Exact)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage("match", "test"));
		await _client.Produce<TestMessage>(Topic, "other-key", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 1);

		messages.Should().HaveCount(1);
		messages[0].Key.Should().Be(messageKey);
		messages[0].Payload.Value.Should().Be("match");
	}

	[Fact]
	public async Task Subscribe_SubstringPattern_ReceivesAllKeysContainingPattern()
	{
		var grainKey = $"sub-{Guid.NewGuid():N}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern("shipment", o => o.PatternType = PatternType.Substring)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "shipment-100", new TestMessage("s1", "test"));
		await _client.Produce<TestMessage>(Topic, "shipment-200", new TestMessage("s2", "test"));
		await _client.Produce<TestMessage>(Topic, "invoice-300", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["shipment-100", "shipment-200"]);
	}

	[Fact]
	public async Task Subscribe_RegexPattern_ReceivesOnlyRegexMatchingMessages()
	{
		var grainKey = $"regex-{Guid.NewGuid():N}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(@"^event-\d+$", o => o.PatternType = PatternType.Regex)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "event-1", new TestMessage("e1", "test"));
		await _client.Produce<TestMessage>(Topic, "event-2", new TestMessage("e2", "test"));
		await _client.Produce<TestMessage>(Topic, "event-abc", new TestMessage("no-match", "test"));
		await _client.Produce<TestMessage>(Topic, "log-1", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["event-1", "event-2"]);
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
