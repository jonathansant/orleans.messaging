using System.Text;
using System.Text.Json;

namespace Orleans.Messaging.SerDes;

public class JsonSerializer<TMessage>(
	JsonSerializerOptions? options = null
) : IMessageSerializer<TMessage>
{
	public void Dispose()
	{
	}

	public ValueTask<TMessage> Deserialize(string queueName, byte[] data)
		=> ValueTask.FromResult(JsonSerializer.Deserialize<TMessage>(Encoding.UTF8.GetString(data), options))!;

	public ValueTask<byte[]> Serialize(string queueName, TMessage data)
	{
		var str = JsonSerializer.Serialize(data, options);
		return ValueTask.FromResult(Encoding.UTF8.GetBytes(str));
	}

	async ValueTask<object?> IMessageSerializer.Deserialize(string queueName, byte[] data)
		=> await Deserialize(queueName, data);
};
