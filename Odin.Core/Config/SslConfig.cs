namespace Odin.Core.Config;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class SslConfig
{
	protected string DebuggerDisplay => $"Path: '{Path}', CertificateFilename: '{CertificateFilename}', KeyFilename: '{KeyFilename}'";

	public string Path { get; set; }
	public string CertificateFilename { get; set; }
	public string KeyFilename { get; set; }
}
