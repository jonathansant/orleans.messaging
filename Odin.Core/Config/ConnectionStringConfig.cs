using System.Text.RegularExpressions;

namespace Odin.Core.Config;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ConnectionStringConfig
{
	protected string DebuggerDisplay => $"ConnectionTemplate: '{ConnectionTemplate}', Server: '{Server}', Database: '{Database}'";

	public virtual string ConnectionTemplate { get; set; }

	public string Server { get; set; }

	public string Database { get; set; }

	public string BuildConnectionString(CredentialsConfig credentials)
	{
		var values = new Dictionary<string, object>(credentials.ToDictionary());
		values.AddRange(this.ToDictionary());

		return ConnectionTemplate.FromTemplate(values);
	}
}
