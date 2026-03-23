
namespace Odin.Core;

public static class TimeSpanExtensions
{
	/// <summary>
	/// Multiply timespan.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="multiplier">Multiplier factor.</param>
	/// <returns>Returns multiplied multiplier.</returns>
	public static TimeSpan Multiply(this TimeSpan value, double multiplier)
	{
		var multipliedTicks = value.Ticks * multiplier;
		var ticks = checked((long)multipliedTicks);
		return TimeSpan.FromTicks(ticks);
	}

	/// <summary>
	/// Compare timespans and return the min multiplier.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="compare">Value to compare with.</param>
	/// <returns>Returns minimum timespan.</returns>
	public static TimeSpan Min(this TimeSpan value, TimeSpan compare)
		=> value < compare ? value : compare;

	/// <summary>
	/// Display in short human readable time e.g. '5.5min(s)' or '10s' or '100ms' etc...
	/// </summary>
	/// <param name="value"></param>
	public static string ToHumanElapsed(this TimeSpan value)
	{
		if (value.TotalHours >= 1)
			return $"{value.TotalHours:#.##}hr(s)";
		if (value.TotalMinutes >= 1)
			return $"{value.TotalMinutes:#.##}min(s)";
		if (value.TotalSeconds >= 1)
			return $"{value.TotalSeconds:#.#}s";

		return $"{value.TotalMilliseconds:#}ms";
	}
}
