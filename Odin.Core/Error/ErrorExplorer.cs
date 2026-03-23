using Odin.Core.TypeExplorerLocator;

namespace Odin.Core.Error;

/// <summary>
/// Attribute which marks class for discovery (usually static) to resolve errors from fields (const/static)
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ErrorExplorerAttribute : Attribute
{
	private string DebuggerDisplay => $"Context: '{Context}'";

	/// <summary>
	/// Gets or sets the context (when not specified declare type name is used as default).
	/// </summary>
	public string Context { get; set; }
}

/// <summary>
/// Representation of a single error which was explored.
/// </summary>
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class ErrorExplorerModel : IExplorerModel
{
	protected string DebuggerDisplay => $"ErrorCode: '{Value}', Context: '{Context}', Source: '{Source}'";

	/// <summary>
	/// Gets the error code.
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// Gets the group context of the error.
	/// </summary>
	public string Context { get; set; }

	/// <summary>
	/// Gets the source of where the error is defined.
	/// </summary>
	public string Source { get; set; }
}

public interface IErrorExplorerLocator : IExplorerLocator<ErrorExplorerAttribute, ErrorExplorerModel, ErrorExplorerLocator>
{
	/// <summary>
	/// Get all available errors grouped by context.
	/// <param name="contextFilter">Context filtering.</param>
	/// </summary>
	IEnumerable<IGrouping<string, ErrorExplorerModel>> GetAllGrouped(string? contextFilter = null);
}

public class ErrorExplorerLocator : ExplorerLocator<ErrorExplorerAttribute, ErrorExplorerModel, ErrorExplorerLocator>, IErrorExplorerLocator
{
	/// <inheritdoc />
	public IEnumerable<IGrouping<string, ErrorExplorerModel>> GetAllGrouped(string? contextFilter = null)
	{
		var result = GetAll().Values.GroupBy(x => x.Context);

		if (!contextFilter.IsNullOrEmpty())
			result = result.Where(x => x.Key.Contains(contextFilter));
		return result;
	}

	protected override IEnumerable<ErrorExplorerModel> ResolveValue(IEnumerable<Type> types)
	{
		var typesWithAttribute = types
			.Select(x => new { Attribute = x.GetAttribute<ErrorExplorerAttribute>(inherit: false), Type = x })
			.Where(x => x.Attribute != null);

		foreach (var kvp in typesWithAttribute)
		{
			foreach (var fieldInfo in kvp.Type.GetFields())
			{
				var error = fieldInfo.IsLiteral
					? (string)fieldInfo.GetRawConstantValue()
					: (string)fieldInfo.GetValue(null);

				yield return new()
				{
					Value = error,
					Context = kvp.Attribute.Context.IfNullOrEmptyReturn(kvp.Type.GetDemystifiedName()),
					Source = $"{fieldInfo.DeclaringType}.{fieldInfo.Name}",
				};
			}
		}
	}
}
