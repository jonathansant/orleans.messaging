namespace Odin.Core.Http;

public static class HttpHeaders
{
	// standard
	public const string AcceptLanguage = "Accept-Language";
	public const string ContentType = "Content-Type";
	public const string Authorization = "Authorization";
	public const string UserAgent = "User-Agent";
	public const string CacheControl = "Cache-Control";
	public const string ForwardedHost = "X-Forwarded-Host";

	public const string XCorrelationId = "X-Correlation-Id";
	public const string XForwardedFor = "X-Forwarded-For";
	public const string XFingerprint = "X-Fingerprint";
	public const string XOriginalForwardedFor = "X-Original-Forwarded-For";
	public const string XApiKey = "X-Api-Key";
	public const string XMockResponse = "X-Mock-Response";
	public const string XDeviceType = "X-Device-Type";

	// non standard
	public static class Odin
	{
		public const string Organization = "X-Odin-Organization";
		public const string Brand = "X-Odin-Brand";
		public const string DeviceType = "X-Odin-Device-Type";
		public const string CorrelationId = "X-Odin-Correlation-Id";
		public const string CountryCode = "X-Odin-Country-Code";
		public const string Locale = "X-Odin-Locale";
		public const string Super = "X-Odin-Super";
		public const string SessionId = "X-Odin-Session-Id";
		public const string SimulatedIp = "X-Odin-Simulated-Ip";
		public const string SessionType = "X-Odin-Session-Type";
		public const string TestType = "X-Odin-Test-Type";
		public const string RealIp = "X-Odin-Real-Ip";
		public const string TrustedSource = "X-Odin-Trusted-Source";
		public const string Fingerprint = "X-Odin-Fingerprint";
		public const string UserAgent = "X-Odin-User-Agent";
		public const string SuspiciousRequest = "X-Odin-Suspicious-Request";
		public const string TestFeature = "X-Odin-Test-Feature";
		public const string Tags = "X-Odin-Tags";
		public const string UserId = "X-Odin-User-Id";
		public const string Username = "X-Odin-Username";
		public const string AffiliateId = "X-Odin-Affiliate-Id";
		public const string AccountType = "X-Odin-Account-Type";
		public const string Jurisdiction = "X-Odin-Jurisdiction";
		public const string MockResponse = "X-Odin-Mock-Response";

		/// <summary>
		/// Gets the AuthScheme header - NOTE: this is used for signalr to pass different auth scheme.
		/// </summary>
		public const string AuthScheme = "X-Odin-Auth-Scheme";
	}

	// todo: move to Midgard - RequestContextMiddleware needs to be extendable
	public static class Midgard
	{
		public const string AffiliateId = "X-Mdg-Affiliate-Id";
		public const string ReturningVisitor = "X-Mdg-Returning-Visitor";
		public const string Registered = "X-Mdg-Registered";
		public const string UserSegments = "X-Mdg-User-Segments";
	}
}
