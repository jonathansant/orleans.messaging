using Odin.Core.Querying.Sorting;
using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

/// <summary>
/// Extension methods for building dynamic LINQ order by strings from OrderBy objects.
/// </summary>
public static class DynamicLinqOrderByBuilder
{
	// Cache for reflection results to avoid repeated expensive lookups
	// Key: (Type, Field), Value: true if field path contains a collection, false otherwise
	private static readonly ConcurrentDictionary<(Type, string), bool> _collectionDetectionCache = new();
	private static readonly Type _enumerableType = typeof(System.Collections.IEnumerable);
	private static readonly Type _stringType = typeof(string);
	/// <param name="orderBy">The collection of OrderBy objects to convert.</param>
	extension(IEnumerable<OrderBy>? orderBy)
	{
		/// <summary>
		/// Builds a dynamic LINQ order by string from a collection of OrderBy objects.
		/// </summary>
		/// <param name="parameters">Output parameter containing the query parameters.</param>
		/// <returns>A dynamic LINQ order by string, or null if input is empty.</returns>
		public string? ToDynamicLinq(out object?[] parameters)
		{
			var parameterList = new List<object?>();
			var orderByArray = orderBy as OrderBy[] ?? orderBy?.ToArray();

			if (orderByArray == null || orderByArray.Length == 0)
			{
				parameters = [];
				return null;
			}

			var expressions = new List<string>();
			var i = 0;

			while (i < orderByArray.Length)
			{
				var current = orderByArray[i];
				var hasCustomValues = current.Values is { Length: > 0 };

				if (hasCustomValues)
				{
					// Check if next OrderBy is for the same field without values (follow-up sort)
					var followUp = i + 1 < orderByArray.Length &&
								   orderByArray[i + 1].Field == current.Field &&
								   orderByArray[i + 1].Values is null or { Length: 0 }
						? orderByArray[++i]
						: (OrderBy?)null;

					expressions.Add(BuildCustomValueExpression(current, followUp, parameterList));
				}
				else
				{
					expressions.Add(BuildStandardExpression(current));
				}

				i++;
			}

			parameters = [.. parameterList];
			return string.Join(", ", expressions);
		}

		/// <summary>
		/// Builds a dynamic LINQ order by string without parameters output.
		/// </summary>
		public string? ToDynamicLinq() => orderBy.ToDynamicLinq(out _);
	}

	/// <param name="query">The queryable to order.</param>
	/// <typeparam name="T">The type of elements in the query.</typeparam>
	extension<T>(IQueryable<T> query)
	{
		/// <summary>
		/// Applies dynamic LINQ ordering to a queryable based on OrderBy objects.
		/// Automatically detects collection properties and applies appropriate aggregation.
		/// </summary>
		/// <param name="orderBy">The collection of OrderBy objects to apply.</param>
		/// <returns>The ordered queryable.</returns>
		public IQueryable<T> OrderBy(IEnumerable<OrderBy>? orderBy)
		{
			if (orderBy == null)
				return query;

			// Auto-detect collection properties and apply aggregation if not specified
			var processedOrderBy = orderBy
				.Select(o => AutoDetectCollectionAggregate(typeof(T), o))
				.ToArray();

			var orderByString = processedOrderBy.ToDynamicLinq(out var parameters);
			return string.IsNullOrEmpty(orderByString)
				? query
				: query.OrderBy(orderByString, parameters);
		}
	}

	private static string BuildStandardExpression(OrderBy orderBy)
	{
		var field = orderBy.Field;

		// Check if this is a collection property that needs aggregation
		if (!string.IsNullOrEmpty(orderBy.Aggregate))
		{
			// Split the field path to identify collection and property
			// e.g., "Heroes.ReleaseDate" becomes "Heroes.Max(ReleaseDate)"
			var lastDotIndex = field.LastIndexOf('.');
			if (lastDotIndex > 0)
			{
				var collectionPath = field[..lastDotIndex];
				var propertyPath = field[(lastDotIndex + 1)..];
				field = $"{collectionPath}.{orderBy.Aggregate}({propertyPath})";
			}
			else
			{
				// If no dot, apply aggregate directly
				field = $"{field}.{orderBy.Aggregate}()";
			}
		}

		return orderBy.Direction == SortDirection.Desc ? $"{field} desc" : field;
	}

