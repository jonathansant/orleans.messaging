using Newtonsoft.Json.Linq;
using Odin.Core;
using Odin.Core.App;
using Odin.Core.Config;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration;

public static class ConfigBuilderExtensions
{
	/// <summary>
	/// Adds the memory configuration provider to <paramref name="configBuilder"/>.
	/// </summary>
	/// <param name="configBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddJsonInMemoryCollection(this IConfigurationBuilder configBuilder)
	{
		if (configBuilder == null)
			throw new ArgumentNullException(nameof(configBuilder));

		configBuilder.Add(new InMemoryJsonConfigurationSource());
		return configBuilder;
	}

	/// <summary>
	/// Adds the memory configuration provider to <paramref name="configBuilder"/>.
	/// </summary>
	/// <param name="configBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
	/// <param name="initialData">The data to add to memory configuration provider.</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddJsonInMemoryCollection(this IConfigurationBuilder configBuilder, JObject initialData)
	{
		if (configBuilder == null)
			throw new ArgumentNullException(nameof(configBuilder));

		configBuilder.Add(new InMemoryJsonConfigurationSource { InitialData = initialData });
		return configBuilder;
	}

	public static IConfigurationBuilder AddMultipleJsonFiles(
		this IConfigurationBuilder configBuilder,
		string[] fileNames,
		string envShortName,
		bool optionalEnvironments = true,
		bool optional = false
	)
	{
		foreach (var fileName in fileNames)
		{
			configBuilder.AddJsonFile($"{fileName}.json", optional);

			if (!envShortName.IsNullOrEmpty())
				configBuilder.AddJsonFile($"{fileName}.{envShortName}.json", optionalEnvironments);
		}

		return configBuilder;
	}

	public static IConfigurationBuilder AddOdinConfigs(
		this IConfigurationBuilder configBuilder,
		string env,
		string? infraClusterId = null,
		string[]? args = null
	)
	{
		var envShortName = AppInfo.MapEnvironmentOrDefault(env);

		configBuilder.AddMultipleJsonFiles(new[] { "appsettings" }, envShortName);

		if (!infraClusterId.IsNullOrEmpty())
			configBuilder.AddMultipleJsonFiles(new[] { $"appsettings.cluster.{infraClusterId}" }, envShortName, optional: true);

		configBuilder.AddEnvironmentVariables();

		if (args != null)
			configBuilder.AddCommandLine(args);

		return configBuilder;
	}
}
