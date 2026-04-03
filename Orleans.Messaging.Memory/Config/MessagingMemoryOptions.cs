using Orleans.Messaging.Config;

namespace Orleans.Messaging.Memory.Config;

public record MessagingMemoryOptions : MessagingOptions
{
	public uint MaxPartitionCount { get; set; } = (uint)Environment.ProcessorCount;
	public int ProduceInitDelayMs { get; set; } = 500;
	public int ProducePollRateMs { get; set; } = 50;

	public override TimeSpan DesiredProducerTimeout { get; set; } = TimeSpan.FromSeconds(1);
}
