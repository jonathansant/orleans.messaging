using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Odin.Core.Querying.Filtering;

[JsonConverter(typeof(StringEnumConverter))]
public enum FilterOperator
{
	[EnumMember(Value = "equals")]
	Equals = 0,
	[EnumMember(Value = "notEquals")]
	NotEquals,
	[EnumMember(Value = "lessThan")]
	LessThan,
	[EnumMember(Value = "lessThanOrEquals")]
	LessThanOrEquals,
	[EnumMember(Value = "greaterThan")]
	GreaterThan,
	[EnumMember(Value = "greaterThanOrEquals")]
	GreaterThanOrEquals,
	[EnumMember(Value = "contains")]
	Contains,
	[EnumMember(Value = "like")]
	Like,
	[EnumMember(Value = "isNot")]
	IsNot,
	[EnumMember(Value = "between")]
	Between
}

/// <summary>
/// A DTO for data retrieved from within filters (when searching within filters).
/// </summary>
/// <typeparam name="TItem"></typeparam>
[GenerateSerializer]
public record OdinFilterOperatorInput<TItem>(
	FilterOperator Operator,
	List<TItem>? Items
);

[GenerateSerializer]
public record OdinFilterOperatorInput
{
	[Id(0)]
	public FilterOperator Operator { get; set; }
	[Id(1)]
	public List<object> Items { get; set; }
}

// todo: consider removing generic version of class and build FilterBuilder which output OdinFilterInput (to avoid polymorphic serialization)
//[GenerateSerializer]
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public record OdinFilterInput
{
	//[Id(0)]
	public OdinFilterOperator And { get; set; } = [];

	//[Id(1)]
	public OdinFilterOperator Or { get; set; } = [];

	public bool IsEmpty() => And.IsNullOrEmpty() && Or.IsNullOrEmpty();
}

// todo: ideally use GenerateSerializer (and remove surrogate below) - https://github.com/dotnet/orleans/issues/8859
//[GenerateSerializer]
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class OdinFilterOperator : List<KeyValuePair<string, OdinFilterOperatorInput>>
{
	protected virtual string DebuggerDisplay
		=> $"[{string.Join(", ", this.Select(x => $"{x.Key} '{x.Value.Operator}' [{string.Join(", ", x.Value.Items)}]"))}]";
	public static OdinFilterOperator Empty { get; } = [];

	public OdinFilterOperator()
	{
	}

	public OdinFilterOperatorInput? this[string key]
		=> this
			.Where(x => x.Key == key)
			.Select(x => x.Value)
			.SingleOrDefault();

	public OdinFilterOperator(IEnumerable<KeyValuePair<string, OdinFilterOperatorInput>> items)
	{
		AddRange(items);
	}

	public OdinFilterOperator Add(string key, OdinFilterOperatorInput item)
	{
		base.Add(new(key, item));
		return this;
	}

	public static OdinFilterOperator CreateInstance(List<KeyValuePair<string, OdinFilterOperatorInput>> items)
		=> items.Any()
			? new(items)
			: Empty;

	public bool ContainsKey(string key)
		=> this.Any(x => string.Equals(x.Key, key, StringComparison.CurrentCultureIgnoreCase));
}

// todo: consider removing this class and build FilterBuilder which output OdinFilterInput (to avoid polymorphic serialization)
[GenerateSerializer]
public class OdinFilterOperator<T> : OdinFilterOperator
{
	public OdinFilterOperator()
	{
	}

	public OdinFilterOperator(IEnumerable<KeyValuePair<Expression<Func<T, object>>, OdinFilterOperatorInput>> items)
		: base(items.Select(x => new KeyValuePair<string, OdinFilterOperatorInput>(x.Key.NameOf(), x.Value)))
	{
	}

	public OdinFilterOperator<T> Add(Expression<Func<T, object>> keySelector, OdinFilterOperatorInput item)
	{
		Add(keySelector.NameOf(), item);
		return this;
	}

	public OdinFilterOperator<T> Add(FilterOperator op, Expression<Func<T, object>> keySelector, object? item)
		=> Add(keySelector,
			new()
			{
				Operator = op,
				Items = [item]
			}
		);
	public OdinFilterOperator<T> Add(FilterOperator op, Expression<Func<T, object>> keySelector, params object[]? items)
		=> Add(keySelector,
			new()
			{
				Operator = op,
				Items = items == null ? [null] : items.ToList()
			}
		);

	/// <summary>
	/// Adds a Between filter that matches values within the specified range (inclusive).
	/// </summary>
	/// <param name="keySelector">The property selector expression.</param>
	/// <param name="from">The lower bound of the range (inclusive).</param>
	/// <param name="to">The upper bound of the range (inclusive).</param>
	/// <returns>The filter operator with the Between condition added.</returns>
	public OdinFilterOperator<T> Between(Expression<Func<T, object>> keySelector, object from, object to)
		=> Add(keySelector,
			new()
			{
				Operator = FilterOperator.Between,
				Items = [from, to]
			}
		);

	public bool ContainsKey(Expression<Func<T, object>> key)
		=> ContainsKey(key.NameOf());
}

[GenerateSerializer]
public struct OdinFilterInputSurrogate
{
	[Id(0)]
	public OdinFilterOperator And;

	[Id(1)]
	public OdinFilterOperator Or;
}

[RegisterConverter]
public sealed class OdinFilterInputSurrogateConverter
	: IConverter<OdinFilterInput, OdinFilterInputSurrogate>
{
	public OdinFilterInput ConvertFromSurrogate(in OdinFilterInputSurrogate surrogate)
		=> new()
		{
			And = surrogate.And,
			Or = surrogate.Or
		};

	public OdinFilterInputSurrogate ConvertToSurrogate(in OdinFilterInput value)
		=> new()
		{
			// hack: to force downcast to non generic
			And = new(value.And),
			Or = new(value.Or),
		};
}

[GenerateSerializer]
public struct OdinFilterOperatorSurrogate
{
	[Id(0)]
	public List<KeyValuePair<string, OdinFilterOperatorInput>> Items;
}

[RegisterConverter]
public sealed class OdinFilterOperatorSurrogateConverter
	: IConverter<OdinFilterOperator, OdinFilterOperatorSurrogate>, IPopulator<OdinFilterOperator, OdinFilterOperatorSurrogate>
{
	public OdinFilterOperator ConvertFromSurrogate(in OdinFilterOperatorSurrogate surrogate)
		=> new(surrogate.Items);

	public OdinFilterOperatorSurrogate ConvertToSurrogate(in OdinFilterOperator value)
		// ReSharper disable once RedundantEnumerableCastCall - not redundant or surrogate will 🔥
		=> new() { Items = value.Cast<KeyValuePair<string, OdinFilterOperatorInput>>().ToList() };

	public void Populate(in OdinFilterOperatorSurrogate surrogate, OdinFilterOperator value)
		=> value.AddRange(surrogate.Items);
}
