using Odin.Core.FlowControl;
using Odin.Core.Utils;
using Odin.Logging.Serilog;
using Orleans.Concurrency;
using Serilog.Context;
using Serilog.Core.Enrichers;
using System.Net;

namespace Odin.Orleans.Core;

/// <summary>
/// Odin Grain implementation interface. e.g. Grain concrete should implement this.
/// </summary>
public interface IOdinGrain : IOdinGrainContract
{
	/// <summary>
	/// Gets the primary key for the grain as string (independent of its original type).
	/// </summary>
	string PrimaryKey { get; }

	/// <summary>
	/// Gets the source type name e.g. 'AppConfigGrain'.
	/// </summary>
	string Source { get; }

	ILogger Logger { get; }
	ILoggingContext LoggingContext { get; }

	/// <summary>
	/// Registers a timer which sends periodic callback to this grain.
	/// </summary>
	/// <param name="asyncCallback"></param>
	/// <param name="state"></param>
	/// <param name="dueTime"></param>
	/// <param name="period"></param>
	/// <param name="isInterleave"></param>
	IDisposable RegisterTimer(Func<object, Task> asyncCallback, object? state, TimeSpan dueTime, TimeSpan period, bool isInterleave = true);
}

/// <summary>
/// Odin Grain public contract interface. e.g. Grain interface should implement this.
/// </summary>
public interface IOdinGrainContract
{
	/// <summary>
	/// Cause force activation in order for grain to be warmed up/preloaded.
	/// </summary>
	Task Activate();

	/// <summary>
	/// Cause force activation in order for grain to be warmed up/preloaded.
	/// </summary>
	[OneWay]
	Task ActivateOneWay();
}

/// <summary>
/// Extensions which applies to both Odin Grains, <see cref="OdinGrain"/> and <see cref="OdinGrain{TState}"/>.
/// </summary>
public static class OdinGrainExtensions
{
	private static readonly StringTokenParserFactory StringTokenParserFactory = new();

	/// <summary>
	/// Parses template key string into an object.
	/// </summary>
	/// <typeparam name="T">Type to cast key data to.</typeparam>
	/// <param name="grain"></param>
	/// <param name="template">Template pattern to parse e.g. '{brand}/{locale}/{id}'</param>
	public static T ParseKey<T>(this IOdinGrain grain, string template)
		where T : new()
		=> StringTokenParserFactory.Get(template)
			.Parse<T>(grain.PrimaryKey);

	/// <summary>
	/// Invoke action wrapped with logging and exception handling, which in only intended for Activation/Deactivation
	/// </summary>
	/// <param name="grain"></param>
	/// <param name="lifecycle"></param>
	/// <param name="action"></param>
	public static async Task InvokeLifecycleAction(
		this IOdinGrain grain,
		string lifecycle,
		Func<Task> action,
		DeactivationReason? deactivationReason
	)
	{
		using (LogContext.Push(
			       new PropertyEnricher(LogPropertyNames.Grain, grain.Source),
			       new PropertyEnricher(LogPropertyNames.GrainMethod, lifecycle),
			       new PropertyEnricher(LogPropertyNames.GrainPrimaryKey, grain.PrimaryKey)
		       ))
		using (grain.LoggingContext.BindScope())
		{
			try
			{
				if (deactivationReason.HasValue)
					grain.Logger.DeactivationLog(grain.Source, lifecycle, grain.PrimaryKey, deactivationReason.Value.ReasonCode);
				else
					grain.Logger.ActivationLog(grain.Source, lifecycle, grain.PrimaryKey);
				await action();
			}
			catch (Exception ex)
			{
				var type = ex.GetType();
				if (OdinOrleansCoreConst.KnownClientNamespaces.Contains(type.Assembly.GetName().Name))
					throw;

				grain.Logger.Error(
					ex,
					"Error while invoking grain lifecycle {lifecycle}, exception: {exceptionMessage}",
					lifecycle,
					ex.Message
				);
				throw new ApiErrorException(OdinErrorCodes.InternalServerError, HttpStatusCode.InternalServerError);
			}
		}
	}

