using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Odin.Core.Auth;

public class OdinTokenParser : ITokenParser
{
	public virtual IdentityToken Parse(JwtSecurityToken securityToken)
	{
		var expiryClaim = securityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Expiration) ?? throw new InvalidOperationException("expiry claim not found!");
		var userIdClaim = securityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("user id claim not found!");
		return new IdentityToken
		{
			UserId = userIdClaim.Value,
			Expiration = int.Parse(expiryClaim.Value)
		};
	}
}
