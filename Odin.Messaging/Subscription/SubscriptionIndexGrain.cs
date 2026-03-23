using Odin.Core.FlowControl;
using Odin.Messaging.Accessors;
using Odin.Messaging.Config;
using Odin.Orleans.Core;
using Odin.Orleans.Core.Tenancy;
using Orleans.Concurrency;

namespace Odin.Messaging.Subscription;

public static partial class GrainFactoryExtensions
{
	public static ISubscriptionIndexGrain GetSubscriptionIndexGrain(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName
	) => grainFactory.GetGrain<ISubscriptionIndexGrain>($"odinKafkaSubscriptionIndex/{serviceKey}/{queueName}");
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

[SharedTenant]
public class SubscriptionIndexGrain : OdinGrain, ISubscriptionIndexGrain
{
	private readonly IConsumerAccessor _consumerAccessor;
	private readonly IPersistentState<SubscriptionIndexGrainState> _store;

	private readonly ScheduledThrottledAction _writeThrottledAction;
	private readonly OdinMessagingOptions _options;
	private readonly SubscriptionIndexGrainKey _key;

	public SubscriptionIndexGrain(
		ILogger<SubscriptionIndexGrain> logger,
		ILoggingContext loggingContext,
		IPersistentStateFactory persistentStateFactory,
		IGrainContext grainContext,
		IServiceProvider serviceProvider
	) : base(logger, loggingContext)
	{
		_key = this.ParseKey<SubscriptionIndexGrainKey>(SubscriptionIndexGrainKey.Template);

		_consumerAccessor = serviceProvider.GetRequiredKeyedService<IConsumerAccessor>(_key.ServiceKey);
		var runtimeService = serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(_key.ServiceKey);

		_options = runtimeService.GetOptions();

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

	public override async Task OnOdinDeactivate()
	{
		await base.OnOdinDeactivate();
		await _writeThrottledAction.DisposeAsync();
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
					Logger.LogDebug(
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
	public const string Template = "odinKafkaSubscriptionIndex/{ServiceKey}/{QueueName}";
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
