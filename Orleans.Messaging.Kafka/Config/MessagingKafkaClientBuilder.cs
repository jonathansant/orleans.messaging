using Microsoft.Extensions.Hosting;
using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.Kafka.Producing;
using Orleans.Messaging.Producing;

namespace Orleans.Messaging.Kafka.Config;

/// <summary>
///     Lean builder for registering Kafka messaging services in an Orleans client (outside the silo).
///     Registers <see cref="IProducerClient" />, <see cref="IMessagingClient" />,
///     <c>ISubscriptionClient</c>, and <c>IMessagingRuntimeOptionsService</c>.
///     Configure via <see cref="WithOptions" /> using <see cref="MessagingKafkaClientOptions" />.
/// </summary>
public sealed class MessagingKafkaClientBuilder : MessagingClientBuilder<MessagingKafkaClientOptions>
{
	internal MessagingKafkaClientBuilder(string key)
		: base(key)
	{
		configureServicesDelegate += s =>
		{
			s.AddOptions<MessagingKafkaClientOptions>(key).Configure(optionsDelegate);
			s.AddKeyedSingleton<IMessagingOptionsService>(
				key,
				(sp, _) => new MessagingOptionsService<MessagingKafkaClientOptions>(
					sp.GetRequiredService<IOptionsMonitor<MessagingKafkaClientOptions>>(), key)
			);
			s.AddKeyedSingleton<IProducerClient, ProducerClient>(
				key,
				(sp, _) => ActivatorUtilities.CreateInstance<ProducerClient>(sp, key)
			);
			s.AddKeyedSingleton<IProducerAccessor, KafkaProducerAccessor>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<KafkaProducerAccessor>(provider, key)
			);
		};
	}

	public MessagingKafkaClientBuilder WithOptions(Action<MessagingKafkaClientOptions> configure)
	{
		optionsDelegate += configure;

		return this;
	}

	public MessagingKafkaClientBuilder WithProducerEnabled(bool enabled)
	{
		optionsDelegate += opt => opt.IsProduceEnabled = enabled;

		return this;
	}
}

public static class MessagingKafkaClientBuilderExtensions
{
	extension(IHostBuilder hostBuilder)
	{
		public MessagingKafkaClientBuilder AddMessagingKafkaClient(string serviceKey)
			=> new(serviceKey);

		public IHostBuilder AddMessagingKafkaClient(string serviceKey, Action<MessagingKafkaClientBuilder> cfg)
		{
			var builder = new MessagingKafkaClientBuilder(serviceKey);
			cfg(builder);
			hostBuilder.ConfigureServices(builder.Build);

			return hostBuilder;
		}
	}

	extension(IServiceCollection services)
	{
		public MessagingKafkaClientBuilder AddMessagingKafkaClient(string serviceKey)
			=> new(serviceKey);

		public IServiceCollection AddMessagingKafkaClient(string serviceKey, Action<MessagingKafkaClientBuilder> cfg)
		{
			var builder = new MessagingKafkaClientBuilder(serviceKey);
			cfg(builder);
			builder.Build(services);

			return services;
		}
	}
}
