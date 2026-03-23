namespace Odin.Orleans.Core;

public static class OdinOrleansCoreConst
{
	public static HashSet<string> KnownClientNamespaces = new HashSet<string>
	{
		"System",
		"System.Private.CoreLib",
		"System.Component",
		"Odin.Core",
		"Odin.Orleans.Client",
		"Odin.Shared.Contracts",
	};
}
