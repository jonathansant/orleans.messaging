using Confluent.Kafka;
using Odin.Messaging.Kafka.Config;
using Odin.Orleans.Core;
using Odin.Orleans.Core.Tenancy;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Odin.Messaging.Kafka.Consuming;

public static partial class GrainFactoryExtensions
{
	public static IAcknowledgerGrain GetAcknowledgerGrain(this IGrainFactory grainFactory, string serviceKey, string topic, string partition)
		=> grainFactory.GetGrain<IAcknowledgerGrain>($"odinKafkaAcknowledgerGrain/{serviceKey}/{topic}/{partition}");
}

public interface IAcknowledgerGrain : IGrainWithStringKey
{
	ValueTask Acknowledge(ImmutableList<AckRequest> ackRequests);
}

[SharedTenant]
public class AcknowledgerGrain : OdinGrain, IAcknowledgerGrain
{
	private readonly ILogger<AcknowledgerGrain> _logger;
	private readonly OdinMessagingKafkaOptions _options;
	private readonly AcknowledgerGrainKey _keyData;
	private IConsumer<byte[], byte[]> _consumer;
	private readonly TopicConfig _topicConfig;

	public AcknowledgerGrain(
		ILogger<AcknowledgerGrain> logger,
		ILoggingContext loggingContext,
		IOptionsMonitor<OdinMessagingKafkaOptions> optionsMonitor
	) : base(logger, loggingContext)
	{
		_logger = logger;
		_keyData = this.ParseKey<AcknowledgerGrainKey>(AcknowledgerGrainKey.Template);
		_options = optionsMonitor.Get(_keyData.ServiceKey);
		_topicConfig = _options.Topics.FindFirst(x => _keyData.TopicId == x.Name);
	}

	public override async Task OnOdinActivate()
	{
		await base.OnOdinActivate();
		AssignToPartition();
	}

	public ValueTask Acknowledge(ImmutableList<AckRequest> ackRequests)
	{
		var offsets = ackRequests
			.OrderBy(x => x.SequenceNo)
			.Select(
				x => new TopicPartitionOffset(
					_keyData.TopicId,
					_topicConfig.IsPartitioned ? int.Parse(_keyData.Partition) : x.GetPartition(),
					new(long.Parse(x.SequenceNo) + 1),
					x.GetLeaderEpoch()
				)
			);

		_consumer.Commit(offsets);

		return ValueTask.CompletedTask;
	}

	private void AssignToPartition()
	{
		var consumerConfig = _options.ToConsumerProperties();
		_consumer = new ConsumerBuilder<byte[], byte[]>(consumerConfig)
				.SetErrorHandler(
					(sender, errorEvent) =>
						_logger.LogError(
							"Acknowledge consumer error reason: {reason}, code: {code}, is broker error: {errorType}",
							errorEvent.Reason,
							errorEvent.Code,
							errorEvent.IsBrokerError
						)
				)
				.Build()
			;
	}
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public struct AcknowledgerGrainKey
{
	private string DebuggerDisplay => $"ServiceKey: '{ServiceKey}', TopicId: '{TopicId}', Partition: '{Partition}'";

	public const string Template = "odinKafkaAcknowledgerGrain/{serviceKey}/{topicId}/{partition}";
	public string TopicId { get; set; }
	public string Partition { get; set; }
	public string ServiceKey { get; set; }
}

[GenerateSerializer]
public record AckRequest
{
	[Id(0)]
	public string SequenceNo { get; init; }
	[Id(1)]
	public Dictionary<string, string> Metadata { get; } = new();

	public AckRequest AddMeta(string key, string value)
	{
		Metadata.Add(key, value);
		return this;
	}
}

public static class AckRequestExtensions
{
	private const string LeaderEpochProp = nameof(ConsumeResult<byte[], byte[]>.LeaderEpoch);
	private const string PartitionProp = nameof(ConsumeResult<byte[], byte[]>.Partition);

	public static AckRequest SetLeaderEpoch(this AckRequest ackRequest, int leaderEpoch)
	{
		ackRequest.AddMeta(LeaderEpochProp, leaderEpoch.ToString());
		return ackRequest;
	}

	public static int GetLeaderEpoch(this AckRequest ackRequest)
		=> int.Parse(ackRequest.Metadata.GetValueOrDefault(LeaderEpochProp));

	public static AckRequest ToAck(this OdinMessage message)
		=> new AckRequest
			{
				SequenceNo = message.QueueIdentity.SequenceKey,
			}
			.SetLeaderEpoch(message.GetLeaderEpoch())
			.SetPartition(message.GetPartition());

	public static AckRequest SetPartition(this AckRequest ackRequest, int partition)
	{
		ackRequest.AddMeta(PartitionProp, partition.ToString());
		return ackRequest;
	}

	public static int GetPartition(this AckRequest ackRequest)
		=> int.Parse(ackRequest.Metadata.GetValueOrDefault(PartitionProp));
}
