using Microsoft.Extensions.Configuration;

namespace Odin.Core.Config;

public class DynamicSection
{
	private readonly IConfiguration _configuration;
	private readonly IDictionary<string, object> _cache;

	public DynamicSection(IConfiguration configuration)
	{
		_configuration = configuration;
		_cache = new Dictionary<string, object>();
	}

	public DynamicSection Get(string key)
	{
		var section = _configuration.GetSection(key);
		return new DynamicSection(section);
	}

	public T? Get<T>(string key)
	{
		if (_cache.TryGetValue(key, out var val))
			return (T)val;

		var config = _configuration.GetSection(key).Get<T>();
		if (config == null)
			return default;

		_cache[key] = config;
		return config;
	}
}
