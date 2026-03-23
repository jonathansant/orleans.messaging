using Microsoft.Extensions.Configuration;

namespace Odin.Core.Config.MultiCluster;

public interface IMultiClusterConfigService
{
	string GetDefaultName();
	ClusterConfig GetDefault();
	ClusterConfig Get(string clusterId);
	Dictionary<string, ClusterConfig> GetAll();
}

public class MultiClusterConfigService : IMultiClusterConfigService
{
	private readonly MultiClusterConfig _config;
	private readonly Dictionary<string, ClusterConfig> _enabledCluster;

	public MultiClusterConfigService(IConfiguration configuration)
	{
		_config = configuration.GetSection("multiCluster").GetDynamic<MultiClusterConfig>();
		_enabledCluster = _config?.Clusters?.Where(cluster => cluster.Value.IsEnabled).ToDictionary(x => x.Key, x => x.Value);
	}

	public string GetDefaultName() => _config.Default;

	public ClusterConfig GetDefault() => Get(_config.Default);

	public ClusterConfig Get(string clusterId)
	{
		var config = GetOrDefault(clusterId) ?? throw new KeyNotFoundException($"Cluster config not found for cluster: '{clusterId}'");
		return config;
	}

	public Dictionary<string, ClusterConfig> GetAll()
	{
		if (_enabledCluster.IsNullOrEmpty())
			throw new ArgumentException("No MultiCluster configuration supplied.");

		return _enabledCluster;
	}

	private ClusterConfig GetOrDefault(string clusterId)
	{
		_config.Clusters.TryGetValue(clusterId, out var result);
		return result;
	}
}
