using Orleans.Messaging.Accessors;
using Orleans.Messaging.Subscription;

namespace Orleans.Messaging.Kafka.Consuming;

public class KafkaConsumerAccessor(
	string serviceKey,
	IGrainFactory grainFactory
) : IConsumerAccessor
{
	public Task RefreshSubscriptionList(string queue, string partition, Dictionary<string, PatternOptions> patterns)
		=> grainFactory.GetConsumerGrain(serviceKey, queue, partition).RefreshSubscriptionList(patterns).AsTask();
}
