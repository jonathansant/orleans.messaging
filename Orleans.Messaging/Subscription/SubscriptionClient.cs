using Orleans.Messaging.FlowControl;
using Orleans.Messaging.Utils;
using Orleans.Concurrency;
using System.Collections.Immutable;

namespace Orleans.Messaging.Subscription;

public interface ISubscriptionClient
{
	Task<string> Subscribe<TMessage>(MessageSubscriptionInput<TMessage> input);
	Task Unsubscribe<TMessage>(string serviceKey, string queueName, string subscriptionPattern, string subscriptionId);
}

public class SubscriptionClient(
	IGrainFactory grainFactory
	// todo: this should accept a service key
) : ISubscriptionClient
{
	public async Task<string> Subscribe<TMessage>(MessageSubscriptionInput<TMessage> input)
	{
		if (input.GrainType.GetInterfaceMethod(input.MethodName) is null)
			throw new InvalidOperationException("Cannot subscribe with anonymous methods or non-grain methods.");

		var grain = grainFactory.GetSubscriptionGrain<TMessage>(input.ServiceKey, input.QueueName, input.KeyPattern);
		return await grain.Subscribe(
			new SubscriptionMeta
			{
				PrimaryKey = input.GrainPrimaryKey,
				GrainType = input.GrainType,
				MethodName = input.MethodName,
				PatternOptions = input.PatternOptions
			}.AsImmutable()
		);
	}

	public Task Unsubscribe<TMessage>(string serviceKey, string queueName, string subscriptionPattern, string subscriptionId)
	{
		var grain = grainFactory.GetSubscriptionGrain<TMessage>(serviceKey, queueName, subscriptionPattern);
		return grain.Unsubscribe(subscriptionId.AsImmutable());
	}
}

public class SubscriptionBuilder<TMessage>
{
	private Type _grainType;
	private string _primaryKey;
	private string _queueName;
	private string _subscriptionKey;
	private Delegate _method;
	private readonly Type _grainInterfaceType = typeof(IGrain);
	private static readonly Type DelegateType = typeof(Func<ImmutableList<Message<TMessage>>, Task>);
	private static readonly Type DelegateParamType = DelegateType.GenericTypeArguments[0];
	private static readonly Type DelegateReturnType = DelegateType.GenericTypeArguments[1];
	private string? _methodStr;
	private PatternOptions _patternOptions;

	public SubscriptionBuilder<TMessage> WithGrainType<TGrain>()
		where TGrain : IGrain
		=> WithGrainType(typeof(TGrain));

	public SubscriptionBuilder<TMessage> WithGrainType(Type grainType)
	{
		if (!grainType.IsInterface)
			throw new InvalidOperationException("Grain type must be the grain interface used to activate the grain.");

		_grainType = grainType;
		return this;
	}

	public SubscriptionBuilder<TMessage> WithGrainType(string grainType)
	{
		var type = Type.GetType(grainType);
		return WithGrainType(type);
	}

	public SubscriptionBuilder<TMessage> WithPrimaryKey(string primaryKey)
	{
		_primaryKey = primaryKey;
		return this;
	}

	public SubscriptionBuilder<TMessage> WithGrainAction(Func<ImmutableList<Message<TMessage>>, Task> method)
	{
		_method = method;
		return this;
	}

	public SubscriptionBuilder<TMessage> WithGrainAction(string method)
	{
		_methodStr = method;
		return this;
	}

	public SubscriptionBuilder<TMessage> WithQueueName(string queueName)
	{
		_queueName = queueName;
		return this;
	}

	public SubscriptionBuilder<TMessage> WithSubscriptionPattern(string subscriptionKey, Action<PatternOptions>? configure = null)
	{
		var options = new PatternOptions();
		configure?.Invoke(options);
		_patternOptions = options;
		_subscriptionKey = subscriptionKey;
		return this;
	}

