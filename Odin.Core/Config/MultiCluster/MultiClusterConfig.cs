namespace Odin.Core.Config.MultiCluster;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class MultiClusterConfig
{
	private string DebuggerDisplay => $"Default: '{Default}', Clusters: '{Clusters}'";

	/// <summary>
	/// Gets or sets the default cluster name.
	/// </summary>
	public string Default { get; set; }

	public Dictionary<string, ClusterConfig> Clusters { get; set; }
}

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ClusterConfig
{
	private string DebuggerDisplay => $"IsEnabled: '{IsEnabled}'";

	public bool IsEnabled { get; set; }
	public DynamicSection Sections { get; set; }
}
