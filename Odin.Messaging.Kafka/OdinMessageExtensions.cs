using Confluent.Kafka;

namespace Odin.Messaging.Kafka;

public static class OdinMessageExtensions
{
	private const string LeaderEpochProp = nameof(ConsumeResult<byte[], byte[]>.LeaderEpoch);
	private const string PartitionProp = nameof(ConsumeResult<byte[], byte[]>.Partition);

	public static int GetLeaderEpoch(this OdinMessage message)
		=> int.Parse(message.QueueIdentity.Metadata.GetValueOrDefault(LeaderEpochProp));

	public static int GetPartition(this OdinMessage message)
		=> int.Parse(message.QueueIdentity.Metadata.GetValueOrDefault(PartitionProp));
}
