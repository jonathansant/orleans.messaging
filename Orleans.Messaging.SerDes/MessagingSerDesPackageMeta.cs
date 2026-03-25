using System.Reflection;

namespace Orleans.Messaging.SerDes;

public class MessagingSerDesPackageMeta
{
	/// <summary>
	/// Get package assembly.
	/// </summary>
	public static Assembly Assembly = typeof(MessagingSerDesPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static Version Version = Assembly.GetName().Version;
}
