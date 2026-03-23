using Microsoft.Extensions.Primitives;

namespace Odin.Core.AssemblyScanning;

/// <summary>
/// Add tag or tags to the class for discovery purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DiscoveryTagAttribute : Attribute
{
	public StringValues Tags { get; set; }

	public DiscoveryTagAttribute(string tag)
	{
		Tags = tag;
	}

	public DiscoveryTagAttribute(params string[] tags)
	{
		Tags = tags;
	}
}

/// <summary>
/// Mark for discovery to ignore this class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DiscoveryIgnoreAttribute : Attribute
{
}
