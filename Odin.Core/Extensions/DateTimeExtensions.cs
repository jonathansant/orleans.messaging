namespace Odin.Core;

public static class DateTimeExtensions
{
	public static string ToDashedDate(this DateTime date)
		=> date.ToString("yyyy-MM-dd");
	public static string ToDashedDate(this DateOnly date)
		=> date.ToString("yyyy-MM-dd");
}
