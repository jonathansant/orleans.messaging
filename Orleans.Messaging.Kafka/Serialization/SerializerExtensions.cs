using Confluent.SchemaRegistry;
using Orleans.Messaging.Kafka.Config;
using Orleans.Messaging.SerDes;
using System.Text.Json;

namespace Orleans.Messaging.Kafka.Serialization;

public static class AvroSerializerExtensions
{
	public static MessagingTopicConfigBuilder WithAvroSerializer(
		this MessagingTopicConfigBuilder builder,
		string schemaRegistryUrl
	)
	{
		var schemaRegistry = new CachedSchemaRegistryClient(
			new SchemaRegistryConfig
			{
				RequestTimeoutMs = 10000,
				Url = schemaRegistryUrl
			}
		);
		builder.WithSerializer(typeof(AvroSerializer<>), schemaRegistry);

		return builder;
	}
}

public static class JsonSerializerExtensions
{
	public static MessagingTopicConfigBuilder WithJsonSerializer(
		this MessagingTopicConfigBuilder builder,
		Action<JsonSerializerOptions>? configure = null
	)
	{
		var options = new JsonSerializerOptions();
		configure?.Invoke(options);

		builder.WithSerializer(typeof(JsonSerializer<>), options);
		return builder;
	}
}

public static class StringSerializerExtensions
{
	public static MessagingTopicConfigBuilder WithStringSerializer(this MessagingTopicConfigBuilder builder)
	{
		builder.WithSerializer(typeof(StringSerializer<>));
		return builder;
	}
}
