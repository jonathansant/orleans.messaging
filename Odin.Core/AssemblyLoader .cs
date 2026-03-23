using System.Reflection;

namespace Odin.Core;

public static class AssemblyLoader
{
	public static void Load(IEnumerable<string> namespaces)
	{
		var entryAssembly = Assembly.GetEntryAssembly();
		var referencedAssemblies = entryAssembly.GetReferencedAssemblies().Where(x => namespaces.Any(n => x.Name.StartsWith(n)));

		foreach (var referencedAssembly in referencedAssemblies)
		{
			Assembly.Load(referencedAssembly);
		}
	}
}
