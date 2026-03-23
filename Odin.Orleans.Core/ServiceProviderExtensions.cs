using Odin.Orleans.Core.GrainReplication;
using Odin.Orleans.Core.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class OrleansCoreServiceCollectionExtensions
{
	public static IServiceCollection AddOdinOrleansCore(this IServiceCollection services)
	{
		services.AddSingleton(typeof(IGrainReplicaDirector<>), typeof(GrainReplicaDirector<>));
		services.AddScoped<ILoggingContext, OrleansSerilogLoggingContext>();

		return services;
	}
}
