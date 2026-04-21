using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Orleans.Messaging.Subscription;
using Orleans.Messaging.Utils;
using PatternMatching;
using Orleans.Messaging.Subscription;

namespace Orleans.Messaging.Consuming;

public interface IDigestingUtilityService
{
	bool HasSubscriptions { get; }

	void PopulateSubscriptionBatch<TBatch>(
		ref Dictionary<string, List<TBatch>> subscriptionBatches,
		Message message,
		Func<Message, TBatch> transformResult = null
	)
		where TBatch : BatchResult, new();

	void UpdateCache(Dictionary<string, PatternOptions> subscriptionTable);

	ValueTask<ISubscriptionGrain> GetSubscriptionGrain(string queue, string subscriptionKey);
}

internal class DigestingUtilityService(
	string serviceKey,
	string queueName,
	IMessagingRuntimeOptionsService runtimeOptionsService,
	IGrainFactory grainFactory
) : IDigestingUtilityService
{
	private readonly ConcurrentDictionary<string, Regex> _subscriptionRegexes = new();
	private Dictionary<string, PatternOptions> _subscriptionTable = new();

	public bool HasSubscriptions => !_subscriptionTable.IsNullOrEmpty();

	public void PopulateSubscriptionBatch<TBatch>(
		ref Dictionary<string, List<TBatch>> subscriptionBatches,
		Message message,
		Func<Message, TBatch> transformResult = null
	)
		where TBatch : BatchResult, new()
	{
		List<KeyValuePair<string, PatternOptions>> subscriptions;
		if (_subscriptionTable.TryGetValue(message.Key, out var patternOptions) && patternOptions.PatternType == PatternType.Exact)
			subscriptions = new KeyValuePair<string, PatternOptions>(message.Key, patternOptions).ToSingleList();
		else
			subscriptions = _subscriptionTable.Where(pattern => pattern.Value.PatternType switch
					{
						PatternType.Exact => false,
						PatternType.Substring => message.Key.Contains(pattern.Key),
						PatternType.Wildcard => WildcardMatcher.Test(pattern.Key, message.Key),
						_ => _subscriptionRegexes[pattern.Key].IsMatch(message.Key)
					}
				)
				.ToList();

		if (subscriptions.IsNullOrEmpty())
			return;

		foreach (var subscription in subscriptions)
		{
			var batch = subscriptionBatches.GetOrAdd(subscription.Key, _ => []);
			batch.Add(transformResult?.Invoke(message) ?? new TBatch { Message = message });
		}
	}

	public void UpdateCache(Dictionary<string, PatternOptions> subscriptionTable)
	{
		_subscriptionRegexes.Clear();

		_subscriptionTable = subscriptionTable;
		foreach (var kvp in _subscriptionTable)
			switch (kvp.Value.PatternType)
			{
				case PatternType.Regex:
					_subscriptionRegexes.TryAdd(kvp.Key, new(kvp.Key, RegexOptions.Compiled));

					break;
			}
	}

	public async ValueTask<ISubscriptionGrain> GetSubscriptionGrain(string queue, string subscriptionKey)
	{
		var grainKey = SubscriptionGrainKeys.Generate(serviceKey, queueName, subscriptionKey);

		var subscriptionGrainType = await runtimeOptionsService.GetSubscriptionGrainType(queue);
		var subscriptionGrain = (ISubscriptionGrain)grainFactory.GetGrain(subscriptionGrainType, grainKey);

		return subscriptionGrain;
	}
}

public sealed class DigestingUtilityServiceFactory(
	IServiceProvider serviceProvider
) : IDigestingUtilityServiceFactory
{
	public IDigestingUtilityService Create(string serviceKey, string queueName)
	{
		var runtimeOptionsService = serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey);

		return ActivatorUtilities.CreateInstance<DigestingUtilityService>(
			serviceProvider,
			serviceKey,
			queueName,
			runtimeOptionsService
		);
	}
}

public interface IDigestingUtilityServiceFactory
{
	IDigestingUtilityService Create(string serviceKey, string queueName);
}

public record BatchResult
{
	public Message Message { get; init; }
}
