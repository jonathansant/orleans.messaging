using Orleans.Concurrency;
using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.Memory.Config;
using Orleans.Messaging.Memory.Producing;
using Orleans.Messaging.Memory.Utilities;
using Orleans.Messaging.Subscription;
using GrainFactoryExtensions = Orleans.Messaging.Memory.Producing.GrainFactoryExtensions;

namespace Orleans.Messaging.Memory.Consuming;

public class MemoryConsumerAccessor : IConsumerAccessor
{
	private readonly IGrainFactory _grainFactory;

	private readonly MessagingMemoryOptions _options;
	private readonly IMessagingMemoryRuntimeOptionsService _runtimeOptionsService;
	private readonly string _serviceKey;

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

		_options = (MessagingMemoryOptions)serviceProvider.GetRequiredKeyedService<IMessagingOptionsService>(serviceKey).GetOptions();
	}

	public async Task RefreshSubscriptionList(string queue, string partition, Dictionary<string, PatternOptions> patterns)
	{
		// todo: consider renaming this
		var producerGrainType = await _runtimeOptionsService.GetProducerGrainType(queue);
		var hashedPartition = SimpleRingHash.Calculate(_options.MaxPartitionCount, partition).ToString();

		var grainKey = GrainFactoryExtensions.GenerateProducerGrainKey(_serviceKey, queue, hashedPartition);

		var refreshTask = ((IMemoryProducerGrain)_grainFactory.GetGrain(producerGrainType, grainKey))
			.RefreshSubscriptionList(patterns.AsImmutable());

		await refreshTask;
	}
}
