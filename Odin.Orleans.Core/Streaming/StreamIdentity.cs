namespace Odin.Orleans.Core.Streaming;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[GenerateSerializer]
public record StreamIdentity
{
	protected string DebuggerDisplay => $"ProviderName: '{ProviderName}', Namespace: '{Namespace}', Id: '{Id}'";

	[Id(0)]
	public string ProviderName { get; set; }
	[Id(1)]
	public string Namespace { get; set; }
	[Id(2)]
	public string Id { get; set; }

	public StreamIdentity WithId(string id)
	{
		Id = id;
		return this;
	}

	public StreamIdentity WithNamespace(string streamNamespace)
	{
		Namespace = streamNamespace;
		return this;
	}

	public StreamIdentity WithProviderName(string providerName)
	{
		ProviderName = providerName;
		return this;
	}

	public override string ToString() => $"{ProviderName}:{Namespace}-{Id}";
}
