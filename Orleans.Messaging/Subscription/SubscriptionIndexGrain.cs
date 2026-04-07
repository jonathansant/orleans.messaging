using Orleans.Messaging.Accessors;
using Orleans.Messaging.Config;
using Orleans.Messaging.FlowControl;
using Orleans.Messaging.Utils;
using Orleans.Concurrency;

namespace Orleans.Messaging.Subscription;

public static partial class GrainFactoryExtensions
{
	public static ISubscriptionIndexGrain GetSubscriptionIndexGrain(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName
	) => grainFactory.GetGrain<ISubscriptionIndexGrain>($"orleansKafkaSubscriptionIndex/{serviceKey}/{queueName}");
}

public interface ISubscriptionIndexGrain : IGrainWithStringKey
{
	[AlwaysInterleave]
	ValueTask Register(string pattern, Immutable<PatternOptions> options = default);

	[AlwaysInterleave]
	ValueTask<Dictionary<string, PatternOptions>> Get();

	[AlwaysInterleave]
	Task<Dictionary<string, PatternOptions>> RegisterConsumer(string queue, string partition);

	[AlwaysInterleave]
	Task Unregister(string key);
}

public class SubscriptionIndexGrain : Grain, ISubscriptionIndexGrain
{
	private readonly ILogger<SubscriptionIndexGrain> _logger;
	private readonly IConsumerAccessor _consumerAccessor;
	private readonly IPersistentState<SubscriptionIndexGrainState> _store;

	private readonly ScheduledThrottledAction _writeThrottledAction;
	private readonly MessagingOptions _options;
	private readonly SubscriptionIndexGrainKey _key;

	public SubscriptionIndexGrain(
		ILogger<SubscriptionIndexGrain> logger,
		IPersistentStateFactory persistentStateFactory,
		IGrainContext grainContext,
		IServiceProvider serviceProvider
	)
	{
		_logger = logger;
		_key = this.ParseKey<SubscriptionIndexGrainKey>(SubscriptionIndexGrainKey.Template);

		_consumerAccessor = serviceProvider.GetRequiredKeyedService<IConsumerAccessor>(_key.ServiceKey);
		_options = (MessagingOptions)serviceProvider.GetRequiredKeyedService<IMessagingOptionsService>(_key.ServiceKey).GetOptions();

		_store = persistentStateFactory.Create<SubscriptionIndexGrainState>(
			grainContext,
			new PersistentStateAttribute("subscriptionIndex", _options.StoreName)
		);

		_writeThrottledAction = this.CreateScheduledThrottledAction(
			_ => _store.WriteStateAsync(),
			opts =>
			{
				opts.ThrottleTime = TimeSpan.FromSeconds(5);
				opts.FlushScheduledOnDispose = true;
				opts.HasLeadingDelay = true;
			}
		);
	}

	public async ValueTask Register(string pattern, Immutable<PatternOptions> patternOptions = default)
	{
		if (!_store.State.Subscriptions.TryAdd(pattern, patternOptions.Value))
			return;

		await _writeThrottledAction.Trigger();
		await RefreshConsumerSubscriptionList();
	}

	public ValueTask<Dictionary<string, PatternOptions>> Get()
		=> ValueTask.FromResult(_store.State.Subscriptions);

	public async Task<Dictionary<string, PatternOptions>> RegisterConsumer(string queue, string partition)
	{
		_store.State.Consumers.Add((_key.ServiceKey, queue, partition));
		await _writeThrottledAction.Trigger();
		return _store.State.Subscriptions;
	}

	public async Task Unregister(string key)
	{
		if (!_store.State.Subscriptions.Remove(key))
			return;

		await _writeThrottledAction.Trigger();
		await RefreshConsumerSubscriptionList();
	}

	public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
	{
		await _writeThrottledAction.DisposeAsync();
		await base.OnDeactivateAsync(reason, cancellationToken);
	}

	private Task RefreshConsumerSubscriptionList()
		=> _store.State.Consumers.ForEachAsync(async x =>
			{
				try
				{
					await _consumerAccessor.RefreshSubscriptionList(x.queue, x.partition, _store.State.Subscriptions);
				}
				catch (OrleansMessageRejectionException ex)
				{
					_logger.LogDebug(
						"Failed to refresh consumer subscription list for consumer {ServiceKey} {Queue} {Partition}, Message - {message}",
						x.serviceKey,
						x.queue,
						x.partition,
						ex.Message
					);
				}
			}
		);
}

public struct SubscriptionIndexGrainKey
{
	public const string Template = "orleansKafkaSubscriptionIndex/{ServiceKey}/{QueueName}";
	public string ServiceKey { get; set; }
	public string QueueName { get; set; }
}

[GenerateSerializer]
public class SubscriptionIndexGrainState
{
	[Id(0)]
	public Dictionary<string, PatternOptions> Subscriptions { get; set; } = new();

	[Id(1)]
	public HashSet<(string serviceKey, string queue, string partition)> Consumers { get; set; } = new();
}
