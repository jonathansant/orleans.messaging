namespace Orleans.Messaging.Config;

public interface IMessagingOptionsService
{
	IMessagingOptions GetOptions();
}

public class MessagingOptionsService<TOptions>(
	IOptionsMonitor<TOptions> optionsMonitor,
	string serviceKey
) : IMessagingOptionsService
	where TOptions : class, IMessagingOptions
{
	public IMessagingOptions GetOptions() => optionsMonitor.Get(serviceKey);
}
