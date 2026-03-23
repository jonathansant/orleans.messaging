using System.Linq.Expressions;

namespace Odin.Core.Querying.Filtering;

/// <summary>
/// Base class for filter builders providing common functionality.
/// </summary>
public abstract class FilterBuilderBase
{
	protected readonly LogicalFilterNode Root;

	protected FilterBuilderBase(LogicalOperator rootOperator)
	{
		Root = new(rootOperator);
	}

	protected FilterBuilderBase(LogicalFilterNode root)
	{
		Root = root;
	}

	/// <summary>
	/// Builds the final filter expression.
	/// </summary>
	public LogicalFilterNode Build() => Root;

	/// <summary>
	/// Deep clones a LogicalNode and all its children.
	/// </summary>
	protected static LogicalFilterNode CloneLogicalNode(LogicalFilterNode filterNode)
	{
		ArgumentNullException.ThrowIfNull(filterNode);
		var cloned = new LogicalFilterNode(filterNode.Operator);
		foreach (var child in filterNode.Children)
		{
			cloned.Children.Add(CloneFilterNode(child));
		}
		return cloned;
	}

	/// <summary>
	/// Deep clones a FilterNode (handles both LogicalNode and ComparisonNode).
	/// </summary>
	protected static FilterNode CloneFilterNode(FilterNode node)
		=> node switch
		{
			LogicalFilterNode logical => CloneLogicalNode(logical),
			ComparisonFilterNode comparison => new ComparisonFilterNode(comparison.Field, comparison.Operator, comparison.Value, comparison.Value2),
			_ => throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}")
		};
}

/// <summary>
/// Fluent builder for constructing filter expressions.
/// </summary>
public class FilterBuilder : FilterBuilderBase
{
	private FilterBuilder(LogicalOperator rootOperator) : base(rootOperator)
	{
	}

	/// <summary>
	/// Creates a builder starting from an existing LogicalNode.
	/// The node and its children are cloned to prevent mutation of the original.
	/// </summary>
	public FilterBuilder(LogicalFilterNode filterNode) : base(CloneLogicalNode(filterNode))
	{
	}

	/// <summary>
	/// Creates a builder with AND as the root operator.
	/// </summary>
	public static FilterBuilder And() => new(LogicalOperator.And);

	/// <summary>
	/// Creates a builder with OR as the root operator.
	/// </summary>
	public static FilterBuilder Or() => new(LogicalOperator.Or);

	/// <summary>
	/// Adds a comparison condition.
	/// </summary>
	public FilterBuilder Where(ComparisonFilterNode filterNode)
	{
		Root.Children.Add(filterNode);
		return this;
	}

	/// <summary>
	/// Adds a comparison condition.
	/// </summary>
	public FilterBuilder Where(string field, CompareOperator op, object? value)
		=> Where(new(field, op, value));

	/// <summary>
	/// Adds a BETWEEN condition.
	/// </summary>
	public FilterBuilder Between(string field, object? from, object? to)
		=> Where(new(field, CompareOperator.Between, from, to));

	/// <summary>
	/// Adds a nested logical group.
	/// </summary>
	public FilterBuilder Group(Func<FilterBuilder, FilterBuilder> configure, LogicalOperator op = LogicalOperator.And)
	{
		var builder = new FilterBuilder(op);
		configure(builder);
		Root.Children.Add(builder.Root);
		return this;
	}
}

/// <summary>
/// Strongly-typed builder for filter expressions with compile-time type safety.
/// Converts expressions to property paths for use with DynamicLinqFilterBuilder.
/// </summary>
/// <typeparam name="T">The entity type being filtered.</typeparam>
// todo: consider adding constraints to T if needed (e.g., class)
public class FilterBuilder<T> : FilterBuilderBase// where T : class
{
	private FilterBuilder(LogicalOperator rootOperator) : base(rootOperator)
	{
	}

	/// <summary>
	/// Creates a builder starting from an existing LogicalNode.
	/// The node and its children are cloned to prevent mutation of the original.
	/// </summary>
	public FilterBuilder(LogicalFilterNode filterNode) : base(CloneLogicalNode(filterNode))
	{
	}

	/// <summary>
	/// Creates a builder with AND as the root operator.
	/// </summary>
	public static FilterBuilder<T> And() => new(LogicalOperator.And);

