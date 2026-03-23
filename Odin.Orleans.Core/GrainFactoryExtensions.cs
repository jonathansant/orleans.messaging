using Odin.Orleans.Core.GrainReplication;

// ReSharper disable once CheckNamespace
namespace Orleans;

public static class GrainFactoryExtensions
{
	public static TGrain GetGrain<TGrain>(this IGrainFactory factory, string key, IGrainReplicaDirector<TGrain> director)
		where TGrain : IGrainWithStringKey
		=> factory.GetGrain<TGrain>(director.GetKey(key));

	public static IGrainReplicaSessionGrain GetReplicaSessionGrain(this IGrainFactory factory, string key)
		=> factory.GetGrain<IGrainReplicaSessionGrain>(key);
}
