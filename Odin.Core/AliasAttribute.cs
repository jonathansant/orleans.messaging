namespace Odin.Core;

[AttributeUsage(AttributeTargets.Field)]
public sealed class AliasAttribute : Attribute
{
	public string Alias { get; }

	public AliasAttribute(string alias)
	{
		Alias = alias;
	}
}
