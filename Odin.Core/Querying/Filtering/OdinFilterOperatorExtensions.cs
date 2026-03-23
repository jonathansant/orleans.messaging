using Odin.Core.Error;
using System.Linq.Expressions;

namespace Odin.Core.Querying.Filtering;

public static class OdinFilterOperatorExtensions
{
	// todo: pass an array as filter supports an array of items
	public static OdinFilterOperator Eq(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.Equals, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator NotEquals(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.NotEquals, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator LessThan(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.LessThan, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator LessThanOrEquals(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.LessThanOrEquals, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator GreaterThan(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.GreaterThan, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator GreaterThanOrEquals(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.GreaterThanOrEquals, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator Contains(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.Contains, Items = value.ToSingleList() });
		return search;
	}

	public static OdinFilterOperator Like(this OdinFilterOperator search, string key, object value)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.Like, Items = value.ToSingleList() });
		return search;
	}

	/// <summary>
	/// Adds a Between filter that matches values within the specified range (inclusive).
	/// </summary>
	/// <param name="search">The filter operator to add to.</param>
	/// <param name="key">The property name to filter on.</param>
	/// <param name="from">The lower bound of the range (inclusive).</param>
	/// <param name="to">The upper bound of the range (inclusive).</param>
	/// <returns>The filter operator with the Between condition added.</returns>
	public static OdinFilterOperator Between(this OdinFilterOperator search, string key, object from, object to)
	{
		if (search == null) throw new ArgumentNullException(nameof(search));

		search.Add(key, new() { Operator = FilterOperator.Between, Items = [from, to] });
		return search;
	}

	public static OdinFilterOperator Add(
		this OdinFilterOperator filter,
		string key,
		List<object> values,
		FilterOperator filterOperator = FilterOperator.Equals
	)
	{
		filter.Add(values.ToFilterOperatorInput(key, filterOperator));
		return filter;
	}

	public static OdinFilterOperator Add<T>(
		this OdinFilterOperator filter,
		Expression<Func<T, object>> keyExpression,
		List<object> values,
		FilterOperator filterOperator = FilterOperator.Equals
	) => Add(filter, keyExpression.GetPropertySelectorPath(x => x.ToCamelCase()), values, filterOperator);

	public static bool Contains(this OdinFilterOperator filterList, string key)
		=> filterList.Any(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

	[Obsolete("Use Get<T> instead")]
	public static List<string>? StringsOrDefault(this OdinFilterOperator filterOperator, string field, bool remove = true)
		=> filterOperator.InputOrDefault<string>(field, remove)?.Items;

	[Obsolete("Use Get<T> instead")]
	public static List<string> Strings(this OdinFilterOperator filterOperator, string field, bool remove = true)
	{
		var strings = filterOperator.StringsOrDefault(field, remove);
		if (strings is not { Count: > 0 })
			throw ErrorResult.RequiredFieldException(field);

		return strings;
	}

	public static OdinFilterOperatorInput<TItem>? InputOrDefault<TItem>(
		this OdinFilterOperator filterOperator,
		string field,
		bool remove = true
	) => filterOperator.SingleInputOrDefault(field, remove)?.To<TItem>();

	[Obsolete("Use Get<T> instead")]
	public static OdinFilterOperatorInput<TItem>? InputEnumOrDefault<TItem>(
		this OdinFilterOperator filterOperator,
		string field,
		bool remove = true
	) where TItem : struct
		=> filterOperator.SingleInputOrDefault(field, remove)?.ToEnum<TItem>();

	[Obsolete("Use Indexer instead")]
	public static OdinFilterOperatorInput? SingleInputOrDefault(this OdinFilterOperator filterOperator, string field, bool remove = true)
	{
		var operatorInputs = filterOperator.SearchForInputs(field, remove);

		return operatorInputs.Count == 0 ? null : operatorInputs.Single(field);
	}

	[Obsolete("Use Find instead")]
	public static List<OdinFilterOperatorInput> SearchForInputs(this OdinFilterOperator filterOperator, string field, bool remove = false)
	{
		var foundItems = filterOperator.FindByKey(field).ToList();

		if (foundItems.Count == 0)
			return [];

		if (remove)
			foundItems.ForEach(foundItem => filterOperator.Remove(foundItem));

		return foundItems.Select(kvp => kvp.Value).ToList();
	}

	[Obsolete("Use Indexer instead")]
	public static FilterOperator? OperatorFor(this OdinFilterOperator filterOperator, string field)
		=> filterOperator.SingleInputOrDefault(field, remove: false)?.Operator;

	/// <summary>
	/// Searches for the field and returns its operand if only one is found.
	/// </summary>
	/// <param name="filterOperator">The filter operator to search in</param>
	/// <param name="field">The field to search for</param>
	/// <param name="remove">If set removes the field from filter when found</param>
	[Obsolete("Use Indexer instead")]
	public static string String(this OdinFilterOperator filterOperator, string field, bool remove = true)
	{
		var refId = filterOperator.StringOrDefault(field, remove);

		return refId
			   ?? throw ErrorResult.AsValidationError()
				   .AddField(field, x => x.AsInvalidItemsAmount(0, minItems: 1, maxItems: 1))
				   .AsApiErrorException();
	}

	/// <summary>
	/// Gets a single operand from the And, and converts it to <see cref="TEnum"/>.
	/// </summary>
	[Obsolete("Use Get<T> instead")]
	public static TEnum? EnumOrDefault<TEnum>(this OdinFilterOperator odinFilterOperator, string field, bool remove = true)
		where TEnum : struct
	{
		var stringOperand = odinFilterOperator.StringOrDefault(field, remove);

		return stringOperand.ToEnumFromJsonOrDefault<TEnum>();
	}

	[Obsolete("Use Indexer instead")]
	public static string? StringOrDefault(this OdinFilterOperator filterOperator, string field, bool remove = true)
		=> filterOperator.SingleInputOrDefault(field, remove)?.StringOperands().Single(field);

	public static List<T> Get<T>(this OdinFilterOperator filterOperator, string field)
		=> filterOperator.Where(x => x.Key.Equals(field, StringComparison.OrdinalIgnoreCase))
			.SelectMany(result => result.Value.Items)
			.Cast<T>()
			.ToList();

	public static List<T> Get<T, TEntity>(this OdinFilterOperator filterOperator, Expression<Func<TEntity, object>> field)
		=> filterOperator.Get<T>(field.GetPropertySelectorPath(x => x.ToCamelCase()));

	public static List<OdinFilterOperatorInput> Find(this OdinFilterOperator filterOperator, string field)
		=> filterOperator
			.Where(x => x.Key.Equals(field, StringComparison.OrdinalIgnoreCase))
			.Select(x => x.Value)
			.ToList();
}

public static class OdinFilterInputExtensions
{
	public static OdinFilterInput Remove(this OdinFilterInput input, HashSet<string> keys)
	{
		if (keys.Count == 0)
			return input;

		input.And.RemoveAll(x => keys.Contains(x.Key));
		input.Or.RemoveAll(x => keys.Contains(x.Key));

		return input;
	}

	public static bool IsNullOrEmpty(this OdinFilterInput? input)
		=> input == null || (input.And.Count == 0 && input.Or.Count == 0);

	public static OdinFilterInput ReplaceItems(this OdinFilterInput filter, string key, Func<string, string> newItemFunc, string? newKey = null)
	{
		filter.Or.ReplaceItems(key, newItemFunc, newKey);
		filter.And.ReplaceItems(key, newItemFunc, newKey);
		return filter;
	}

	public static List<OdinFilterOperatorInput> Find(this OdinFilterInput filter, string field)
	{
		var andResults = filter.And.Find(field);
		var orResults = filter.Or.Find(field);
		return andResults.Union(orResults).ToList();
	}

	public static OdinFilterInput Subset(this OdinFilterInput filter, IEnumerable<string> fields)
	{
		var fieldsList = fields.ToList();
		return new()
		{
			And = filter.And.Subset(fieldsList),
			Or = filter.Or.Subset(fieldsList)
		};
	}
}

public static class OdinFilterOperatorInputExtensions
{
	public static bool IsSizeComparer(this OdinFilterOperatorInput operatorInput)
		=> operatorInput.IsLessThan()
		   || operatorInput.IsGreaterThan()
		   || operatorInput.IsLessThanOrEqual()
		   || operatorInput.IsGreaterThanOrEqual();

	public static bool IsLessThan(this OdinFilterOperatorInput operatorInput)
		=> operatorInput is { Operator: FilterOperator.LessThan };

	public static bool IsGreaterThan(this OdinFilterOperatorInput operatorInput)
		=> operatorInput is { Operator: FilterOperator.GreaterThan };

	public static bool IsLessThanOrEqual(this OdinFilterOperatorInput operatorInput)
		=> operatorInput is { Operator: FilterOperator.LessThanOrEquals };

	public static bool IsGreaterThanOrEqual(this OdinFilterOperatorInput operatorInput)
		=> operatorInput is { Operator: FilterOperator.GreaterThanOrEquals };

	public static bool IsBetween(this OdinFilterOperatorInput operatorInput)
		=> operatorInput is { Operator: FilterOperator.Between };

	/// <summary>
	/// Ensures only one operand exists in the input and returns it.
	/// </summary>
	public static object Single(this OdinFilterOperatorInput operatorInput, string field)
		=> operatorInput.Items.Single(field);

	public static OdinFilterOperatorInput<TItem> ToEnum<TItem>(this OdinFilterOperatorInput operatorInput)
		where TItem : struct
		=> new(
			operatorInput.Operator,
			operatorInput.Items.Select(x => ((string)x).ToEnumFromJson<TItem>()).ToList()
		);

	public static OdinFilterOperatorInput<TItem> To<TItem>(this OdinFilterOperatorInput operatorInput)
		=> new(
			operatorInput.Operator,
			operatorInput.Items.Cast<TItem>().ToList()
		);

	public static IEnumerable<string?> StringOperands(this OdinFilterOperatorInput operatorInput)
		=> operatorInput.Items.Cast<string?>();

	public static IEnumerable<string?> StringOperands(this IEnumerable<OdinFilterOperatorInput> odinFilterInputs)
		=> odinFilterInputs.Select(x => x.StringOperands()).Flatten();

	public static List<TEnum> EnumOperands<TEnum>(this OdinFilterOperatorInput operatorInput, string propName)
		where TEnum : Enum
		=> operatorInput.StringOperands().ToEnumList<TEnum>(propName).ToList();

	public static IEnumerable<KeyValuePair<string, OdinFilterOperatorInput>> FindByKey(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string key
	) => filterList.Where(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

	public static KeyValuePair<string, OdinFilterOperatorInput>? Remove(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string key
	)
	{
		var kvp = filterList.FindByKey(key).FirstOrDefault();

		if (kvp.Equals(default(KeyValuePair<string, OdinFilterOperatorInput>)))
			return null;

		filterList.Remove(kvp);
		return kvp;
	}

	public static bool KeyExists(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string key
	)
	{
		var kvp = filterList.FindByKey(key).FirstOrDefault();
		return !kvp.Equals(default(KeyValuePair<string, OdinFilterOperatorInput>));
	}

	/// <summary>
	/// Replaces the <see cref="OdinFilterOperatorInput"/> items for a given key, keeping the same <see cref="FilterOperator"/>> and key.
	/// </summary>
	public static bool ReplaceItems(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string key,
		List<string> newItems,
		string? newKey = null
	)
	{
		var existingFilterPair = filterList.Remove(key);

		if (existingFilterPair == null)
			return false;

		var newFilter = newItems.ToFilterOperatorInput(newKey ?? key, existingFilterPair.Value.Value.Operator);
		filterList.Add(newFilter);
		return true;
	}

	/// <summary>
	/// Replaces the <see cref="OdinFilterOperatorInput"/> items for a given key, keeping the same <see cref="FilterOperator"/>>.
	/// </summary>
	public static bool ReplaceItems(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string key,
		Func<string, string> newItemFunc,
		string? newKey = null
	)
	{
		var existingFilterPair = filterList.Remove(key);

		if (existingFilterPair == null)
			return false;

		var newItems = existingFilterPair.Value.Value.Items.Cast<string>().Select(newItemFunc);
		var newFilter = newItems.ToFilterOperatorInput(newKey ?? key, existingFilterPair.Value.Value.Operator);
		filterList.Add(newFilter);
		return true;
	}

	/// <summary>
	/// Replaces the key and the whole <see cref="OdinFilterOperatorInput"/> for a given key.
	/// </summary>
	public static void ReplaceByKey(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string oldKey,
		string newKey,
		OdinFilterOperatorInput newValue
	)
	{
		filterList.Remove(oldKey);
		filterList.Add(
			new(
				newKey,
				newValue
			)
		);
	}

	public static void UpdateByKey(
		this List<KeyValuePair<string, OdinFilterOperatorInput>> filterList,
		string key,
		OdinFilterOperatorInput newValue
	) => filterList.ReplaceByKey(key, key, newValue);

	public static IEnumerable<string> StringValues(this KeyValuePair<string, OdinFilterOperatorInput> filterOperatorInput)
		=> filterOperatorInput.Value.Items.Cast<string>();

	public static OdinFilterOperator Subset(this OdinFilterOperator filterOp, IEnumerable<string> fields)
	{
		var items = filterOp.Where(kvp => fields.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase));
		return OdinFilterOperator.CreateInstance(items.ToList());
	}
}
