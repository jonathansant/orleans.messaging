namespace Odin.Core.FlowControl;

public interface IThrottledActionFactory
{
	/// <summary>
	/// Create a new instance.
	/// </summary>
	/// <param name="action">Action to be invoked throttled.</param>
	/// <param name="configure">Configure action options.</param>
	ThrottledAction Create(Func<object[], Task> action, Action<ThrottledActionOptions>? configure = null);
}

public class ThrottledActionFactory : IThrottledActionFactory
{
	private readonly ILogger _logger;

	public ThrottledActionFactory(
		ILogger<ThrottledActionFactory> logger
	)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public ThrottledAction Create(Func<object[], Task> action, Action<ThrottledActionOptions>? configure = null)
		=> new(action, _logger, configure);
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ThrottledActionOptions
{
	protected string DebuggerDisplay
		=> $"ThrottleTime: {ThrottleTime}, FlushScheduledOnDispose: {FlushScheduledOnDispose}, HasLeadingDelay: {HasLeadingDelay}";

	/// <summary>
	/// Delay to wait for throttling before invoking action.
	/// </summary>
	public TimeSpan ThrottleTime { get; set; } = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// Determines whether to flush (trigger) scheduled on dispose.
	/// </summary>
	public bool FlushScheduledOnDispose { get; set; }

	/// <summary>
	/// Delay the first trigger i.e. it will NOT trigger instantly the first time but it will take <see cref="ThrottleTime"/> amount of time to trigger (defaults: true).
	/// </summary>
	public bool HasLeadingDelay { get; set; } = true;

	// todo: implement option to queue and await invoke (as it is) or release immediately during invoke (e.g. dont await invoke before starting throttle delay)

	public override string ToString() => $"{{ {DebuggerDisplay} }}";
}

/// <summary>
/// Throttle actions execution.
/// <br /><b>Example:</b><br />
/// - trigger [CAD]<br />
/// - trigger [USD] <br />
/// - <i>...delayed by throttling</i><br />
/// - invoked (after delay)<br />
/// <returns>Task will completes when the execution is complete.</returns>
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ThrottledAction
{
	protected string DebuggerDisplay => $"IsExecuting: {IsExecuting}, IsScheduled: {_isScheduled}, Options: {Options}";

	private readonly Func<object[], Task> _action;
	private readonly ILogger _logger;
	private readonly SemaphoreSlim _actionLock = new(1);
	private CancellationTokenSource? _delayCts;
	protected readonly ThrottledActionOptions Options = new();
	private bool _isScheduled;
	private TaskCompletionSource? _taskCompletion;

	/// <summary>
	/// Determine whether action is executing.
	/// </summary>
	public bool IsExecuting { get; private set; }

	/// <summary>
	/// Get reference to task completion.
	/// </summary>
	public Task CompletionTask => _taskCompletion?.Task ?? Task.CompletedTask;

	public ThrottledAction(
		Func<object[], Task> action,
		ILogger logger,
		Action<ThrottledActionOptions>? configure = null
	)
	{
		_action = action;
		_logger = logger;
		configure?.Invoke(Options);
	}

	/// <summary>
	/// Configure options.
	/// </summary>
	/// <param name="configure">Function to configure options.</param>
	public ThrottledAction WithOptions(Action<ThrottledActionOptions> configure)
	{
		configure(Options);
		return this;
	}

	/// <summary>
	/// Trigger execute.
	/// </summary>
	/// <returns>Returns task which completes after the execution of the action.</returns>
	public async Task Execute(params object[] args)
	{
		if (_taskCompletion != null && IsExecuting)
			//_logger.Warn(">>> ThrottledAction - AWAITING INVOKE args: {args}", args[0]);
			await _taskCompletion.Task;

		if (!_isScheduled)
		{
			await _actionLock.WaitAsync();

			if (_isScheduled && _taskCompletion != null)
			{
				_actionLock.Release();
				//_logger.Warn(">>> ThrottledAction - INNER TASK args: {args}", args[0]);
				await _taskCompletion.Task;
				//_logger.Warn(">>> ThrottledAction - INNER TASK END RETURN args: {args}", args[0]);
				return;
			}

			//_logger.Warn(">>> ThrottledAction - NEW TSC args: {args}", args[0]);
			_taskCompletion = new();
			_isScheduled = true;
			_actionLock.Release();

			if (Options.HasLeadingDelay)
			{
				_delayCts = new();
				try
				{
					await Task.Delay(Options.ThrottleTime, _delayCts.Token);
				}
				catch (OperationCanceledException) when (_delayCts.IsCancellationRequested)
				{
					// delay is cancelled
				}
				finally
				{
					_delayCts.Dispose();
					_delayCts = null;
				}
			}

			IsExecuting = true;
			try
			{
				//_logger.Warn(">>> ThrottledAction - INVOKING args: {args}", args[0]);

				await _action(args);
				_isScheduled = false;
				IsExecuting = false;
				//_logger.Warn(">>> ThrottledAction - INVOKING END args: {args}", args[0]);
				_taskCompletion.SetResult();
			}
			catch (Exception ex)
			{
				_isScheduled = false;
				IsExecuting = false;
				//_logger.Warn(">>> ThrottledAction - ERROR args: {args}", args[0]);
				_taskCompletion.SetException(ex);
				throw;
			}
			finally
			{
				if (!_isScheduled)
					_taskCompletion = null;
				//else
				//{
				//	_logger.Warn(">>> ThrottledAction - INVOKING END - SKIP NULLING SCHEDULED args: {args}", args[0]);
				//}
			}
		}
		else
			//_logger.Warn(">>> ThrottledAction - END RETURN AWAITING args: {args}", args[0]);
			if (_taskCompletion != null)
				await _taskCompletion.Task;
			else
			{
				//_logger.Error(">>> ThrottledAction - Uqq imbad? args: {args}", args[0]);
			}
	}

	/// <summary>
	/// Cancel delay and executes next scheduled immediately if any.
	/// </summary>
	public async Task ExecuteScheduledImmediately()
	{
		if (_delayCts != null)
		{
			_delayCts.Cancel();
			await CompletionTask;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (Options.FlushScheduledOnDispose)
			await ExecuteScheduledImmediately();
		_delayCts?.Dispose();
	}
}
