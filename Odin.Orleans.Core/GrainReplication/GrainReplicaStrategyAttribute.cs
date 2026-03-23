namespace Odin.Orleans.Core.GrainReplication;

[AttributeUsage(AttributeTargets.Interface)]
public abstract class GrainReplicaStrategyAttribute : Attribute
{
	public Type StrategyType { get; }
	public int MaxCount { get; }
	public bool CapMaxClusterSize { get; }
	public double ClusterSizeRatio { get; }

	public GrainReplicaStrategyAttribute(Type strategyType, int maxCount, bool capMaxClusterSize, double clusterSizeRatio)
	{
		StrategyType = strategyType;
		MaxCount = maxCount;
		CapMaxClusterSize = capMaxClusterSize;
		ClusterSizeRatio = clusterSizeRatio;
	}
}

public sealed class RoundRobinGrainReplicaStrategyAttribute : GrainReplicaStrategyAttribute
{
	/// <summary>
	/// Round robin replica strategy.
	/// </summary>
	/// <param name="maxCount">Define the absolute max count.</param>
	/// <param name="capMaxClusterSize">Determine whether replicas should never exceed the cluster size.</param>
	/// <param name="clusterSizeRatio">Scale according to the cluster size e.g. 0.5 when replica is 6 = 3.</param>
	public RoundRobinGrainReplicaStrategyAttribute(int maxCount = 0, bool capMaxClusterSize = true, double clusterSizeRatio = 1)
		: base(typeof(RoundRobinReplicaStrategy), maxCount, capMaxClusterSize, clusterSizeRatio)
	{
	}
}

public sealed class RandomGrainReplicaStrategyAttribute : GrainReplicaStrategyAttribute
{
	/// <summary>
	/// Random replica strategy.
	/// </summary>
	/// <param name="maxCount">Define the absolute max count.</param>
	/// <param name="capMaxClusterSize">Determine whether replicas should never exceed the cluster size.</param>
	/// <param name="clusterSizeRatio">Scale according to the cluster size e.g. 0.5 when replica is 6 = 3.</param>
	public RandomGrainReplicaStrategyAttribute(int maxCount = 0, bool capMaxClusterSize = true, double clusterSizeRatio = 1)
		: base(typeof(RandomReplicaStrategy), maxCount, capMaxClusterSize, clusterSizeRatio)
	{
	}
}
