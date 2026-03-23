using Odin.Messaging.Memory.Config;
using Odin.Messaging.Memory.Producing;
using Odin.Messaging.Subscription;
using System.Collections.Concurrent;

namespace Odin.Messaging.Memory.Utilities;

public interface IMessagingMemoryRuntimeOptionsService : IMessagingRuntimeOptionsService
{
	void RegisterQueueType(string queue, Type contractType);
}

public class MessagingMemoryRuntimeOptionsService(
	IServiceProvider serviceProvider,
	string serviceKey
) : MessagingRuntimeOptionsService(serviceKey, serviceProvider), IMessagingMemoryRuntimeOptionsService
{
	private readonly ConcurrentDictionary<string, Type> _queueTypes = new();
	private readonly Type _subscriptionGrainType = typeof(ISubscriptionGrain<>);
	private readonly Type _producerGrainType = typeof(IMemoryProducerGrain<>);
	private readonly ConcurrentDictionary<string, Type> _producerGrainTypes = new();
	private readonly ConcurrentDictionary<string, Type> _subscriptionGrainTypes = new();

	public override Type OptionsType { get; } = typeof(OdinMessagingMemoryOptions);

	public override Type GetSubscriptionGrainType(string queue)
		=> _subscriptionGrainTypes.GetOrAdd(queue, k => _subscriptionGrainType.MakeGenericType(_queueTypes[k]));

	public override Type GetProducerGrainType(string queue)
		=> _producerGrainTypes.GetOrAdd(queue, k => _producerGrainType.MakeGenericType(_queueTypes[k]));

	public void RegisterQueueType(string queue, Type contractType)
		=> _queueTypes.TryAdd(queue, contractType);
}