	/// <summary>
	/// Creates a builder with OR as the root operator.
	/// </summary>
	public static FilterBuilder<T> Or() => new(LogicalOperator.Or);

	/// <summary>
	/// Adds a comparison condition.
	/// </summary>
	public FilterBuilder<T> Where(ComparisonFilterNode filterNode)
	{
		Root.Children.Add(filterNode);
		return this;
	}

	/// <summary>
	/// Adds a comparison condition using a strongly-typed property selector.
	/// </summary>
	public FilterBuilder<T> Where<TProp>(Expression<Func<T, TProp>> selector, CompareOperator op, object? value)
	{
		var fieldPath = GetPropertyPath(selector);
		return Where(new(fieldPath, op, value));
	}

	/// <summary>
	/// Adds a BETWEEN condition using a strongly-typed property selector.
	/// </summary>
	public FilterBuilder<T> Between<TProp>(Expression<Func<T, TProp>> selector, object? from, object? to)
	{
		var fieldPath = GetPropertyPath(selector);
		return Where(new(fieldPath, CompareOperator.Between, from, to));
	}

	/// <summary>
	/// Adds a nested logical group.
	/// </summary>
	public FilterBuilder<T> Group(Func<FilterBuilder<T>, FilterBuilder<T>> configure, LogicalOperator op = LogicalOperator.And)
	{
		var builder = new FilterBuilder<T>(op);
		configure(builder);
		Root.Children.Add(builder.Root);
		return this;
	}

	// todo: move in Expression extensions
	/// <summary>
	/// Extracts the property path from an expression in an optimized way.
	/// Handles nested properties like x => x.Universe.Key -> "Universe.Key"
	/// </summary>
	internal static string GetPropertyPath<TProp>(Expression<Func<T, TProp>> selector)
	{
		// Fast path for simple member access
		if (selector.Body is MemberExpression memberExpr)
			return BuildPropertyPath(memberExpr);

		// Handle conversions (e.g., object to string, nullable to value type)
		if (selector.Body is UnaryExpression
			{
				NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
				Operand: MemberExpression unaryMember
			})
			return BuildPropertyPath(unaryMember);

		throw new ArgumentException(
			$"Expression '{selector}' is not a valid property selector. Only simple property access is supported.",
			nameof(selector));
	}

	/// <summary>
	/// Extracts the collection and property path for collection operations.
	/// Example: collection: x => x.Tags, property: t => t.Tag.Key -> "Tags.Tag.Key"
	/// </summary>
	internal static string GetCollectionPropertyPath<TItem, TProp>(
		Expression<Func<T, IEnumerable<TItem>>> collectionSelector,
		Expression<Func<TItem, TProp>> propertySelector
	)
	{
		var collectionPath = GetPropertyPath(collectionSelector);
		var propertyPath = ExtractPropertyPath(propertySelector);
		return $"{collectionPath}.{propertyPath}";
	}

	/// <summary>
	/// Extracts the property path from a generic expression (used for collection item selectors).
	/// </summary>
	private static string ExtractPropertyPath<TSource, TProp>(Expression<Func<TSource, TProp>> selector)
	{
		// Fast path for simple member access
		if (selector.Body is MemberExpression memberExpr)
			return BuildPropertyPath(memberExpr);

		// Handle conversions
		if (selector.Body is UnaryExpression
			{
				NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked,
				Operand: MemberExpression unaryMember
			})
			return BuildPropertyPath(unaryMember);

		throw new ArgumentException(
			$"Expression '{selector}' is not a valid property selector. Only simple property access is supported.",
			nameof(selector));
	}

	/// <summary>
	/// Builds the property path from a member expression efficiently.
	/// Uses a stack-based approach to avoid recursion and string concatenation.
	/// </summary>
	private static string BuildPropertyPath(MemberExpression memberExpression)
	{
		// Use a list to collect property names in reverse order
		var properties = new List<string>(4); // Pre-allocate for typical depth

		var current = memberExpression;
		while (current != null)
		{
			properties.Add(current.Member.Name);
			current = current.Expression as MemberExpression;
		}

		// Reverse and join with '.' separator
		// For single property, avoid allocation
		if (properties.Count == 1)
			return properties[0];

		// Build path from end to start (reversing the list)
		properties.Reverse();
		return string.Join(".", properties);
	}
}
