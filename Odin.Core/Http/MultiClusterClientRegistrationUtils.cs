using Odin.Core.Config.MultiCluster;

namespace Odin.Core.Http;

public static class MultiClusterClientRegistrationUtils
{
	public static void RegisterClient(
		IMultiClusterConfigService configService,
		Type httpClientType,
		Type entityType,
		Func<string, object?> getOrCreateClient,
		IDictionary<string, object> clients,
		Action<string, object> baseRegisterAction)
	{
		var name = entityType.Name;
		foreach (var cluster in configService.GetAll())
		{
			var key = GenerateKey(cluster.Key, name);
			if (clients.ContainsKey(key))
				throw new AggregateException($"Type already registered '{name}'");

			var httpClient = getOrCreateClient(cluster.Key);
			clients.Add(key, httpClient);
			baseRegisterAction(GenerateKey(cluster.Key, httpClientType.Name), httpClient);
		}
	}

	public static string GenerateKey(string clusterId, string name) => $"{name}:{clusterId}";
}
