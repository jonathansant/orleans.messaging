
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Logging;

public static class LoggerExtensions
{
	public static void Trace(this ILogger logger, string message, params object[] args)
		=> logger.LogTrace(message, args);

	public static void Debug(this ILogger logger, string message, params object[] args)
		=> logger.LogDebug(message, args);

	public static void Info(this ILogger logger, string message, params object[] args)
		=> logger.LogInformation(message, args);

	public static void Warn(this ILogger logger, string message, params object[] args)
		=> logger.LogWarning(message, args);
	public static void Warn(this ILogger logger, Exception ex, string message, params object[] args)
		=> logger.LogWarning(ex, message, args);

	public static void Error(this ILogger logger, string message, params object[] args)
		=> logger.LogError(message, args);
	public static void Error(this ILogger logger, Exception ex, string message, params object[] args)
		=> logger.LogError(ex, message, args);
	public static void Error(this ILogger logger, EventId eventId, Exception ex, string message, params object[] args)
		=> logger.LogError(eventId, ex, message, args);

	public static void Critical(this ILogger logger, Exception ex, string message, params object[] args)
		=> logger.LogCritical(ex, message, args);
	public static void Critical(this ILogger logger, string message, params object[] args)
		=> logger.LogCritical(message, args);
	public static void Critical(this ILogger logger, EventId eventId, Exception ex, string message, params object[] args)
		=> logger.LogCritical(eventId, ex, message, args);
}
