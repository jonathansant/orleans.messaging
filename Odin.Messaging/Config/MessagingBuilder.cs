using Odin.Messaging.Consuming;
using Odin.Messaging.Subscription;
using Orleans.Serialization;

namespace Odin.Messaging.Config;

public abstract class OdinMessagingBuilder<TOptions>
	where TOptions : OdinMessagingOptions, new()
{
	protected readonly ISiloBuilder SiloBuilder;
	protected Action<TOptions> OptionsDelegate;
	protected Action<IServiceCollection> ConfigureServicesDelegate;

	protected OdinMessagingBuilder(ISiloBuilder siloBuilder, string? key)
	{
		SiloBuilder = siloBuilder;
		key ??= "default-messaging-builder";

		ConfigureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<ISubscriptionClient, SubscriptionClient>(key);

			services.AddKeyedSingleton<IOdinMessagingClient, OdinMessagingClient>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<OdinMessagingClient>(provider, key)
			);

			services.AddKeyedSingleton(GetType(), key, ((_, _) => this));
			services.Configure<ExceptionSerializationOptions>(options => options.SupportedNamespacePrefixes.Add("Odin"));
			services.AddSingleton<IOdinDigestingUtilityServiceFactory, OdinDigestingUtilityServiceFactory>();
		};
	}

	public OdinMessagingBuilder<TOptions> WithStoreName(string name)
	{
		OptionsDelegate += options => options.StoreName = name;
		return this;
	}

	public OdinMessagingBuilder<TOptions> WithProducerRetries(int maxRetries, TimeSpan? maxRetryDelay = null)
	{
		OptionsDelegate += options => options.ProducerRetryOptions = new(maxRetries)
		{
			RetryDelay = maxRetryDelay ?? TimeSpan.FromMilliseconds(10),
		};

		return this;
	}

	public OdinMessagingBuilder<TOptions> WithEnsureHandlerDeliveryOnFailure()
	{
		OptionsDelegate += options => options.EnsureHandlerDeliveryOnFailure = true;
		return this;
	}

	public OdinMessagingBuilder<TOptions> Configure(Action<IServiceCollection> configure)
	{
		ConfigureServicesDelegate += configure;
		return this;
	}

	public void Build()
		=> SiloBuilder.ConfigureServices(ConfigureServicesDelegate);
}
