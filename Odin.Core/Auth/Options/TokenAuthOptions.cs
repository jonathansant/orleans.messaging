using System.Security.Claims;

namespace Odin.Core.Auth.Options;

[GenerateSerializer]
public class TokenAuthOptions : BaseTokenOptions
{
	[Id(0)]
	public int TokenExpirationMinutes { get; set; }
	[Id(1)]
	public ClaimsIdentity ClaimsIdentity { get; set; }
}
