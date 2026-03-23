using System.ComponentModel;

namespace Odin.Core.Querying.Filtering;

/// <summary>
/// Base class for all filter nodes in the expression tree.
/// </summary>
[GenerateSerializer]
public abstract record FilterNode
{
}

/// <summary>
/// Represents a logical operator (AND/OR) with child nodes.
/// </summary>
[GenerateSerializer]
public record LogicalFilterNode : FilterNode
{
	[Id(0)]
	public LogicalOperator Operator { get; set; }

	[Id(1)]
	public List<FilterNode> Children { get; set; } = [];

	public bool HasFilters => Children.Count > 0;

	public LogicalFilterNode(LogicalOperator op)
	{
		Operator = op;
	}

	public LogicalFilterNode(LogicalOperator op, params FilterNode[] children)
	{
		Operator = op;
		Children.AddRange(children);
	}

	public override string ToString()
	{
		switch (Children.Count)
		{
			case 0:
				return string.Empty;
			case 1:
				return Children[0].ToString() ?? string.Empty;
		}

		var separator = Operator == LogicalOperator.And ? " AND " : " OR ";
		var childStrings = Children
			.Select(c =>
			{
				var str = c.ToString() ?? string.Empty;
				// Wrap nested logical nodes in parentheses for clarity
				return c is LogicalFilterNode ? $"({str})" : str;
			})
			.Where(s => !string.IsNullOrEmpty(s));

		return string.Join(separator, childStrings);
	}
}

/// <summary>
/// Represents a comparison operation on a field.
/// </summary>
[GenerateSerializer]
public record ComparisonFilterNode : FilterNode
{
	[Id(0)]
	public string Field { get; set; } = string.Empty;

	[Id(1)]
	public CompareOperator Operator { get; set; }

	[Id(2)]
	public object? Value { get; set; }

	[Id(3)]
	public object? Value2 { get; set; } // For BETWEEN operator

	public ComparisonFilterNode() { }

	public ComparisonFilterNode(string field, CompareOperator op, object? value)
	{
		Field = field ?? throw new ArgumentNullException(nameof(field));
		Operator = op;
		Value = value;
	}

	public ComparisonFilterNode(string field, CompareOperator op, object? value, object? value2)
	{
		Field = field ?? throw new ArgumentNullException(nameof(field));
		Operator = op;
		Value = value;
		Value2 = value2;
	}

	public override string ToString()
	{
		var op = Operator switch
		{
			CompareOperator.Equals => "eq",
			CompareOperator.NotEquals => "ne",
			CompareOperator.LessThan => "lt",
			CompareOperator.LessThanOrEqual => "le",
			CompareOperator.GreaterThan => "gt",
			CompareOperator.GreaterThanOrEqual => "ge",
			CompareOperator.Contains => "contains",
			CompareOperator.NotContains => "not contains",
			CompareOperator.Like => "like",
			CompareOperator.NotLike => "not like",
			CompareOperator.StartsWith => "starts with",
			CompareOperator.EndsWith => "ends with",
			CompareOperator.Between => "between",
			CompareOperator.In => "in",
			CompareOperator.NotIn => "not in",
			CompareOperator.IsNull => "is null",
			CompareOperator.IsNotNull => "is not null",
			CompareOperator.HasOneOf => "has one of",
			CompareOperator.HasAll => "has all",
			CompareOperator.HasExactly => "has exactly",
			CompareOperator.HasNoneOf => "has none of",
			CompareOperator.IsEmpty => "is empty",
			CompareOperator.IsNotEmpty => "is not empty",
			_ => "?"
		};

		return Operator switch
		{
			// Handle operators that don't need a value
			CompareOperator.IsNull or CompareOperator.IsNotNull => $"{Field}: {op}",
			CompareOperator.IsEmpty or CompareOperator.IsNotEmpty => $"{Field}: {op}",
			// Handle BETWEEN operator
			CompareOperator.Between => $"{Field}: {op} {FormatValue(Value)} and {FormatValue(Value2)}",
			// Handle standard operators
			_ => $"{Field}: {op} {FormatValue(Value)}"
		};
	}

	private static string FormatValue(object? value)
		=> value switch
		{
			null => "null",
			string s => $"'{s}'",
			DateOnly @do => $"'{@do:yyyy-MM-dd}'",
			DateTime dt => $"'{dt:yyyy-MM-dd}'",
			DateTimeOffset dto => $"'{dto:yyyy-MM-dd}'",
			bool b => b.ToString().ToLowerInvariant(),
			Array arr => $"[{string.Join(", ", arr.Cast<object>().Select(FormatValue))}]",
			IEnumerable<object> enumerable => $"[{string.Join(", ", enumerable.Select(FormatValue))}]",
			_ => value.ToString() ?? "null"
		};
}

[Description("Logical operators for combining filter conditions.")]
public enum LogicalOperator
{
	And,
	Or
}

[Description("Comparison operators for field-level filtering.")]
public enum CompareOperator
{
	Equals,
	NotEquals,
	LessThan,
	LessThanOrEqual,
	GreaterThan,
	GreaterThanOrEqual,
	Contains,
	NotContains,
	Like,
	NotLike,
	StartsWith,
	EndsWith,
	Between,
	In,
	NotIn,
	IsNull,
	IsNotNull,
	// Collection operators
	HasOneOf,
	HasAll,
	HasExactly,
	HasNoneOf,
	IsEmpty,
	IsNotEmpty
}
