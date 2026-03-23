using Odin.Core.TypeExplorerLocator;

namespace Odin.Core.Auth.Permissions;

/// <summary>
/// Attribute which marks class for discovery to resolve permissions from fields (const/static)
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class PermissionExplorerAttribute : Attribute
{
}

/// <summary>
/// Representation of a single error which was explored.
/// </summary>
public record PermissionExplorerModel : IExplorerModel
{
	/// <summary>
	/// Gets the permission name.
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// Gets the permission action type. eg 'create'.
	/// </summary>
	public string ActionType { get; set; }
}

public interface IPermissionLocator : IExplorerLocator<PermissionExplorerAttribute, PermissionExplorerModel, PermissionLocator>
{
}

public class PermissionLocator : ExplorerLocator<PermissionExplorerAttribute, PermissionExplorerModel, PermissionLocator>, IPermissionLocator
{
	protected override IEnumerable<PermissionExplorerModel> ResolveValue(IEnumerable<Type> types)
	{
		foreach (var entity in base.ResolveValue(types))
		{
			entity.ActionType = entity.Value.SubstringLastIndexOf('.');
			yield return entity;
		}
	}
}