	/// <summary>
	/// Register a timer which sends a callback once.
	/// </summary>
	/// <param name="grain"></param>
	/// <param name="asyncCallback"></param>
	/// <param name="state"></param>
	/// <param name="dueTime"></param>
	/// <param name="period"></param>
	/// <param name="isInterleave"></param>
	public static IDisposable RegisterTimerOnce(
		this IOdinGrain grain,
		Func<object, Task> asyncCallback,
		object? state,
		TimeSpan dueTime,
		TimeSpan? period = null,
		bool isInterleave = true
	)
		=> grain.RegisterTimer(asyncCallback, state, dueTime, period ?? TimeSpan.FromMilliseconds(-1), isInterleave);

	/// <summary>
	/// Creates a new scheduled throttled action.
	/// </summary>
	/// <param name="grain"></param>
	/// <param name="action">Action to be invoked.</param>
	/// <param name="configure">Options to be configured.</param>
	public static ScheduledThrottledAction CreateScheduledThrottledAction(
		this IOdinGrain grain,
		Func<object[], Task> action,
		Action<ScheduledThrottledActionOptions>? configure = null
	)
		=> new(grain.RegisterTimerOnce, action, grain.LoggingContext, configure);

	public static ScheduledDebounceAction CreateScheduledDebounceAction(
		this IOdinGrain grain,
		Func<object[], Task> action,
		Action<ScheduledDebounceActionOptions>? configure = null
	)
		=> new(grain.RegisterTimerOnce, action, grain.LoggingContext, configure);

	/// <summary>
	/// Creates a new throttled action.
	/// </summary>
	/// <param name="grain"></param>
	/// <param name="action">Action to be invoked.</param>
	/// <param name="configure">Options to be configured.</param>
	public static ThrottledAction CreateThrottledAction(
		this IOdinGrain grain,
		Func<object[], Task> action,
		Action<ThrottledActionOptions>? configure = null
	)
		=> new(action, grain.Logger, configure);
}

public abstract class OdinGrain<TState> : Grain<TState>, IOdinGrain
	where TState : new()
{
	public ILogger Logger { get; }
	public ILoggingContext LoggingContext { get; }

	private string? _primaryKey;
	public string PrimaryKey => _primaryKey ??= this.GetPrimaryKeyAny();

	public string Source { get; }

	protected OdinGrain(
		ILogger logger,
		ILoggingContext loggingContext
	)
	{
		Source = GetType().GetDemystifiedName();
		Logger = logger;
		LoggingContext = loggingContext;
	}

	public virtual Task Activate() => Task.CompletedTask;
	public Task ActivateOneWay() => Activate();

	public sealed override async Task OnActivateAsync(CancellationToken cancellationToken)
		=> await this.InvokeLifecycleAction("Activate", OnOdinActivate, deactivationReason: null);

	public sealed override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
		=> await this.InvokeLifecycleAction("Deactivate", OnOdinDeactivate, reason);

	/// <summary>
	/// Odin hook for <c>Activation</c> which is wrapped inside an <see cref="OnActivateAsync"/> for global handling e.g. error handling + logging.
	/// Override this instead of <see cref="OnActivateAsync"/>.
	/// </summary>
	public virtual Task OnOdinActivate() => Task.CompletedTask;

	/// <summary>
	/// Odin hook for <c>Deactivation</c> which is wrapped inside an <see cref="OnDeactivateAsync"/> for global handling e.g. error handling + logging.
	/// Override this instead of <see cref="OnDeactivateAsync"/>.
	/// </summary>
	public virtual Task OnOdinDeactivate() => Task.CompletedTask;

	/// <summary>
	/// Mitigates e-tag mismatch exceptions.
	/// </summary>
	/// <remarks>see: https://github.com/dotnet/orleans/issues/920 </remarks>
	protected async Task SafeWriteStateAsync(Func<Task> stateWriter)
	{
		await stateWriter();

		try
		{
			await WriteStateAsync();
		}
		catch (Exception ex)
		{
			Logger.Warn(ex, "[{grain}] E-Tag Mismatch occurred for key: {grainPrimaryKey}.", Source, PrimaryKey);

			await ReadStateAsync();
			await stateWriter();
			await WriteStateAsync();
		}
	}

	/// <summary>
	/// Mitigates e-tag mismatch exceptions.
	/// </summary>
	/// <remarks>see: https://github.com/dotnet/orleans/issues/920 </remarks>
	protected async Task SafeWriteStateAsync(Action stateWriter)
	{
		stateWriter();

		try
		{
			await WriteStateAsync();
		}
		catch (Exception ex)
		{
			Logger.Warn(ex, "[{grain}] E-Tag Mismatch occurred for key: {grainPrimaryKey}.", Source, PrimaryKey);

			await ReadStateAsync();
			stateWriter();
			await WriteStateAsync();
		}
	}

	IDisposable IOdinGrain.RegisterTimer(
		Func<object, Task> asyncCallback,
		object? state,
		TimeSpan dueTime,
		TimeSpan period,
		bool isInterleave
	)
		=> this.RegisterGrainTimer(
			asyncCallback,
			state,
			new()
			{
				DueTime = dueTime,
				Period = period,
				Interleave = isInterleave
			}
		);
}

