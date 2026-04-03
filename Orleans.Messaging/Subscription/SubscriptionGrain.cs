using System.Collections.Immutable;
using System.Web;
using Orleans.Concurrency;
using Orleans.Messaging.Config;
using Orleans.Messaging.FlowControl;
using Orleans.Messaging.Utils;

namespace Orleans.Messaging.Subscription;

public static partial class GrainFactoryExtensions
{
	public static ISubscriptionGrain GetSubscriptionGrain<TMessage>(
		this IGrainFactory grainFactory,
		string serviceKey,
		string queueName,
		string key
	) => grainFactory.GetGrain<ISubscriptionGrain<TMessage>>(GenerateSubscriptionGrainKey(serviceKey, queueName, key));

	public static string GenerateSubscriptionGrainKey(string serviceKey, string queueName, string pattern)
		=> $"orleansMessagingSubscription/{serviceKey}/{queueName}/{HttpUtility.UrlEncode(pattern)}";
}

public interface ISubscriptionGrain : IGrainWithStringKey
{
	[OneWay]
	Task PushOneWay(ImmutableList<Message> batch);

	Task Push(ImmutableList<Message> batch);

	[AlwaysInterleave]
	Task<string> Subscribe(Immutable<SubscriptionMeta> messageHandler);

	[AlwaysInterleave]
	[OneWay]
	Task SubscribeOneWay(Immutable<SubscriptionMeta> messageHandler);

	[AlwaysInterleave]
	Task Unsubscribe(Immutable<string> subscriptionId);
}

public interface ISubscriptionGrain<TMessage> : ISubscriptionGrain { }

public class SubscriptionGrain<TMessage> : Grain, ISubscriptionGrain<TMessage>
{
	private readonly SubscriptionGrainKey _key;
	private readonly ILogger<SubscriptionGrain<TMessage>> _logger;
	private readonly MessagingOptions _options;
	private readonly IPersistentState<SubscriptionGrainState> _store;
	private readonly Dictionary<string, MethodInfo> _subscriptionMethods = new();

	private readonly ScheduledThrottledAction _writeThrottledAction;
	private string? _primaryKey;

