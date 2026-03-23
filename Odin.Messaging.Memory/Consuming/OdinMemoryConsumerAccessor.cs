using Odin.Messaging.Accessors;
using Odin.Messaging.Memory.Config;
using Odin.Messaging.Memory.Producing;
using Odin.Messaging.Memory.Utilities;
using Odin.Messaging.Subscription;
using Orleans.Concurrency;
using GrainFactoryExtensions = Odin.Messaging.Memory.Producing.GrainFactoryExtensions;

namespace Odin.Messaging.Memory.Consuming;

public class OdinMemoryConsumerAccessor : IConsumerAccessor
{
	private readonly IMessagingMemoryRuntimeOptionsService _runtimeOptionsService;

	private readonly OdinMessagingMemoryOptions _options;
	private readonly string _serviceKey;
	private readonly IGrainFactory _grainFactory;

	public OdinMemoryConsumerAccessor(
		string serviceKey,
		IGrainFactory grainFactory,
		IServiceProvider serviceProvider
	)
	{
		_serviceKey = serviceKey;
		_grainFactory = grainFactory;
		_runtimeOptionsService =
			(IMessagingMemoryRuntimeOptionsService)serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(serviceKey);

		_options = (OdinMessagingMemoryOptions)_runtimeOptionsService.GetOptions();
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
