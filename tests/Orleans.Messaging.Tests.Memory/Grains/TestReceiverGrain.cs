using System.Collections.Concurrent;
using Orleans.Concurrency;

namespace Orleans.Messaging.Tests.Memory.Grains;

public class TestReceiverGrain : Grain, ITestReceiverGrain
{
	// Static store keyed by grain primary key – safe for in-process TestCluster.
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
