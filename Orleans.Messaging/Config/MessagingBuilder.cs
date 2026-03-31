using Orleans.Messaging.Consuming;
using Orleans.Messaging.Subscription;
using Orleans.Serialization;

namespace Orleans.Messaging.Config;

public abstract class MessagingBuilder<TOptions>
	where TOptions : MessagingOptions, new()
{
	private static readonly IBrokerRegistry BrokerRegistry = new BrokerRegistry();
	private readonly IServiceCollection? _directServices;
	private readonly ISiloBuilder? _siloBuilder;
	protected Action<IServiceCollection> ConfigureServicesDelegate;
	protected Action<TOptions> OptionsDelegate;

	protected MessagingBuilder(ISiloBuilder siloBuilder, string? key)
	{
		_siloBuilder = siloBuilder;
		InitializeDefaultServices(key);
	}

	protected MessagingBuilder(IServiceCollection services, string? key)
	{
		_directServices = services;
		InitializeDefaultServices(key);
	}

	protected bool IsSiloBuild => _siloBuilder is not null;

	private void InitializeDefaultServices(string? key)
	{
		key ??= "default-messaging-builder";

		BrokerRegistry.Add(key);

		ConfigureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<ISubscriptionClient, SubscriptionClient>(key);

			services.AddKeyedSingleton<IMessagingClient, MessagingClient>(
				key,
				(provider, _) => ActivatorUtilities.CreateInstance<MessagingClient>(provider, key)
			);

			services.AddKeyedSingleton(GetType(), key, (_, _) => this);
			services.Configure<ExceptionSerializationOptions>(options => options.SupportedNamespacePrefixes.Add("Orleans"));
			services.AddSingleton<IDigestingUtilityServiceFactory, DigestingUtilityServiceFactory>();
			services.AddSingleton<IBrokerRegistry>(_ => (BrokerRegistry)BrokerRegistry);
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
			RetryDelay = maxRetryDelay ?? TimeSpan.FromMilliseconds(10)
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
	{
		if (_directServices is not null)
			ConfigureServicesDelegate(_directServices);
		else
			_siloBuilder!.ConfigureServices(ConfigureServicesDelegate);
	}
}
