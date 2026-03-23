using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

/// <summary>
/// A JSON converter factory for <see cref="Delta{T}"/> that properly handles serialization and deserialization
/// based on the generic type T's property types. This ensures that polymorphic types (with JsonDerivedType attributes)
/// and nested Delta objects are serialized with their proper type discriminators.
/// </summary>
public class DeltaJsonConverterFactory : JsonConverterFactory
{
	private static readonly ConcurrentDictionary<Type, JsonConverter> ConverterCache = new();

	/// <inheritdoc />
	public override bool CanConvert(Type typeToConvert)
		=> typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Delta<>);

	/// <inheritdoc />
	public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
		=> ConverterCache.GetOrAdd(typeToConvert, static type =>
		{
			var genericArgument = type.GetGenericArguments()[0];
			var converterType = typeof(DeltaJsonConverter<>).MakeGenericType(genericArgument);
			return (JsonConverter)Activator.CreateInstance(converterType)!;
		});

	/// <summary>
	/// Gets or creates a converter for the specified inner type.
	/// Used internally to avoid repeated reflection when serializing nested Delta types.
	/// </summary>
	internal static JsonConverter GetOrCreateConverter(Type innerType)
	{
		var deltaType = typeof(Delta<>).MakeGenericType(innerType);
		return ConverterCache.GetOrAdd(deltaType, static type =>
		{
			var genericArgument = type.GetGenericArguments()[0];
			var converterType = typeof(DeltaJsonConverter<>).MakeGenericType(genericArgument);
			return (JsonConverter)Activator.CreateInstance(converterType)!;
		});
	}
}

/// <summary>
/// A JSON converter for <see cref="Delta{T}"/> that serializes properties based on the type information
/// from T, ensuring proper handling of polymorphic types and nested Delta objects.
/// </summary>
/// <typeparam name="T">The type that defines the shape of the Delta.</typeparam>
public class DeltaJsonConverter<T> : JsonConverter<Delta<T>>
{
	private static readonly Dictionary<string, PropertyInfo> PropertyCache;
	private static readonly Dictionary<string, PropertyInfo> AllPropertyCache;
	private static readonly bool IsPolymorphic;
	private static readonly string TypeDiscriminatorPropertyName;
	private static readonly Dictionary<Type, string> DerivedTypeDiscriminators;
	private static readonly Dictionary<string, Type> DiscriminatorToType;
	private static readonly Dictionary<Type, HashSet<string>> DerivedTypeDeclaredProperties;
	private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> DerivedTypePropertyCache;
	private static readonly ConcurrentDictionary<JsonSerializerOptions, JsonSerializerOptions> CaseInsensitiveOptionsCache = [];
	private static readonly ConcurrentDictionary<Type, byte> PolymorphicTypeCache = [];

	static DeltaJsonConverter()
	{
		var baseType = typeof(T);

		// Cache base type properties
		PropertyCache = baseType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

		// Check if T is polymorphic
		var polymorphicAttr = baseType.GetCustomAttribute<JsonPolymorphicAttribute>();
		IsPolymorphic = polymorphicAttr is not null;
		TypeDiscriminatorPropertyName = polymorphicAttr?.TypeDiscriminatorPropertyName ?? "$type";

		// Build derived type mappings
		DerivedTypeDiscriminators = [];
		DiscriminatorToType = new(StringComparer.OrdinalIgnoreCase);
		DerivedTypeDeclaredProperties = [];
		DerivedTypePropertyCache = [];

		var derivedTypeAttrs = baseType.GetCustomAttributes<JsonDerivedTypeAttribute>().ToList();

		foreach (var attr in derivedTypeAttrs)
		{
			if (attr.TypeDiscriminator is string discriminator)
			{
				DerivedTypeDiscriminators[attr.DerivedType] = discriminator;
				DiscriminatorToType[discriminator] = attr.DerivedType;
			}

			// Cache declared-only properties for derived type detection during serialization
			DerivedTypeDeclaredProperties[attr.DerivedType] = attr.DerivedType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Select(p => p.Name)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			// Cache all properties for the derived type
			DerivedTypePropertyCache[attr.DerivedType] = attr.DerivedType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);
		}

		// Build unified property cache including all derived type properties
		AllPropertyCache = new(PropertyCache, StringComparer.OrdinalIgnoreCase);
		foreach (var derivedProps in DerivedTypePropertyCache.Values)
			foreach (var (name, prop) in derivedProps)
				AllPropertyCache.TryAdd(name, prop);

