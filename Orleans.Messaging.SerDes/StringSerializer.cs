using System.Text;

namespace Orleans.Messaging.SerDes;

public class StringSerializer<TMessage> : IMessageSerializer<string>
{
	public ValueTask<string> Deserialize(string queueName, byte[] data)
		=> ValueTask.FromResult(Encoding.UTF8.GetString(data));

	public ValueTask<byte[]> Serialize(string queueName, string data)
		=> ValueTask.FromResult(Encoding.UTF8.GetBytes(data));

	public void Dispose() { }

	async ValueTask<object> IMessageSerializer.Deserialize(string queueName, byte[] data)
		=> await Deserialize(queueName, data);
}
