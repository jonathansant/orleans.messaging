namespace Odin.Core.FlowControl;

public class CooldownOptions
{
	public CooldownFailureMode FailureMode { get; set; } = CooldownFailureMode.NoCooldown;
}

public enum CooldownFailureMode
{
	/// <summary>
	/// Always set on cooldown, even if no executions were successful.
	/// </summary>
	AlwaysCooldown,

	/// <summary>
	/// Set on cooldown, however throw invalid state on next attempt execution until off cooldown (only if not succeeded at least once).
	/// </summary>
	InvalidState,

	/// <summary>
	/// Do not set cooldown when no executions were successful.
	/// </summary>
	NoCooldown
}

/// <summary>
/// Cooldown timer manager.
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class Cooldown
{
	protected string DebuggerDisplay => $"IsOnCooldown: {IsOnCooldown}, RemainingCooldown: '{RemainingCooldown}'";
	protected readonly CooldownOptions Options;
	private static readonly CooldownOptions DefaultOptions = new();

	protected Task? LockTask { get; set; }
	protected readonly object ExecuteLock = new();

	/// <summary>
	/// Indicates whether it has been executed successfully at least once.
	/// </summary>
	protected bool IsSuccessful { get; set; }

	/// <summary>
	/// Get the default cooldown duration.
	/// </summary>
	public TimeSpan DefaultDuration { get; }

	/// <summary>
	/// Get the expiration time (utc) of the cooldown.
	/// </summary>
	public DateTime? Expiration { get; private set; }

	/// <summary>
	/// Determine whether its on cooldown or not.
	/// </summary>
	public bool IsOnCooldown => Expiration.HasValue && Expiration.Value > DateTime.UtcNow;

	/// <summary>
	/// Get the remaining cooldown in timespan.
	/// </summary>
	public TimeSpan? RemainingCooldown
	{
		get
		{
			if (!IsOnCooldown)
				return null;
			return Expiration!.Value.Subtract(DateTime.UtcNow);
		}
	}

	public Cooldown(CooldownOptions? options = null)
	{
		Options = options ?? DefaultOptions;
	}

	public Cooldown(TimeSpan defaultDuration, CooldownOptions? options = null)
	{
		Options = options ?? DefaultOptions;
		DefaultDuration = defaultDuration;
	}

	public Cooldown(TimeSpan defaultDuration, Action<CooldownOptions> configure)
	{
		Options = new CooldownOptions();
		configure(Options);
		DefaultDuration = defaultDuration;
	}

	/// <summary>
	/// Initiate the cooldown from the current time, according to the specified duration (or else the default).
	/// </summary>
	public void Set(TimeSpan? duration = null) => SetFrom(DateTime.UtcNow, duration);

	/// <summary>
	/// Initiate the cooldown from the specified time, according to the specified duration (or else the default).
	/// </summary>
	public void SetFrom(DateTime initiationTime, TimeSpan? duration = null)
	{
		duration = duration ?? DefaultDuration;
		if (!duration.HasValue)
			throw new ArgumentException($"Either {nameof(duration)} or {nameof(DefaultDuration)} must be specified.", nameof(duration));

		Expiration = initiationTime.Add(duration.Value);
	}

	/// <summary>
	/// Clears current cooldown.
	/// </summary>
	public void Clear() => Expiration = null;

	/// <summary>
	/// Try execute when not on cooldown, and set cooldown after execution.
	/// </summary>
	/// <param name="action">Action to invoke when not on cooldown.</param>
	/// <returns>Returns true when execute and false when on cooldown.</returns>
	public bool TryExecute(Action action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		if (IsOnCooldown && EnsureSucceededOnce())
			return false;

		var hasExecuted = false;
		try
		{
			lock (ExecuteLock)
				if (!IsOnCooldown)
				{
					action();
					hasExecuted = true;
					IsSuccessful = true;

					Set();
				}
		}
		catch
		{
			if (Options.FailureMode != CooldownFailureMode.NoCooldown)
				Set();
			throw;
		}

		return hasExecuted;
	}

	/// <summary>
	/// Try execute when not on cooldown, and set cooldown after execution.
	/// </summary>
	/// <param name="func">Action to invoke when not on cooldown.</param>
	/// <returns>Returns true when execute and false when on cooldown.</returns>
	public async Task<bool> TryExecuteAsync(Func<Task> func)
	{
		if (func == null)
			throw new ArgumentNullException(nameof(func));

		if (IsOnCooldown && EnsureSucceededOnce())
			return false;

		var hasExecuted = false;
		try
		{
			if (LockTask == null)
			{
				lock (ExecuteLock)
					LockTask = func();
				hasExecuted = true;
			}

			await LockTask;
			IsSuccessful = true;

			Set();
		}
		catch
		{
			if (Options.FailureMode != CooldownFailureMode.NoCooldown)
				Set();
			throw;
		}
		finally
		{
			LockTask = null;
		}

		return hasExecuted;
	}

	protected bool EnsureSucceededOnce()
	{
		if (!IsSuccessful && Options.FailureMode == CooldownFailureMode.InvalidState)
			throw new InvalidOperationException("On cooldown however in an invalid state due it was never succeeded at least once - must at least succeed once in order not to throw.");

		return true;
	}
}

public class Cooldown<T> : Cooldown
{
	private T? _result;
	private Task<T>? _resultTask;

	public Cooldown(CooldownOptions? options = null) : base(options)
	{
	}

	public Cooldown(TimeSpan defaultDuration, CooldownOptions? options = null) : base(defaultDuration, options)
	{
	}

	public Cooldown(TimeSpan defaultDuration, Action<CooldownOptions> configure) : base(defaultDuration, configure)
	{
	}

	/// <summary>
	/// Execute when not on cooldown, and set cooldown after execution and return result.
	/// </summary>
	/// <param name="func">Action to invoke when not on cooldown.</param>
	/// <returns>Returns true when execute and false when on cooldown.</returns>
	public async Task<T?> ExecuteAsync(Func<Task<T>> func)
	{
		if (func == null)
			throw new ArgumentNullException(nameof(func));

		if (IsOnCooldown && EnsureSucceededOnce())
			return _result;

		try
		{
			if (LockTask == null)
			{
				lock (ExecuteLock)
				{
					// todo: add option to persist previous value, and return it in case next one fails to reuse previous.
					_result = default;
					IsSuccessful = false;
					_resultTask = func();
					LockTask = _resultTask;
				}
			}

			await LockTask!;
			_result = _resultTask!.Result;
			IsSuccessful = true;

			Set();
		}
		catch
		{
			if (Options.FailureMode != CooldownFailureMode.NoCooldown)
				Set();
			throw;
		}
		finally
		{
			LockTask = null;
		}

		return _result;
	}
}
