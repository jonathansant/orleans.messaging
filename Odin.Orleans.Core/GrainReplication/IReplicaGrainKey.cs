namespace Odin.Orleans.Core.GrainReplication;

public interface IReplicaGrainKey
{
	string ReplicaIndex { get; set; }

	string ToReplicaAgnosticKey();
}
