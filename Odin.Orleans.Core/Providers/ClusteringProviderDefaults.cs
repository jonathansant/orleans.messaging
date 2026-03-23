using Odin.Core.App;

namespace Odin.Orleans.Core.Providers;

public static class ClusteringProviderDefaults
{
	/// <summary>
	/// Gets the Default ClusteringProvider type to use for both Silo and Client.
	/// </summary>
	// ReSharper disable once UnassignedReadonlyField
	public static readonly ClusteringProviderType? DefaultClusteringProvider;

	/// <summary>
	/// Gets the default ClusteringProvider for local-env.
	/// </summary>
	private const ClusteringProviderType DefaultDevClusteringProviderType = ClusteringProviderType.Static;
	/// <summary>
	/// Gets the default ClusteringProvider for remote e.g. docker or non-local-env.
	/// </summary>
	private const ClusteringProviderType DefaultRemoteClusteringProviderType = ClusteringProviderType.Consul;

	public static ClusteringProviderType GetClusteringProviderOrDefault(IAppInfo appInfo, ClusteringProviderType? specified)
	{
		if (specified.HasValue)
			return specified.Value;

		if (!appInfo.IsDevelopment || appInfo.IsDockerized)
			return DefaultRemoteClusteringProviderType;

		return DefaultDevClusteringProviderType;
	}
}
