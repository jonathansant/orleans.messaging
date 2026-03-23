using IdentityModel;
using System.Collections.ObjectModel;

namespace Odin.Core.User;

public interface IUserContext
{
	/// <summary>
	/// Gets the user id.
	/// </summary>
	string? Id { get; set; }

	/// <summary>
	/// Gets the username of the user.
	/// </summary>
	string? Username { get; set; }

	/// <summary>
	/// Gets the auth clientId e.g. 'client_id' claim.
	/// </summary>
	string? ClientId { get; }

	/// <summary>
	/// Determine whether the user is authenticated or not.
	/// </summary>
	bool IsAuthenticated { get; }

	string LoginSessionId { get; }

	/// <summary>
	/// Gets the user claims.
	/// </summary>
	public IReadOnlyDictionary<string, List<string>> Claims { get; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public class UserContext : IUserContext
{
	private string DebuggerDisplay => $"IsAuthenticated: {IsAuthenticated}, Id: '{Id}', Username: '{Username}', ClientId: '{ClientId}'";

	[Id(0)]
	private ReadOnlyDictionary<string, List<string>>? _claimsReadOnly;

	[Id(1)]
	public string? Id { get; set; }

	[Id(2)]
	private Dictionary<string, List<string>> _claims = [];

	[Id(4)]
	public string LoginSessionId { get; set; }

	public Dictionary<string, List<string>> Claims
	{
		get => _claims;
		set
		{
			_claims = value;
			_claimsReadOnly = null;
		}
	}

	IReadOnlyDictionary<string, List<string>> IUserContext.Claims
		=> _claimsReadOnly ?? new ReadOnlyDictionary<string, List<string>>(Claims);

	[Id(3)]
	public string? Username { get; set; }

	public string? ClientId => Claims.GetValueOrDefault(JwtClaimTypes.ClientId)?.FirstOrDefault();

	public bool IsAuthenticated => !Id.IsNullOrEmpty() || !ClientId.IsNullOrEmpty();
}

public static class UserContextExtensions
{
	public static bool HasClaim(this IUserContext userContext, string claimType)
		=> userContext.Claims.ContainsKey(claimType);

	public static bool HasClaim(this IUserContext userContext, string claimType, string claimValueCheck)
		=> userContext.Claims.TryGetValue(claimType, out var claimValues) && claimValues.Contains(claimValueCheck);

	public static string? FindFirstClaim(this IUserContext userContext, string claimType)
		=> userContext.Claims.TryGetValue(claimType, out var claim) ? claim[0] : null;

	public static List<string>? FindClaim(this IUserContext userContext, string claimType)
		=> userContext.Claims.GetValueOrDefault(claimType);
}