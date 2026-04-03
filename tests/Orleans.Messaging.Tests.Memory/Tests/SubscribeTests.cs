using Orleans.Messaging.Tests.Memory.Fixtures;

namespace Orleans.Messaging.Tests.Memory.Tests;

[Collection("Memory")]
public class SubscribeTests
{
	private const string Topic = "test-messages";

	private readonly IMessagingClient _client;
	private readonly IGrainFactory _grains;

	public SubscribeTests(MemoryClusterFixture fixture)
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
			.WithSubscriptionPattern("order", o => o.PatternType = PatternType.Substring)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "order-123", new TestMessage("order-1", "test"));
		await _client.Produce<TestMessage>(Topic, "order-456", new TestMessage("order-2", "test"));
		await _client.Produce<TestMessage>(Topic, "invoice-789", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["order-123", "order-456"]);
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
			.WithSubscriptionPattern(@"^payment-\d+$", o => o.PatternType = PatternType.Regex)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "payment-100", new TestMessage("match-1", "test"));
		await _client.Produce<TestMessage>(Topic, "payment-200", new TestMessage("match-2", "test"));
		await _client.Produce<TestMessage>(Topic, "payment-abc", new TestMessage("no-match", "test"));
		await _client.Produce<TestMessage>(Topic, "refund-100", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["payment-100", "payment-200"]);
	}

	private static async Task<List<Message<TestMessage>>> WaitForMessages(
		ITestReceiverGrain grain,
		int expectedCount,
		TimeSpan? timeout = null)
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