	/// <summary>
	/// Builds a custom value ordering expression using nested conditional logic.
	/// Example: ["valla", "diablo", "kerrigan"] with Desc → kerrigan(0), diablo(1), valla(2), others(3)
	/// </summary>
	private static string BuildCustomValueExpression(OrderBy orderBy, OrderBy? followUp, List<object?> parameters)
	{
		// Build nested conditionals: (Field == value ? priority : (Field == value2 ? priority2 : ... default))
		var conditions = new List<string>();
		for (var i = 0; i < orderBy.Values!.Length; i++)
		{
			parameters.Add(orderBy.Values[i]);
			var priority = orderBy.Direction == SortDirection.Desc
				? orderBy.Values.Length - i - 1  // Reverse: last value gets priority 0
				: i;                              // Normal: first value gets priority 0

			conditions.Add($"({orderBy.Field} == @{parameters.Count - 1} ? {priority} : ");
		}

		// Non-matched values all get the same priority (N) to act as ties
		var expression = string.Join("", conditions) + orderBy.Values.Length + new string(')', conditions.Count);

		// Add follow-up sort for non-matched values if specified
		if (followUp.HasValue)
		{
			var direction = followUp.Value.Direction == SortDirection.Desc ? " desc" : "";
			return $"{expression}, {orderBy.Field}{direction}";
		}

		return expression;
	}

	/// <summary>
	/// Automatically detects if a field path contains a collection property and applies appropriate aggregation.
	/// Uses reflection to check property types along the path with caching for performance.
	/// </summary>
	/// <param name="rootType">The root type being queried.</param>
	/// <param name="orderBy">The OrderBy specification.</param>
	/// <returns>An OrderBy with Aggregate set if a collection is detected, otherwise the original.</returns>
	private static OrderBy AutoDetectCollectionAggregate(Type rootType, OrderBy orderBy)
	{
		// If aggregate is already specified, return as-is
		if (!string.IsNullOrEmpty(orderBy.Aggregate))
			return orderBy;

		// Check if field contains a dot (nested path)
		if (!orderBy.Field.Contains('.'))
			return orderBy; // No nested path, return original

		// Check cache first - key is (Type, Field) only, not direction
		var cacheKey = (rootType, orderBy.Field);
		if (_collectionDetectionCache.TryGetValue(cacheKey, out var hasCollection))
		{
			// Cache hit - apply appropriate aggregate based on direction if collection detected
			if (hasCollection)
			{
				var aggregate = orderBy.Direction == SortDirection.Desc ? "Max" : "Min";
				return orderBy with { Aggregate = aggregate };
			}
			return orderBy;
		}

		// Cache miss - detect if the field path contains a collection
		var isCollection = DetectCollectionInPath(rootType, orderBy.Field);
		_collectionDetectionCache.TryAdd(cacheKey, isCollection);

		if (isCollection)
		{
			var aggregate = orderBy.Direction == SortDirection.Desc ? "Max" : "Min";
			return orderBy with { Aggregate = aggregate };
		}

		return orderBy;
	}

	/// <summary>
	/// Core logic for detecting collection properties in a field path - separated for caching.
	/// </summary>
	/// <param name="rootType">The root type being queried.</param>
	/// <param name="field">The field path to check.</param>
	/// <returns>True if the field path contains a collection property, false otherwise.</returns>
	private static bool DetectCollectionInPath(Type rootType, string field)
	{
		// Use Span for efficient string splitting without allocation
		ReadOnlySpan<char> fieldSpan = field.AsSpan();
		var currentType = rootType;

		var lastDotIndex = fieldSpan.LastIndexOf('.');
		if (lastDotIndex <= 0)
			return false;

		// Process path segments (everything before the last property)
		var pathSpan = fieldSpan[..lastDotIndex];
		var startIndex = 0;

		while (startIndex < pathSpan.Length)
		{
			var dotIndex = pathSpan[startIndex..].IndexOf('.');
			var segmentSpan = dotIndex >= 0
				? pathSpan.Slice(startIndex, dotIndex)
				: pathSpan[startIndex..];

			var propertyName = segmentSpan.ToString();
			var property = currentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

			if (property == null)
				return false; // Property not found

			var propertyType = property.PropertyType;

			// Check if this property is a collection (but not string)
			if (propertyType != _stringType && _enumerableType.IsAssignableFrom(propertyType))
			{
				// Get the element type of the collection
				if (propertyType.IsGenericType)
				{
					var genericArgs = propertyType.GetGenericArguments();
					if (genericArgs.Length > 0)
					{
						// Found a collection, continue with element type
						currentType = genericArgs[0];
						startIndex += dotIndex >= 0 ? dotIndex + 1 : segmentSpan.Length;
						return true; // Collection found
					}
				}
				return false; // Non-generic collection
			}

			currentType = propertyType;
			startIndex += dotIndex >= 0 ? dotIndex + 1 : segmentSpan.Length;
		}

		return false;
	}
}
