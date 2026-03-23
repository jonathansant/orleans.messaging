namespace Odin.Core.Auth.Options;

public class BaseTokenOptions
{
	public string Issuer { get; set; }
	public string Audience { get; set; }
	public string SecurityKey { get; set; }
	public AsymmetricTokenOptions AsymmetricTokenOptions { get; set; }
	public bool UseAsymmetricToken { get; set; }
}

public record AsymmetricTokenOptions(string PrivateKey, string PublicKey);
