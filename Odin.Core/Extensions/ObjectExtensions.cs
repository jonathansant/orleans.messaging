using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Odin.Core.Error;
using Odin.Core.Json;
using System.Collections;
using System.Data.HashFunction;
using System.Data.HashFunction.xxHash;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Odin.Core;

public static class ObjectExtensions
{
	internal static readonly IxxHash HashFunction = StringExtensions.HashFunction;

	private static readonly JsonSerializerSettings CloneSerializerSettings = new()
	{
		TypeNameHandling = TypeNameHandling.All,
		PreserveReferencesHandling = PreserveReferencesHandling.Objects,
		DateFormatHandling = DateFormatHandling.IsoDateFormat,
		DefaultValueHandling = DefaultValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		NullValueHandling = NullValueHandling.Ignore,
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
		TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
		Formatting = Formatting.None,
		ContractResolver = new DefaultContractResolver
		{
			NamingStrategy = new DefaultNamingStrategy
			{
				ProcessDictionaryKeys = false
			}
		}
	};

	public static async Task<string> ComputeHash(this object obj)
		=> (await ToHashValue(obj)).AsBase64String();

	public static async Task<string> ComputeHashAsHex(this object obj)
		=> (await ToHashValue(obj)).AsHexString();

	public static string ComputeHashSync(this object obj)
		=> ToHashValueSync(obj).AsBase64String();
	public static string ComputeHashAsHexSync(this object obj)
		=> ToHashValueSync(obj).AsHexString();

	private static IHashValue ToHashValueSync(object obj)
	{
		var jsonString = obj.ToJsonUtf8Bytes();
		using var stream = new MemoryStream(jsonString);
		return HashFunction.ComputeHash(stream);
	}

	private static async Task<IHashValue> ToHashValue(object obj)
	{
		var jsonString = obj.ToJsonUtf8Bytes();
		await using var stream = new MemoryStream(jsonString);
		return await HashFunction.ComputeHashAsync(stream);
	}

	/// <summary>
	/// Serializes an object to Json as utf8 bytes.
	/// </summary>
	/// <param name="value">Object to serialize.</param>
	public static byte[] ToJsonUtf8Bytes(this object value)
		=> JsonSerializer.SerializeToUtf8Bytes(value, JsonUtils.JsonBasicSettings);

	/// <summary>
	/// Serializes an object to Json.
	/// </summary>
	/// <param name="value">Object to serialize.</param>
	public static string ToJsonString(this object value)
		=> JsonSerializer.Serialize(value, JsonUtils.JsonBasicSettings);

	// todo: move IsEligible to a separate file
	/// <summary>
	/// Determine whether value is eligible by checking inclusion and exclusion sets.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="value"></param>
	/// <param name="inclusionSet">Collection which are only eligible, when null/empty everything is allowed.</param>
	/// <param name="exclusionSet">Collection which are not eligible.</param>
	/// <param name="comparer"></param>
	/// <returns>Returns true when eligible.</returns>
	public static bool IsEligible<T>(this T value, HashSet<T>? inclusionSet, HashSet<T>? exclusionSet, IEqualityComparer<T>? comparer = null)
	{
		if (!exclusionSet.IsNullOrEmpty() && exclusionSet.Contains(value, comparer))
			return false;

		if (inclusionSet.IsNullOrEmpty())
			return true;

		return inclusionSet.Contains(value, comparer);
	}

	/// <summary>
	/// Determine whether a collection of checks are all eligible.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection">Collection to do checks on. When null/empty and inclusion set is specified it will be false.</param>
	/// <param name="inclusionSet">Collection which are only eligible, when null/empty everything is allowed.</param>
	/// <param name="exclusionSet">Collection which are not eligible.</param>
	/// <param name="comparer"></param>
	public static bool IsEligibleAll<T>(this ICollection<T> collection, HashSet<T> inclusionSet, HashSet<T> exclusionSet, IEqualityComparer<T>? comparer = null)
	{
		if (collection.IsNullOrEmpty() && !inclusionSet.IsNullOrEmpty())
			return false;

		return collection.All(item => item.IsEligible(inclusionSet, exclusionSet, comparer));
	}

