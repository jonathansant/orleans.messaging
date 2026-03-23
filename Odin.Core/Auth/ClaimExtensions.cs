using IdentityModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Odin.Core.Auth;

public static class ClaimsExtensions
{
	public static Claim? GetClaim(this JwtSecurityToken token, string key)
		=> token.Claims.Get(key);

	public static string? GetClaimValue(this JwtSecurityToken token, string key)
		=> token.GetClaim(key)?.Value;

	public static Claim? Get(this ClaimsIdentity identity, string key)
		=> identity.Claims.Get(key);

	public static string? GetValue(this ClaimsIdentity identity, string key, string? fallbackKey = null)
		=> identity.Claims.GetValue(key, fallbackKey);

	public static Claim? Get(this IEnumerable<Claim> claims, string key)
		=> claims.FirstOrDefault(x => x.Type == key);

	public static string? GetValue(this IEnumerable<Claim> claims, string key, string? fallbackKey = null)
	{
		var enumerable = claims as Claim[] ?? claims.ToArray();
		var claim = enumerable.Get(key) ?? (fallbackKey is not null ? enumerable.Get(fallbackKey) : null);
		return claim?.Value;
	}

	/// <summary>
	/// Get ClientId, optionally retrieves the top most value or as is clientId.
	/// </summary>
	/// <param name="claims">user claims</param>
	/// <param name="topMostValue">When value is 'true', this will return top most value eg: 'asg.client' => 'asg'</param>
	/// <returns>ClientId</returns>
	public static string? GetClientId(this IEnumerable<Claim> claims, bool topMostValue = false)
	{
		var clientId = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.ClientId)?.Value;

		if (!topMostValue || clientId.IsNullOrEmpty()) return clientId;

		var index = clientId.IndexOf(".");
		return index < 0 ? clientId : clientId.Substring(0, index);
	}
}
