using FluentlyHttpClient;
using Microsoft.Extensions.DependencyInjection;
using Odin.Core.Config.MultiCluster;

namespace Odin.Core.Http;

public interface IMultiClusterClientResolver
{
	object GetHttpClient(Type type, string? clusterId = null);
	object? GetHttpClientOrDefault(Type type, string? clusterId = null);

	THttpClient GetHttpClient<THttpClient>(string? clusterId = null)
		where THttpClient : class;

	THttpClient GetDefaultHttpClient<THttpClient>()
		where THttpClient : class;

	void Register(Type httpClient, IFluentHttpClient clusterHttpClient);
	void Register(string key, object httpClient);
}

public class MultiClusterClientResolver(
	IMultiClusterConfigService configService,
	IServiceProvider serviceProvider
) : IMultiClusterClientResolver
{
	private readonly Dictionary<string, object> _clients = new();

	public void Register(Type httpClient, IFluentHttpClient clusterHttpClient)
		=> TryRegister(
			httpClient,
			clusterHttpClient,
			client => throw new InvalidOperationException($"Type already registered '{client.Name}'")
		);

	public void Register(string key, object httpClient)
		=> _clients.TryAdd(key, httpClient);

	public THttpClient GetDefaultHttpClient<THttpClient>()
		where THttpClient : class
		=> GetHttpClient<THttpClient>(configService.GetDefaultName());

	public object? GetHttpClientOrDefault(Type type, string? clusterId = null)
	{
		TryGetHttpClient(type, clusterId, out var client);
		return client;
	}

	public object GetHttpClient(Type type, string? clusterId = null)
	{
		if (!TryGetHttpClient(type, clusterId, out var client))
			throw new ArgumentException("No client registered for the requested clusterId.", clusterId);

		return client!;
	}

	public THttpClient GetHttpClient<THttpClient>(string? clusterId = null)
		where THttpClient : class
		=> (THttpClient)GetHttpClient(typeof(THttpClient), clusterId);

	private bool TryRegister(Type httpClient, IFluentHttpClient clusterHttpClient, Action<Type>? onExists = null)
	{
		var registered = false;
		var name = httpClient.Name;
		foreach (var cluster in configService.GetAll())
		{
			var key = GenerateKey(cluster.Key, name);
			if (_clients.ContainsKey(key))
			{
				onExists?.Invoke(httpClient);
				continue;
			}

			var client = ActivatorUtilities.CreateInstance(serviceProvider, httpClient, clusterHttpClient);
			_clients.Add(key, client);
			registered = true;
		}

		return registered;
	}
	private bool TryGetHttpClient(Type type, string? clusterId, out object? client)
	{
		if (clusterId.IsNullOrEmpty())
			clusterId = configService.GetDefaultName();

		var name = type.IsInterface ? type.Name[1..] : type.Name;

		var key = GenerateKey(clusterId, name);

		return _clients.TryGetValue(key, out client);
	}

	protected static string GenerateKey(string clusterId, string name) => $"{name}:{clusterId}";
}
