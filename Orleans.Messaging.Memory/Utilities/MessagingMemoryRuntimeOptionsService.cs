using System.Collections.Concurrent;
using Orleans.Messaging.Memory.Config;
using Orleans.Messaging.Memory.Producing;
using Orleans.Messaging.Subscription;

namespace Orleans.Messaging.Memory.Utilities;

public interface IMessagingMemoryRuntimeOptionsService : IMessagingRuntimeOptionsService
{
	ValueTask RegisterQueueType(string queue, Type contractType);
}

public class MessagingMemoryRuntimeOptionsService(
	IServiceProvider serviceProvider,
	string serviceKey
) : MessagingRuntimeOptionsService(serviceKey, serviceProvider), IMessagingMemoryRuntimeOptionsService
{
	private readonly Type _producerGrainType = typeof(IMemoryProducerGrain<>);
	private readonly ConcurrentDictionary<string, Type> _producerGrainTypes = new();
	private readonly ConcurrentDictionary<string, Type> _queueTypes = new();
	private readonly Type _subscriptionGrainType = typeof(ISubscriptionGrain<>);
	private readonly ConcurrentDictionary<string, Type> _subscriptionGrainTypes = new();
	private IGrainFactory _grainFactory = serviceProvider.GetRequiredService<IGrainFactory>();

	public override Type OptionsType { get; } = typeof(MessagingMemoryOptions);

	public override async ValueTask<Type> GetSubscriptionGrainType(string queue)
	{
		if (_queueTypes.TryGetValue(queue, out var typeArguments))
			return _subscriptionGrainTypes.GetOrAdd(queue, k => _subscriptionGrainType.MakeGenericType(typeArguments));

		typeArguments = await GetQueueTypeFromCache(queue);

		return _subscriptionGrainTypes.GetOrAdd(queue, k => _subscriptionGrainType.MakeGenericType(typeArguments));
	}

	public override async ValueTask<Type> GetProducerGrainType(string queue)
	{
		if (_queueTypes.TryGetValue(queue, out var typeArguments))
			return _producerGrainTypes.GetOrAdd(queue, k => _producerGrainType.MakeGenericType(typeArguments));

		typeArguments = await GetQueueTypeFromCache(queue);

		return _producerGrainTypes.GetOrAdd(queue, k => _producerGrainType.MakeGenericType(typeArguments));
	}

	// diglett
	public async ValueTask RegisterQueueType(string queue, Type contractType)
	{
		var isAdded = _queueTypes.TryAdd(queue, contractType);
		if (isAdded)
			await _grainFactory
				.GetQueueTypeGrain(serviceKey, queue)
				.SetType(contractType.AssemblyQualifiedName!);
	}

	private async ValueTask<Type?> GetQueueTypeFromCache(string queue)
	{
		var assemblyName = await _grainFactory.GetQueueTypeGrain(serviceKey, queue).GetTypeName();

		if (assemblyName.IsNullOrEmpty())
			throw new InvalidOperationException($"Queue type for queue {queue} has not been registered.");

		var typeArguments = Type.GetType(assemblyName);
		_queueTypes.TryAdd(queue, typeArguments);

		return typeArguments;
	}
}

public static partial class GrainFactoryExtensions
{
	public static IQueueTypeGrain GetQueueTypeGrain(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName
	) => grainFactory.GetGrain<IQueueTypeGrain>($"orleansMessagingMemoryQueueType/{serviceKey}/{queueName}");
}

public interface IQueueTypeGrain : IGrainWithStringKey
{
	ValueTask SetType(string assemblyQualifiedName);
	ValueTask<string> GetTypeName();
}

public class QueueTypeGrain : Grain, IQueueTypeGrain
{
	private readonly IPersistentState<QueueTypeGrainState> _store;

	public QueueTypeGrain(
		IPersistentStateFactory persistentStateFactory,
		IGrainContext grainContext,
		IOptionsMonitor<MessagingMemoryOptions> optionsMonitor
	) : base(grainContext)
	{
		var key = this.ParseKey<QueueTypeGrainKey>(QueueTypeGrainKey.Template);
		var options = optionsMonitor.Get(key.ServiceKey);

		_store = persistentStateFactory.Create<QueueTypeGrainState>(
			grainContext,
			new PersistentStateAttribute("queueType", options.StoreName)
		);
	}

	public async ValueTask SetType(string assemblyQualifiedName)
	{
		if (_store.State.AssemblyQualifiedName == assemblyQualifiedName)
			return;

		_store.State.AssemblyQualifiedName = assemblyQualifiedName;
		await _store.WriteStateAsync();
	}

	public ValueTask<string> GetTypeName() => ValueTask.FromResult(_store.State.AssemblyQualifiedName)!;
}

public struct QueueTypeGrainKey
{
	public const string Template = "orleansMessagingMemoryQueueType/{ServiceKey}/{QueueName}";
	public string ServiceKey { get; set; }
	public string QueueName { get; set; }
}

[GenerateSerializer]
public record QueueTypeGrainState
{
	[Id(0)]
	public string? AssemblyQualifiedName { get; set; }
}
