using Humanizer;
using Newtonsoft.Json.Linq;
using Odin.Core.Error;
using Odin.Core.Timing;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

[GenerateSerializer]
[DebuggerDisplay(OdinDiagnostics.DebuggerDisplayStringFormat)]
public class Delta : IDictionary<string, object?>
{
	protected string DebuggerDisplay => $"{Data.ToDebugString()}";

	protected static readonly Regex JsonPathRegex = new("[^a-zA-Z]+", RegexOptions.Compiled);

	[Id(0)]
	protected Dictionary<string, object?> Data = new();

	public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
		=> Data.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public void Add(KeyValuePair<string, object?> item)
		=> Data.Add(item.Key.Pascalize(), item.Value);

	public void Clear()
		=> Data.Clear();

	public bool Contains(KeyValuePair<string, object?> item)
		=> Data.Contains(item);

	public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
		=> ((IDictionary<string, object?>)Data).CopyTo(array, arrayIndex);

	public bool Remove(KeyValuePair<string, object?> item)
		=> Data.Remove(item.Key);

	public int Count => Data.Count;

	public bool IsReadOnly => false;

	public void Add(string key, object? value)
		=> Data.Add(key.Pascalize(), value);

	public bool ContainsKey(string key)
		=> Data.ContainsKey(key);

	public bool Remove(string key)
		=> Data.Remove(key);

	public bool TryGetValue(string key, out object? value)
		=> Data.TryGetValue(key, out value);

	public object? this[string key]
	{
		get => Data[key];
		set => Data[key.Pascalize()] = Set(value);
	}

	public ICollection<string> Keys => Data.Keys;
	public ICollection<object?> Values => Data.Values;

	private static object? Set(object? value)
		=> value is JObject jObject ? ParseJObject(jObject) : value;

	private static object? ParseJObject(JToken jToken)
	{
		switch (jToken.Type)
		{
			case JTokenType.Object:
				var jObject = jToken.ToObject<Dictionary<string, object>>();
				return jObject?.ToDictionary(item => item.Key.Pascalize(), item => item.Value is JToken token ? ParseJObject(token) : item.Value);

			case JTokenType.Array:
				return jToken.Select(ParseJObject).ToList();

			case JTokenType.Integer:
			case JTokenType.Float:
			case JTokenType.String:
			case JTokenType.Boolean:
			case JTokenType.Null:
			case JTokenType.Undefined:
			default:
				return jToken.Value<string>();

			case JTokenType.None:
			case JTokenType.Constructor:
			case JTokenType.Property:
			case JTokenType.Comment:
			case JTokenType.Date:
			case JTokenType.Raw:
			case JTokenType.Bytes:
			case JTokenType.Guid:
			case JTokenType.Uri:
			case JTokenType.TimeSpan:
				break;
		}
		return null;
	}

	/// <summary>
	/// Compares the <paramref name="source"/> and <paramref name="objCompareWith"/> objects and creates a delta object with the
	/// properties that are different from the <paramref name="source"/>.
	/// NOTES:
	/// - When types do not match, and the property is not found in the <paramref name="objCompareWith"/> object, it will be added to the delta.
	/// - Types are derived from Generic and not from the object itself, this is done to omit properties when down casting.
	/// </summary>
	/// <param name="source">Object to compare and take value from.</param>
	/// <param name="objCompareWith">Object to compare with.</param>
	public static Delta<T> GetDelta<T, TCompare>(T source, TCompare objCompareWith)
	{
		var delta = new Delta<T>();

		var sourceType = typeof(T);
		var compareType = typeof(TCompare);
		var sourceProps = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		if (sourceType == compareType)
		{
			foreach (var prop in sourceProps)
			{
				var sourceValue = prop.GetValue(source);
				var slaveValue = prop.GetValue(objCompareWith);
				if (!Equals(sourceValue, slaveValue))
					delta[prop.Name] = sourceValue;
			}
		}
		else
		{
			var compareProps = compareType
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.ToDictionary(x => x.Name);
			foreach (var prop in sourceProps)
			{
				var sourceValue = prop.GetValue(source);
				if (!compareProps.TryGetValue(prop.Name, out var compareProp))
				{
					delta[prop.Name] = sourceValue;
					continue;
				}
				var slaveValue = compareProp.GetValue(objCompareWith);
				if (!Equals(sourceValue, slaveValue))
					delta[prop.Name] = sourceValue;
			}
		}

		return delta;
	}
}

[GenerateSerializer]
[JsonConverter(typeof(DeltaJsonConverterFactory))]
public class Delta<T> : Delta
{
	public Delta()
	{
	}

	public Delta(Dictionary<string, object?> source)
	{
		Data = source;
	}

	public Delta(Delta<T> delta) : this(new Dictionary<string, object?>(delta.ToDictionary()))
	{
	}

	public Dictionary<string, object?> ToDictionary()
		=> Data;

	public new IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		=> Data.GetEnumerator();

	// todo: [RTMW-2045] handle nesting
	public Delta<T> Set<TProp>(Expression<Func<T, TProp>> property, TProp value)
	{
		var propertyName = property.GetPropertyName();
		Data[propertyName] = value;
		return this;
	}

	public Delta<T> Set(IDictionary<string, object?> data)
	{
		Data.AddRangeOverride(data);
		return this;
	}

	public Delta<T> Remove<TProp>(Expression<Func<T, TProp>> property)
	{
		property.NamesOf().ToList().ForEach(propName => Data.Remove(propName));
		return this;
	}

