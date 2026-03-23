namespace Odin.Messaging.SerDes;

public interface IOdinMessageSerializer
{
	ValueTask<object> Deserialize(string queueName, byte[] data);
}

public interface IOdinMessageSerializer<TMessage> : IDisposable, IOdinMessageSerializer
{
	new ValueTask<TMessage> Deserialize(string queueName, byte[] data);
	ValueTask<byte[]> Serialize(string queueName, TMessage data);
}
