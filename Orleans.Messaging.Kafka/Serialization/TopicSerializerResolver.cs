using Orleans.Messaging.SerDes;

namespace Orleans.Messaging.Kafka.Serialization;

public interface ITopicSerializerResolver
{
	IMessageSerializer Resolve(string topicName);
	IMessageSerializer<TMessage> Resolve<TMessage>(string topicName);
	void Register(string topicName, IMessageSerializer serializer);
}

// todo: replace with keyed services
internal sealed class TopicSerializerResolver : ITopicSerializerResolver
{
	private readonly Dictionary<string, IMessageSerializer> _serializers = new();

	public IMessageSerializer Resolve(string topicName)
	{
		if (_serializers.TryGetValue(topicName, out var serializer))
			return serializer;

		throw new ArgumentException($"No serializer found for topic {topicName}", nameof(topicName));
	}

	public IMessageSerializer<TMessage> Resolve<TMessage>(string topicName)
		=> (IMessageSerializer<TMessage>)Resolve(topicName);

	public void Register(string topicName, IMessageSerializer serializer)
		=> _serializers.Add(topicName, serializer);
}
