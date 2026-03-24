using System.Diagnostics;

namespace Odin.Messaging.FlowControl;

public enum ScheduledDebounceActionState
{
	Ready = 0,
	Executing,
	Scheduled,
	Cooldown
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GenerateSerializer]
public class ScheduledDebounceActionTriggerResult
{
	protected string DebuggerDisplay
		=> $"Status: {Status}, ScheduledTimeRemaining: '{ScheduledTimeRemaining}', ScheduledTime: '{ScheduledTime}'";

	[Id(0)]
	public ScheduledDebounceActionState Status { get; set; }
	[Id(1)]
	public TimeSpan? ScheduledTimeRemaining { get; set; }
	[Id(2)]
	public DateTime? ScheduledTime { get; set; }
}

public enum ScheduledDebounceType
{
	Leading,
	Trailing,
	LeadingAndTrailing
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
[GenerateSerializer]
public record ScheduledDebounceActionOptions
{
	protected string DebuggerDisplay
		=> $"ThrottleTime: {ThrottleTime}, FlushScheduledOnDispose: {FlushScheduledOnDispose}";

	[Id(0)]
	public TimeSpan ThrottleTime { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Determines whether to flush (trigger) scheduled on dispose.
	/// </summary>
	[Id(1)]
	public bool FlushScheduledOnDispose { get; set; }

	/// <summary>
	/// This configures whether to take the first or last action when multiple actions are scheduled.
	/// </summary>
	[Id(2)]
	public ScheduledDebounceType ScheduledDebounceType { get; set; }

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}

/// <summary>
/// Scheduled debounce actions execution which will be executed after timer expires.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ScheduledDebounceAction : IAsyncDisposable
{
	protected string DebuggerDisplay
		=> $"State: {State}, IsScheduled: {IsScheduled}, ScheduledTimeRemaining: '{ScheduledTimeRemaining}', " +
		$"ScheduledTime: {ScheduledTime}, IsExecuting: {IsExecuting}";

	private readonly Func<object[], Task> _action;
	private readonly RegisterTimerDelegate _registerTimer;
	private object[]? _lastArgs;
	private IDisposable? _timer;
	protected readonly ScheduledDebounceActionOptions Options = new();

	public ScheduledDebounceActionState State { get; private set; }

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
	public bool IsScheduled => State == ScheduledDebounceActionState.Scheduled;

	public ScheduledDebounceAction(
		RegisterTimerDelegate registerTimer,
		Func<object[], Task> action,
		Action<ScheduledDebounceActionOptions>? configure = null
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
	public ScheduledDebounceAction WithOptions(Action<ScheduledDebounceActionOptions> configure)
	{
		configure(Options);
		return this;
	}

	/// <summary>
	/// Triggers the function to execute or schedule for execution.
	/// </summary>
	/// <param name="args">Additional arguments to the execution function.</param>
	public Task<ScheduledDebounceActionTriggerResult> Trigger(params object[] args)
	{
		_lastArgs = args;
		var result = new ScheduledDebounceActionTriggerResult();
		switch (State)
		{
			case ScheduledDebounceActionState.Ready when Options.ScheduledDebounceType == ScheduledDebounceType.Trailing:
				ScheduledTime = DateTime.UtcNow.Add(Options.ThrottleTime);
				ScheduleExecute(args);
				break;
			case ScheduledDebounceActionState.Ready:
				result.Status = ScheduledDebounceActionState.Executing;
				ScheduleExecute(args);
				return Task.FromResult(result);
			case ScheduledDebounceActionState.Executing:
				State = ScheduledDebounceActionState.Scheduled;
				break;
			case ScheduledDebounceActionState.Cooldown:
				State = ScheduledDebounceActionState.Scheduled;
				if (Options.ScheduledDebounceType != ScheduledDebounceType.Leading)
					ScheduleExecute(args);
				break;
			case ScheduledDebounceActionState.Scheduled:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		result.Status = ScheduledDebounceActionState.Scheduled;
		result.ScheduledTimeRemaining = ScheduledTimeRemaining;
		result.ScheduledTime = ScheduledTime;

		return Task.FromResult(result);
	}

	/// <summary>
	/// Forces the action to go on cooldown. Only works while NOT <see cref="ScheduledDebounceActionState.Scheduled"/>.
	/// </summary>
	public ScheduledDebounceAction SetCooldown()
	{
		if (State == ScheduledDebounceActionState.Scheduled)
			return this;
		ForceSetCooldown();
		return this;
	}

	public async ValueTask DisposeAsync()
	{
		_timer?.Dispose();

		if (State == ScheduledDebounceActionState.Scheduled && Options.FlushScheduledOnDispose)
			await _action(_lastArgs!);
	}

	private async Task ExecAndCooldown(params object[] args)
	{
		State = ScheduledDebounceActionState.Executing;
		IsExecuting = true;

		await _action(Options.ScheduledDebounceType == ScheduledDebounceType.LeadingAndTrailing ? _lastArgs : args);
		IsExecuting = false;

		switch (State)
		{
			case ScheduledDebounceActionState.Scheduled:
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
		State = ScheduledDebounceActionState.Cooldown;
		SetTimeoutForReady();
	}

	private void SetTimeoutForReady()
		=> SetTimeoutFn(_ =>
		{
			State = ScheduledDebounceActionState.Ready;
			ScheduledTime = null;
			return Task.CompletedTask;
		}, Options.ThrottleTime);

	private void SetTimeoutFn(Func<object[], Task> action, TimeSpan timeout, params object[] args)
	{
		_timer?.Dispose();
		_timer = _registerTimer(_ => action(args), null!, timeout);
	}
}
