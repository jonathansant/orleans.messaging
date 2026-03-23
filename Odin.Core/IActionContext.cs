using FluentlyHttpClient;
using Odin.Core.Auth;
using Odin.Core.Countries;
using Odin.Core.DeviceDetection;
using Odin.Core.Http.Lifecycle;
using Odin.Core.User;

namespace Odin.Core;

public interface IActionContext
{
	/// <summary>
	/// Ensures that entity is authenticated else it will throw.
	/// </summary>
	/// <param name="claim">Claim to ensure is present.</param>
	/// <param name="allowClaimValue">Claim value to allow.</param>
	/// <exception cref="UnauthorizedException">Thrown when user is not authenticated.</exception>
	void EnsureAuthenticated(string? claim = null, string? allowClaimValue = null);

	/// <summary>
	/// Gets or sets the request unique id for tracing.
	/// </summary>
	string CorrelationId { get; }

	DeviceType DeviceType { get; }

	/// <summary>
	/// Gets or sets the mock response for the action context
	/// </summary>
	string? MockResponse { get; }

	OdinActionRef ToRef();
}

public interface IRoleBasedActionContext
{
	public RoleBasedAccessControlContext? RbacContext { get; }
}

public interface IUserActionContext : IActionContext
{
	/// <summary>
	/// Get http headers from Action context.
	/// </summary>
	FluentHttpHeaders? Headers { get; }

	/// <summary>
	/// Gets the user agent from request.
	/// </summary>
	string UserAgent { get; }

	/// <summary>
	/// Gets the locale from request.
	/// </summary>
	string? Locale { get; }

	/// <summary>
	/// Gets the Language Code from request.
	/// </summary>
	string? LanguageCode { get; }

	/// <summary>
	/// Gets the iso country from request.
	/// </summary>
	IsoCountry? Country { get; }

	/// <summary>
	/// Gets the country code from request.
	/// </summary>
	string CountryCode { get; }

	/// <summary>
	/// Gets the auth token from request.
	/// </summary>
	string? AuthToken { get; }

	/// <summary>
	/// Gets the device type from request.
	/// </summary>
	DeviceType DeviceType { get; }

	/// <summary>
	/// Gets the request IP.
	/// </summary>
	string RemoteIp { get; }

	/// <summary>
	/// Gets or sets session type.
	/// </summary>
	string? SessionType { get; }

	/// <summary>
	/// Gets session id.
	/// </summary>
	string? SessionId { get; }

	/// <summary>
	/// Gets test type.
	/// </summary>
	string? TestType { get; }

	/// <summary>
	/// Gets fingerprint.
	/// </summary>
	string? Fingerprint { get; }

	/// <summary>
	/// Gets the user id (uuid) from user context.
	/// </summary>
	string? UserId { get; }

	/// <summary>
	/// Gets the auth client id.
	/// </summary>
	string? ClientId { get; }

	/// <summary>
	/// Gets the platform authentication token of the user.
	/// </summary>
	string? PlatformAuthToken { get; }

	/// <summary>
	/// Determine whether the user is authenticated or not.
	/// </summary>
	bool IsAuthenticated { get; }

	/// <summary>
	/// Gets the user context.
	/// </summary>
	IUserContext UserContext { get; }

	/// <summary>
	/// Gets the brand host that the request was made from.
	/// </summary>
	string Host { get; }

	string? AffiliateId { get; }

	HashSet<string>? Tags { get; }

	string? LoginSessionId { get; }
}