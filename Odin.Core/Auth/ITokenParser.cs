using System.IdentityModel.Tokens.Jwt;

namespace Odin.Core.Auth;

/// <summary>
/// Interface for parsing security token into <see cref="IdentityToken"/>.
/// </summary>
public interface ITokenParser
{
	/// <summary>
	/// Parses security token and return identity token.
	/// </summary>
	/// <param name="securityToken">Security token to parse.</param>
	/// <returns>Returns identity token.</returns>
	IdentityToken Parse(JwtSecurityToken securityToken);
}
