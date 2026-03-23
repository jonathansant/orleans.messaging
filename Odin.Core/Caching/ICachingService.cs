namespace Odin.Core.Caching;

public interface ICachingService
{
	Task<T> GetOrSet<T>(string key, Func<Task<T>> task);
	Task Delete(string key);
}
