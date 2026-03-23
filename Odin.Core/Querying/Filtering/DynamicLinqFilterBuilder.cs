using Odin.Core.Querying.Filtering;
using System.Linq.Dynamic.Core;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

/// <summary>
/// Extension methods for building dynamic LINQ query strings from LogicalNode filter expressions.
/// Uses System.Linq.Dynamic.Core library for runtime query execution.
/// </summary>
public static class DynamicLinqFilterBuilder
{
	/// <param name="filter">The filter expression to convert.</param>
	extension(LogicalFilterNode? filter)
	{
		/// <summary>
		/// Builds a dynamic LINQ query string from a filter expression.
		/// </summary>
		/// <param name="parameters">Output parameter containing the query parameters.</param>
		/// <returns>A dynamic LINQ query string.</returns>
		public string? ToDynamicLinq(out object?[] parameters)
		{
			var parameterList = new List<object?>();

			if (filter == null || filter.Children.Count == 0)
			{
				parameters = [];
				return null;
			}

			var query = BuildNode(filter, parameterList);
			parameters = [.. parameterList];
			return query;
		}
	}

	/// <param name="query">The queryable to filter.</param>
	/// <typeparam name="T">The type of elements in the query.</typeparam>
	extension<T>(IQueryable<T> query)
	{
		/// <summary>
		/// Applies dynamic LINQ filtering to a queryable based on a filter expression.
		/// </summary>
		/// <param name="filter">The filter expression to apply.</param>
		/// <returns>The filtered queryable.</returns>
		public IQueryable<T> Where(LogicalFilterNode? filter)
		{
			var filterString = filter.ToDynamicLinq(out var parameters);
			if (string.IsNullOrEmpty(filterString))
				return query;
			return query.Where(filterString, parameters);
		}
	}

	private static string BuildNode(FilterNode node, List<object?> parameters)
		=> node switch
		{
			LogicalFilterNode logical => BuildLogicalNode(logical, parameters),
			ComparisonFilterNode comparison => BuildComparisonNode(comparison, parameters),
			_ => throw new NotSupportedException($"Unknown node type: {node.GetType().Name}")
		};

	private static string BuildLogicalNode(LogicalFilterNode filterNode, List<object?> parameters)
	{
		switch (filterNode.Children.Count)
		{
			case 0:
				return "true";
			case 1:
				return BuildNode(filterNode.Children[0], parameters);
		}

		var op = filterNode.Operator == LogicalOperator.And ? " AND " : " OR ";
		var parts = filterNode.Children.Select(child => BuildNode(child, parameters));
		return $"({string.Join(op, parts)})";
	}

	private static string BuildComparisonNode(ComparisonFilterNode filterNode, List<object?> parameters)
	{
		var field = filterNode.Field;
		var op = filterNode.Operator;
		var value = filterNode.Value;
		var value2 = filterNode.Value2;

		return op switch
		{
			CompareOperator.Equals => BuildComparison(field, "==", value, parameters),
			CompareOperator.NotEquals => BuildComparison(field, "!=", value, parameters),
			CompareOperator.LessThan => BuildComparison(field, "<", value, parameters),
			CompareOperator.LessThanOrEqual => BuildComparison(field, "<=", value, parameters),
			CompareOperator.GreaterThan => BuildComparison(field, ">", value, parameters),
			CompareOperator.GreaterThanOrEqual => BuildComparison(field, ">=", value, parameters),

			CompareOperator.Contains => BuildStringOperation(field, "Contains", value, parameters),
			CompareOperator.NotContains => $"!{BuildStringOperation(field, "Contains", value, parameters)}",
			CompareOperator.StartsWith => BuildStringOperation(field, "StartsWith", value, parameters),
			CompareOperator.EndsWith => BuildStringOperation(field, "EndsWith", value, parameters),

			CompareOperator.Like => BuildLikeOperation(field, value, parameters),
			CompareOperator.NotLike => $"!{BuildLikeOperation(field, value, parameters)}",

			CompareOperator.Between => BuildBetweenOperation(field, value, value2, parameters),

			CompareOperator.In => BuildInOperation(field, value, parameters),
			CompareOperator.NotIn => $"!{BuildInOperation(field, value, parameters)}",

			CompareOperator.IsNull => $"{field} == null",
			CompareOperator.IsNotNull => $"{field} != null",

			// Collection operators
			CompareOperator.HasOneOf => BuildCollectionOperation(field, value, parameters, CollectionMatchType.HasOneOf),
			CompareOperator.HasAll => BuildCollectionOperation(field, value, parameters, CollectionMatchType.HasAll),
			CompareOperator.HasExactly => BuildCollectionOperation(field, value, parameters, CollectionMatchType.HasExactly),
			CompareOperator.HasNoneOf => $"!{BuildCollectionOperation(field, value, parameters, CollectionMatchType.HasOneOf)}",
			CompareOperator.IsEmpty => $"!{field}.Any()",
			CompareOperator.IsNotEmpty => $"{field}.Any()",

			_ => throw new NotSupportedException($"Operator {op} is not supported")
		};
	}

