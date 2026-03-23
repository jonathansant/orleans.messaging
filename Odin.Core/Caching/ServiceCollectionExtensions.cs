using Microsoft.Extensions.DependencyInjection;

namespace Odin.Core.Caching;

public static class CacheServiceCollectionExtensions
{
	public static void AddOdinCaching(this IServiceCollection services)
	{
		services.AddMemoryCache();
		services.AddSingleton<ICachingService, CachingService>();
	}
}
