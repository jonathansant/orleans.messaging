using System.Collections.Concurrent;

namespace Orleans.Messaging.Tests.Grains;

public interface ITestReceiverGrain : IGrainWithStringKey
{
	Task HandleMessages(ImmutableList<Message<TestMessage>> messages);

	Task<List<Message<TestMessage>>> GetReceivedMessages();

	Task Reset();
}

[GenerateSerializer]
public record TestMessage(
	[property: Id(0)] string Value,
	[property: Id(1)] string Category
);

public class TestReceiverGrain : Grain, ITestReceiverGrain
{
	private static readonly ConcurrentDictionary<string, List<Message<TestMessage>>> Store = new();

	public Task HandleMessages(ImmutableList<Message<TestMessage>> messages)
	{
		Store.GetOrAdd(this.GetPrimaryKeyString(), _ => []).AddRange(messages);

		return Task.CompletedTask;
	}

	public Task<List<Message<TestMessage>>> GetReceivedMessages()
		=> Task.FromResult(Store.GetOrAdd(this.GetPrimaryKeyString(), _ => []));

	public Task Reset()
	{
		Store[this.GetPrimaryKeyString()] = [];

		return Task.CompletedTask;
	}
}

public class PlaygroundActivate(
	IGrainFactory grainFactory
) : IStartupTask
{
	public async Task Execute(CancellationToken cancellationToken)
		=> await grainFactory.GetGrain<ITestReceiverGrain>("test")
			.HandleMessages([new() { Payload = new("Hello", "Greeting") }]);
}
