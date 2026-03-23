using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Odin.Messaging.Kafka.Config;
using Odin.Orleans.Core;
using Odin.Orleans.Core.Tenancy;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Odin.Messaging.Kafka;

public static partial class GrainFactoryExtensions
{
	public static ITopicGrain GetTopicGrain(this IGrainFactory grainFactory, string serviceKey)
		=> grainFactory.GetGrain<ITopicGrain>(TopicKey.Create(serviceKey));
}

public record struct TopicKey
{
	public static readonly string Template = "odinMessagingTopics/{serviceKey}";

	public static string Create(string serviceKey)
		=> Template.FromTemplate(new Dictionary<string, object> { ["serviceKey"] = serviceKey });

	public string ServiceKey { get; set; }
}

public interface ITopicGrain : IOdinGrainContract, IGrainWithStringKey
{
	ValueTask<ImmutableList<TopicMetadata>> GetBrokerTopics();
}

[SharedTenant]
public class TopicGrain : OdinGrain, ITopicGrain
{
	private readonly TopicKey _keyData;
	private readonly OdinMessagingKafkaOptions _options;
	private readonly IServiceProvider _serviceProvider;
	private readonly IPersistentState<TopicGrainState> _store;

	public TopicGrain(
		ILogger<TopicGrain> logger,
		ILoggingContext loggingContext,
		IPersistentStateFactory persistentStateFactory,
		IGrainContext grainContext,
		IOptionsMonitor<OdinMessagingKafkaOptions> optionsMonitor,
		IServiceProvider serviceProvider
	) : base(logger, loggingContext)
	{
		_keyData = this.ParseKey<TopicKey>(TopicKey.Template);
		_options = optionsMonitor.Get(_keyData.ServiceKey);
		_serviceProvider = serviceProvider;

		_store = persistentStateFactory.Create<TopicGrainState>(
			grainContext,
			new PersistentStateAttribute("topic", _options.StoreName)
		);
	}

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();
		await Initialize();
	}

	public ValueTask<ImmutableList<TopicMetadata>> GetBrokerTopics()
		=> ValueTask.FromResult(_store.State.BrokerMetadata);

	private async Task Initialize()
	{
		var adminConfig = new AdminClientBuilder(_options.ToAdminProperties());
		using var admin = adminConfig.Build();

		_store.State.BrokerMetadata = GetBrokerMeta(admin);
		await _store.WriteStateAsync();

		await CreateTopics(admin);
		await CreateDeadLetterTopics(admin);
	}

	private async ValueTask CreateTopics(IAdminClient admin)
	{
		foreach (var topic in _options.Topics)
		{
			var topicMeta = _store.State.BrokerMetadata.FindSingle(kt => kt.Topic == topic.Name);
			if (topic.AutoCreate && topicMeta == null)
				await CreateTopic(admin, topic);
		}

		_store.State.BrokerMetadata = GetBrokerMeta(admin);
		await _store.WriteStateAsync();
	}

	private ImmutableList<TopicMetadata> GetBrokerMeta(IAdminClient admin)
		=> admin.GetMetadata(_options.AdminRequestTimeout)
			.Topics
			.Select(x => x.FromConfluent())
			.ToImmutableList();

	private async Task CreateDeadLetterTopics(IAdminClient admin)
	{
		var deadLetterTopics = _options.Topics
			.Where(x => x.ProcessingFailedHandlingMode is ProcessingFailedHandlingMode.Dlq
			            && x.Type is not TopicType.Producer
			            && _store.State.BrokerMetadata.All(t => t.Topic != x.DeadLetterQueueName)
			)
			.Select(x => x.DeadLetterQueueName)
			.ToList();

		await deadLetterTopics.ForEachAsync(topic => CreateDeadLetterTopic(_store.State.BrokerMetadata, admin, topic));
		_store.State.BrokerMetadata = GetBrokerMeta(admin);

		await _store.WriteStateAsync();
	}

	private async Task CreateDeadLetterTopic(ImmutableList<TopicMetadata> meta, IAdminClient admin, string topic)
	{
		var dlqTopicMeta = meta.FindSingle(kt => kt.Topic == topic);

		var topicBuilder = new MessagingTopicConfigBuilder(_serviceProvider, topic, serviceKey: _keyData.ServiceKey);

		//todo: config
		topicBuilder
			.WithTopicType(TopicType.Producer)
			.WithContract<byte[]>()
			.WithCreationOptions(
				new()
				{
					Partitions = 5,
					ReplicationFactor = 1,
					AutoCreate = true
				}
			);

		if (dlqTopicMeta == null)
			await CreateTopic(admin, topicBuilder.TopicConfig);
	}

	private static Task CreateTopic(IAdminClient client, TopicConfig topic)
	{
		var topicSpecification = new TopicSpecification
		{
			Name = topic.Name,
			NumPartitions = topic.Partitions,
			ReplicationFactor = topic.ReplicationFactor
		};

		if (topic.RetentionPeriodInMs.HasValue)
		{
			topicSpecification.Configs = new()
			{
				{
					"retention.ms",
					topic.RetentionPeriodInMs.ToString()
				}
			};
		}

		return client.CreateTopicsAsync(topicSpecification.ToSingleList());
	}
}

[GenerateSerializer]
public record TopicGrainState
{
	[Id(0)]
	public ImmutableList<TopicMetadata> BrokerMetadata { get; set; }
}

[GenerateSerializer]
public record TopicMetadata
{
	[Id(0)]
	public string Topic { get; set; }

	[Id(1)]
	public List<PartitionMetadata> Partitions { get; set; }
}

[GenerateSerializer]
public record PartitionMetadata
{
	[Id(0)]
	public int PartitionId { get; set; }

	[Id(1)]
	public int Leader { get; set; }

	[Id(2)]
	public int[] Replicas { get; set; }

	[Id(3)]
	public int[] InSyncReplicas { get; set; }
}

public static class TopicMetadataExtensions
{
	public static TopicMetadata FromConfluent(this Confluent.Kafka.TopicMetadata topicMeta)
		=> new()
		{
			Partitions = topicMeta.Partitions.Select(x => x.FromConfluent()).ToList(),
			Topic = topicMeta.Topic
		};

	public static PartitionMetadata FromConfluent(this Confluent.Kafka.PartitionMetadata partitionMeta)
		=> new()
		{
			Leader = partitionMeta.Leader,
			Replicas = partitionMeta.Replicas,
			PartitionId = partitionMeta.PartitionId,
			InSyncReplicas = partitionMeta.InSyncReplicas
		};
}