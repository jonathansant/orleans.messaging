using Odin.Core.Querying.Filtering;
using System.Linq.Expressions;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

/// <summary>
/// Extension methods providing convenient comparison and logical operations for FilterBuilder.
/// </summary>
public static class FilterBuilderExtensions
{
	extension(FilterBuilder builder)
	{
		/// <summary>
		/// Adds an equals condition.
		/// </summary>
		public FilterBuilder Eq(string field, object? value)
			=> builder.Where(field, CompareOperator.Equals, value);

		/// <summary>
		/// Adds a not equals condition.
		/// </summary>
		public FilterBuilder NotEquals(string field, object? value)
			=> builder.Where(field, CompareOperator.NotEquals, value);

		/// <summary>
		/// Adds a less than condition.
		/// </summary>
		public FilterBuilder LessThan(string field, object value)
			=> builder.Where(field, CompareOperator.LessThan, value);

		/// <summary>
		/// Adds a less than or equal condition.
		/// </summary>
		public FilterBuilder LessThanOrEqual(string field, object value)
			=> builder.Where(field, CompareOperator.LessThanOrEqual, value);

		/// <summary>
		/// Adds a greater than condition.
		/// </summary>
		public FilterBuilder GreaterThan(string field, object value)
			=> builder.Where(field, CompareOperator.GreaterThan, value);

		/// <summary>
		/// Adds a greater than or equal condition.
		/// </summary>
		public FilterBuilder GreaterThanOrEqual(string field, object value)
			=> builder.Where(field, CompareOperator.GreaterThanOrEqual, value);

		/// <summary>
		/// Adds a contains condition.
		/// </summary>
		public FilterBuilder Contains(string field, string value)
			=> builder.Where(field, CompareOperator.Contains, value);

		/// <summary>
		/// Adds a not contains condition.
		/// </summary>
		public FilterBuilder NotContains(string field, string value)
			=> builder.Where(field, CompareOperator.NotContains, value);

		/// <summary>
		/// Adds a starts with condition.
		/// </summary>
		public FilterBuilder StartsWith(string field, string value)
			=> builder.Where(field, CompareOperator.StartsWith, value);

		/// <summary>
		/// Adds an ends with condition.
		/// </summary>
		public FilterBuilder EndsWith(string field, string value)
			=> builder.Where(field, CompareOperator.EndsWith, value);

		/// <summary>
		/// Adds a like condition.
		/// </summary>
		public FilterBuilder Like(string field, string pattern)
			=> builder.Where(field, CompareOperator.Like, pattern);

		/// <summary>
		/// Adds a not like condition.
		/// </summary>
		public FilterBuilder NotLike(string field, string pattern)
			=> builder.Where(field, CompareOperator.NotLike, pattern);

		/// <summary>
		/// Adds an IN condition.
		/// </summary>
		public FilterBuilder In(string field, params ICollection<object> values)
			=> builder.Where(field, CompareOperator.In, values);

		/// <summary>
		/// Adds a NOT IN condition.
		/// </summary>
		public FilterBuilder NotIn(string field, params ICollection<object> values)
			=> builder.Where(field, CompareOperator.NotIn, values);

		/// <summary>
		/// Adds an IS NULL condition.
		/// </summary>
		public FilterBuilder Null(string field)
			=> builder.Where(field, CompareOperator.IsNull, null);

		/// <summary>
		/// Adds an IS NOT NULL condition.
		/// </summary>
		public FilterBuilder NotNull(string field)
			=> builder.Where(field, CompareOperator.IsNotNull, null);

		/// <summary>
		/// Adds a collection IS EMPTY condition.
		/// </summary>
		public FilterBuilder Empty(string field)
			=> builder.Where(field, CompareOperator.IsEmpty, null);

		/// <summary>
		/// Adds a collection IS NOT EMPTY condition.
		/// </summary>
		public FilterBuilder NotEmpty(string field)
			=> builder.Where(field, CompareOperator.IsNotEmpty, null);

		/// <summary>
		/// Adds a collection HAS ONE OF condition.
		/// </summary>
		public FilterBuilder HasOneOf(string field, params ICollection<object> values)
			=> builder.Where(field, CompareOperator.HasOneOf, values);

		/// <summary>
		/// Adds a collection HAS ALL condition.
		/// </summary>
		public FilterBuilder HasAll(string field, params ICollection<object> values)
			=> builder.Where(field, CompareOperator.HasAll, values);

		/// <summary>
		/// Adds a collection HAS EXACTLY condition.
		/// </summary>
		public FilterBuilder HasExactly(string field, params ICollection<object> values)
			=> builder.Where(field, CompareOperator.HasExactly, values);

