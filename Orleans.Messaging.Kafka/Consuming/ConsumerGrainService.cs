using Orleans.Messaging.FlowControl;
using Orleans.Messaging.Kafka.Config;
using Orleans.Runtime.Services;
using Orleans.Services;
using System.Collections.Immutable;
using System.IO.Hashing;
using System.Text;

namespace Orleans.Messaging.Kafka.Consuming;

public interface IConsumerGrainServiceClient
{
	Task InitializeConsumersOnAllSilos();
}

public class ConsumerGrainServiceClient(
	IServiceProvider serviceProvider
) : GrainServiceClient<IConsumerGrainService>(serviceProvider), IConsumerGrainServiceClient
{
	public Task InitializeConsumersOnAllSilos()
	{
		var membership = serviceProvider.GetRequiredService<IClusterMembershipService>();
		var activeSilos = membership.CurrentSnapshot.Members
				.Where(x => x.Value.Status == SiloStatus.Active)
				.Select(x => x.Value.SiloAddress)
			;

		return activeSilos.ForEachAsync(async silo => await GetGrainService(silo).InitializeConsumers());
	}
}

public interface IConsumerGrainService : IGrainService
{
	Task InitializeConsumers();
};

public record KafkaInstances(
	List<string> ServiceKeys
);

public class ConsumerGrainService : GrainService, IConsumerGrainService
{
	private const string Unpartitioned = "unpartitioned";

	private readonly Silo _silo;
	private readonly IGrainFactory _grainFactory;
	private readonly ILogger<ConsumerGrainService> _logger;
	private readonly Random _random = new();

	private readonly List<string> _serviceKeys;
	private readonly Dictionary<string, bool> _shouldRebalanceByServiceKey;
	private readonly Dictionary<string, MessagingTimer> _rebalanceConsumerTimerByServiceKey;
	private readonly Dictionary<string, Dictionary<string, List<int>>> _consumerTopicPartitionsByServiceKey;
	private readonly Dictionary<string, MessagingKafkaOptions> _optionsByServiceKey;
	private readonly Dictionary<string, ImmutableList<TopicMetadata>> _metaByServiceKey;
	private readonly Dictionary<string, List<MessagingTimer>> _pollTimersByServiceKey;

	private readonly List<string> _emptyLogString = "empty".ToSingleList();

	public ConsumerGrainService(
		GrainId id,
		Silo silo,
		IGrainFactory grainFactory,
		ILogger<ConsumerGrainService> logger,
		ILoggerFactory loggerFactory,
		IOptionsMonitor<MessagingKafkaOptions> optionsMonitor,
		KafkaInstances? kafkaInstances = null
	) : base(id, silo, loggerFactory)
	{
		_silo = silo;
		_grainFactory = grainFactory;
		_logger = logger;

		_serviceKeys = kafkaInstances?.ServiceKeys ?? [MessageBrokerNames.Default];

		_shouldRebalanceByServiceKey = new(_serviceKeys.Count);
		_rebalanceConsumerTimerByServiceKey = new(_serviceKeys.Count);
		_consumerTopicPartitionsByServiceKey = new(_serviceKeys.Count);
		_optionsByServiceKey = new(_serviceKeys.Count);
		_metaByServiceKey = new(_serviceKeys.Count);

		_pollTimersByServiceKey = new(_serviceKeys.Count);

		foreach (var serviceKey in _serviceKeys)
		{
			_shouldRebalanceByServiceKey[serviceKey] = false;
			_optionsByServiceKey[serviceKey] = optionsMonitor.Get(serviceKey);
			_pollTimersByServiceKey[serviceKey] = [];
		}
	}

	public override async Task Start()
	{
		await base.Start();
		_logger.LogInformation(">> Starting consumer service on {silo}", _silo.SiloAddress.ToString());
	}

	public override async Task Stop()
	{
		_logger.LogInformation(">> Stopping consumer service on {silo}", _silo.SiloAddress.ToString());

		foreach (var serviceKey in _serviceKeys)
		{
			if (!_optionsByServiceKey[serviceKey].IsConsumeEnabled)
				continue;

			if (!_pollTimersByServiceKey[serviceKey].IsNullOrEmpty())
				await _pollTimersByServiceKey[serviceKey].ForEachAsync(async x => await x.DisposeAsync());

			await (_rebalanceConsumerTimerByServiceKey[serviceKey]?.DisposeAsync() ?? ValueTask.CompletedTask);
		}

		await base.Stop();

		_logger.LogInformation("<< Stopped consumer service on {silo}", _silo.SiloAddress.ToString());
	}