		// Pre-cache polymorphic status for property types we know about
		foreach (var prop in AllPropertyCache.Values)
			if (HasPolymorphicAttribute(prop.PropertyType))
				PolymorphicTypeCache.TryAdd(prop.PropertyType, 0);
	}

	/// <inheritdoc />
	public override Delta<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException($"Expected StartObject token but got {reader.TokenType}");

		var delta = new Delta<T>();
		string? discriminatorValue = null;
		var properties = new List<(string Name, JsonElement Element)>();

		using var doc = JsonDocument.ParseValue(ref reader);
		foreach (var prop in doc.RootElement.EnumerateObject())
		{
			if (IsPolymorphic && prop.Name.Equals(TypeDiscriminatorPropertyName, StringComparison.OrdinalIgnoreCase))
			{
				discriminatorValue = prop.Value.GetString();
				delta[TypeDiscriminatorPropertyName] = discriminatorValue;
				continue;
			}

			properties.Add((prop.Name, prop.Value.Clone()));
		}

		// Determine the target property cache based on discriminator
		var targetProps = PropertyCache;
		if (discriminatorValue is not null && DiscriminatorToType.TryGetValue(discriminatorValue, out var derivedType))
			targetProps = DerivedTypePropertyCache.GetValueOrDefault(derivedType, PropertyCache);

		// Get or create case-insensitive options for deserialization
		var deserializeOptions = GetCaseInsensitiveOptions(options);

		// Deserialize properties
		foreach (var (name, element) in properties)
			if (targetProps.TryGetValue(name, out var propInfo) || AllPropertyCache.TryGetValue(name, out propInfo))
				delta[propInfo.Name] = DeserializeElement(element, propInfo.PropertyType, deserializeOptions);
			else
				delta[name] = DeserializeElement(element, typeof(object), deserializeOptions);

		return delta;
	}

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, Delta<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();

		// For polymorphic types, try to determine and write the discriminator
		Type? derivedType = null;
		if (IsPolymorphic && value.Count > 0)
		{
			derivedType = DetectDerivedType(value);
			if (derivedType is not null && DerivedTypeDiscriminators.TryGetValue(derivedType, out var discriminator))
			{
				var discriminatorName = options.PropertyNamingPolicy?.ConvertName(TypeDiscriminatorPropertyName) ?? TypeDiscriminatorPropertyName;
				writer.WriteString(discriminatorName, discriminator);
			}
		}

		// Get the appropriate property cache for lookups
		var targetProps = derivedType is not null && DerivedTypePropertyCache.TryGetValue(derivedType, out var derivedProps)
			? derivedProps
			: PropertyCache;

		foreach (var (key, propValue) in value)
		{
			var propertyName = options.PropertyNamingPolicy?.ConvertName(key) ?? key;
			writer.WritePropertyName(propertyName);

			if (propValue is null)
			{
				writer.WriteNullValue();
				continue;
			}

			// Look up property type for proper serialization
			if (targetProps.TryGetValue(key, out var propertyInfo) || AllPropertyCache.TryGetValue(key, out propertyInfo))
				SerializeValue(writer, propValue, propertyInfo.PropertyType, options);
			else
				JsonSerializer.Serialize(writer, propValue, propValue.GetType(), options);
		}

		writer.WriteEndObject();
	}

	private static Type? DetectDerivedType(Delta<T> value)
	{
		foreach (var (type, declaredProps) in DerivedTypeDeclaredProperties)
			if (value.Keys.Any(declaredProps.Contains))
				return type;

		return null;
	}

	private static void SerializeValue(Utf8JsonWriter writer, object value, Type declaredType, JsonSerializerOptions options)
	{
		var valueType = value.GetType();

		// Handle Delta types - prefer declared type for polymorphic info, fall back to actual type
		if (IsDeltaType(declaredType, out var innerType))
		{
			WriteDeltaValue(writer, value, innerType, options);
			return;
		}

		if (IsDeltaType(valueType, out var actualInnerType))
		{
			WriteDeltaValue(writer, value, actualInnerType, options);
			return;
		}

		// For polymorphic types, System.Text.Json handles discriminators automatically
		// when we serialize using the declared base type
		if (IsTypePolymorphic(declaredType))
		{
			JsonSerializer.Serialize(writer, value, declaredType, options);
			return;
		}

		// Standard serialization - prefer declared type if compatible
		var serializeType = declaredType.IsAssignableFrom(valueType) ? declaredType : valueType;
		JsonSerializer.Serialize(writer, value, serializeType, options);
	}

	private static void WriteDeltaValue(Utf8JsonWriter writer, object value, Type innerType, JsonSerializerOptions options)
	{
		// Use cached converter to avoid reflection on every call
		var converter = DeltaJsonConverterFactory.GetOrCreateConverter(innerType);
		var writeMethod = converter.GetType().GetMethod(nameof(Write), BindingFlags.Public | BindingFlags.Instance)!;
		writeMethod.Invoke(converter, [writer, value, options]);
	}

	private static object? DeserializeElement(JsonElement element, Type propertyType, JsonSerializerOptions options)
	{
		if (element.ValueKind == JsonValueKind.Null)
			return null;

		if (IsDeltaType(propertyType, out var innerType))
		{
			var deltaType = typeof(Delta<>).MakeGenericType(innerType);
			return JsonSerializer.Deserialize(element.GetRawText(), deltaType, options);
		}

		return JsonSerializer.Deserialize(element.GetRawText(), propertyType, options);
	}

	private static JsonSerializerOptions GetCaseInsensitiveOptions(JsonSerializerOptions options)
		=> CaseInsensitiveOptionsCache.GetOrAdd(options, static opts => new(opts)
		{
			PropertyNameCaseInsensitive = true
		});

	private static bool IsDeltaType(Type type, out Type innerType)
	{
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Delta<>))
		{
			innerType = type.GetGenericArguments()[0];
			return true;
		}

		innerType = null!;
		return false;
	}

	private static bool IsTypePolymorphic(Type type)
	{
		// Check pre-cached types first
		if (PolymorphicTypeCache.ContainsKey(type))
			return true;

		// Fall back to reflection for unknown types (and cache the result)
		if (HasPolymorphicAttribute(type))
		{
			PolymorphicTypeCache.TryAdd(type, 0);
			return true;
		}

		return false;
	}

	private static bool HasPolymorphicAttribute(Type type)
		=> type.GetCustomAttribute<JsonPolymorphicAttribute>() is not null
		   || type.GetCustomAttributes<JsonDerivedTypeAttribute>().Any();
}
