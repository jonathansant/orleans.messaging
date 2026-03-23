namespace Odin.Core.Auth.Options;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class IdentityAuthConfig
{
	protected string DebuggerDisplay => $"ClientId: '{ClientId}', SecurityKey: '{SecurityKey}', Endpoint: '{Endpoint}'";

	public string SecurityKey { get; set; }
	public string ClientId { get; set; }
	public string Endpoint { get; set; }

	public List<string> Scopes { get; set; } = new List<string>();

	/// <summary>
	/// Ensure configuration is valid or throws.
	/// </summary>
	public void EnsureValid()
	{
		ClientId.IfNullOrEmptyThen(() => throw new ArgumentException($"{nameof(ClientId)} must be provided."));
		SecurityKey.IfNullOrEmptyThen(() => throw new ArgumentException($"{nameof(SecurityKey)} must be provided."));
	}
}
