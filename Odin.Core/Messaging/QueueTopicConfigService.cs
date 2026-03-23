using Odin.Core.App;
using Odin.Core.FlowControl;
using System.Text.RegularExpressions;

namespace Odin.Core;

public interface IQueueConfigService
{
	string BrokerKey { get; }
	string GetTopicName(string key);
	TopicSettings Get(string key);
	TopicSettings? GetOrDefault(string key);
	List<TopicSettings> GetAll();
	List<(TopicSettings TopicSettings, QueueMetaConfig QueueConfig)> GetAllWithQueueConfigs();
	List<TopicSettings> GetAllEnabled();
	List<(TopicSettings TopicSettings, QueueMetaConfig QueueConfig)> GetAllEnabledWithQueueConfig();
	string GetTemplate(QueueMetaConfig queue);
}

[GenerateSerializer]
public class TopicSettings
{
	[Id(0)]
	public bool IsEnabled { get; set; }

	[Id(1)]
	public bool AutoCreate { get; set; }

	[Id(2)]
	public string TopicName { get; set; }

	[Id(3)]
	public string Pattern { get; set; }

	[Id(4)]
	public string AvroUrl { get; set; }

	[Id(5)]
	public TimeSpan? PollRate { get; set; }

	[Id(6)]
	public int? BatchSize { get; set; }

	[Id(7)]
	public string ContractAlias { get; set; }

	// [Id(8)]
	// public bool IsPublisher { get; set; }

	[Id(15)]
	public QueueType QueueType { get; set; }

	[Id(9)]
	public HashSet<string> SplitByHeaders { get; set; }

	[Id(10)]
	public bool? IsPartitioned { get; set; }

	[Id(11)]
	public int Partitions { get; set; }

	[Id(12)]
	public ScheduledThrottledActionOptions? DelayOptions { get; set; }

	[Id(13)]
	public SerializerType? SerializerType { get; set; }

	[Id(14)]
	public JsonSerializerConfiguration? JsonSerializerSettings { get; set; }
}

public class QueueConfigService : IQueueConfigService
{
	private readonly MessageBrokerConfig _config;
	private readonly IAppInfo _appInfo;
	private readonly Dictionary<string, (TopicSettings TopicSettings, QueueMetaConfig QueueConfig)?> _topicsWithQueueConfigs = new();

	public string BrokerKey => field;

	public QueueConfigService(
		MessageBrokerConfig config,
		IAppInfo appInfo,
		string brokerKey
	)
	{
		_config = config;
		_appInfo = appInfo;
		BrokerKey = brokerKey;

		Initialize();
	}

	private void Initialize()
	{
		if (_config.Type == MessageBrokerType.Memory || _config.Queues == null || _config.Queues.Items.IsNullOrEmpty())
			return;

		foreach (var queue in _config.Queues.Items)
		{
			var topicSettings = new TopicSettings
			{
				IsEnabled = queue.Value.IsEnabled,
				AutoCreate = queue.Value.AutoCreate,
				QueueType = queue.Value.QueueType,
				Pattern = queue.Value.Pattern,
				TopicName = Build(queue.Value),
				AvroUrl = queue.Value.AvroUrl.IsNullOrEmpty() ? _config.Kafka.Schema?.Url : queue.Value.AvroUrl,
				ContractAlias = queue.Value.ContractAlias,
				SplitByHeaders = queue.Value.SplitByHeaders,
				PollRate = queue.Value.PollRate.HasValue ? TimeSpan.FromMilliseconds(queue.Value.PollRate.Value) : null,
				BatchSize = queue.Value.BatchSize,
				IsPartitioned = queue.Value.IsPartitioned,
				Partitions = queue.Value.Partitions,
				DelayOptions = queue.Value.DelayOptions,
				SerializerType = queue.Value.SerializerType,
				JsonSerializerSettings = queue.Value.JsonSerializerSettings
			};

			_topicsWithQueueConfigs.Add(
				queue.Key,
				(topicSettings, queue.Value)
			);
		}
	}

	private string Build(QueueMetaConfig queue)
	{
		var template = GetTemplate(queue);

		var templateParams = new Dictionary<string, object>
		{
			{ "serviceName", queue.ServiceName },
			{ "name", queue.TopicName },
			{ "version", queue.Version }
		};

		if (_appInfo is not null)
			templateParams.Add("env", _appInfo.Environment);

		return template.FromTemplate(templateParams)
			.ToLowerInvariant();
	}

	public string GetTemplate(QueueMetaConfig queue)
	{
		if (queue.QueueType is QueueType.Publisher or QueueType.InOut && !_config.Queues.PublisherTemplate.IsNullOrEmpty())
			return _config.Queues.PublisherTemplate;

		return queue.Template ?? _config.Queues.Template;
	}

	public string GetTopicName(string key)
		=> _topicsWithQueueConfigs.GetValueOrDefault(key)?.TopicSettings?.TopicName;

	public TopicSettings? GetOrDefault(string key)
		=> _topicsWithQueueConfigs.GetValueOrDefault(key)?.TopicSettings;

	public TopicSettings Get(string key)
	{
		var topicSettings = GetOrDefault(key);
		ArgumentNullException.ThrowIfNull(topicSettings, nameof(topicSettings));

		return topicSettings;
	}

	public List<TopicSettings> GetAll()
		=> _topicsWithQueueConfigs.Values.Select(x => x!.Value.TopicSettings).ToList();

	public List<(TopicSettings TopicSettings, QueueMetaConfig QueueConfig)> GetAllWithQueueConfigs()
		=> _topicsWithQueueConfigs.Values.Select(x => x!.Value).ToList();

	public List<TopicSettings> GetAllEnabled()
		=> _topicsWithQueueConfigs.Values.Where(x => x!.Value.TopicSettings.IsEnabled).Select(x => x!.Value.TopicSettings).ToList();

	public List<(TopicSettings TopicSettings, QueueMetaConfig QueueConfig)> GetAllEnabledWithQueueConfig()
		=> _topicsWithQueueConfigs.Values.Where(x => x!.Value.TopicSettings.IsEnabled).Select(x => x!.Value).ToList();
}

public static class TopicSettingsExtensions
{
	public static string GetPattern(this TopicSettings settings, Dictionary<string, object> dictionary)
		=> settings.Pattern.FromTemplate(dictionary)
			.ToLowerInvariant();
}