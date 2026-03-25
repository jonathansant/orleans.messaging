namespace Orleans.Messaging;

public class MessagingPackageMeta
{
	/// <summary>
	/// Get package assembly.
	/// </summary>
	public static Assembly Assembly = typeof(MessagingPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static Version Version = Assembly.GetName().Version;
}