		/// <summary>
		/// Adds a collection HAS NONE OF condition.
		/// </summary>
		public FilterBuilder HasNoneOf(string field, params ICollection<object> values)
			=> builder.Where(field, CompareOperator.HasNoneOf, values);

		/// <summary>
		/// Adds a nested AND group.
		/// </summary>
		public FilterBuilder And(Func<FilterBuilder, FilterBuilder> configure)
			=> builder.Group(configure, LogicalOperator.And);

		/// <summary>
		/// Adds a nested OR group.
		/// </summary>
		public FilterBuilder Or(Func<FilterBuilder, FilterBuilder> configure)
			=> builder.Group(configure, LogicalOperator.Or);
	}


	extension<T>(FilterBuilder<T> builder) where T : class
	{
		/// <summary>
		/// Adds an equals condition using a strongly-typed property selector.
		/// </summary>
		public FilterBuilder<T> Eq<TProp>(Expression<Func<T, TProp>> selector, TProp? value)
			=> builder.Where(selector, CompareOperator.Equals, value);

		/// <summary>
		/// Adds a not equals condition using a strongly-typed property selector.
		/// </summary>
		public FilterBuilder<T> NotEquals<TProp>(Expression<Func<T, TProp>> selector, TProp? value)
			=> builder.Where(selector, CompareOperator.NotEquals, value);

		/// <summary>
		/// Adds a less than condition using a strongly-typed property selector.
		/// </summary>
		public FilterBuilder<T> LessThan<TProp>(Expression<Func<T, TProp>> selector, TProp value)
			=> builder.Where(selector, CompareOperator.LessThan, value);

		/// <summary>
		/// Adds a less than or equal condition using a strongly-typed property selector.
		/// </summary>
		public FilterBuilder<T> LessThanOrEqual<TProp>(Expression<Func<T, TProp>> selector, TProp value)
			=> builder.Where(selector, CompareOperator.LessThanOrEqual, value);

		/// <summary>
		/// Adds a greater than condition using a strongly-typed property selector.
		/// </summary>
		public FilterBuilder<T> GreaterThan<TProp>(Expression<Func<T, TProp>> selector, TProp value)
			=> builder.Where(selector, CompareOperator.GreaterThan, value);

		/// <summary>
		/// Adds a greater than or equal condition using a strongly-typed property selector.
		/// </summary>
		public FilterBuilder<T> GreaterThanOrEqual<TProp>(Expression<Func<T, TProp>> selector, TProp value)
			=> builder.Where(selector, CompareOperator.GreaterThanOrEqual, value);

		/// <summary>
		/// Adds a contains condition for string properties.
		/// </summary>
		public FilterBuilder<T> Contains(Expression<Func<T, string>> selector, string value)
			=> builder.Where(selector, CompareOperator.Contains, value);

		/// <summary>
		/// Adds a not contains condition for string properties.
		/// </summary>
		public FilterBuilder<T> NotContains(Expression<Func<T, string>> selector, string value)
			=> builder.Where(selector, CompareOperator.NotContains, value);

		/// <summary>
		/// Adds a starts with condition for string properties.
		/// </summary>
		public FilterBuilder<T> StartsWith(Expression<Func<T, string>> selector, string value)
			=> builder.Where(selector, CompareOperator.StartsWith, value);

		/// <summary>
		/// Adds an ends with condition for string properties.
		/// </summary>
		public FilterBuilder<T> EndsWith(Expression<Func<T, string>> selector, string value)
			=> builder.Where(selector, CompareOperator.EndsWith, value);

		/// <summary>
		/// Adds a like condition for string properties.
		/// </summary>
		public FilterBuilder<T> Like(Expression<Func<T, string>> selector, string pattern)
			=> builder.Where(selector, CompareOperator.Like, pattern);

		/// <summary>
		/// Adds a not like condition for string properties.
		/// </summary>
		public FilterBuilder<T> NotLike(Expression<Func<T, string>> selector, string pattern)
			=> builder.Where(selector, CompareOperator.NotLike, pattern);

		/// <summary>
		/// Adds an IN condition with strongly-typed values.
		/// </summary>
		public FilterBuilder<T> In<TProp>(Expression<Func<T, TProp>> selector, params TProp[] values)
			=> builder.Where(selector, CompareOperator.In, values.Cast<object>().ToArray());

		/// <summary>
		/// Adds an IN condition with strongly-typed values from an enumerable.
		/// </summary>
		public FilterBuilder<T> In<TProp>(Expression<Func<T, TProp>> selector, IEnumerable<TProp> values)
			=> builder.Where(selector, CompareOperator.In, values.Cast<object>().ToArray());