	public async Task InitializeConsumers()
	{
		foreach (var serviceKey in _serviceKeys)
		{
			var options = _optionsByServiceKey[serviceKey];
			if (!options.IsConsumeEnabled)
			{
				_logger
					.LogInformation(
						"<< Started consumer service for {serviceKey} on {silo} but set as disabled",
						serviceKey,
						_silo.SiloAddress.ToString()
					);
				continue;
			}

			_metaByServiceKey[serviceKey] = await _grainFactory.GetTopicGrain(serviceKey).GetBrokerTopics();
			_rebalanceConsumerTimerByServiceKey[serviceKey] = new(
				async _ => await Rebalance(serviceKey),
				new(TimeSpan.FromMinutes(5))
			);

			_rebalanceConsumerTimerByServiceKey[serviceKey].StartTimer();

			var metas = await _metaByServiceKey.GetOrAddAsync(
				serviceKey,
				async key => await _grainFactory.GetTopicGrain(key).GetBrokerTopics()
			);

			await options
				.Topics
				.Where(x => x.Type is not TopicType.Producer)
				.Join(metas, topicConfig => topicConfig.Name, brokerTopic => brokerTopic.Topic, (topic, meta) => (topic, meta: meta))
				.ForEachAsync(config =>
					{
						if (config.topic.IsPartitioned)
							return config.meta.Partitions
								.Select(x => _grainFactory.GetConsumerGrain(serviceKey, config.topic.Name, x.PartitionId.ToString()))
								.ForEachAsync(async consumerGrain => await consumerGrain.Initialize());

						return _grainFactory.GetConsumerGrain(serviceKey, config.topic.Name, Unpartitioned).Initialize();
					}
				);
		}

		_logger.LogInformation("<< Started consumer service on {silo}", _silo.SiloAddress.ToString());
	}

	private TimeSpan CalculatePollRate(string topic, string serviceKey)
	{
		var options = _optionsByServiceKey[serviceKey];

		var configuredPollRate = options.Topics.FindFirst(x => x.Name == topic).PollRate?.TotalMilliseconds
		                         ?? options.PollRate.TotalMilliseconds
			;

		return TimeSpan.FromMilliseconds(
			// random poll rate between 90% and 150% of the configured poll rate
			configuredPollRate * ((double)_random.Next(9, 15) / 10)
		);
	}

