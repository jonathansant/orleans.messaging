using Orleans.Messaging.Tests.Memory.Fixtures;

namespace Orleans.Messaging.Tests.Memory.Tests;

[Collection("Memory")]
public class ProduceTests
{
	private const string Topic = "test-messages";

	private readonly IMessagingClient _client;
	private readonly IGrainFactory _grains;

	public ProduceTests(MemoryClusterFixture fixture)
	{
		_client = fixture.ServiceProvider
			.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
		_grains = fixture.GrainFactory;
	}

	[Fact]
	public async Task Produce_SingleMessage_IsReceived()
	{
		var grainKey = $"prod1-{Guid.NewGuid():N}";
		var messageKey = $"key-{grainKey}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(messageKey, o => o.PatternType = PatternType.Exact)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage("hello", "test"));

		var messages = await WaitForMessages(receiver, 1);

		messages.Should().HaveCount(1);
		messages[0].Payload.Should().BeEquivalentTo(new TestMessage("hello", "test"));
		messages[0].Key.Should().Be(messageKey);
	}

	[Fact]
	public async Task Produce_MultipleMessages_AllReceived()
	{
		var grainKey = $"prod2-{Guid.NewGuid():N}";
		var messageKey = $"key-{grainKey}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);
		const int count = 5;

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(messageKey, o => o.PatternType = PatternType.Exact)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		for (var i = 0; i < count; i++)
			await _client.Produce<TestMessage>(Topic, messageKey, new TestMessage($"value-{i}", "batch"));

		var messages = await WaitForMessages(receiver, count);

		messages.Should().HaveCount(count);
		messages.Select(m => m.Payload.Value)
			.Should().BeEquivalentTo(Enumerable.Range(0, count).Select(i => $"value-{i}"));
	}

	[Fact]
	public async Task Produce_MessageWithHeaders_HeadersPreserved()
	{
		var grainKey = $"prod3-{Guid.NewGuid():N}";
		var messageKey = $"key-{grainKey}";
		var receiver = _grains.GetGrain<ITestReceiverGrain>(grainKey);

		await _client.Subscribe<TestMessage>(b => b
			.WithGrainType<ITestReceiverGrain>()
			.WithPrimaryKey(grainKey)
			.WithQueueName(Topic)
			.WithSubscriptionPattern(messageKey, o => o.PatternType = PatternType.Exact)
			.WithGrainAction(nameof(ITestReceiverGrain.HandleMessages))
		);

		var msg = new TestMessage("with-headers", "test").AsMessage(messageKey);
		msg.AddHeader("x-tenant", "acme");
		msg.AddHeader("x-version", "1");
		await _client.Produce<TestMessage>(Topic, msg);

		var messages = await WaitForMessages(receiver, 1);

		messages.Should().HaveCount(1);
		messages[0].Headers.Should().ContainKey("x-tenant").WhoseValue.Should().Be("acme");
		messages[0].Headers.Should().ContainKey("x-version").WhoseValue.Should().Be("1");
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
