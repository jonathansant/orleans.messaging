using Odin.Core.App;

namespace Odin.Logging.Serilog.Elasticsearch;

public class ElasticsearchConfigOptions
{
	public bool IsEnabled { get; set; }
	public string Url { get; set; }
	public string IndexPrefix { get; set; }
	public string AppName { get; set; }
	public string UserName { get; set; }
	public string Password { get; set; }
	public string ServiceType { get; set; }
	public string Environment { get; set; }
	public int BatchMessageLimit { get; set; } = 1000;
	public int FileSizeLimitKb { get; set; } = 10;
	public string BufferFilePath { get; set; }
	public int LogShippingIntervalSeconds { get; set; } = 3;
	public int NumberOfShardsPerIndex { get; set; } = 1;

	public ElasticsearchConfigOptions From(IAppInfo appInfo)
	{
		AppName = appInfo.ShortName;
		Environment = appInfo.Environment;
		ServiceType = appInfo.ServiceType;
		return this;
	}
}
