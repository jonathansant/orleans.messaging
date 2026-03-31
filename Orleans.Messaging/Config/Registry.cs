namespace Orleans.Messaging.Config;

public interface IBrokerRegistry
{
	void Add(string broker);

	HashSet<string> GetAll();
}

public class BrokerRegistry : IBrokerRegistry
{
	private readonly HashSet<string> _brokers = [];

	public void Add(string broker) =>
		_brokers.Add(broker);

	public HashSet<string> GetAll() =>
		_brokers;
}
