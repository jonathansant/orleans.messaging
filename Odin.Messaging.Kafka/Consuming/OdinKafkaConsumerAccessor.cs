using Odin.Messaging.Accessors;
using Odin.Messaging.Subscription;

namespace Odin.Messaging.Kafka.Consuming;

public class OdinKafkaConsumerAccessor(
	string serviceKey,
	IGrainFactory grainFactory
) : IConsumerAccessor
{
	public Task RefreshSubscriptionList(string queue, string partition, Dictionary<string, PatternOptions> patterns)
		=> grainFactory.GetConsumerGrain(serviceKey, queue, partition).RefreshSubscriptionList(patterns).AsTask();
}
