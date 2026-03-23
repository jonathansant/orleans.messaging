namespace Odin.Logging.Serilog;

public static class LogPropertyNames
{
	// request
	public const string Device = "device";
	public const string RemoteIp = "remoteIp";
	public const string UserAgent = "userAgent";
	public const string CountryCode = "countryCode";
	public const string Locale = "locale";
	public const string CorrelationId = "correlationId";
	public const string RequestMethod = "requestMethod";
	public const string SessionId = "sessionId";
	public const string SessionType = "sessionType";
	public const string TestType = "testType";
	public const string TestFeature = "testFeature";
	public const string MockResponse = "mockResponse";
	public const string Fingerprint = "fingerprint";
	public const string SuspiciousIp = "suspiciousIp";
	public const string AffiliateId = "affiliateId";
	public const string Tags = "tags";

	// brand
	public const string Brand = "brand";
	public const string Organization = "organization";

	// user
	public const string ClientId = "clientId";
	public const string UserId = "userId";
	public const string User = "user";
	public const string Username = "username";
	public const string UserRegistrationCountryCode = "userRegistrationCountryCode";
	public const string CurrencyCode = "currencyCode";

	// orleans
	public const string Grain = "grain";
	public const string GrainPrimaryKey = "grainPrimaryKey";
	public const string GrainMethod = "grainMethod";

	// web
	public const string Gql = "gqlRequest";
	public const string GqlResultPerf = "gqlPerf";
	public const string GqlErrorCodes = "gqlErrorCode";

	// app
	public const string AppInstanceId = "appInstanceId";
	public const string App = "appName";
	public const string ServiceType = "serviceType";
	public const string Environment = "environment";
	public const string AppVersion = "appVersion";
	public const string GitCommit = "gitCommit";
	public const string ClusterGroup = "clusterGroup";
	public const string InfraClusterId = "infraClusterId";
	public const string ClusterId = "clusterId";
}