	/// <summary>
	/// Determine whether a collection of checks at least one is included and all values are not excluded.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="collection">Collection to do checks on. When null/empty and inclusion set is specified it will be false.</param>
	/// <param name="inclusionSet">Collection which are only eligible, when null/empty everything is allowed.</param>
	/// <param name="exclusionSet">Collection which are not eligible.</param>
	/// <param name="comparer"></param>
	public static bool IsEligibleAny<T>(this ICollection<T> collection, HashSet<T>? inclusionSet = null, HashSet<T>? exclusionSet = null, IEqualityComparer<T>? comparer = null)
	{
		if (collection.IsNullOrEmpty() && !inclusionSet.IsNullOrEmpty())
			return false;

		var isIncluded = false;

		foreach (var item in collection)
		{
			if (exclusionSet?.Contains(item, comparer) == true)
				return false;

			if (inclusionSet?.Contains(item, comparer) == false)
				continue;

			isIncluded = true;
			if (exclusionSet.IsNullOrEmpty())
				break;
		}

		return inclusionSet.IsNullOrEmpty() || isIncluded;
	}

	/// <summary>
	/// Converts a POCO to a <see cref="Dictionary{TKey,TValue}" />" having the property name as key
	/// and property value as value.
	/// </summary>
	public static IDictionary<string, object> ToDictionary(
		this object arguments,
		bool omitNullValues = false,
		Func<string, string>? keyTransform = null
	)
	{
		keyTransform ??= key => key;
		switch (arguments)
		{
			case null:
				return new Dictionary<string, object>();
			case IDictionary<string, object> dictionary:
				return dictionary;
		}

		if (arguments is not IDictionary untypedDictionary)
			return arguments.GetType()
				.GetProperties()
				.Where(p => p.CanRead && p.GetCustomAttribute<IgnoreDataMemberAttribute>() == null)
				.Select(x => (key: x.Name, value: x.GetValue(arguments)))
				.Where(x => !omitNullValues || x.value != null)
				.ToDictionary(p => keyTransform(p.key), p => p.value!);

		var result = (IDictionary<string, object>)new Dictionary<string, object>();

		foreach (var key in untypedDictionary.Keys)
			result.Add(keyTransform(key.ToString()), untypedDictionary[key]);

		return result;
	}

	/// <summary>
	/// Wraps the value in a ValueTask.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="value">Value to wrap.</param>
	/// <returns></returns>
	public static ValueTask<T> AsValueTask<T>(this T value)
		=> new(value);

	/// <summary>
	/// Merge dictionary values into object.
	/// </summary>
	public static T MergeFrom<T>([DisallowNull] this T obj, Dictionary<string, object> values)
	{
		ArgumentNullException.ThrowIfNull(obj);

		var objType = obj.GetType();
		foreach (var (key, value) in values)
		{
			var property = objType.GetProperty(key);
			if (property?.CanWrite == true)
			{
				var currentValue = property.GetValue(obj);
				var targetType = GetEffectiveTargetType(property.PropertyType, currentValue);
				var convertedValue = value.ConvertType(targetType);
				property.SetValue(obj, convertedValue);
			}
		}

		return obj;
	}

	private static Type GetEffectiveTargetType(Type propertyType, object? currentValue)
	{
		// If current value exists and is a reference type that's more specific
		// than the property type, use the more specific type for conversion
		if (currentValue != null &&
			!propertyType.IsValueType &&
			currentValue.GetType() != propertyType &&
			propertyType.IsInstanceOfType(currentValue))
		{
			return currentValue.GetType();
		}

		return propertyType;
	}

