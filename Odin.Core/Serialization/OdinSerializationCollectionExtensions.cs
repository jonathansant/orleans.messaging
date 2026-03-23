using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Odin.Core.Serialization;

public static class OdinSerializationCollectionExtensions
{
	public static IServiceCollection AddOdinSerialization(this IServiceCollection services)
	{
		services
			.AddKeyedSingleton<IOdinFileSerializer, OdinFileCsvSerializer>(OdinSerializerKeys.Csv)
			;

		return services;
	}
}