	public bool Contains<TProp>(Expression<Func<T, TProp>> property)
	{
		var propertyName = property.GetPropertyName();
		return Data.ContainsKey(propertyName);
	}

	public bool TryGet<TProp>(Expression<Func<T, TProp>> propExpr, out TProp? value)
	{
		if (!Contains(propExpr))
		{
			value = default;
			return false;
		}

		value = Get(propExpr);
		return true;
	}

	/// <summary>
	/// Gets a deep-cloned instance of the data as type T by serializing and deserializing the current value.
	/// </summary>
	/// <remarks>This method creates a deep copy of the data by serializing it to JSON and then deserializing it
	/// back to type T. This approach may not preserve object references or types that are not supported by the configured
	/// JSON serializer options.</remarks>
	/// <returns>An instance of type T representing a deep copy of the current data, or null if the data cannot be deserialized.</returns>
	public T? Get() => Get<T>();

	/// <summary>
	/// Deserializes the current data to an instance of the specified derived type.
	/// </summary>
	/// <remarks>This method serializes the current data and then deserializes it to the requested type. Use this
	/// method when you need to obtain the data as a specific derived type. The operation may fail and return null if the
	/// data does not match the structure of TDerived.</remarks>
	/// <typeparam name="TDerived">The type to which the data is deserialized. Must be compatible with the structure of the current data.</typeparam>
	/// <returns>An instance of type TDerived containing the deserialized data, or null if the data cannot be deserialized to the
	/// specified type.</returns>
	public TDerived? Get<TDerived>()
	{
		var serialized = JsonSerializer.Serialize(Data, DeltaExtensions.Options);
		return JsonSerializer.Deserialize<TDerived>(serialized, DeltaExtensions.Options);
	}

	public TProp? Get<TProp>(Expression<Func<T, TProp>> propExpr)
	{
		try
		{
			var propertyName = propExpr.GetPropertyName();

			if (!Data.TryGetValue(propertyName, out var result))
				return default;
			string serialized;
			switch (result)
			{
				case null:
					return default;
				case TProp prop:
					return prop;
				case List<object> items:
					if (!items.Any())
						return default;

					var itemType = typeof(TProp).GetGenericArguments()[0];
					var currentType = items[0].GetType();
					if (currentType == itemType)
						return items.CastToList<TProp>(itemType);

					serialized = JsonSerializer.Serialize(items);
					return JsonSerializer.Deserialize<TProp>(serialized, DeltaExtensions.Options);
				case Dictionary<string, object> items:
					if (!items.Any())
						return default;

					serialized = JsonSerializer.Serialize(items);
					return JsonSerializer.Deserialize<TProp>(serialized, DeltaExtensions.Options);
				case JsonElement element:
					result = element.ValueKind switch
					{
						JsonValueKind.Array => element.Deserialize<TProp>(DeltaExtensions.Options),
						JsonValueKind.Object => element.Deserialize<TProp>(DeltaExtensions.Options),
						JsonValueKind.Number => element.Deserialize<TProp>(DeltaExtensions.Options),
						_ => element.ParseJsonElement()
					};
					// todo: consider updating property with the result so any other calls won't need to parse it again
					//this[propertyName] = result;
					return (TProp?)result;
				case Delta delta:
					return JsonSerializer.Deserialize<TProp>(JsonSerializer.Serialize(delta.ToDictionary()), DeltaExtensions.Options);
				default:
					var propertyType = typeof(TProp);
					if (propertyType.IsEnum
						|| (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && propertyType.GenericTypeArguments[0].IsEnum))
					{
						var enumType = propertyType.IsEnum ? propertyType : propertyType.GenericTypeArguments[0];
						if (!Enum.TryParse(enumType, result.ToString(), ignoreCase: true, out var enumResult))
							throw ErrorResult.AsValidationError()
								.AddField(propertyName, x => x.AsInvalid(result, propertyType))
								.AsApiErrorException();
						return (TProp)enumResult;
					}

					var cardinalType = propertyType.GetCardinalType();

					if (cardinalType == typeof(TimeSpan) && typeof(T).GetProperty(propertyName)?.ShouldConvertToTimeSpan() is true)
						result = result.ToString()?.ToTimeSpanFromDuration();

					return (TProp?)result.ConvertType<TProp>(propertyName);
			}
		}
		catch (JsonException ex)
		{
			var path = JsonPathRegex.Replace(ex.Path!, "");

			throw new ErrorResult()
				.WithValidationFailure(OdinErrorCodes.InvalidRequest)
				.AddData(path, ex.Message)
				.AsApiErrorException();
		}
	}
}

public class ObjectToInferredTypesConverter : JsonConverter<object>
{
	public override object Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.True => true,
			JsonTokenType.False => false,
			JsonTokenType.Number when reader.TryGetInt32(out var i) => (int?)i,
			JsonTokenType.Number when reader.TryGetInt64(out var l) => (long?)l,
			JsonTokenType.Number when reader.TryGetDecimal(out var d) => (decimal?)d,
			JsonTokenType.String when reader.TryGetDateTime(out var datetime) => (DateTime?)datetime,
			JsonTokenType.String => reader.GetString()!,
			_ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
		};

	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		var defaultOptions = new JsonSerializerOptions(options);
		defaultOptions.Converters.Remove(this);
		JsonSerializer.Serialize(writer, value, defaultOptions);
	}
}
