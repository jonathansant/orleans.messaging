namespace Odin.Core.Http;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class EndpointConfig
{
	private string DebuggerDisplay => $"Origin: '{Origin}', Path: '{Path}'";
	public string Origin { get; set; }
	public string Path { get; set; }

	public string GetUrl()
		=> !Path.IsNullOrEmpty() ? System.IO.Path.Combine(Origin, Path) : Origin;

	public override string ToString()
		=> DebuggerDisplay;
}
