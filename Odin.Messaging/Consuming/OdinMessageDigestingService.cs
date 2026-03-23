using Odin.Messaging.Subscription;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using GrainFactoryExtensions = Odin.Messaging.Subscription.GrainFactoryExtensions;

namespace Odin.Messaging.Consuming;

public interface IOdinDigestingUtilityService
{
	void PopulateSubscriptionBatch<TBatch>(
		ref Dictionary<string, List<TBatch>> subscriptionBatches,
		OdinMessage message,
		Func<OdinMessage, TBatch> transformResult = null
	) where TBatch : BatchResult, new();

	bool HasSubscriptions { get; }

	void UpdateCache(Dictionary<string, PatternOptions> subscriptionTable);

	ISubscriptionGrain GetSubscriptionGrain(string queue, string subscriptionKey);
}

internal class OdinDigestingUtilityService(
	string serviceKey,
	string queueName,
	IMessagingRuntimeOptionsService runtimeOptionsService,
	IGrainFactory grainFactory
) : IOdinDigestingUtilityService
{
	private readonly ConcurrentDictionary<string, Regex> _subscriptionRegexes = new();
	private Dictionary<string, PatternOptions> _subscriptionTable = new();

	public bool HasSubscriptions => !_subscriptionTable.IsNullOrEmpty();

	public void PopulateSubscriptionBatch<TBatch>(
		ref Dictionary<string, List<TBatch>> subscriptionBatches,
		OdinMessage message,
		Func<OdinMessage, TBatch> transformResult = null
	) where TBatch : BatchResult, new()
	{
		List<KeyValuePair<string, PatternOptions>> subscriptions;
		if (_subscriptionTable.TryGetValue(message.Key, out var patternOptions) && patternOptions.PatternType == PatternType.Exact)
			subscriptions = new KeyValuePair<string, PatternOptions>(message.Key, patternOptions).ToSingleList();
		else
			subscriptions = _subscriptionTable.Where(x => x.Value.PatternType switch
					{
						PatternType.Exact => false,
						PatternType.Substring => message.Key.Contains(x.Key),
						_ => _subscriptionRegexes[x.Key].IsMatch(message.Key)
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
		_subscriptionRegexes.AddRange(_subscriptionTable.ToDictionary(x => x.Key, x => new Regex(x.Key, RegexOptions.Compiled)));
	}

	public ISubscriptionGrain GetSubscriptionGrain(string queue, string subscriptionKey)
	{
		var grainKey = GrainFactoryExtensions.GenerateSubscriptionGrainKey(
			serviceKey,
			queueName,
			subscriptionKey
		);

		var subscriptionGrainType = runtimeOptionsService.GetSubscriptionGrainType(queue);
		var subscriptionGrain = (ISubscriptionGrain)grainFactory.GetGrain(subscriptionGrainType, grainKey);

		return subscriptionGrain;
	}
}

public sealed class OdinDigestingUtilityServiceFactory(
	IServiceProvider serviceProvider
) : IOdinDigestingUtilityServiceFactory
{
	public IOdinDigestingUtilityService Create(string serviceKey, string queueName)
	{
		var runtimeOptionsService = serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey);
		return ActivatorUtilities.CreateInstance<OdinDigestingUtilityService>(
			serviceProvider,
			serviceKey,
			queueName,
			runtimeOptionsService
		);
	}
}

public interface IOdinDigestingUtilityServiceFactory
{
	IOdinDigestingUtilityService Create(string serviceKey, string queueName);
}

public record BatchResult
{
	public OdinMessage Message { get; init; }
}
