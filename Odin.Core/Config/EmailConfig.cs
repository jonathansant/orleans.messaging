namespace Odin.Core.Config;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class EmailConfig
{
	protected string DebuggerDisplay => $"FromEmail: '{FromEmail}'";

	public string FromEmail { get; set; }
	public string AccessKey { get; set; }
	public string SecretKey { get; set; }
}
