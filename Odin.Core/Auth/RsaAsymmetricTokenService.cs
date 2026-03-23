using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Odin.Core.Auth;

public interface IAsymmetricTokenService
{
	SigningCredentials GetSigningCredentialsFromPrivateKey(string privateKey);
	SecurityKey GetSecurityKeyFromPublicKey(string publicKey);
}

public class RsaAsymmetricTokenService : IAsymmetricTokenService
{
	private static readonly Regex KeyRegex = new(@"^(?:-+[A-Z\s]+-+)(.*?)(?:-+[A-Z\s]+-+)$");

	public SigningCredentials GetSigningCredentialsFromPrivateKey(string privateKey)
	{
		privateKey = FormatKey(privateKey);

		var keyBytes = Convert.FromBase64String(privateKey);

		var rsa = RSA.Create();
		rsa.ImportRSAPrivateKey(keyBytes, out _);

		var rsaSecurityKey = new RsaSecurityKey(rsa);

		return new(rsaSecurityKey, SecurityAlgorithms.RsaSha256)
		{
			CryptoProviderFactory = new() { CacheSignatureProviders = false }
		};
	}

	public SecurityKey GetSecurityKeyFromPublicKey(string publicKey)
	{
		publicKey = FormatKey(publicKey);

		var keyBytes = Convert.FromBase64String(publicKey);

		var rsa = RSA.Create();
		rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

		return new RsaSecurityKey(rsa);
	}

	private static string FormatKey(string key)
		=> KeyRegex.Replace(key, "$1");
}
