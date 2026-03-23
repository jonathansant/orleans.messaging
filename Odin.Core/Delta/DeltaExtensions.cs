using Humanizer;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

public static class DeltaExtensions
{
	public static Func<JsonSerializerOptions> OptionsProvider { get; set; } = () => DefaultOptions;
	public static JsonSerializerOptions Options => OptionsProvider();
	private static readonly JsonSerializerOptions DefaultOptions = new()
	{
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		PropertyNameCaseInsensitive = true,
		Converters = { new ObjectToInferredTypesConverter(), new DeltaJsonConverterFactory() }
	};

	internal static string GetPropertyName<T, T2>(this Expression<Func<T, T2>> exp)
	{
		var member = exp.Body as MemberExpression;
		var unary = exp.Body as UnaryExpression;

		return ((member ?? unary?.Operand as MemberExpression)?.Member as PropertyInfo)?.Name!;
	}

	public static Delta<T> AsDelta<T>(this T sourceModel)
	{
		ArgumentNullException.ThrowIfNull(sourceModel);
		var sourceDictionary = sourceModel.ToDictionary(omitNullValues: true);
		var deltaDictionary = sourceDictionary.ParseJObjectProps(typeof(T));
		return new(deltaDictionary);
	}

	/// <summary>
	/// Converts a derived type instance to a Delta of the base type.
	/// This is useful for polymorphic scenarios where you need to assign
	/// a derived type to a Delta of its base type, but preserving the derived type's properties.
	/// </summary>
	/// <example>
	/// Delta&lt;CharacterBase&gt; = new TankCharacter { ... }.AsDelta&lt;CharacterBase, TankCharacter&gt;();
	/// </example>
	public static Delta<TBase> AsDelta<TBase, TDerived>(this TDerived sourceModel)
		where TDerived : TBase
	{
		ArgumentNullException.ThrowIfNull(sourceModel);
		var sourceDictionary = sourceModel.ToDictionary(omitNullValues: true);
		var deltaDictionary = sourceDictionary.ParseJObjectProps(typeof(TDerived));
		return new(deltaDictionary);
	}

	/// <summary>
	/// Merge Delta values into object.
	/// </summary>
	public static T MergeFrom<T>([DisallowNull] this T obj, Delta<T> delta)
	{
		ArgumentNullException.ThrowIfNull(obj);

		return delta.Count == 0
			? obj
			: obj.MergeFrom(delta.ToDictionary());
	}

	/// <summary>
	/// Merge Delta values into object immutable (clones new object).
	/// </summary>
	public static T MergeFromAsImmutable<T>([DisallowNull] this T obj, Delta<T> delta)
	{
		ArgumentNullException.ThrowIfNull(obj);
		return delta.Count == 0
			? obj.DeepClone()
			: obj.MergeFromAsImmutable(delta.ToDictionary());
	}

	/// <summary>
	/// Merge Delta objects.
	/// </summary>
	public static Delta<T> MergeFrom<T, TDest>(this Delta<T> dest, Delta<TDest> source)
	{
		ArgumentNullException.ThrowIfNull(dest);
		ArgumentNullException.ThrowIfNull(source);

		return source.CopyTo(dest);
	}

	/// <summary>
	/// Merge Delta objects immutable (clones new object).
	/// </summary>
	public static Delta<T> MergeFromAsImmutable<T, TDest>(this Delta<T> dest, Delta<TDest> source)
	{
		ArgumentNullException.ThrowIfNull(dest);
		ArgumentNullException.ThrowIfNull(source);

		var result = new Delta<T>(dest);
		return source.CopyTo(result);
	}

	/// <summary>
	/// Creates a new copy of the delta.
	/// </summary>
	public static Delta<T> Clone<T>(this Delta<T> delta)
	{
		ArgumentNullException.ThrowIfNull(delta);
		return new(delta);
	}

	// TODO: custom parsers can be probably removed since its now handled by the JsonConverter
	public static Dictionary<string, object> ParseJObjectProps(
		this IDictionary<string, object> dict,
		Type parentType,
		Dictionary<Type, Dictionary<string, Type>>? derivedChildTypes = null
	)
	{
		// Type / generic type of item
		var itemType = parentType.GetItemType();

		// Create dictionary to be used as Delta
		var modelDictionary = new Dictionary<string, object>();

		foreach (var (key, value) in dict)
		{
			if (!IsPropertyValid(key.Pascalize(), itemType))
				continue;

			if (value is JsonElement element)
				switch (element.ValueKind)
				{
					case JsonValueKind.Undefined:
					case JsonValueKind.Object:
						var itemKey = itemType.IsPrimitive() ? key : key.Pascalize();
						modelDictionary = ParseObject(modelDictionary, itemKey, itemType, value, parentType, derivedChildTypes);
						break;
					case JsonValueKind.Array:
						var arrayKey = itemType.IsPrimitive() ? key : key.Pascalize();
						modelDictionary = ParseArray(modelDictionary, arrayKey, itemType, element);
						break;
					default:
						modelDictionary[key.Pascalize()] = element.ParseJsonElement();
						break;
				}
			else
				modelDictionary[key.Pascalize()] = value;
		}

		return modelDictionary;
	}

