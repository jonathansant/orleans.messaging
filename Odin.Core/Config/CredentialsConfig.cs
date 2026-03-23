namespace Odin.Core.Config;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class CredentialsConfig
{
	protected string DebuggerDisplay => $"Username: '{Username}', Password: '{Password}'";

	public string Username { get; set; }
	public string Password { get; set; }
}
