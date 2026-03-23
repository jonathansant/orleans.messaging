namespace Odin.Messaging.Config;

public interface IOdinMessagingOptions
{
}

public record OdinMessagingOptions : IOdinMessagingOptions
{
	public virtual TimeSpan DesiredProducerTimeout { get; set; } = TimeSpan.FromMilliseconds(175);
	public string StoreName { get; set; } = "default-store";
	public bool IsProduceEnabled { get; set; } = true;
	public ProducerRetryOptions ProducerRetryOptions { get; set; } = new();
	public bool EnsureHandlerDeliveryOnFailure { get; set; } = false;
}

public record struct ProducerRetryOptions(
	int MaxRetries = 2
)
{
	public TimeSpan RetryDelay { get; init; }
}

public interface IQueueDirectory
{
	List<string> GetAllQueues();
}
