using Odin.Messaging.Config;

namespace Odin.Messaging;

public interface IMessagingRuntimeOptionsService
{
	public Type OptionsType { get; }
	public Type OptionsMonitorType { get; }

	public OdinMessagingOptions GetOptions();
	Type GetSubscriptionGrainType(string queue);
	Type GetProducerGrainType(string queue);
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

	public OdinMessagingOptions GetOptions()
		=> ((IOptionsMonitor<OdinMessagingOptions>)_serviceProvider.GetRequiredService(OptionsMonitorType))
			.Get(_serviceKey);

	public abstract Type GetSubscriptionGrainType(string queue);

	public abstract Type GetProducerGrainType(string queue);
}
