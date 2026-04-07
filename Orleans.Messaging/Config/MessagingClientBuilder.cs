using Orleans.Messaging.Subscription;

namespace Orleans.Messaging.Config;

public abstract class MessagingClientBuilder<TOptions>
	where TOptions : class, new()
{
	protected Action<IServiceCollection> configureServicesDelegate;
	protected Action<TOptions> optionsDelegate = _ => { };

	protected MessagingClientBuilder(string serviceKey)
	{
		configureServicesDelegate += services =>
		{
			services.AddKeyedSingleton<ISubscriptionClient, SubscriptionClient>(serviceKey);

			services.AddKeyedSingleton<IMessagingClient, MessagingClient>(
				serviceKey,
				(provider, _) => ActivatorUtilities.CreateInstance<MessagingClient>(provider, serviceKey)
			);
		};
	}

	public void Build(IServiceCollection services)
		=> configureServicesDelegate(services);
}
