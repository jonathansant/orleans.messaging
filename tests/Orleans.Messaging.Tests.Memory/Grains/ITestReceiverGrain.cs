using Orleans.Concurrency;

namespace Orleans.Messaging.Tests.Memory.Grains;

public interface ITestReceiverGrain : IGrainWithStringKey
{
	[SubscriptionHandler]
	Task HandleMessages(ImmutableList<Message<TestMessage>> messages);

	Task<List<Message<TestMessage>>> GetReceivedMessages();

	Task Reset();
}

[GenerateSerializer]
public record TestMessage(
	[property: Id(0)] string Value,
	[property: Id(1)] string Category
);
