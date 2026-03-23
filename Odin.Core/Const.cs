namespace Odin.Core;

public static class Const
{
	// todo: make it configurable?
	public static HashSet<string> OwnedNamespaces = new()
	{
		"Odin",
		"Midgard",
		"Vor",
		"HorizonClient",
		"Vanir",
		"Heimdall",
		"Asgard",

		// external
		"SignalR.Orleans",
	};

	public static HashSet<string> CustomLocales = [];

	public const string Iso8601DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
	public const string Iso8601DateTimeFormatWithMilliseconds = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
	public const string TimestampMilliSecondsDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
}
