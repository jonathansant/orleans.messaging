using Confluent.Kafka;

namespace Orleans.Messaging.Kafka;

public static class MessageExtensions
{
	private const string LeaderEpochProp = nameof(ConsumeResult<byte[], byte[]>.LeaderEpoch);
	private const string PartitionProp = nameof(ConsumeResult<byte[], byte[]>.Partition);

	public static int GetLeaderEpoch(this Message message)
		=> int.Parse(message.QueueIdentity.Metadata.GetValueOrDefault(LeaderEpochProp));

	public static int GetPartition(this Message message)
		=> int.Parse(message.QueueIdentity.Metadata.GetValueOrDefault(PartitionProp));
}