	public MessageSubscriptionInput<TMessage> Build()
	{
		if (_grainType is null || !_grainType.IsInterface || !_grainType.IsAssignableTo(_grainInterfaceType))
			throw new InvalidOperationException($"Invalid grain type '{_grainType?.Name}'");

		if (_primaryKey is null)
			throw new InvalidOperationException("Primary key must be specified.");

		if (_method is null && _methodStr is null)
			throw new InvalidOperationException("Grain method must be specified.");

		if (_method is not null)
		{
			if (_method.GetType() != DelegateType)
				throw new InvalidOperationException("Grain method must be specified.");

			_methodStr = _method.Method.Name;
		}
		else if (_methodStr is not null)
		{
			var method = _grainType.GetInterfaceMethod(_methodStr);

			var methodParams = method?.GetParameters();
			var equalArgs = methodParams is { Length: 1 } && methodParams[0].ParameterType == DelegateParamType;
			var equalReturn = method?.ReturnType == DelegateReturnType;

			if (!equalArgs || !equalReturn)
				throw new InvalidOperationException(
					$"Invalid method signature. Expected '{DelegateParamType}' => '{DelegateReturnType}', "
					+ $"but current method has '{methodParams?.FirstOrDefault()?.ParameterType}' => '{method?.ReturnType}'"
				);

			_methodStr = method?.Name;
		}

		if (_queueName is null)
			throw new InvalidOperationException("Queue name must be specified.");

		if (_subscriptionKey is null)
			throw new InvalidOperationException("Subscription key must be specified.");

		var input = new MessageSubscriptionInput<TMessage>
		{
			GrainType = _grainType,
			GrainPrimaryKey = _primaryKey,
			QueueName = _queueName,
			KeyPattern = _subscriptionKey,
			MethodName = _methodStr!,
			PatternOptions = _patternOptions,
		};

		return input;
	}
}

[GenerateSerializer]
public record PatternOptions
{
	[Id(0)]
	public PatternType PatternType { get; set; }

	[Id(1)]
	public ScheduledThrottledActionOptions SubscriptionDelayOptions { get; set; }
}

public enum PatternType
{
	Regex,
	Exact,
	Substring,
	Wildcard
}

/// <param name="ServiceKey">e.g. Default, External (<see cref="MessageBrokerNames"/>)</param>
public record TopicSubscription(
	string ServiceKey,
	string SubscriptionId,
	string TopicName,
	string SubscriptionPattern
);

[GenerateSerializer]
public record MessageSubscriptionInput<TMessage>
{
	[Id(0)]
	public string KeyPattern { get; set; }

	[Id(1)]
	public string QueueName { get; set; }

	[Id(2)]
	public string GrainPrimaryKey { get; set; }

	[Id(3)]
	public Type GrainType { get; set; }

	[Id(4)]
	public string MethodName { get; set; }

	[Id(5)]
	public PatternOptions PatternOptions { get; set; }

	[Id(6)]
	public ScheduledThrottledActionOptions ScheduledThrottledActionOptions { get; set; }

	[Id(7)]
	public string ServiceKey { get; set; } = "defaultBroker";
}

public static class SubscriptionClientExtensions
{
	public static SubscriptionBuilder<TMessage> CreateSubscriptionConfig<TMessage>(
		this IGrain grain,
		string queueName,
		string pattern,
		string serviceKey
	)
	{
		var grainType = InferGrainType(grain.GetType(), (GrainReference)grain);
		return CreateSubscriptionConfig<TMessage>(grain, grainType, queueName, pattern, serviceKey);
	}

	public static SubscriptionBuilder<TMessage> CreateSubscriptionConfig<TMessage, TGrainType>(
		this IGrain grain,
		string queueName,
		string pattern,
		string serviceKey
	)
		where TGrainType : IGrain
		=> CreateSubscriptionConfig<TMessage>(grain, typeof(TGrainType), queueName, pattern, serviceKey);

	public static SubscriptionBuilder<TMessage> CreateSubscriptionConfig<TMessage>(
		this IGrain grain,
		Type grainType,
		string queueName,
		string pattern,
		string serviceKey
	)
	{
		var builder = new SubscriptionBuilder<TMessage>()
				.WithPrimaryKey(grain.GetPrimaryKeyString())
				.WithGrainType(grainType)
				.WithQueueName(queueName)
				.WithSubscriptionPattern(pattern)
			;

		return builder;
	}

	private static Type InferGrainType(Type grainType, GrainReference grainRef)
	{
		var grainInterfaces = grainType
				.GetInterfaces()
				.Where(x => x.Name.Contains(grainRef.InterfaceName))
				.ToList()
			;

		if (grainInterfaces.Count == 1)
			return grainInterfaces.Single();

		throw new ArgumentException(
			$"Generic grain type specified {grainType.GetDemystifiedName()}. Use 'CreateSubscriptionConfig<TMessage, TGrainType>' instead",
			nameof(grainType)
		);
	}
}