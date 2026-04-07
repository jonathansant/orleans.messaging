using Microsoft.Extensions.Hosting;
using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.Memory.Producing;
using Orleans.Messaging.Producing;

namespace Orleans.Messaging.Memory.Config;

/// <summary>
///     Lean builder for registering in-memory messaging services in an Orleans client
///     (outside the silo). Omits silo-only infrastructure: <see cref="IConsumerAccessor" />.
///     Configure via <see cref="WithOptions" /> using <see cref="MessagingMemoryOptions" />.
/// </summary>
public sealed class MessagingMemoryClientBuilder : MessagingClientBuilder<MessagingMemoryClientOptions>
{
	public MessagingMemoryClientBuilder(string key)
		: base(key)
	{
		configureServicesDelegate += s =>
		{
			s.AddKeyedSingleton<IProducerClient, ProducerClient>(
				key,
				(sp, _) => ActivatorUtilities.CreateInstance<ProducerClient>(sp, key)
			);
			s.AddOptions<MessagingMemoryClientOptions>(key).Configure(optionsDelegate);
			s.AddKeyedSingleton<IMessagingOptionsService>(
				key,
				(sp, _) => new MessagingOptionsService<MessagingMemoryClientOptions>(
					sp.GetRequiredService<IOptionsMonitor<MessagingMemoryClientOptions>>(), key)
			);
			s.AddKeyedSingleton<IProducerAccessor, MemoryProducerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<MemoryProducerAccessor>(provider, key)
			);
		};
	}

	public MessagingMemoryClientBuilder WithOptions(Action<MessagingMemoryClientOptions> configure)
	{
		optionsDelegate += configure;

		return this;
	}

	public MessagingMemoryClientBuilder WithProducerEnabled(bool enabled)
	{
		optionsDelegate += opt => opt.IsProduceEnabled = enabled;

		return this;
	}
}

public static class MessagingMemoryClientBuilderExtensions
{
	extension(IHostBuilder hostBuilder)
	{
		public MessagingMemoryClientBuilder AddMessagingMemoryClient(string serviceKey)
			=> new(serviceKey);

		public IHostBuilder AddMessagingMemoryClient(string serviceKey, Action<MessagingMemoryClientBuilder> cfg)
		{
			var builder = new MessagingMemoryClientBuilder(serviceKey);
			cfg(builder);
			hostBuilder.ConfigureServices(builder.Build);

			return hostBuilder;
		}
	}

	extension(IServiceCollection services)
	{
		public MessagingMemoryClientBuilder AddMessagingMemoryClient(string serviceKey)
			=> new(serviceKey);

		public IServiceCollection AddMessagingMemoryClient(string serviceKey, Action<MessagingMemoryClientBuilder> cfg)
		{
			var builder = new MessagingMemoryClientBuilder(serviceKey);
			cfg(builder);
			builder.Build(services);

			return services;
		}
	}
}
