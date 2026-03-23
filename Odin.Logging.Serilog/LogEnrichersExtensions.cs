using Odin.Core.Http.Lifecycle;
using Odin.Core.Logging;
using Serilog;

namespace Odin.Logging.Serilog;

public static class LogEnrichersExtensions
{
	/// <summary>
	/// Set diagnostics context based from request context.
	/// </summary>
	/// <remarks>Data which is not pushed in LogContext however they are included in the HTTP call. e.g. not passed for every log but at least logged once.</remarks>
	/// <param name="diagnosticContext"></param>
	/// <param name="requestContext"></param>
	public static void SetFromRequestContext(this IDiagnosticContext diagnosticContext, IApiRequestContext requestContext)
	{
		diagnosticContext.Set(LogPropertyNames.UserAgent, requestContext.UserAgent);
		diagnosticContext.Set(LogPropertyNames.Fingerprint, requestContext.Fingerprint);
	}

	/// <summary>
	/// Set diagnostics context based from Api request context.
	/// </summary>
	/// <param name="loggingContext"></param>
	/// <param name="requestContext"></param>
	public static void SetFromRequestContext(this ILoggingContext loggingContext, IApiRequestContext requestContext)
	{
		loggingContext.SetFromRequestContext((IRequestContext)requestContext);
		loggingContext.Set(LogPropertyNames.Locale, requestContext.Locale);
		loggingContext.Set(LogPropertyNames.SessionType, requestContext.SessionType);
		loggingContext.Set(LogPropertyNames.SessionId, requestContext.SessionId);
		loggingContext.Set(LogPropertyNames.SuspiciousIp, requestContext.SuspiciousIp);
		loggingContext.Set(LogPropertyNames.TestType, requestContext.TestType);
		loggingContext.Set(LogPropertyNames.TestFeature, requestContext.TestFeature);
		loggingContext.Set(LogPropertyNames.MockResponse, requestContext.MockResponse);
		loggingContext.Set(LogPropertyNames.Brand, requestContext.Brand);
		loggingContext.Set(LogPropertyNames.Organization, requestContext.Organization);
		loggingContext.Set(LogPropertyNames.AffiliateId, requestContext.AffiliateId);
		loggingContext.Set(LogPropertyNames.Tags, requestContext.Tags);
	}

	/// <summary>
	/// Set diagnostics context based from request context.
	/// </summary>
	/// <param name="loggingContext"></param>
	/// <param name="requestContext"></param>
	public static void SetFromRequestContext(this ILoggingContext loggingContext, IRequestContext requestContext)
	{
		loggingContext.Set(LogPropertyNames.Device, requestContext.DeviceType);
		loggingContext.Set(LogPropertyNames.RemoteIp, requestContext.RemoteIpAddress);
		loggingContext.Set(LogPropertyNames.CorrelationId, requestContext.CorrelationId);
		loggingContext.Set(LogPropertyNames.UserAgent, requestContext.UserAgent);
		loggingContext.Set(LogPropertyNames.CountryCode, requestContext.CountryCode);
	}
}
