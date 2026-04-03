using Orleans.Messaging.Config;

namespace Orleans.Messaging;

public interface IMessagingRuntimeOptionsService
{
	public Type OptionsType { get; }
	public Type OptionsMonitorType { get; }

	public MessagingOptions GetOptions();
	ValueTask<Type> GetSubscriptionGrainType(string queue);
	ValueTask<Type> GetProducerGrainType(string queue);
}

public abstract class MessagingRuntimeOptionsService : IMessagingRuntimeOptionsService
{
	private readonly string _serviceKey;
	private readonly IServiceProvider _serviceProvider;
	public abstract Type OptionsType { get; }
	public Type OptionsMonitorType { get; }

	protected MessagingRuntimeOptionsService(
		string serviceKey,
		IServiceProvider serviceProvider
	)
	{
		_serviceKey = serviceKey;
		_serviceProvider = serviceProvider;
		OptionsMonitorType = typeof(IOptionsMonitor<>).MakeGenericType(OptionsType);
	}

	public MessagingOptions GetOptions()
		=> ((IOptionsMonitor<MessagingOptions>)_serviceProvider.GetRequiredService(OptionsMonitorType))
			.Get(_serviceKey);

	public abstract ValueTask<Type> GetSubscriptionGrainType(string queue);

	public abstract ValueTask<Type> GetProducerGrainType(string queue);
}