	/// <summary>
	/// Merge dictionary values into object immutable (clones new object).
	/// </summary>
	public static T MergeFromAsImmutable<T>([DisallowNull] this T obj, Dictionary<string, object> values)
	{
		ArgumentNullException.ThrowIfNull(obj);

		var clone = obj.DeepClone()!;
		return clone.MergeFrom(values);
	}

	/// <summary>
	/// Deep copies an object.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static T DeepCloneAsJson<T>(this T obj)
		=> JsonConvert.DeserializeObject<T>(
			JsonConvert.SerializeObject(obj, CloneSerializerSettings),
			CloneSerializerSettings
		);

	/// <summary>
	/// Deep copies an object.
	/// </summary>
	public static T DeepClone<T>(this T obj)
		=> Force.DeepCloner.DeepClonerExtensions.DeepClone(obj);

	/// <summary>
	/// Deep copies an object to existing object.
	/// </summary>
	public static T DeepCloneTo<T>(this T obj, T objTo)
		where T : class
		=> Force.DeepCloner.DeepClonerExtensions.DeepCloneTo(obj, objTo);

	/// <summary>
	/// Converts nullable to debug string e.g. show value or return "null" when null.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="nullable"></param>
	public static string ToDebugString<T>(this T? nullable)
		where T : struct
		=> nullable.HasValue ? nullable.Value.ToString()! : "null";

	/// <summary>
	/// Chainable this with no action, small utility to allow empty lambdas e.g.
	/// <code>
	/// dep => dep
	/// .Noop()</code>
	/// </summary>
	/// <returns>Returns this.</returns>
	public static T Noop<T>(this T @this)
		where T : class
		=> @this;

	/// <summary>
	/// Chainable this.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="action"></param>
	/// <returns>Returns this.</returns>
	public static T With<T>(this T @this, Action<T> action)
		where T : class
	{
		ArgumentNullException.ThrowIfNull(action);
		action(@this);
		return @this;
	}

	/// <summary>
	/// Conditional chainable this.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="this"></param>
	/// <param name="condition">Condition to perform.</param>
	/// <param name="trueAction">Action to perform when condition is met.</param>
	/// <param name="falseAction">Action to perform when condition is not met.</param>
	/// <returns>Returns this.</returns>
	/// <exception cref="ArgumentNullException"></exception>
	public static T WithWhen<T>(this T @this, Predicate<T> condition, Action<T> trueAction, Action<T>? falseAction = null)
		where T : class
	{
		ArgumentNullException.ThrowIfNull(condition);
		if (trueAction == null && falseAction == null)
			throw new ArgumentNullException(nameof(trueAction), $"{nameof(WithWhen)} neither true/false action were provided.");

		if (condition(@this))
			trueAction?.Invoke(@this);
		else
			falseAction?.Invoke(@this);
		return @this;
	}

	/// <summary>
	/// Compares the objects and get the delta (values from <paramref name="source"/>).
	/// </summary>
	public static Delta<T> GetDelta<T, TCompare>(this T source, TCompare objCompareWith)
		=> Delta.GetDelta(source, objCompareWith);

	/// <summary>
	/// Deep compare objects (using JsonSerialization).
	/// </summary>
	/// <param name="src">Source object.</param>
	/// <param name="dest">Destination object to compare with.</param>
	public static bool IsEqualDeep(this object? src, object? dest)
		=> (src is not null) && src.ToStringJson() == dest?.ToStringJson();

	/// <summary>
	/// Converts a value to a specified type.
	/// </summary>
	/// <param name="value">Value to convert.</param>
	/// <param name="type">Type to convert to.</param>
	/// <param name="propName">Property name</param>
	/// <returns>Returns value typed.</returns>
	/// <exception cref="InvalidCastException">Thrown when object cannot be converted.</exception>
	public static object? ConvertType(this object? value, Type type, string? propName = null)
	{
		if (value == null)
			return null;

