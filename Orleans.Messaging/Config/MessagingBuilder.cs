using Orleans.Messaging.Consuming;
using Orleans.Messaging.Subscription;
using Orleans.Serialization;

namespace Orleans.Messaging.Config;

public abstract class MessagingBuilder<TOptions>
	where TOptions : MessagingOptions, new()
{
	protected readonly ISiloBuilder SiloBuilder;
	protected Action<TOptions> OptionsDelegate;
	protected Action<IServiceCollection> ConfigureServicesDelegate;

	protected MessagingBuilder(ISiloBuilder siloBuilder, string? key)
	{
		SiloBuilder = siloBuilder;
		key ??= "default-messaging-builder";

		ConfigureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<ISubscriptionClient, SubscriptionClient>(key);

			services.AddKeyedSingleton<IMessagingClient, MessagingClient>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<MessagingClient>(provider, key)
			);

			services.AddKeyedSingleton(GetType(), key, ((_, _) => this));
			services.Configure<ExceptionSerializationOptions>(options => options.SupportedNamespacePrefixes.Add("Orleans"));
			services.AddSingleton<IDigestingUtilityServiceFactory, DigestingUtilityServiceFactory>();
		};
	}

	public MessagingBuilder<TOptions> WithStoreName(string name)
	{
		OptionsDelegate += options => options.StoreName = name;
		return this;
	}

	public MessagingBuilder<TOptions> WithProducerRetries(int maxRetries, TimeSpan? maxRetryDelay = null)
	{
		OptionsDelegate += options => options.ProducerRetryOptions = new(maxRetries)
		{
			RetryDelay = maxRetryDelay ?? TimeSpan.FromMilliseconds(10),
		};

		return this;
	}

	public MessagingBuilder<TOptions> WithEnsureHandlerDeliveryOnFailure()
	{
		OptionsDelegate += options => options.EnsureHandlerDeliveryOnFailure = true;
		return this;
	}

	public MessagingBuilder<TOptions> Configure(Action<IServiceCollection> configure)
	{
		ConfigureServicesDelegate += configure;
		return this;
	}

	public void Build()
		=> SiloBuilder.ConfigureServices(ConfigureServicesDelegate);
}
