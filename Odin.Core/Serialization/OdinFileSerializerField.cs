using Odin.Core.Expressions;
using System.Linq.Expressions;

namespace Odin.Core.Serialization;

public interface IOdinFileSerializerField<TModel>
{
	ExpressionPropSelector<TModel, object> PropSelector { get; }
	object? ModifyPropValue(object? value);
	string RenameProp(string propName);
}

/// <summary>
/// Used to have access of TProperty when modifying or renaming properties.
/// </summary>
public record OdinFileSerializerField<TModel, TProperty> : IOdinFileSerializerField<TModel>
{
	public required ExpressionPropSelector<TModel, TProperty> TypedPropSelector { get; init; }

	public Func<TProperty?, object?>? ModifyPropValueFunc { get; set; }

	public Func<string, string>? RenamePropFunc { get; set; }

	// Interface implementation - converts typed selector to object selector
	public ExpressionPropSelector<TModel, object> PropSelector =>
		new(ConvertToObjectExpression(TypedPropSelector.Expression));

	public object? ModifyPropValue(object? value)
	{
		if (ModifyPropValueFunc == null) return value;

		if (value == null)
			return ModifyPropValueFunc((TProperty?)value);

		return value is TProperty typedValue ? ModifyPropValueFunc(typedValue) : value;
	}

	public string RenameProp(string value)
		=> RenamePropFunc == null
			? TypedPropSelector.Name
			: RenamePropFunc(value);

	private static Expression<Func<TModel, object>> ConvertToObjectExpression(
		Expression<Func<TModel, TProperty>> selector)
	{
		var param = selector.Parameters[0];
		var body = selector.Body;

		if (body.Type != typeof(object))
			body = Expression.Convert(body, typeof(object));

		return Expression.Lambda<Func<TModel, object>>(body, param);
	}
}