	private static string BuildComparison(string field, string op, object? value, List<object?> parameters)
	{
		var paramIndex = parameters.Count;
		parameters.Add(value);
		return $"{field} {op} @{paramIndex}";
	}

	private static string BuildStringOperation(string field, string method, object? value, List<object?> parameters)
	{
		var paramIndex = parameters.Count;
		parameters.Add(value);
		return $"{field}.{method}(@{paramIndex})";
	}

	private static string BuildLikeOperation(string field, object? value, List<object?> parameters)
	{
		if (value is not string pattern)
			throw new ArgumentException("LIKE operator requires a string pattern");

		// Convert SQL LIKE pattern to appropriate operation
		if (pattern.StartsWith("%") && pattern.EndsWith("%"))
		{
			// %text% -> Contains
			var text = pattern.Trim('%');
			return BuildStringOperation(field, "Contains", text, parameters);
		}
		else if (pattern.StartsWith("%"))
		{
			// %text -> EndsWith
			var text = pattern.TrimStart('%');
			return BuildStringOperation(field, "EndsWith", text, parameters);
		}
		else if (pattern.EndsWith("%"))
		{
			// text% -> StartsWith
			var text = pattern.TrimEnd('%');
			return BuildStringOperation(field, "StartsWith", text, parameters);
		}
		else
		{
			// Exact match
			return BuildComparison(field, "==", pattern, parameters);
		}
	}

	private static string BuildBetweenOperation(string field, object? from, object? to, List<object?> parameters)
	{
		var fromIndex = parameters.Count;
		parameters.Add(from);

		var toIndex = parameters.Count;
		parameters.Add(to);

		return $"({field} >= @{fromIndex} AND {field} <= @{toIndex})";
	}

	private static string BuildInOperation(string field, object? value, List<object?> parameters)
	{
		if (value is not IEnumerable<object> enumerable)
			throw new ArgumentException("IN operator requires a collection of values");

		var values = enumerable as ICollection<object> ?? enumerable.ToArray();

		// Build individual equality checks with OR conditions
		// This avoids the type compatibility issues with Contains
		var conditions = new List<string>();
		foreach (var val in values)
		{
			var paramIndex = parameters.Count;
			parameters.Add(val);
			conditions.Add($"{field} == @{paramIndex}");
		}

		return conditions.Count == 1 ? conditions[0] : $"({string.Join(" OR ", conditions)})";
	}

	private enum CollectionMatchType
	{
		HasOneOf,
		HasAll,
		HasExactly
	}

	private static string BuildCollectionOperation(string field, object? value, List<object?> parameters, CollectionMatchType matchType)
	{
		if (value is not IEnumerable<object> enumerable)
			throw new ArgumentException($"{matchType} operator requires a collection of values");

		var values = enumerable as ICollection<object> ?? enumerable.ToArray();

		// Extract the collection path and property name
		// E.g., "Tags.Tag.Name" -> collection: "Tags", property: "Tag.Name"
		var parts = field.Split('.');
		if (parts.Length < 2)
			throw new ArgumentException($"Collection field must have at least 2 parts: {field}");

		var collectionPath = parts[0];
		var propertyPath = string.Join(".", parts.Skip(1));

		switch (matchType)
		{
			case CollectionMatchType.HasOneOf:
				// For HasOneOf: at least one of the specified values exists in the collection
				// Tags.Any(x => @0.Contains(x.Tag.Key))
				{
					var paramIndex = parameters.Count;
					// Convert to array for Contains compatibility
					parameters.Add(values.ToArray());
					return $"{collectionPath}.Any(x => @{paramIndex}.Contains(x.{propertyPath}))";
				}

			case CollectionMatchType.HasAll:
				// For HasAll: all specified values exist in the collection (but can have more)
				// Build individual checks: Tags.Any(x => x.Tag.Key == @0) AND Tags.Any(x => x.Tag.Key == @1)
				{
					var conditions = new List<string>();
					foreach (var val in values)
					{
						var paramIndex = parameters.Count;
						parameters.Add(val);
						conditions.Add($"{collectionPath}.Any(x => x.{propertyPath} == @{paramIndex})");
					}
					return conditions.Count == 1 ? conditions[0] : $"({string.Join(" AND ", conditions)})";
				}

			case CollectionMatchType.HasExactly:
				// For HasExactly: collection contains exactly the specified values, no more, no less
				// Build count check and individual value checks
				{
					var conditions = new List<string>();

					// Add count check
					var countParamIndex = parameters.Count;
					parameters.Add(values.Count);
					conditions.Add($"{collectionPath}.Count() == @{countParamIndex}");

					// Add checks for each value
					foreach (var val in values)
					{
						var paramIndex = parameters.Count;
						parameters.Add(val);
						conditions.Add($"{collectionPath}.Any(x => x.{propertyPath} == @{paramIndex})");
					}

					return $"({string.Join(" AND ", conditions)})";
				}

			default:
				throw new NotSupportedException($"Collection match type {matchType} is not supported");
		}
	}
}
