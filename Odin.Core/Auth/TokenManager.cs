using Microsoft.IdentityModel.Tokens;
using Odin.Core.Auth.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Odin.Core.Auth;

public interface ITokenManager
{
	string Generate(TokenAuthOptions tokenAuthOptions);
	ClaimsIdentity? Validate(TokenValidationOptions tokenValidationOptions);
	string? GetClaimValue(string claimType, string token);
	IdentityToken Parse(string token);
	bool CanReadToken(string token);
	JwtSecurityToken ReadToken(string token);
}

public class TokenManager : ITokenManager
{
	private readonly ITokenParser _tokenParser;
	private readonly IAsymmetricTokenService _asymmetricTokenService;
	private readonly JwtSecurityTokenHandler _handler;

	public TokenManager(
		ITokenParser tokenParser,
		IAsymmetricTokenService asymmetricTokenService
	)
	{
		_tokenParser = tokenParser;
		_asymmetricTokenService = asymmetricTokenService;
		_handler = new();
	}

	public string Generate(TokenAuthOptions tokenAuthOptions)
	{
		var identity = tokenAuthOptions.ClaimsIdentity;

		var requestAt = DateTime.Now;
		var expiresIn = requestAt + TimeSpan.FromMinutes(tokenAuthOptions.TokenExpirationMinutes);

		var signingCredentials = GetSigningCredentials(tokenAuthOptions);

		var securityToken = _handler.CreateToken(new SecurityTokenDescriptor
		{
			Audience = tokenAuthOptions.Audience,
			Issuer = tokenAuthOptions.Issuer,
			SigningCredentials = signingCredentials,
			Subject = identity,
			Expires = expiresIn
		});

		return _handler.WriteToken(securityToken);
	}

	public ClaimsIdentity? Validate(TokenValidationOptions tokenValidationOptions)
	{
		var validationResult = _handler.ValidateToken(tokenValidationOptions.Token, new TokenValidationParameters
		{
			IssuerSigningKey = tokenValidationOptions.UseAsymmetricToken
								? _asymmetricTokenService.GetSecurityKeyFromPublicKey(tokenValidationOptions.AsymmetricTokenOptions.PublicKey)
								: GetSymmetricSecurityKey(tokenValidationOptions.SecurityKey),
			ValidAudience = tokenValidationOptions.Audience,
			ValidIssuer = tokenValidationOptions.Issuer,

			// When receiving a token, check that it is still valid.
			ValidateLifetime = true,

			// This defines the maximum allowable clock skew - i.e.
			// provides a tolerance on the token expiry time
			// when validating the lifetime. As we're creating the tokens
			// locally and validating them on the same machines which
			// should have synchronised time, this can be set to zero.
			// Where external tokens are used, some leeway here could be
			// useful.
			ClockSkew = TimeSpan.FromMinutes(0),

			// Since the userId is enough, we do not need to identify the user by a username
			// and hence we can identify user and the Identity by the userId.
			// This option extracts the NameClaimType from the NameIdClaimType (userId)
			NameClaimType = ClaimTypes.NameIdentifier
		}, out _);

		return validationResult.Identities.FirstOrDefault();
	}

	public string? GetClaimValue(string claimType, string token)
	{
		var claims = ReadToken(token).Claims;
		var claim = claims.FirstOrDefault(x => x.Type == claimType);

		return claim?.Value;
	}

	public IdentityToken Parse(string token)
	{
		var jwtSecurity = _handler.ReadJwtToken(token);
		return _tokenParser.Parse(jwtSecurity);
	}

	public bool CanReadToken(string token)
		=> _handler.CanReadToken(token);

	public JwtSecurityToken ReadToken(string token)
		=> _handler.ReadJwtToken(token);

	private static SymmetricSecurityKey GetSymmetricSecurityKey(string tokenKey)
		=> new SymmetricSecurityKey(Encoding.ASCII.GetBytes(tokenKey));

	private SigningCredentials GetSigningCredentials(TokenAuthOptions tokenAuthOptions)
	{
		if (tokenAuthOptions.UseAsymmetricToken)
			return _asymmetricTokenService.GetSigningCredentialsFromPrivateKey(tokenAuthOptions.AsymmetricTokenOptions.PrivateKey);

		var securityKey = GetSymmetricSecurityKey(tokenAuthOptions.SecurityKey);
		return new SigningCredentials(
			securityKey,
			SecurityAlgorithms.HmacSha256Signature
		);
	}
}
