using Odin.Messaging.Config;

namespace Odin.Messaging.Memory.Config;

public record OdinMessagingMemoryOptions : OdinMessagingOptions
{
	public uint MaxPartitionCount { get; init; } = (uint)Environment.ProcessorCount;
	public int ProduceInitDelayMs { get; init; } = 500;
	public int ProducePollRateMs { get; init; } = 50;

	public override TimeSpan DesiredProducerTimeout { get; set; } = TimeSpan.FromSeconds(1);
}
