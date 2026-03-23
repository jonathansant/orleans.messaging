using System.Collections.ObjectModel;
using System.Reflection;

namespace Odin.Core.TypeExplorerLocator;

[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
[AttributeUsage(AttributeTargets.Field)]
public sealed class ExplorerSkipAttribute : Attribute
{
}

public interface IExplorerModel
{
	public string Value { get; set; }
}

public interface IExplorerLocator<TAttribute, TModel, out TLocator>
	where TAttribute : Attribute
	where TModel : IExplorerModel
	where TLocator : IExplorerLocator<TAttribute, TModel, TLocator>
{
	/// <summary>
	/// Locate Type for the specified assembly.
	/// </summary>
	TLocator ForAssembly(Assembly assembly);

	/// <summary>
	/// Locate Type in assembly for the specified type.
	/// </summary>
	/// <typeparam name="T">Type to resolve assembly within.</typeparam>
	TLocator ForAssembly<T>();

	/// <summary>
	/// Locate Type for types specified.
	/// </summary>
	TLocator ForTypes(IEnumerable<Type> types);

	/// <summary>
	/// Check if that specific key exists.
	/// </summary>
	bool Has(string key);

	/// <summary>
	/// Get entity by key.
	/// </summary>
	TModel Get(string key);

	/// <summary>
	/// Get all available entities.
	/// </summary>
	ReadOnlyDictionary<string, TModel> GetAll();
}

public abstract class ExplorerLocator<TAttribute, TModel, TLocator> : IExplorerLocator<TAttribute, TModel, TLocator>
	where TAttribute : Attribute
	where TModel : IExplorerModel, new()
	where TLocator : IExplorerLocator<TAttribute, TModel, TLocator>
{
	private readonly Dictionary<string, TModel> _items = new();
	private ReadOnlyDictionary<string, TModel> _itemsReadOnly;

	public TLocator ForAssembly<T>()
		=> ForAssembly(typeof(T).GetTypeInfo().Assembly);

	public TLocator ForAssembly(Assembly assembly)
		=> ForTypes(assembly.GetTypes());

	/// <inheritdoc />
	public TLocator ForTypes(IEnumerable<Type> types)
	{
		_items.AddRange(ResolveValue(types).ToDictionary(x => x.Value));
		_itemsReadOnly = null;

		return (TLocator)(object)this;
	}

	public bool Has(string key)
		=> _items.ContainsKey(key);

	public TModel Get(string key)
	{
		_items.TryGetValue(key, out var value);
		return value;
	}

	public ReadOnlyDictionary<string, TModel> GetAll()
		=> _itemsReadOnly ??= new(_items);

	protected virtual IEnumerable<TModel> ResolveValue(IEnumerable<Type> types)
	{
		var typesWithAttribute = types
			.Select(x => new { Attribute = x.GetAttribute<TAttribute>(inherit: false), Type = x })
			.Where(x => x.Attribute != null);

		foreach (var kvp in typesWithAttribute)
		{
			foreach (var fieldInfo in kvp.Type.GetFields())
			{
				var shouldSkip = fieldInfo.GetCustomAttribute<ExplorerSkipAttribute>() != null;
				if (shouldSkip) continue;

				var value = fieldInfo.IsLiteral
					? (string)fieldInfo.GetRawConstantValue()
					: (string)fieldInfo.GetValue(null);

				yield return new()
				{
					Value = value
				};
			}
		}
	}
}
