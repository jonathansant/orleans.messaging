using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.Memory.Consuming;
using Orleans.Messaging.Memory.Producing;
using Orleans.Messaging.Memory.Utilities;
using Orleans.Messaging.Producing;

namespace Orleans.Messaging.Memory.Config;

public class MessagingMemoryBuilder : MessagingBuilder<MessagingMemoryOptions>
{
	public MessagingMemoryBuilder(ISiloBuilder siloBuilder, string? key)
		: base(siloBuilder, key)
	{
		RegisterMemoryServices(key);
	}

	public MessagingMemoryBuilder(IServiceCollection services, string? key)
		: base(services, key)
	{
		RegisterMemoryServices(key);
	}

	private void RegisterMemoryServices(string? key)
	{
		key ??= "defaultBroker";

		ConfigureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<IProducerClient, ProducerClient>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<ProducerClient>(provider, key)
			);
			services.AddOptions<MessagingMemoryOptions>(key).Configure(OptionsDelegate);
			services.AddKeyedSingleton<IMessagingRuntimeOptionsService, MessagingMemoryRuntimeOptionsService>(
				key,
				(sp, _) => ActivatorUtilities.CreateInstance<MessagingMemoryRuntimeOptionsService>(sp, key)
			);

			services.AddKeyedSingleton<IProducerAccessor, MemoryProducerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<MemoryProducerAccessor>(provider, key)
			);

			services.AddKeyedSingleton<IConsumerAccessor, MemoryConsumerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<MemoryConsumerAccessor>(provider, key)
			);
		};
	}

	public MessagingMemoryBuilder WithOptions(Action<MessagingMemoryOptions> configure)
	{
		OptionsDelegate += configure;
		return this;
	}
}
