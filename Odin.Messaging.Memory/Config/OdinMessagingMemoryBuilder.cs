using Odin.Messaging.Accessors;
using Odin.Messaging.Config;
using Odin.Messaging.Memory.Consuming;
using Odin.Messaging.Memory.Producing;
using Odin.Messaging.Memory.Utilities;
using Odin.Messaging.Producing;

namespace Odin.Messaging.Memory.Config;

public class OdinMessagingMemoryBuilder : OdinMessagingBuilder<OdinMessagingMemoryOptions>
{
	public OdinMessagingMemoryBuilder(ISiloBuilder siloBuilder, string? key)
		: base(siloBuilder, key)
	{
		key ??= OdinMessageBrokerNames.Default;

		ConfigureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<IProducerClient, ProducerClient>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<ProducerClient>(provider, key)
			);
			services.AddOptions<OdinMessagingMemoryOptions>(key).Configure(OptionsDelegate);
			services.AddKeyedSingleton<IMessagingRuntimeOptionsService, MessagingMemoryRuntimeOptionsService>(
				key,
				(sp, _) => ActivatorUtilities.CreateInstance<MessagingMemoryRuntimeOptionsService>(sp, key)
			);

			services.AddKeyedSingleton<IOdinProducerAccessor, OdinMemoryProducerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<OdinMemoryProducerAccessor>(provider, key)
			);

			services.AddKeyedSingleton<IConsumerAccessor, OdinMemoryConsumerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<OdinMemoryConsumerAccessor>(provider, key)
			);
		};
	}

	public OdinMessagingMemoryBuilder WithOptions(Action<OdinMessagingMemoryOptions> configure)
	{
		OptionsDelegate += configure;
		return this;
	}
}
