namespace Orleans.Messaging.Memory;

public class MessagingMemoryPackageMeta
{
	/// <summary>
	/// Get package assembly.
	/// </summary>
	public static Assembly Assembly = typeof(MessagingMemoryPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static Version Version = Assembly.GetName().Version;
}
