using Destructurama;
using Microsoft.Extensions.Configuration;
using Odin.Core.App;
using Odin.Logging.Serilog;
using Odin.Logging.Serilog.Elasticsearch;
using Serilog.Formatting.Json;

// ReSharper disable once CheckNamespace
namespace Serilog;

public enum LoggingConsoleMode
{
	Cli = 0,
	Server = 1,
	Development = 2,
}

public static class LoggerConfigExtensions
{
	public static LoggerConfiguration AddConsole(this LoggerConfiguration loggerConfig, LoggingConsoleMode mode)
	{
		Debugging.SelfLog.Enable(Console.Error);

		string template;
		switch (mode)
		{
			case LoggingConsoleMode.Cli:
				template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
				break;
			case LoggingConsoleMode.Server:
				loggerConfig.WriteTo.Async(writeTo => writeTo.Console(new JsonFormatter(renderMessage: true)), blockWhenFull: false);
				return loggerConfig;
			case LoggingConsoleMode.Development:
				template = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}";
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
		}
		loggerConfig.WriteTo.Async(writeTo => writeTo.Console(outputTemplate: template), blockWhenFull: false);
		return loggerConfig;
	}

	/// <summary>
	/// Configures logging which requires minimal configuration, which should be used early stages during bootup.
	/// </summary>
	public static LoggerConfiguration ConfigureMinimal(
		this LoggerConfiguration loggerConfig,
		LoggingConsoleMode mode = LoggingConsoleMode.Server
	)
		=> loggerConfig
			.WithGlobalEnrich()
			.AddConsole(mode);

	public static LoggerConfiguration WithGlobalEnrich(this LoggerConfiguration loggerConfig)
	{
		loggerConfig
			.Enrich.FromLogContext()
			.Enrich.WithMachineName()
			.Enrich.WithProperty(LogPropertyNames.AppInstanceId, Guid.NewGuid())
			;
		return loggerConfig;
	}

	/// <summary>
	/// Full logger configuration for Odin projects (calls other extensions).
	/// </summary>
	/// <param name="loggerConfig"></param>
	/// <param name="config"></param>
	/// <param name="appInfo"></param>
	/// <param name="mode">Determines which mode to use for console format style.</param>
	public static LoggerConfiguration Configure(
		this LoggerConfiguration loggerConfig,
		IConfiguration config,
		IAppInfo appInfo,
		LoggingConsoleMode mode = LoggingConsoleMode.Server
	)
	{
		var elasticSearchOptions = config.GetSection("Elasticsearch").Get<ElasticsearchConfigOptions>()
		                           ?? new ElasticsearchConfigOptions { IsEnabled = false };

		loggerConfig.WithGlobalEnrich()
			.ReadFrom.Configuration(config)
			.WithAppInfoContext(appInfo)
			.Destructure.UsingAttributes();

		if (appInfo.IsDevelopment)
		{
			loggerConfig.UseDevelopment();
		}
		else
			loggerConfig.AddConsole(mode);

		if (elasticSearchOptions.IsEnabled)
			loggerConfig.AddElasticsearch(elasticSearchOptions.From(appInfo));

		return loggerConfig;
	}

	public static LoggerConfiguration WithAppInfoContext(this LoggerConfiguration loggerConfig, IAppInfo appInfo)
	{
		loggerConfig.Enrich.WithProperty(LogPropertyNames.App, appInfo.ShortName)
			.Enrich.WithProperty(LogPropertyNames.ServiceType, appInfo.ServiceType)
			.Enrich.WithProperty(LogPropertyNames.Environment, appInfo.Environment)
			.Enrich.WithProperty(LogPropertyNames.AppVersion, appInfo.Version)
			.Enrich.WithProperty(LogPropertyNames.ClusterId, appInfo.ClusterId)
			.Enrich.WithProperty(LogPropertyNames.GitCommit, appInfo.GitCommit)
			.Enrich.WithProperty(LogPropertyNames.ClusterGroup, appInfo.ClusterGroup)
			.Enrich.WithProperty(LogPropertyNames.InfraClusterId, appInfo.InfraClusterId)
			;

		return loggerConfig;
	}

	public static LoggerConfiguration UseDevelopment(
		this LoggerConfiguration loggerConfig,
		LoggingConsoleMode mode = LoggingConsoleMode.Development
	)
	{
		loggerConfig.AddConsole(mode);
		return loggerConfig;
	}

	public static LoggerConfiguration WithStringPropertyObfuscation(
		this LoggerConfiguration config,
		params PropertyPatterns[] properties
	) => config.Enrich.With(new StringObfuscationEnricher(properties));
}
