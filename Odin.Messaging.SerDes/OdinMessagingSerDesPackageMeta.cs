using System.Reflection;

namespace Odin.Messaging.SerDes;

public class OdinMessagingSerDesPackageMeta
{
	/// <summary>
	/// Get package assembly.
	/// </summary>
	public static Assembly Assembly = typeof(OdinMessagingSerDesPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static Version Version = Assembly.GetName().Version;
}