		var cardinalType = type.GetCardinalType();
		var valueType = value.GetType();
		if (cardinalType == valueType || valueType.InheritsFrom(cardinalType))
			return value;
		if (cardinalType == typeof(object))
			return (object)value;
		if (cardinalType == typeof(DateTime))
			return value switch
			{
				int v => v.ToDateTimeFromUnix(),
				long v => v.ToDateTimeFromUnix(),
				double v => v.ToDateTimeFromUnix(),
				string v => DateTime.TryParse(v, out var dt)
					? dt
					: throw
						GetErrorResult()
						.AsApiErrorException(),
				_ => throw new InvalidCastException($"Cannot convert '{value}' to '{cardinalType}'.")
			};
		if (cardinalType == typeof(DateOnly))
			return value switch
			{
				int v => v.ToDateOnlyFromUnix(),
				long v => v.ToDateOnlyFromUnix(),
				double v => v.ToDateOnlyFromUnix(),
				string v => DateOnly.TryParse(v, CultureInfo.InvariantCulture, out var dt)
					? dt
					: throw
						GetErrorResult()
						.AsApiErrorException(),
				DateTime v => v.ToDateOnly(),
				_ => throw new InvalidCastException($"Cannot convert '{value}' to '{cardinalType}'.")
			};
		if (cardinalType.IsEnum)
		{
			if (Enum.TryParse(cardinalType, value.ToString(), true, out var enumValue))
				return enumValue!;
			throw new InvalidCastException($"Cannot convert '{value}' to '{cardinalType}'.");
		}

		if (cardinalType == typeof(TimeSpan) && value is string)
			return value switch
			{
				string v => TimeSpan.TryParse(v, out var ts)
					? ts
					: throw
						GetErrorResult()
							.AsApiErrorException(),
				_ => throw new InvalidCastException($"Cannot convert '{value}' to '{cardinalType}'.")
			};

		if (valueType.IsGenericType && valueType.GetGenericTypeDefinition().InheritsFrom(typeof(IDictionary<,>)))
		{
			var toObjectMethod = typeof(CollectionExtensions).GetMethod(
				nameof(CollectionExtensions.ToObject), [valueType, typeof(bool), typeof(Func<string, string>)]
			);
			Type[] typeArguments = [cardinalType];
			var genericMethod = toObjectMethod!.MakeGenericMethod(typeArguments);
			object?[] parameters = [value, false, null];
			var result = genericMethod.Invoke(null, parameters);

			return result;
		}

		if (cardinalType == typeof(JsonElement))
		{
			if (value is JsonElement alreadyElement)
				return alreadyElement;

			return JsonSerializer.SerializeToElement(value);
		}

		return Convert.ChangeType(value, cardinalType);

		ErrorResult GetErrorResult()
		{
			var errorResult = ErrorResult.AsValidationError();

			if (!propName.IsNullOrEmpty())
				errorResult.AddField(propName, x => x.AsInvalid(value)
					.WithErrorMessage($"Cannot convert '{value}' to '{cardinalType}'.")
				);

			return errorResult;
		}
	}

	/// <summary>
	/// Converts a value to a specified type.
	/// </summary>
	/// <typeparam name="T">Type to convert to.</typeparam>
	/// <param name="value">Value to convert.</param>
	/// <param name="propName">Property name</param>
	/// <returns>Returns value typed.</returns>
	/// <exception cref="InvalidCastException">Thrown when object cannot be converted.</exception>
	public static object? ConvertType<T>(this object? value, string? propName = null)
		=> value.ConvertType(typeof(T), propName);

	public static string? ToStringify(this object? value)
		=> value switch
		{
			null => string.Empty,
			string str => str,
			IEnumerable<string> stringList => $"[{string.Join(", ", stringList)}]",
			IEnumerable<object> objList => $"[{string.Join(", ", objList)}]",
			_ => value.ToString()
		};
}
