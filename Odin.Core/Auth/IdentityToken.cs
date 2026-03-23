namespace Odin.Core.Auth;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class IdentityToken
{
	protected string DebuggerDisplay => $"UserId: '{UserId}', Expiration: '{Expiration}'";

	/// <summary>
	/// Gets or sets user id.
	/// </summary>
	public string UserId { get; set; }

	/// <summary>
	/// Gets or sets token expiration.
	/// </summary>
	public long Expiration { get; set; }
}
