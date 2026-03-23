using Odin.Messaging.SerDes;

namespace Odin.Messaging.Kafka.Serialization;

public interface ITopicSerializerResolver
{
	IOdinMessageSerializer Resolve(string topicName);
	IOdinMessageSerializer<TMessage> Resolve<TMessage>(string topicName);
	void Register(string topicName, IOdinMessageSerializer serializer);
}

// todo: replace with keyed services
internal sealed class TopicSerializerResolver : ITopicSerializerResolver
{
	private readonly Dictionary<string, IOdinMessageSerializer> _serializers = new();

	public IOdinMessageSerializer Resolve(string topicName)
	{
		if (_serializers.TryGetValue(topicName, out var serializer))
			return serializer;

		throw new ArgumentException($"No serializer found for topic {topicName}", nameof(topicName));
	}

	public IOdinMessageSerializer<TMessage> Resolve<TMessage>(string topicName)
		=> (IOdinMessageSerializer<TMessage>)Resolve(topicName);

	public void Register(string topicName, IOdinMessageSerializer serializer)
		=> _serializers.Add(topicName, serializer);
}
