namespace Orleans.Messaging.Config;

public interface IMessagingOptions
{
	bool IsProduceEnabled { get; }
}

public record MessagingOptions : IMessagingOptions
{
	public virtual TimeSpan DesiredProducerTimeout { get; set; } = TimeSpan.FromMilliseconds(175);
	public string StoreName { get; set; } = "default-store";
	public ProducerRetryOptions ProducerRetryOptions { get; set; } = new();
	public bool EnsureHandlerDeliveryOnFailure { get; set; } = false;
	public bool IsProduceEnabled { get; set; } = true;
}

public record struct ProducerRetryOptions(
	int MaxRetries = 2
)
{
	public TimeSpan RetryDelay { get; init; }
}

public abstract record MessaginClientOptions : IMessagingOptions
{
	public bool IsProduceEnabled { get; set; } = true;
}

public interface IQueueDirectory
{
	List<string> GetAllQueues();
}
