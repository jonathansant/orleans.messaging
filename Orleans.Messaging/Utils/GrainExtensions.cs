using Orleans.Messaging.FlowControl;

namespace Orleans.Messaging.Utils;

public static class GrainExtensions
{
	private static readonly StringTokenParserFactory StringTokenParserFactory = new();

	/// <summary>
	/// Returns the primary key of the grain of any type as a string.
	/// </summary>
	public static string GetPrimaryKeyAny(this Grain grain)
		=> grain.GetPrimaryKeyString()
		   ?? (grain.IsPrimaryKeyBasedOnLong()
			   ? grain.GetPrimaryKeyLong().ToString()
			   : grain.GetPrimaryKey().ToString());

	/// <summary>
	/// Parses template key string into an object.
	/// </summary>
	/// <typeparam name="T">Type to cast key data to.</typeparam>
	/// <param name="grain"></param>
	/// <param name="template">Template pattern to parse e.g. '{brand}/{locale}/{id}'</param>
	public static T ParseKey<T>(this Grain grain, string template)
		where T : new()
		=> StringTokenParserFactory.Get(template)
			.Parse<T>(grain.GetPrimaryKeyAny());

	/// <summary>
	/// Register a timer which sends a callback once.
	/// </summary>
	public static IDisposable RegisterTimerOnce(
		this Grain grain,
		Func<object, Task> asyncCallback,
		object? state,
		TimeSpan dueTime,
		TimeSpan? period = null,
		bool isInterleave = true
	)
		=> grain.RegisterGrainTimer(
			asyncCallback,
			state,
			new()
			{
				DueTime = dueTime,
				Period = period ?? TimeSpan.FromMilliseconds(-1),
				Interleave = isInterleave
			}
		);

	/// <summary>
	/// Creates a new scheduled throttled action.
	/// </summary>
	public static ScheduledThrottledAction CreateScheduledThrottledAction(
		this Grain grain,
		Func<object[], Task> action,
		Action<ScheduledThrottledActionOptions>? configure = null
	)
		=> new(grain.RegisterTimerOnce, action, configure);

	/// <summary>
	/// Creates a new scheduled debounce action.
	/// </summary>
	public static ScheduledDebounceAction CreateScheduledDebounceAction(
		this Grain grain,
		Func<object[], Task> action,
		Action<ScheduledDebounceActionOptions>? configure = null
	)
		=> new(grain.RegisterTimerOnce, action, configure);
}
