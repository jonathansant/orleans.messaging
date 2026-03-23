namespace Odin.Core;

public static class BooleanExtensions
{
	public static string ToYesNoString(this bool value)
		=> value ? "Yes" : "No";

	public static bool IsYes(this string value)
		=> value.Equals("yes", StringComparison.OrdinalIgnoreCase);
}
