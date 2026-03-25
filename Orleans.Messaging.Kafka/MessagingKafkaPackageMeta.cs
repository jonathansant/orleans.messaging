namespace Orleans.Messaging.Kafka;

public class MessagingKafkaPackageMeta
{
	/// <summary>
	/// Get package assembly.
	/// </summary>
	public static Assembly Assembly = typeof(MessagingKafkaPackageMeta).Assembly;

	/// <summary>
	/// Gets the package version.
	/// </summary>
	public static Version Version = Assembly.GetName().Version;
}
