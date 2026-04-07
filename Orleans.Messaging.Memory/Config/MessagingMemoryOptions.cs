using Orleans.Messaging.Config;

namespace Orleans.Messaging.Memory.Config;

public interface IMessagingMemoryOptions : IMessagingOptions
{
	uint MaxPartitionCount { get; set; }
}

public record MessagingMemoryOptions : MessagingOptions, IMessagingMemoryOptions
{
	public int ProduceInitDelayMs { get; set; } = 500;
	public int ProducePollRateMs { get; set; } = 50;

	public override TimeSpan DesiredProducerTimeout { get; set; } = TimeSpan.FromSeconds(1);
	public uint MaxPartitionCount { get; set; } = (uint)Environment.ProcessorCount;
}

public record MessagingMemoryClientOptions : MessaginClientOptions, IMessagingMemoryOptions
{
	public uint MaxPartitionCount { get; set; } = (uint)Environment.ProcessorCount;
}