	private async ValueTask Rebalance(string serviceKey)
	{
		Dictionary<string, List<int>> consumerTopicPartitions = null;
		var siloAddress = _silo.SiloAddress.ToString();
		try
		{
			consumerTopicPartitions = GetConsumerGroupings(serviceKey);
			_consumerTopicPartitionsByServiceKey[serviceKey] = consumerTopicPartitions;

			if (consumerTopicPartitions.IsNullOrEmpty())
			{
				_logger.NoTopicsConfigured(serviceKey, siloAddress);
				return;
			}

			_logger.RebalancingTopics(
				silo: siloAddress,
				serviceKey: serviceKey,
				topics: string.Join(',', consumerTopicPartitions.Select(x => x.Key))
			);

			if (!_pollTimersByServiceKey[serviceKey].IsNullOrEmpty())
				await _pollTimersByServiceKey[serviceKey].ForEachAsync(async x => await x.DisposeAsync());

			_pollTimersByServiceKey[serviceKey] = consumerTopicPartitions.Select(topicPartitions =>
					{
						var pollRate = CalculatePollRate(topicPartitions.Key, serviceKey);

						var pollTimer = new MessagingTimer(
								async _ => await TriggerConsume(topicPartitions.Key, topicPartitions.Value, serviceKey),
								new(pollRate, InitialDelay: pollRate)
							).OnError(ex =>
								{
									_logger.LogError(
										ex,
										"Error on timer triggering consumer for serviceKey {serviceKey} topic {topic} on partition {partition}",
										serviceKey,
										topicPartitions.Key,
										topicPartitions.Value
									);
									return Task.CompletedTask;
								}
							)
							;

						return pollTimer;
					}
				)
				.ToList();

			_pollTimersByServiceKey[serviceKey].Each(timer => timer.StartTimer());

			if (!_shouldRebalanceByServiceKey[serviceKey])
			{
				_shouldRebalanceByServiceKey[serviceKey] = true;
				return;
			}

			await RebalanceConsumers(serviceKey);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Error rebalancing topics on silo {silo} responsible for serviceKey {serviceKey} topics {topics}",
				siloAddress,
				serviceKey,
				string.Join(',', consumerTopicPartitions?.Select(x => x.Key) ?? _emptyLogString)
			);
		}
	}

	private async Task RebalanceConsumers(string serviceKey)
		=> await _consumerTopicPartitionsByServiceKey[serviceKey]
			.ForEachAsync(consumer => consumer.Value.ForEachAsync(async partition =>
					{
						if (_optionsByServiceKey[serviceKey].Topics.FindFirst(x => x.Name == consumer.Key).IsPartitioned)
							await _grainFactory.GetConsumerGrain(serviceKey, consumer.Key, partition.ToString()).Rebalance();
						else
							await _grainFactory.GetConsumerGrain(serviceKey, consumer.Key, Unpartitioned).Rebalance();
					}
				)
			);

	private async Task TriggerConsume(string topic, List<int> partitions, string serviceKey)
	{
		try
		{
			_logger.TriggeringConsumer(serviceKey: serviceKey, topic: topic, silo: _silo.SiloAddress.ToString());

			if (_optionsByServiceKey[serviceKey].Topics.FindFirst(x => x.Name == topic).IsPartitioned)
				await partitions.ForEachAsync(async partition =>
					{
						var consumerGrain = _grainFactory.GetConsumerGrain(serviceKey, topic, partition.ToString());
						await consumerGrain.ConsumeAndPublish();
					}
				);
			else
			{
				var consumerGrain = _grainFactory.GetConsumerGrain(serviceKey, topic, Unpartitioned);
				await consumerGrain.ConsumeAndPublish();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(
				ex,
				"Error triggering consumer on silo {silo} responsible for serviceKey {serviceKey} topic {topic}",
				_silo.SiloAddress.ToString(),
				serviceKey,
				topic
			);
		}
	}

	private Dictionary<string, List<int>> GetConsumerGroupings(string serviceKey)
	{
		var serviceMeta = _metaByServiceKey[serviceKey];
		var topicConfigs = _optionsByServiceKey[serviceKey]
				.Topics.Where(x => x.Type is not TopicType.Producer)
				.Where(x => RingRange.InRange(BitConverter.ToUInt32(XxHash64.Hash(Encoding.UTF8.GetBytes(x.Name)))))
				.ToList()
			;

		var consumerGroupings = topicConfigs
			.Join(
				serviceMeta,
				topicConfig => topicConfig.Name,
				brokerTopic => brokerTopic.Topic,
				(topicConfig, meta) => (topicConfig, meta)
			)
			.ToDictionary(
				configMetaTuple => configMetaTuple.topicConfig.Name,
				configMetaTuple => !configMetaTuple.topicConfig.IsPartitioned
					? (-1).ToSingleList()
					: configMetaTuple.meta.Partitions.Select(x => x.PartitionId).ToList()
			);

		if (consumerGroupings.Count != topicConfigs.Count)
		{
			_logger.LogWarning(
				"Not all topics are assigned to a consumer group for serviceKey {serviceKey}. " + "Topics: {topics}",
				serviceKey,
				string.Join(',', new HashSet<string>(topicConfigs.Select(x => x.Name)).Except(consumerGroupings.Keys))
			);
		}

		return consumerGroupings;
	}
}

internal static partial class LogExtensions
{
	[LoggerMessage(
		Level = LogLevel.Information,
		Message = "Rebalancing topics on silo {silo} for serviceKey {serviceKey} topics {topics}"
	)]
	internal static partial void RebalancingTopics(this ILogger logger, string silo, string serviceKey, string topics);

	[LoggerMessage(
		Level = LogLevel.Debug,
		Message = "Triggering consumer for serviceKey {serviceKey} topic {topic} on silo {silo}"
	)]
	internal static partial void TriggeringConsumer(this ILogger logger, string serviceKey, string topic, string silo);

	[LoggerMessage(
		Level = LogLevel.Warning,
		Message = "Empty topic for serviceKey {serviceKey} on silo {silo}"
	)]
	internal static partial void NoTopicsConfigured(this ILogger logger, string serviceKey, string silo);
}