	public SubscriptionGrain(
		ILogger<SubscriptionGrain<TMessage>> logger,
		IPersistentStateFactory persistentStateFactory,
		IGrainContext grainContext,
		IServiceProvider serviceProvider
	) : base(grainContext)
	{
		_logger = logger;

		_key = this.ParseKey<SubscriptionGrainKey>(SubscriptionGrainKey.Template);
		_key.Pattern = HttpUtility.UrlDecode(_key.Pattern);

		var runtimeOptionsService = serviceProvider.GetRequiredKeyedService<IMessagingRuntimeOptionsService>(_key.ServiceKey);
		_options = runtimeOptionsService.GetOptions();

		_store = persistentStateFactory.Create<SubscriptionGrainState>(
			grainContext,
			new PersistentStateAttribute("subscription", _options.StoreName)
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

	private string PrimaryKey => _primaryKey ??= this.GetPrimaryKeyAny();

	public async Task PushOneWay(ImmutableList<Message> batch)
		=> await Push(batch);

	public async Task Push(ImmutableList<Message> batch)
	{
		var messageIds = batch.Select(x => x.MessageId).ToList();
		var joinedIds = string.Join(',', messageIds);

		_logger.ReceivedMessageBatch(_key.Pattern, _key.QueueName, joinedIds);

		var messageBatch = batch.Cast<Message<TMessage>>().ToImmutableList();
		var isFaulted = false;
		List<MessageFailedMeta> messageFailedMetas = [];

		await _store.State.Subscriptions.ForEachAsync(async subscription =>
			{
				var messagesToPush = _options.EnsureHandlerDeliveryOnFailure
						? messageBatch
							.Where(x => !_store.State.FailedMessageIdsWithSubsMetas.ContainsKey(x.MessageId)
										|| (_store.State.FailedMessageIdsWithSubsMetas.TryGetValue(x.MessageId, out var metas)
											&& metas.Contains(subscription.Value.ToSubscriptionIdString()))
							)
							.ToImmutableList()
						: messageBatch
					;

				var messageHandler = GetMessageHandler(subscription);

				try
				{
					var (_, grain) = CreateGrainReference(subscription.Value);
					await (Task)messageHandler.Invoke(grain, [messagesToPush])!;

					if (_logger.IsEnabled(LogLevel.Debug))
						messageBatch.ForEach(message => _logger.SuccessfullyPushedMessage(
								$"{subscription.Value.GrainType.GetDemystifiedName()}-{subscription.Value.PrimaryKey}",
								_key.Pattern,
								_key.QueueName,
								message.MessageId,
								message
							)
						);

					foreach (var messageId in messageIds)
					{
						var faultedSubList = _store.State.FailedMessageIdsWithSubsMetas.TryGetValue(messageId, out var subscriptions);
						if (faultedSubList && subscriptions.Contains(subscription.ToString()))
							subscriptions.Remove(messageId);

						if (subscriptions.IsNullOrEmpty())
							_store.State.FailedMessageIdsWithSubsMetas.Remove(messageId);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(
						ex,
						"Subscriber failed to process message batch from subscription: {subscriptionGrain} with key {subscriptionKey} on queue name {queueName} messageIds: {messageIds}",
						$"{subscription.Value.GrainType.GetDemystifiedName()}-{subscription.Value.PrimaryKey}",
						_key.Pattern,
						_key.QueueName,
						joinedIds
					);

					if (_options.EnsureHandlerDeliveryOnFailure)
						messageIds.ForEach(m
							=> _store.State.FailedMessageIdsWithSubsMetas.GetOrAdd(m, _ => [])
								.Add(subscription.Value.ToSubscriptionIdString())
						);

					isFaulted = true;
					messageFailedMetas.Add(
						new()
						{
							Exception = ex,
							MessageIds = _store.State.FailedMessageIdsWithSubsMetas.Keys.ToList(),
							SubscriberKey = subscription.Value.ToSubscriptionIdString()
						}
					);
				}
			}
		);

		if (isFaulted) // todo: send specific message which failed
			throw new SubscriptionMessageProcessingException(
				"Failed to push message batch to subscriber!",
				messageFailedMetas
			);

		await _store.WriteStateAsync();
	}

	public async Task<string> Subscribe(Immutable<SubscriptionMeta> subscriptionMeta)
	{
		var key = GenerateKey(subscriptionMeta.Value);
		var isNewSubscription = _store.State.Subscriptions.TryAdd(key, subscriptionMeta.Value);
		await _writeThrottledAction.Trigger();

		if (isNewSubscription)
			await GrainFactory.GetSubscriptionIndexGrain(_key.ServiceKey, _key.QueueName)
				.Register(
					_key.Pattern,
					subscriptionMeta.Value.PatternOptions.AsImmutable()
				);
		else
		{
			var patternOpts = _store.State.Subscriptions[key].PatternOptions;

			// todo: this needs to be improved by doing it by subscription instead of by pattern
			if (patternOpts.PatternType != subscriptionMeta.Value.PatternOptions.PatternType)
				throw new InvalidOperationException(
					$"Cannot change pattern type for subscription {key} on queue {_key.QueueName} from {patternOpts.PatternType} to {subscriptionMeta.Value.PatternOptions.PatternType}"
				);
		}

		return key;
	}

	public async Task SubscribeOneWay(Immutable<SubscriptionMeta> subscriptionMeta)
		=> await Subscribe(subscriptionMeta);

	public async Task Unsubscribe(Immutable<string> subscriptionId)
	{
		_store.State.Subscriptions.Remove(subscriptionId.Value);
		await _writeThrottledAction.Trigger();

		if (_store.State.Subscriptions.IsNullOrEmpty())
			await GrainFactory.GetSubscriptionIndexGrain(_key.ServiceKey, _key.QueueName).Unregister(_key.Pattern);
	}

	public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
	{
		await _writeThrottledAction.DisposeAsync();
		await base.OnDeactivateAsync(reason, cancellationToken);
	}

	private (string key, IGrain grain) CreateGrainReference(SubscriptionMeta subscriptionMeta)
	{
		var key = GenerateKey(subscriptionMeta);

		var grain = GrainFactory.GetGrain(
			subscriptionMeta.GrainType,
			subscriptionMeta.PrimaryKey
		);

		return (key, grain);
	}

	private string GenerateKey(SubscriptionMeta subscriptionMeta)
		=> $"{PrimaryKey}|{subscriptionMeta.GrainType.GetDemystifiedName()}/{subscriptionMeta.PrimaryKey}";

	private MethodInfo GetMessageHandler(KeyValuePair<string, SubscriptionMeta> subscription)
	{
		var type = subscription.Value.GrainType;
		var key = $"{type.GetDemystifiedName()}_{subscription.Value.MethodName}";
		var handleMessage = _subscriptionMethods.GetOrAdd(
			key,
			_ => type.GetInterfaceMethod(subscription.Value.MethodName)
		);

		if (handleMessage == null)
			throw new InvalidOperationException(
				$"Could not find method {subscription.Value.MethodName} on type {type.GetDemystifiedName()}"
			);

		return handleMessage;
	}
}

[GenerateSerializer]
public record SubscriptionGrainState
{
	[Id(0)]
	public Dictionary<string, SubscriptionMeta> Subscriptions { get; set; } = new();

	[Id(1)]
	public Dictionary<string, HashSet<string>> FailedMessageIdsWithSubsMetas { get; set; } = new();
}

[GenerateSerializer]
public record SubscriptionMeta
{
	[Id(0)]
	public Type GrainType { get; set; }

	[Id(1)]
	public string PrimaryKey { get; set; }

	[Id(2)]
	public string MethodName { get; set; }

	[Id(3)]
	public PatternOptions PatternOptions { get; set; }

	public string ToSubscriptionIdString()
		=> $"{GrainType.GetDemystifiedName()}-{PrimaryKey}.{MethodName}";
}

public struct SubscriptionGrainKey
{
	public static string Template { get; } = "orleansMessagingSubscription/{ServiceKey}/{QueueName}/{Pattern}";

	public string ServiceKey { get; set; }
	public string QueueName { get; set; }
	public string Pattern { get; set; }
}

internal static partial class LogExtensions
{
	[LoggerMessage(
		Level = LogLevel.Debug,
		Message = "Received message batch for subscription {subscriptionKey} on queue name {queueName} messageIds {messageIds}"
	)]
	internal static partial void ReceivedMessageBatch(this ILogger logger, string subscriptionKey, string queueName, string messageIds);

	[LoggerMessage(
		Level = LogLevel.Debug,
		Message =
			"Subscriber successfully processed message from subscription: {subscriptionGrain} with key {subscriptionKey} on queue name {queueName} messageId: {messageId} message {message}"
	)]
	internal static partial void SuccessfullyPushedMessage(
		this ILogger logger,
		string subscriptionGrain,
		string subscriptionKey,
		string queueName,
		string messageId,
		object message
	);
}