		/// <summary>
		/// Adds a NOT IN condition with strongly-typed values.
		/// </summary>
		public FilterBuilder<T> NotIn<TProp>(Expression<Func<T, TProp>> selector, params TProp[] values)
			=> builder.Where(selector, CompareOperator.NotIn, values.Cast<object>().ToArray());

		/// <summary>
		/// Adds a NOT IN condition with strongly-typed values from an enumerable.
		/// </summary>
		public FilterBuilder<T> NotIn<TProp>(Expression<Func<T, TProp>> selector, IEnumerable<TProp> values)
			=> builder.Where(selector, CompareOperator.NotIn, values.Cast<object>().ToArray());

		/// <summary>
		/// Adds an IS NULL condition.
		/// </summary>
		public FilterBuilder<T> Null<TProp>(Expression<Func<T, TProp>> selector)
			=> builder.Where(selector, CompareOperator.IsNull, null);

		/// <summary>
		/// Adds an IS NOT NULL condition.
		/// </summary>
		public FilterBuilder<T> NotNull<TProp>(Expression<Func<T, TProp>> selector)
			=> builder.Where(selector, CompareOperator.IsNotNull, null);

		/// <summary>
		/// Adds a collection IS EMPTY condition.
		/// </summary>
		public FilterBuilder<T> Empty<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector)
			=> builder.Where(selector, CompareOperator.IsEmpty, null);

		/// <summary>
		/// Adds a collection IS NOT EMPTY condition.
		/// </summary>
		public FilterBuilder<T> NotEmpty<TItem>(Expression<Func<T, IEnumerable<TItem>>> selector)
			=> builder.Where(selector, CompareOperator.IsNotEmpty, null);

		/// <summary>
		/// Adds a collection HAS ONE OF condition with strongly-typed values.
		/// Checks if the collection contains at least one of the specified values.
		/// </summary>
		public FilterBuilder<T> HasOneOf<TItem, TProp>(
			Expression<Func<T, IEnumerable<TItem>>> collectionSelector,
			Expression<Func<TItem, TProp>> propertySelector,
			params TProp[] values
		) => builder.WhereCollection(collectionSelector, propertySelector, CompareOperator.HasOneOf, values);

		/// <summary>
		/// Adds a collection HAS ALL condition with strongly-typed values.
		/// Checks if the collection contains all of the specified values.
		/// </summary>
		public FilterBuilder<T> HasAll<TItem, TProp>(
			Expression<Func<T, IEnumerable<TItem>>> collectionSelector,
			Expression<Func<TItem, TProp>> propertySelector,
			params TProp[] values
		) => builder.WhereCollection(collectionSelector, propertySelector, CompareOperator.HasAll, values);

		/// <summary>
		/// Adds a collection HAS EXACTLY condition with strongly-typed values.
		/// Checks if the collection contains exactly the specified values.
		/// </summary>
		public FilterBuilder<T> HasExactly<TItem, TProp>(
			Expression<Func<T, IEnumerable<TItem>>> collectionSelector,
			Expression<Func<TItem, TProp>> propertySelector,
			params TProp[] values
		) => builder.WhereCollection(collectionSelector, propertySelector, CompareOperator.HasExactly, values);

		/// <summary>
		/// Adds a collection HAS NONE OF condition with strongly-typed values.
		/// Checks if the collection contains none of the specified values.
		/// </summary>
		public FilterBuilder<T> HasNoneOf<TItem, TProp>(
			Expression<Func<T, IEnumerable<TItem>>> collectionSelector,
			Expression<Func<TItem, TProp>> propertySelector,
			params TProp[] values
		) => builder.WhereCollection(collectionSelector, propertySelector, CompareOperator.HasNoneOf, values);

		/// <summary>
		/// Adds a nested AND group.
		/// </summary>
		public FilterBuilder<T> And(Func<FilterBuilder<T>, FilterBuilder<T>> configure)
			=> builder.Group(configure, LogicalOperator.And);

		/// <summary>
		/// Adds a nested OR group.
		/// </summary>
		public FilterBuilder<T> Or(Func<FilterBuilder<T>, FilterBuilder<T>> configure)
			=> builder.Group(configure, LogicalOperator.Or);

		/// <summary>
		/// Internal helper for collection operations.
		/// </summary>
		internal FilterBuilder<T> WhereCollection<TItem, TProp>(
			Expression<Func<T, IEnumerable<TItem>>> collectionSelector,
			Expression<Func<TItem, TProp>> propertySelector,
			CompareOperator op,
			TProp[] values
		)
		{
			var fieldPath = FilterBuilder<T>.GetCollectionPropertyPath(collectionSelector, propertySelector);
			return builder.Where(new(fieldPath, op, values.Cast<object>().ToArray()));
		}
	}
}