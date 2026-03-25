using System.Diagnostics;

namespace Orleans.Messaging.FlowControl;

public delegate IDisposable RegisterTimerDelegate(
	Func<object, Task> asyncCallback,
	object state,
	TimeSpan dueTime,
	TimeSpan? period = null,
	bool isInterleaved = true
);

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GenerateSerializer]
public record ScheduledThrottledActionOptions
{
	protected string DebuggerDisplay
		=> $"ThrottleTime: {ThrottleTime}, FlushScheduledOnDispose: {FlushScheduledOnDispose}, HasLeadingDelay: {HasLeadingDelay}";

	[Id(0)]
	public TimeSpan ThrottleTime { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Determines whether to flush (trigger) scheduled on dispose.
	/// </summary>
	[Id(1)]
	public bool FlushScheduledOnDispose { get; set; }

	/// <summary>
	/// Delay the first trigger i.e. it will NOT trigger instantly the first time but it will take <see cref="ThrottleTime"/> amount of time to trigger
	/// </summary>
	[Id(2)]
	public bool HasLeadingDelay { get; set; }

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}

public enum ScheduledThrottledActionState
{
	Ready = 0,
	Executing,
	Scheduled,
	Cooldown
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GenerateSerializer]
public class ScheduledThrottledActionTriggerResult
{
	protected string DebuggerDisplay
		=> $"Status: {Status}, ScheduledTimeRemaining: '{ScheduledTimeRemaining}', ScheduledTime: '{ScheduledTime}'";

	[Id(0)]
	public ScheduledThrottledActionState Status { get; set; }

	[Id(1)]
	public TimeSpan? ScheduledTimeRemaining { get; set; }

	[Id(2)]
	public DateTime? ScheduledTime { get; set; }
}

/// <summary>
/// Scheduled actions execution which will be executed after timer expires.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ScheduledThrottledAction : IAsyncDisposable
{
	protected string DebuggerDisplay
		=> $"State: {State}, IsScheduled: {IsScheduled}, ScheduledTimeRemaining: '{ScheduledTimeRemaining}', "
		   + $"ScheduledTime: {ScheduledTime}, IsExecuting: {IsExecuting}";

	private readonly Func<object[], Task> _action;
	private readonly RegisterTimerDelegate _registerTimer;
	private object[]? _lastArgs;
	private IDisposable? _timer;
	protected readonly ScheduledThrottledActionOptions Options = new();

	public ScheduledThrottledActionState State { get; private set; }

	/// <summary>
	/// Determine whether action is executing. Even when its scheduled to execute again.
	/// </summary>
	public bool IsExecuting { get; private set; }

	/// <summary>
	/// Get the remaining time for execution.
	/// </summary>
	public TimeSpan? ScheduledTimeRemaining
	{
		get
		{
			if (!(ScheduledTime.HasValue && ScheduledTime.Value > DateTime.UtcNow))
				return null;
			return ScheduledTime.Value.Subtract(DateTime.UtcNow);
		}
	}

	/// <summary>
	/// Get the next scheduled execution datetime (utc).
	/// </summary>
	public DateTime? ScheduledTime { get; private set; }

	/// <summary>
	/// Determine whether its scheduled for execution or not.
	/// </summary>
	public bool IsScheduled => State == ScheduledThrottledActionState.Scheduled;

	public ScheduledThrottledAction(
		RegisterTimerDelegate registerTimer,
		Func<object[], Task> action,
		Action<ScheduledThrottledActionOptions>? configure = null
	)
	{
		_registerTimer = registerTimer;
		_action = action;
		configure?.Invoke(Options);
	}

	/// <summary>
	/// Configure options.
	/// </summary>
	/// <param name="configure">Function to configure options.</param>
	public ScheduledThrottledAction WithOptions(Action<ScheduledThrottledActionOptions> configure)
	{
		configure(Options);
		return this;
	}

	/// <summary>
	/// Triggers the function to execute or schedule for execution.
	/// </summary>
	/// <param name="args">Additional arguments to the execution function.</param>
	public Task<ScheduledThrottledActionTriggerResult> Trigger(params object[] args)
	{
		var result = new ScheduledThrottledActionTriggerResult();
		switch (State)
		{
			case ScheduledThrottledActionState.Ready when Options.HasLeadingDelay:
				ScheduledTime = DateTime.UtcNow.Add(Options.ThrottleTime);
				ScheduleExecute(args);
				break;
			case ScheduledThrottledActionState.Ready:
				result.Status = ScheduledThrottledActionState.Executing;
				ScheduleExecute(args);
				return Task.FromResult(result);
			case ScheduledThrottledActionState.Executing:
				State = ScheduledThrottledActionState.Scheduled;
				break;
			case ScheduledThrottledActionState.Cooldown:
				State = ScheduledThrottledActionState.Scheduled;
				ScheduleExecute(args);
				break;
			case ScheduledThrottledActionState.Scheduled:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		_lastArgs = args;
		result.Status = ScheduledThrottledActionState.Scheduled;
		result.ScheduledTimeRemaining = ScheduledTimeRemaining;
		result.ScheduledTime = ScheduledTime;

		return Task.FromResult(result);
	}

	/// <summary>
	/// Forces the action to go on cooldown. Only works while NOT <see cref="ScheduledThrottledActionState.Scheduled"/>.
	/// </summary>
	public ScheduledThrottledAction SetCooldown()
	{
		if (State == ScheduledThrottledActionState.Scheduled)
			return this;
		ForceSetCooldown();
		return this;
	}

	public async ValueTask DisposeAsync()
	{
		_timer?.Dispose();

		if (State == ScheduledThrottledActionState.Scheduled && Options.FlushScheduledOnDispose)
			await _action(_lastArgs!);
	}

	private async Task ExecAndCooldown(params object[] args)
	{
		State = ScheduledThrottledActionState.Executing;
		IsExecuting = true;

		await _action(args);
		IsExecuting = false;

		switch (State)
		{
			case ScheduledThrottledActionState.Scheduled:
				ScheduleExecute(_lastArgs!);
				break;
			default:
				_lastArgs = null;
				ForceSetCooldown();
				break;
		}
	}

	private void ScheduleExecute(params object[] args)
		=> SetTimeoutFn(async objects => await ExecAndCooldown(objects), ScheduledTimeRemaining ?? TimeSpan.Zero, args);

	private void ForceSetCooldown()
	{
		ScheduledTime = DateTime.UtcNow.Add(Options.ThrottleTime);
		State = ScheduledThrottledActionState.Cooldown;
		SetTimeoutForReady();
	}

	private void SetTimeoutForReady()
		=> SetTimeoutFn(
			_ =>
			{
				State = ScheduledThrottledActionState.Ready;
				ScheduledTime = null;
				return Task.CompletedTask;
			},
			Options.ThrottleTime
		);

	private void SetTimeoutFn(Func<object[], Task> action, TimeSpan timeout, params object[] args)
	{
		_timer?.Dispose();
		_timer = _registerTimer(_ => action(args), null!, timeout);
	}
}
