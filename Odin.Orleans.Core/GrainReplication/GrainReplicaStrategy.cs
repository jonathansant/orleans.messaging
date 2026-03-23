using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Odin.Core.Config;
using Odin.Core.Utils;

namespace Odin.Orleans.Core.GrainReplication;

public abstract class GrainReplicaStrategy
{
	protected int MaxCount { get; }
	protected bool CapMaxClusterSize { get; }

	/// <summary>
	/// Initializes a new GrainReplicaStrategy.
	/// </summary>
	/// <param name="clusterConfigOptions">Cluster config.</param>
	/// <param name="attribute">Strategy attribute</param>
	protected GrainReplicaStrategy(
		IOptions<ClusterConfig> clusterConfigOptions,
		GrainReplicaStrategyAttribute attribute
	) : this(clusterConfigOptions, attribute.MaxCount, attribute.CapMaxClusterSize, attribute.ClusterSizeRatio)
	{
	}

	/// <summary>
	/// Initializes a new GrainReplicaStrategy.
	/// </summary>
	/// <param name="clusterConfigOptions">Cluster config.</param>
	/// <param name="maxCount">Absolute max count of replicas.</param>
	/// <param name="capMaxClusterSize">Determine whether to cap according to the cluster size.</param>
	/// <param name="clusterSizeRatio">Scale according to the cluster size e.g. 0.5 when replica is 6 = 3.</param>
	protected GrainReplicaStrategy(
		IOptions<ClusterConfig> clusterConfigOptions,
		int maxCount,
		bool capMaxClusterSize,
		double clusterSizeRatio
	)
	{
		var clusterSize = clusterConfigOptions.Value.Size;
		CapMaxClusterSize = capMaxClusterSize;
		MaxCount = ComputeMaxCount(clusterSize, capMaxClusterSize, maxCount, clusterSizeRatio);
	}

	/// <summary>
	/// Calculate responsible replica index.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public abstract int CalculateResponsible(string key);

	public static int ComputeMaxCount(int clusterSize, bool capMaxClusterSize, int maxCount, double clusterSizeRatio)
	{
		var absoluteMax = maxCount == 0 ? int.MaxValue : maxCount;
		var computedMaxCount = maxCount;
		if (capMaxClusterSize)
		{
			absoluteMax = absoluteMax.Clamp(1, clusterSize);
			computedMaxCount = clusterSize;
		}

		if (clusterSizeRatio > 0)
			computedMaxCount = (int)Math.Floor(clusterSize * clusterSizeRatio);

		return computedMaxCount.Clamp(1, absoluteMax);
	}

	public static GrainReplicaStrategy Create<TGrain>(IServiceProvider serviceProvider)
		=> Create(typeof(TGrain), serviceProvider);

	public static GrainReplicaStrategy Create(Type grain, IServiceProvider serviceProvider)
	{
		var attribute = grain.GetAttribute<GrainReplicaStrategyAttribute>(inherit: false);
		var grainName = grain.GetDemystifiedName();

		if (attribute == null)
			throw new ArgumentException($"Replica grain '{grainName}' has no {nameof(GrainReplicaStrategyAttribute)} attributed to it.");

		return (GrainReplicaStrategy)ActivatorUtilities.CreateInstance(serviceProvider, attribute.StrategyType, attribute);
	}
}

public class RandomReplicaStrategy : GrainReplicaStrategy
{
	public RandomReplicaStrategy(
		RandomGrainReplicaStrategyAttribute attribute,
		IOptions<ClusterConfig> clusterConfigOptions
	) : base(clusterConfigOptions, attribute)
	{
	}

	public override int CalculateResponsible(string key)
		=> RandomUtils.GenerateNumber(min: 0, max: MaxCount);
}

public class RoundRobinReplicaStrategy : GrainReplicaStrategy
{
	private readonly Dictionary<string, int> _next = [];

	public RoundRobinReplicaStrategy(
		RoundRobinGrainReplicaStrategyAttribute attribute,
		IOptions<ClusterConfig> clusterConfigOptions
	) : base(clusterConfigOptions, attribute)
	{
	}

	public override int CalculateResponsible(string key)
	{
		if (!_next.TryGetValue(key, out var current))
		{
			_next[key] = 1;
			return 0;
		}
		var responsible = current % MaxCount;
		_next[key] = responsible + 1;
		return responsible;
	}
}
