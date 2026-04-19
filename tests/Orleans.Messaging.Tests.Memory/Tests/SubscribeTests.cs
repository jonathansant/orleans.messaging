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

	[Fact]
	public async Task Subscribe_WildcardPattern_MatchesSuffixGlob()
	{
		var grainKey = $"wc-suffix-{Guid.NewGuid():N}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern("order-*", o => o.PatternType = PatternType.Wildcard)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "order-123", new TestMessage("match-1", "test"));
		await _client.Produce<TestMessage>(Topic, "order-456", new TestMessage("match-2", "test"));
		await _client.Produce<TestMessage>(Topic, "invoice-789", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["order-123", "order-456"]);
	}

	[Fact]
	public async Task Subscribe_WildcardPattern_MatchesPrefixGlob()
	{
		var grainKey = $"wc-prefix-{Guid.NewGuid():N}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern("*.shipped", o => o.PatternType = PatternType.Wildcard)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "parcel.shipped", new TestMessage("match-1", "test"));
		await _client.Produce<TestMessage>(Topic, "package.shipped", new TestMessage("match-2", "test"));
		await _client.Produce<TestMessage>(Topic, "parcel.pending", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["parcel.shipped", "package.shipped"]);
	}

	[Fact]
	public async Task Subscribe_WildcardPattern_SingleCharWildcard()
	{
		var grainKey = $"wc-single-{Guid.NewGuid():N}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern("event-?", o => o.PatternType = PatternType.Wildcard)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "event-1", new TestMessage("match-1", "test"));
		await _client.Produce<TestMessage>(Topic, "event-a", new TestMessage("match-2", "test"));
		await _client.Produce<TestMessage>(Topic, "event-12", new TestMessage("no-match", "test"));

		var messages = await WaitForMessages(receiver, 2);

		messages.Should().HaveCount(2);
		messages.Select(m => m.Key).Should().BeEquivalentTo(["event-1", "event-a"]);
	}

	[Fact]
	public async Task Subscribe_WildcardPattern_DoesNotCrossContaminateOtherSubscriptions()
	{
		var grainKeyA = $"wc-a-{Guid.NewGuid():N}";
		var grainKeyB = $"wc-b-{Guid.NewGuid():N}";
		var receiverA = _grains.GetGrain<ITestReceiverGrain>(grainKeyA);
		var receiverB = _grains.GetGrain<ITestReceiverGrain>(grainKeyB);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKeyA)
			.WithQueueName(Topic)
			.WithSubscriptionPattern("order-*", o => o.PatternType = PatternType.Wildcard)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKeyB)
			.WithQueueName(Topic)
			.WithSubscriptionPattern("payment-*", o => o.PatternType = PatternType.Wildcard)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, "order-100", new TestMessage("order", "test"));
		await _client.Produce<TestMessage>(Topic, "payment-200", new TestMessage("payment", "test"));

		var messagesA = await WaitForMessages(receiverA, 1);
		var messagesB = await WaitForMessages(receiverB, 1);

		messagesA.Should().HaveCount(1);
		messagesA[0].Key.Should().Be("order-100");

		messagesB.Should().HaveCount(1);
		messagesB[0].Key.Should().Be("payment-200");
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