public abstract class OdinGrain : Grain, IOdinGrain
{
	public ILogger Logger { get; }
	public ILoggingContext LoggingContext { get; }

	private string? _primaryKey;
	public string PrimaryKey => _primaryKey ??= this.GetPrimaryKeyAny();

	public string Source { get; }

	protected OdinGrain(
		ILogger logger,
		ILoggingContext loggingContext
	)
	{
		Source = GetType().GetDemystifiedName();
		Logger = logger;
		LoggingContext = loggingContext;
	}

	public virtual Task Activate() => Task.CompletedTask;
	public Task ActivateOneWay() => Activate();

	public sealed override async Task OnActivateAsync(CancellationToken cancellationToken)
		=> await this.InvokeLifecycleAction("Activate", OnOdinActivate, deactivationReason: null);

	public sealed override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
		=> await this.InvokeLifecycleAction("Deactivate", OnOdinDeactivate, reason);

	/// <summary>
	/// Odin hook for <c>Activation</c> which is wrapped inside an <see cref="OnActivateAsync"/> for global handling e.g. error handling + logging.
	/// Override this instead of <see cref="OnActivateAsync"/>.
	/// </summary>
	public virtual Task OnOdinActivate() => Task.CompletedTask;

	/// <summary>
	/// Odin hook for <c>Deactivation</c> which is wrapped inside an <see cref="OnDeactivateAsync"/> for global handling e.g. error handling + logging.
	/// Override this instead of <see cref="OnDeactivateAsync"/>.
	/// </summary>
	public virtual Task OnOdinDeactivate() => Task.CompletedTask;

	IDisposable IOdinGrain.RegisterTimer(
		Func<object, Task> asyncCallback,
		object? state,
		TimeSpan dueTime,
		TimeSpan period,
		bool isInterleave
	)
		=> this.RegisterGrainTimer(
			asyncCallback,
			state,
			new()
			{
				DueTime = dueTime,
				Period = period,
				Interleave = isInterleave
			}
		);
}

internal static partial class LogExtensions
{
	[LoggerMessage(
		Level = LogLevel.Information,
		Message = "[{grain}] {lifecycle} for key: {grainPrimaryKey} - DeactivationReason: {deactivationReasonCode}"
	)]
	internal static partial void DeactivationLog(
		this ILogger logger,
		string grain,
		string lifecycle,
		string grainPrimaryKey,
		DeactivationReasonCode deactivationReasonCode
	);

	[LoggerMessage(
		Level = LogLevel.Information,
		Message = "[{grain}] {lifecycle} for key: {grainPrimaryKey}"
	)]
	internal static partial void ActivationLog(this ILogger logger, string grain, string lifecycle, string grainPrimaryKey);
}
