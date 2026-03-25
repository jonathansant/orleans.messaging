namespace Orleans.Messaging.SerDes;

public interface IMessageSerializer
{
	ValueTask<object> Deserialize(string queueName, byte[] data);
}

public interface IMessageSerializer<TMessage> : IDisposable, IMessageSerializer
{
	new ValueTask<TMessage> Deserialize(string queueName, byte[] data);
	ValueTask<byte[]> Serialize(string queueName, TMessage data);
}
