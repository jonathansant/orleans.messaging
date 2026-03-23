using System.Linq.Expressions;

namespace Odin.Core.Serialization;

public class OdinFileSerializerFieldBuilder<TModel>
{
	private readonly List<IOdinFileSerializerField<TModel>> _fields = [];

	/// <summary>
	/// Adds a field with both value transformation and name
	/// </summary>
	public OdinFileSerializerFieldBuilder<TModel> AddField<TProperty>(
		Expression<Func<TModel, TProperty>> selector,
		Func<TProperty, object>? value = null,
		string? name = null)
	{
		var field = new OdinFileSerializerField<TModel, TProperty>
		{
			TypedPropSelector = new(selector),
			ModifyPropValueFunc = value,
			RenamePropFunc = name == null
				? null
				: _ => name
		};

		_fields.Add(field);
		return this;
	}

	/// <summary>
	/// Builds the final list of fields
	/// </summary>
	public List<IOdinFileSerializerField<TModel>> Build() => _fields;
}

/// <summary>
/// Static factory for creating file serializer fields builders
/// </summary>
public static class OdinFileSerializerFieldBuilder
{
	public static OdinFileSerializerFieldBuilder<TModel> For<TModel>() => new();
}
