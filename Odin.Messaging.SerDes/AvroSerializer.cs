using Chr.Avro.Confluent;
using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace Odin.Messaging.SerDes;

public sealed class AvroSerializer<TMessage>(
	ISchemaRegistryClient schemaRegistry
) : IOdinMessageSerializer<TMessage>
{
	private readonly AsyncSchemaRegistryDeserializer<TMessage> _deserializer = new(schemaRegistry);
	private readonly AsyncSchemaRegistrySerializer<TMessage> _serializer = new(schemaRegistry);

	public async ValueTask<TMessage> Deserialize(string queueName, byte[] data)
		=> await _deserializer.DeserializeAsync(
			new(data),
			false,
			new(MessageComponentType.Value, queueName)
		);

	public async ValueTask<byte[]> Serialize(string queueName, TMessage data)
		=> await _serializer.SerializeAsync(
			data,
			new(MessageComponentType.Value, queueName)
		);

	public void Dispose()
		=> schemaRegistry.Dispose();

	async ValueTask<object> IOdinMessageSerializer.Deserialize(string queueName, byte[] data)
		=> await Deserialize(queueName, data);
}
