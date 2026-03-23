using FluentlyHttpClient;
using Odin.Core.Auth;
using Odin.Core.DeviceDetection;
using Odin.Core.Json;

namespace Odin.Core.Http.Lifecycle;

// todo: perhaps make it extendable to have MidgardRequestContext and VorRequestContext etc...
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public class RequestContext : IRequestContext
{
	private string DebuggerDisplay => $"Locale: '{Locale}', UserAgent: '{UserAgent}', RemoteIpAddress: '{RemoteIpAddress}', CorrelationId: '{CorrelationId}', Token: '{Token}'";

	/// <summary>
	/// Gets or sets the locale e.g. 'en-GB'.
	/// </summary>
	[Id(0)]
	public string Locale { get; set; } = null!;

	/// <summary>
	/// Gets or sets the country code e.g. 'MT'.
	/// </summary>
	[Id(1)]
	public string CountryCode { get; set; } = null!;

	/// <summary>
	/// Gets or sets the auth token.
	/// </summary>
	[Id(2)]
	public string? Token { get; set; }

	/// <summary>
	/// Gets or sets the host.
	/// </summary>
	[Id(3)]
	public string Host { get; set; } = null!;

	/// <summary>
	/// Gets or sets the user agent.
	/// </summary>
	[Id(4)]
	public string UserAgent { get; set; } = null!;

	/// <summary>
	/// Gets or sets session id.
	/// </summary>
	[Id(5)]
	[HashIgnore]
	public string? SessionId { get; set; }

	/// <summary>
	/// Gets or sets test session type e.g. "test".
	/// </summary>
	[Id(6)]
	public string? SessionType { get; set; }

	/// <summary>
	/// Gets or sets the request remote ip address.
	/// </summary>
	[Id(7)]
	[HashIgnore]
	public string RemoteIpAddress { get; set; } = null!;

	/// <summary>
	/// Gets or sets the request unique id for tracing.
	/// </summary>
	[Id(8)]
	public string CorrelationId { get; set; } = null!;

	/// <summary>
	/// Gets or sets the device type.
	/// </summary>
	[Id(9)]
	public DeviceType DeviceType { get; set; }

	/// <summary>
	/// Gets or sets the test type e.g. "negative".
	/// </summary>
	[Id(10)]
	public string? TestType { get; set; }

	/// <summary>
	/// Gets or sets the test feature e.g. "kyc".
	/// </summary>
	[Id(11)]
	public string? TestFeature { get; set; }

	/// <summary>
	/// Gets or sets the fingerprint.
	/// </summary>
	[Id(12)]
	public string? Fingerprint { get; set; }

	/// <summary>
	/// Gets or sets suspicious ip.
	/// </summary>
	[Id(13)]
	public string? SuspiciousIp { get; set; }

	/// <summary>
	/// Gets or sets the headers.
	/// </summary>
	[Id(14)]
	[HashIgnore]
	public FluentHttpHeaders? Headers { get; set; }

	[Id(15)]
	public string? Organization { get; set; }

	[Id(16)]
	public string? Brand { get; set; }

	[Id(17)]
	public HashSet<string>? Tags { get; set; }

	[Id(18)]
	public string? AffiliateId { get; set; }

	[Id(19)]
	public string? Jurisdiction { get; set; }

	[Id(20)]
	public string? MockResponse { get; set; }

	[Id(21)]
	public RoleBasedAccessControlContext? RbacContext { get; set; }
}

public interface IRequestContext
{
	/// <summary>
	/// Gets or sets the country code e.g. 'MT'.
	/// </summary>
	string CountryCode { get; set; }

	/// <summary>
	/// Gets or sets the host.
	/// </summary>
	string Host { get; set; }

	/// <summary>
	/// Gets or sets the user agent.
	/// </summary>
	string UserAgent { get; set; }

	/// <summary>
	/// Gets or sets the request remote ip address.
	/// </summary>
	string RemoteIpAddress { get; set; }

	/// <summary>
	/// Gets or sets the request unique id for tracing.
	/// </summary>
	string CorrelationId { get; set; }

	/// <summary>
	/// Gets or sets the device type.
	/// </summary>
	DeviceType DeviceType { get; set; }

	/// <summary>
	/// Gets or sets the jurisdiction.
	/// </summary>
	string Jurisdiction { get; set; }

	/// <summary>
	/// Mock response, used for testing purposes.
	/// </summary>
	string? MockResponse { get; set; }

	RoleBasedAccessControlContext? RbacContext { get; set; }
}

[GenerateSerializer]
public class RoleBasedAccessControlContext
{
	[Id(0)]
	public HashSet<string> Permissions { get; set; } = new();
	[Id(1)]
	public HashSet<string> Roles { get; set; } = new();

	public bool HasClaim(string claim)
		=> Roles.Any(x => x == OdinRoleValues.SuperAdmin) || Permissions.Any(x => x == claim);

	public bool HasClaims(string[] claims)
		=> Roles.Any(x => x == OdinRoleValues.SuperAdmin) || claims.All(claim => Permissions.Contains(claim));

	public bool HasRole(string role)
		=> Roles.Any(x => x == role);
}

public interface IApiRequestContext : IRequestContext
{
	/// <summary>
	/// Gets or sets the locale e.g. 'en-GB'.
	/// </summary>
	string? Locale { get; set; }

	/// <summary>
	/// Gets or sets the auth token.
	/// </summary>
	string? Token { get; set; }

	/// <summary>
	/// Gets or sets session id.
	/// </summary>
	string? SessionId { get; set; }

	/// <summary>
	/// Gets or sets test session type e.g. "test".
	/// </summary>
	string? SessionType { get; set; }

	/// <summary>
	/// Gets or sets the test type e.g. "negative".
	/// </summary>
	string? TestType { get; set; }

	/// <summary>
	/// Gets or sets the test feature e.g. "kyc".
	/// </summary>
	string? TestFeature { get; set; }

	/// <summary>
	/// Gets or sets the fingerprint.
	/// </summary>
	string? Fingerprint { get; set; }

	/// <summary>
	/// Gets or sets suspicious ip.
	/// </summary>
	string? SuspiciousIp { get; set; }

	/// <summary>
	/// Forwarded mock response, used for testing purposes.
	/// </summary>
	string? MockResponse { get; set; }

	/// <summary>
	/// Gets or sets the headers.
	/// </summary>
	FluentHttpHeaders? Headers { get; set; }

	string? Organization { get; set; }

	string? Brand { get; set; }

	HashSet<string>? Tags { get; set; }

	string? AffiliateId { get; set; }

	/// <summary>
	/// Gets or sets the origin.
	/// </summary>
	string Origin { get; set; }
}
