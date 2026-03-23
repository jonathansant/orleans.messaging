using Odin.Core.DeviceDetection;
using Odin.Logging.Serilog;
using Orleans.Runtime;
using Serilog.Core;

namespace Odin.Orleans.Core;

public static partial class OdinOrleansRequestContext
{
	private const string IsExceptionConversionEnabledKey = "isExceptionConversionEnabled";
	private const string CorrelationIdKey = "correlationId";
	private const string SessionIdKey = "sessionId";
	private const string TagsKey = "tags";
	private const string UserIdKey = "userId";
	private const string UsernameKey = "username";
	private const string LoggingContextKey = "loggingContext";
	private const string TransactionKey = "transactionId";
	private const string AggregationKey = "aggregationId";
	private const string TenantKey = "tenant";
	private const string MockResponseKey = "mockResponse";
	private const string DeviceTypeKey = "deviceType";

	public static bool? GetExceptionConversionEnabled()
		=> RequestContext.Get(IsExceptionConversionEnabledKey) as bool?;

	public static void SetExceptionConversionEnabled(bool? value)
		=> RequestContext.Set(IsExceptionConversionEnabledKey, value);

	public static void RemoveExceptionConversionEnabled()
		=> RequestContext.Remove(IsExceptionConversionEnabledKey);

	public static string? GetTenant()
	{
		if (RequestContext.Get(TenantKey) is string value)
			return value;
		return null;
	}

	public static void SetTenant(string? value)
		=> RequestContext.Set(TenantKey, value);

	public static string? GetUserId()
	{
		if (RequestContext.Get(UserIdKey) is string id)
			return id;
		return null;
	}

	public static string? GetUsername()
	{
		if (RequestContext.Get(UsernameKey) is string username)
			return username;
		return null;
	}

	public static void SetUserId(string? value)
	{
		RequestContext.Set(UserIdKey, value);
		SetLoggingContextItem(LogPropertyNames.UserId, value);
	}

	public static void SetUsername(string? value)
	{
		RequestContext.Set(UsernameKey, value);
		SetLoggingContextItem(LogPropertyNames.Username, value);
	}

	public static string GetCorrelationId()
	{
		if (RequestContext.Get(CorrelationIdKey) is string id)
			return id;
		id = $"odin.orleans:{Guid.NewGuid()}";

		SetCorrelationId(id);
		return id;
	}

	public static string? GetMockResponse()
		=> RequestContext.Get(MockResponseKey) as string;

	public static DeviceType GetDeviceType()
		=> RequestContext.Get(DeviceTypeKey) as DeviceType? ?? DeviceType.Unknown;

	public static string? GetSessionId()
		=> RequestContext.Get(SessionIdKey) as string;

	public static string? GetOnlyCorrelationId()
	{
		if (RequestContext.Get(CorrelationIdKey) is string id)
			return id;
		return null;
	}

	public static void SetCorrelationId(string value)
	{
		RequestContext.Set(CorrelationIdKey, value);
		SetLoggingContextItem(LogPropertyNames.CorrelationId, value);
	}

	public static void SetMockResponse(string value)
	{
		RequestContext.Set(MockResponseKey, value);
		SetLoggingContextItem(LogPropertyNames.MockResponse, value);
	}

	public static void SetDeviceType(DeviceType value)
	{
		RequestContext.Set(DeviceTypeKey, value);
		SetLoggingContextItem(LogPropertyNames.Device, value);
	}

	public static void SetSessionId(string value)
	{
		RequestContext.Set(SessionIdKey, value);
		SetLoggingContextItem(LogPropertyNames.SessionId, value);
	}

	public static HashSet<string>? GetTags()
	{
		if (RequestContext.Get(TagsKey) is HashSet<string> id)
			return id;
		return null;
	}

	public static void SetTags(HashSet<string> value) => RequestContext.Set(TagsKey, value);//SetLoggingContextItem(LogPropertyNames.Tags, value);

	public static Dictionary<string, LogContextData>? GetLogContext()
		=> RequestContext.Get(LoggingContextKey) as Dictionary<string, LogContextData>;

	public static void SetLogContext(Dictionary<string, LogContextData> value)
		=> RequestContext.Set(LoggingContextKey, value);

	public static void SetLoggingContextItem(string propName, object? value, bool destructureObject = false)
		=> SetLoggingContextItem(new()
		{
			PropertyName = propName,
			Value = value,
			DestructureObject = destructureObject
		});

	public static void SetLoggingContextItem(LogContextData data)
	{
		var contextData = GetLogContext();
		contextData ??= new();
		contextData[data.PropertyName] = data;

		SetLogContext(contextData);
	}

	public static ILogEventEnricher[] GetLoggingEnrichers()
	{
		GetCorrelationId(); /* ensure correlationId is initialized */

		var contextData = GetLogContext();
		return contextData == null
			? Array.Empty<ILogEventEnricher>()
			: contextData.Values.Select(x => x.ToLogEventEnricher()).ToArray();
	}

	public static string? GetTransactionId()
	{
		if (RequestContext.Get(TransactionKey) is string id)
			return id;
		return null;
	}

	public static void SetTransactionId(string value)
	{
		RequestContext.Set(TransactionKey, value);
		SetLoggingContextItem(TransactionKey, value);
	}

	public static string? GetAggregationId()
	{
		if (RequestContext.Get(AggregationKey) is string id)
			return id;
		return null;
	}

	public static void SetAggregationId(string value)
	{
		RequestContext.Set(AggregationKey, value);
		SetLoggingContextItem(AggregationKey, value);
	}

	public static void ClearTransactionId()
	{
		if (RequestContext.Get(TransactionKey) is string id)
			RequestContext.Remove(TransactionKey);
	}
}
