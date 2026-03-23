using Odin.Core.Http;
using Odin.Logging.Serilog.Elasticsearch;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.Elasticsearch;
using System.Collections.Specialized;
using System.Text;

// ReSharper disable once CheckNamespace
namespace Serilog;

public static class LoggerConfigElasticsearchExtensions
{
	public static LoggerConfiguration AddElasticsearch(
		this LoggerConfiguration loggerConfig,
		ElasticsearchConfigOptions options
	)
	{
		var appName = options.AppName.Replace('/', '.');

		loggerConfig.WriteTo.Elasticsearch(
			new ElasticsearchSinkOptions(new Uri(options.Url))
			{
				AutoRegisterTemplate = true,
				NumberOfShards = options.NumberOfShardsPerIndex,
				CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage: true),
				IndexFormat = $"{options.Environment}-{appName}-{options.ServiceType}-{{0:yyy.MM.dd}}",
				InlineFields = true,
				BatchPostingLimit = options.BatchMessageLimit,
				BufferFileSizeLimitBytes = options.FileSizeLimitKb * 1024,
				BufferLogShippingInterval = TimeSpan.FromSeconds(options.LogShippingIntervalSeconds),
				BufferBaseFilename = options.BufferFilePath,
				ModifyConnectionSettings = connection => connection.GlobalHeaders(new NameValueCollection
				{
					{ HttpHeaders.Authorization, $"Basic {CreateAuthToken(options.UserName, options.Password)}" }
				})
			});

		return loggerConfig;
	}

	private static string CreateAuthToken(string userName, string password)
		=> Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
}
