using Microsoft.Extensions.DependencyInjection;

namespace Odin.Core.Config;

public interface IConfigBuilderFactory
{
	IConfigBuilder Get(string key);

	IConfigBuilder Get<TConfigBuilder>(string key)
		where TConfigBuilder : IConfigBuilder;
}

public class ConfigBuilderFactory : IConfigBuilderFactory
{
	private readonly IServiceProvider _serviceProvider;
	private readonly Dictionary<string, IConfigBuilder> _builders = new Dictionary<string, IConfigBuilder>();

	public ConfigBuilderFactory(
		IServiceProvider serviceProvider
	)
	{
		_serviceProvider = serviceProvider;
	}

	public IConfigBuilder Get(string key)
		=> Get<ConfigBuilder>(key);

	public IConfigBuilder Get<TConfigBuilder>(string key)
		where TConfigBuilder : IConfigBuilder
	{
		if (_builders.TryGetValue(key, out var builder))
			return builder;

		builder = ActivatorUtilities.CreateInstance<TConfigBuilder>(_serviceProvider);
		_builders[key] = builder;
		return builder;
	}
}
