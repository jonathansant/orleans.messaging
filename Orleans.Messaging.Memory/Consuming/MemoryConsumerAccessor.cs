using Orleans.Messaging.Accessors;
using Orleans.Messaging.Memory.Config;
using Orleans.Messaging.Memory.Producing;
using Orleans.Messaging.Memory.Utilities;
using Orleans.Messaging.Subscription;
using Orleans.Concurrency;
using GrainFactoryExtensions = Orleans.Messaging.Memory.Producing.GrainFactoryExtensions;

namespace Orleans.Messaging.Memory.Consuming;

public class MemoryConsumerAccessor : IConsumerAccessor
{
	private readonly IMessagingMemoryRuntimeOptionsService _runtimeOptionsService;

	private readonly MessagingMemoryOptions _options;
	private readonly string _serviceKey;
	private readonly IGrainFactory _grainFactory;

	public MemoryConsumerAccessor(
		string serviceKey,
		IGrainFactory grainFactory,
		IServiceProvider serviceProvider
	)
	{
		_serviceKey = serviceKey;
		_grainFactory = grainFactory;
		_runtimeOptionsService =
			(IMessagingMemoryRuntimeOptionsService)serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey);

		_options = (MessagingMemoryOptions)_runtimeOptionsService.GetOptions();
	}

	public Task RefreshSubscriptionList(string queue, string partition, Dictionary<string, PatternOptions> patterns)
	{
		// todo: consider renaming this
		var producerGrainType = _runtimeOptionsService.GetProducerGrainType(queue);
		var hashedPartition = SimpleRingHash.Calculate(_options.MaxPartitionCount, partition).ToString();

		var grainKey = GrainFactoryExtensions.GenerateProducerGrainKey(_serviceKey, queue, hashedPartition);
		return ((IMemoryProducerGrain)_grainFactory.GetGrain(producerGrainType, grainKey)).RefreshSubscriptionList(patterns.AsImmutable())
			.AsTask();
	}
}
