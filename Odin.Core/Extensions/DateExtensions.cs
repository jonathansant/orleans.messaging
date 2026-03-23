namespace Odin.Core;

public static class DateExtensions
{
	private static readonly TimeSpan DefaultExpiryPadding = TimeSpan.FromMinutes(10);
	private static readonly DateTime UnixInitialDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Compares date provided if its between start and end date.
	/// </summary>
	/// <param name="dateTime">Date value to check.</param>
	/// <param name="startDate">Start date to compare with.</param>
	/// <param name="endDate">End date to compare with.</param>
	/// <param name="compareTime">Determine whether to compare time or not.</param>
	public static bool IsBetween(this DateTime dateTime, DateTime startDate, DateTime endDate, bool compareTime = false)
		=> compareTime
			? dateTime >= startDate && dateTime <= endDate
			: dateTime.Date >= startDate.Date && dateTime.Date <= endDate.Date;

	// todo: unit test
	/// <summary>
	/// Compares date provided if its between start and end date.
	/// </summary>
	/// <param name="date">Date value to check.</param>
	/// <param name="startDate">Start date to compare with.</param>
	/// <param name="endDate">End date to compare with.</param>
	public static bool IsBetween(this DateOnly date, DateOnly startDate, DateOnly endDate)
		=> date >= startDate && date <= endDate;

	/// <summary>
	/// Converts to a universal date string e.g. 2019-05-15.
	/// </summary>
	/// <param name="dateTime">Date to convert to universal date string.</param>
	public static string ToUniversalDateString(this DateTime dateTime)
		=> dateTime.ToString("yyyy-MM-dd");

	/// <summary>
	/// Get epoch (unix) time stamp e.g. 1512655587
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	public static int GetEpochTimeStamp(this DateTime dateTime)
		=> dateTime.ToUnixSeconds();

	/// <summary>
	/// Converts to unix (epoc) timestamp in seconds e.g. 1512655587
	/// </summary>
	/// <param name="dateTime">Value to convert.</param>
	/// <returns>Returns date in unix.</returns>
	public static int ToUnixSeconds(this DateTime dateTime)
		=> (int)dateTime.Subtract(UnixInitialDateTime).TotalSeconds;

	/// <summary>
	/// Converts to unix (epoc) timestamp e.g. 1512655587
	/// </summary>
	/// <param name="value">Value to convert.</param>
	/// <returns>Returns date in unix.</returns>
	public static int ToUnixSeconds(this DateOnly value)
		=> (int)value.ToDateTime(TimeOnly.MinValue).Subtract(UnixInitialDateTime).TotalSeconds;

	/// <summary>
	/// Converts a Unix time to UTC <see cref="DateTime"/>.
	/// </summary>
	/// <param name="seconds">Value to convert in seconds.</param>
	/// <returns></returns>
	public static DateTime FromUnixToUtcDateTime(this long seconds)
		=> DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

	/// <summary>
	/// Converts a Unix time to UTC <see cref="DateTime"/>.
	/// </summary>
	/// <param name="milliseconds">Value to convert in milliseconds.</param>
	/// <returns></returns>
	public static DateTime FromUnixToUtcDateTimeMilliseconds(this long milliseconds)
		=> DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;

	// todo: refactor with C#11 number generic
	/// <summary>
	/// Get DateTime from unix timestamp.
	/// </summary>
	/// <param name="value">Unix timestamp e.g. e.g. 1512655587</param>
	public static DateTime ToDateTimeFromUnix(this int value)
		=> UnixInitialDateTime.AddSeconds(value).ToLocalTime();

	/// <summary>
	/// Get DateTime from unix timestamp.
	/// </summary>
	/// <param name="value">Unix timestamp e.g. e.g. 1512655587</param>
	public static DateTime ToDateTimeFromUnix(this long value)
		=> UnixInitialDateTime.AddSeconds(value).ToLocalTime();

	/// <summary>
	/// Get DateTime from unix timestamp.
	/// </summary>
	/// <param name="value">Unix timestamp e.g. e.g. 1512655587</param>
	public static DateTime ToDateTimeFromUnix(this double value)
		=> UnixInitialDateTime.AddSeconds(value).ToLocalTime();

	/// <summary>
	/// Get DateOnly from unix timestamp.
	/// </summary>
	/// <param name="value">Unix timestamp e.g. e.g. 1512655587</param>
	public static DateOnly ToDateOnlyFromUnix(this int value)
		=> DateOnly.FromDateTime(value.ToDateTimeFromUnix());

	/// <summary>
	/// Get DateOnly from unix timestamp.
	/// </summary>
	/// <param name="value">Unix timestamp e.g. e.g. 1512655587</param>
	public static DateOnly ToDateOnlyFromUnix(this long value)
		=> DateOnly.FromDateTime(value.ToDateTimeFromUnix());

	/// <summary>
	/// Get DateOnly from unix timestamp.
	/// </summary>
	/// <param name="value">Unix timestamp e.g. e.g. 1512655587</param>
	public static DateOnly ToDateOnlyFromUnix(this double value)
		=> DateOnly.FromDateTime(value.ToDateTimeFromUnix());

	/// <summary>
	/// Checks whether an offset is expired relative to now.
	/// </summary>
	/// <param name="offset"></param>
	/// <param name="padding">Default 10 minutes</param>
	/// <returns></returns>
	public static bool IsExpired(this DateTimeOffset offset, TimeSpan padding = default)
	{
		if (padding == default)
			padding = DefaultExpiryPadding;

		return offset - DateTimeOffset.UtcNow <= padding;
	}

	/// <summary>
	/// Converts DateTime to DateOnly.
	/// </summary>
	public static DateOnly ToDateOnly(this DateTime value)
		=> DateOnly.FromDateTime(value);

	/// <summary>
	/// Converts DateTime to DateOnly.
	/// </summary>
	public static DateOnly? ToDateOnly(this DateTime? dateTime)
		=> dateTime == null ? null : DateOnly.FromDateTime(dateTime.Value);

	/// <summary>
	/// Converts DateTime to TimeOnly.
	/// </summary>
	public static TimeOnly? ToTimeOnly(this DateTime? dateTime)
		=> dateTime == null ? null : TimeOnly.FromDateTime(dateTime.Value);

	/// <summary>
	/// Converts DateTime to TimeOnly.
	/// </summary>
	public static TimeOnly ToTimeOnly(this DateTime value)
		=> TimeOnly.FromDateTime(value);

	/// <summary>
	/// Creates a new DateTime equivalent to the provided DateTime but with the milliseconds set to 0.
	/// </summary>
	public static DateTime TrimMs(this DateTime dateTime)
		=> new(
			year: dateTime.Year,
			month: dateTime.Month,
			day: dateTime.Day,
			hour: dateTime.Hour,
			minute: dateTime.Minute,
			second: dateTime.Second,
			millisecond: 0,
			kind: dateTime.Kind
		);
}
