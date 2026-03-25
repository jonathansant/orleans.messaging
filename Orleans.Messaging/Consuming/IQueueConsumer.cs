namespace Orleans.Messaging.Consuming;

public interface IQueueConsumer<TMessage>
{
	ValueTask Add(TMessage message);
	ValueTask<TMessage?> Consume();
}