	public static Delta<TDest> CopyTo<TSource, TDest>(this Delta<TSource> source, Delta<TDest>? dest)
	{
		dest ??= [];
		var destType = typeof(TDest);
		var isSameType = typeof(TSource) == destType;

		foreach (var (key, value) in source)
		{
			if (isSameType || destType.HasProperty(key))
				dest[key] = value;
		}

		return dest;
	}

	public static Delta<T> RemoveMany<T>(this Delta<T> source, HashSet<Expression<Func<T, object>>> properties)
	{
		foreach (var prop in properties)
			source.Remove(prop);

		return source;
	}

	public static bool Any<T>(this Delta<T> source, HashSet<Expression<Func<T, object>>> properties)
		=> properties.Any(source.Contains);

	private static Type GetItemType(this Type parentType)
	{
		if (parentType.BaseType == typeof(Delta) || parentType.IsGenericType && parentType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			return parentType.GetGenericArguments()[0];

		return parentType;
	}

	private static bool IsPropertyValid(string key, Type itemType)
		=> itemType.IsPrimitive() || itemType.HasProperty(key)
								  || (itemType.IsGenericType && itemType.GetGenericArguments()[0].HasProperty(key));

	public static object? ParseJsonElement(this JsonElement element)
	{
		var result = element.ValueKind switch
		{
			JsonValueKind.Object => element.Deserialize<object>(Options),
			JsonValueKind.String => element.TryGetDateTime(out var dateTime) ? dateTime : element.GetString()?.Trim(),
			JsonValueKind.Number => element.Deserialize<object>(Options),
			JsonValueKind.True => element.GetBoolean(),
			JsonValueKind.False => element.GetBoolean(),
			JsonValueKind.Null => null,
			_ => null
		};
		return result;
	}

	private static Dictionary<string, object> ParseObject(
			Dictionary<string, object> dict,
			string key,
			Type itemType,
			object value,
			Type parentType,
			Dictionary<Type, Dictionary<string, Type>>? derivedChildTypes = null
		)
	{
		var innerType = itemType.IsPrimitive() ? itemType : itemType.GetProperty(key)?.PropertyType;
		var isDictionary = parentType.IsGenericType && parentType.GetGenericTypeDefinition() == typeof(Dictionary<,>);
		innerType = isDictionary ? parentType.GetGenericArguments()[1] : innerType;

		// Handle polymorphic types (abstract classes or interfaces)
		if (innerType != null && derivedChildTypes != null && derivedChildTypes.TryGetValue(innerType, out var derivedChildren))
		{
			dict[key] = new Dictionary<string, object>();
			var jsonElement = (JsonElement)value;
			var tempDict = jsonElement.Deserialize<Dictionary<string, object>>(Options);

			if (tempDict != null && derivedChildren != null)
			{
				// Find all derived types of the abstract type
				if (derivedChildren.Any() && tempDict.TryGetValue("type", out var typeDiscriminator)
										  && derivedChildren.ContainsKey(typeDiscriminator.ToString() ?? string.Empty))
				{
					var derivedType = derivedChildren[typeDiscriminator.ToString()!];
					innerType = derivedType;
				}
			}

			dict[key] = tempDict.ParseJObjectProps(innerType, derivedChildTypes);
			return dict;
		}

		var x = ((JsonElement)value).Deserialize<Dictionary<string, object>>(Options);
		dict[key] = x.ParseJObjectProps(innerType);
		return dict;
	}

	private static Dictionary<string, object> ParseArray(Dictionary<string, object> dict, string key, Type type, JsonElement element)
	{
		var items = new List<object>();
		dict[key] = items;
		var array = element.Deserialize<List<JsonElement>>(Options);

		foreach (var item in array!)
		{
			if (item.ValueKind is JsonValueKind.Object)
			{
				var parsedDict = item.Deserialize<Dictionary<string, object>>(Options)!;
				var result = new Dictionary<string, object>();
				foreach (var (prop, value) in parsedDict)
				{
					var propValueType = value?.GetType();
					if (propValueType == null || propValueType.IsPrimitive())
						result.Add(prop.Pascalize(), value);
					else
						result = ParseObject(parsedDict, prop.Pascalize(), propValueType, value, propValueType, null);
				}
				items.Add(result);
			}
			else
			{
				var result = item.ParseJsonElement();
				items.Add(result);
			}
		}
		return dict;
	}
}
