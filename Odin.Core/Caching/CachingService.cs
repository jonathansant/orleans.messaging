using Microsoft.Extensions.Caching.Memory;

namespace Odin.Core.Caching;

public class CachingService : ICachingService
{
	private readonly IMemoryCache _memoryCache;

	public CachingService(IMemoryCache memoryCache)
	{
		_memoryCache = memoryCache;
	}

	public Task<T> GetOrSet<T>(string key, Func<Task<T>> task)
		=> _memoryCache.GetOrCreateAsync(key, entry => task())!;

	public Task Delete(string key)
	{
		_memoryCache.Remove(key);
		return Task.CompletedTask;
	}
}
