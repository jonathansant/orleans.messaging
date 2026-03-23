using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable once CheckNamespace
namespace Odin.Core;

public static class ConfigurationExtensions
{
	/// <summary>
	/// Converts the <paramref name="configuration"/> value to a <see cref="JObject"/>.
	/// </summary>
	/// <param name="configuration"></param>
	/// <returns></returns>
	public static JObject AsJObject(this IConfiguration configuration)
		=> PopulateJObject(configuration);

	private static JObject PopulateJObject(IConfiguration configuration)
	{
		var jObject = new JObject();

		foreach (var section in configuration.GetChildren())
		{
			if (section.GetChildren().Any())
			{
				jObject.Add(section.Key, PopulateJObject(section));
			}
			else
			{
				// todo: handle arrays as JArray
				jObject.Add(section.Key, new JValue(section.Value));
			}
		}

		return jObject;
	}

	private static readonly Lazy<MethodInfo> GetValueAsDeltaMethod = new(() => typeof(ConfigurationExtensions).GetCachedMethod(nameof(GetValueAsDelta)));
	private static readonly Dictionary<string, Type> TypeCache = new() { { "list", typeof(List<>) }, { "hashset", typeof(HashSet<>) }, { nameof(Nullable), typeof(Nullable<>) }, { nameof(IDictionary), typeof(IDictionary) }, { nameof(Delta), typeof(Delta) }, { nameof(IEnumerable), typeof(IEnumerable) }, {nameof(JsonElement), typeof(JsonElement)} };
	public static Delta<T> GetValueAsDelta<T>(this IConfiguration configuration)
	{
		var type = typeof(T);
		var delta = new Delta<T>();

		if (type.IsGenericType && (type.GetGenericTypeDefinition() == TypeCache["list"] || type.GetGenericTypeDefinition() == TypeCache["hashset"]))
			type = type.GetGenericArguments()[0];

		var properties = type.GetProperties().ToDictionary(x => x.Name.ToLower(), x => x.PropertyType);

		var nullableType = TypeCache[nameof(Nullable)];
		var deltaType = TypeCache[nameof(Delta)];
		var jsonElementType = TypeCache[nameof(JsonElement)];
		var dictionaryType = TypeCache[nameof(IDictionary)];
		var iEnumerableType = TypeCache[nameof(IEnumerable)];
		foreach (var kvp in configuration.GetChildren())
		{
			var key = kvp.Key;

			if (kvp.Value is { } val)
			{
				// skip empty values
				if (val.IsNullOrEmpty())
					continue;

				if (properties.TryGetValue(key.ToLower(), out var propertyType))
				{
					if (propertyType.IsEnum
						|| (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == nullableType && propertyType.GenericTypeArguments[0].IsEnum))
					{
						var enumType = propertyType.IsEnum ? propertyType : propertyType.GenericTypeArguments[0];
						if (Enum.TryParse(enumType, val, ignoreCase: true, out var enumResult))
							delta[key] = enumResult;
						else
							throw new InvalidCastException($"Invalid enum value '{val}' for '{propertyType}'");
					}
					else if (propertyType == jsonElementType)
						// Convert.ChangeType does not support JsonElement; parse the raw string as JSON.
						delta[key] = ParseJsonElementFromConfigValue(val);
					else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == nullableType && propertyType.GenericTypeArguments[0] == jsonElementType)
						// Same handling for Nullable<JsonElement>; the value may be a plain scalar (string, number, bool).
						delta[key] = ParseJsonElementFromConfigValue(val);
					else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == nullableType)
						delta[key] = Convert.ChangeType(val, propertyType.GenericTypeArguments[0]);
					else
						delta[key] = Convert.ChangeType(val, propertyType);
				}
				else
					delta[key] = val;

				continue;
			}

			if (!kvp.GetChildren().Any())
				throw new ArgumentOutOfRangeException(nameof(kvp));

			if (properties.TryGetValue(key.ToLower(), out var baseType))
			{
				var propertyType = baseType;
				if (baseType.IsGenericType && deltaType.IsAssignableFrom(baseType))
					propertyType = baseType.GetGenericArguments()[0];

				if (propertyType.IsJsonDerivedType())
				{
					var derivedKey = propertyType.GetCustomAttribute<JsonPolymorphicAttribute>()?.TypeDiscriminatorPropertyName ?? "$type";
					var derivedType = kvp.GetSection(derivedKey).Value;
					propertyType = propertyType.GetTypeByDiscriminator(derivedType);
				}

				var method = GetValueAsDeltaMethod.Value?.MakeGenericMethod(propertyType);
				var nestedDelta = method?.Invoke(kvp, [kvp]);

				if (baseType.IsGenericType && !dictionaryType.IsAssignableFrom(baseType)
										   && !deltaType.IsAssignableFrom(baseType) && iEnumerableType.IsAssignableFrom(baseType))
				{
					var list = ((Core.Delta)nestedDelta).Select(x => x.Value).ToList();
					delta[key] = list;
				}
				else
					delta[key] = nestedDelta;
			}
			else
			{
				var propertyType = type;
				if (type.IsGenericType && deltaType.IsAssignableFrom(type))
					propertyType = type.GetGenericArguments()[0];

				if (propertyType.IsJsonDerivedType())
				{
					var derivedKey = propertyType.GetCustomAttribute<JsonPolymorphicAttribute>()?.TypeDiscriminatorPropertyName ?? "$type";
					var derivedType = kvp.GetSection(derivedKey).Value;
					propertyType = propertyType.GetTypeByDiscriminator(derivedType);
				}

				var isDictionary = false;
				if (propertyType.IsGenericType && dictionaryType.IsAssignableFrom(propertyType))
				{
					propertyType = propertyType.GetGenericArguments()[1];
					key = kvp.Key;
					isDictionary = true;
				}

				var method = GetValueAsDeltaMethod.Value?.MakeGenericMethod(propertyType);
				var nestedDelta = method?.Invoke(kvp, [kvp]);

				if (isDictionary && propertyType.IsGenericType && !deltaType.IsAssignableFrom(propertyType)
					&& !dictionaryType.IsAssignableFrom(propertyType) && iEnumerableType.IsAssignableFrom(propertyType))
				{
					var list = ((Core.Delta)nestedDelta).Select(x => x.Value).ToList();
					delta[key] = list;
				}
				else
					delta[key] = nestedDelta;
			}
		}
		return delta;
	}

	/// <summary>
	/// Parses a configuration string value into a <see cref="JsonElement"/>.
	/// Numbers and booleans are valid JSON and parsed directly.
	/// Plain strings (e.g. "hello") are not valid JSON on their own, so they are
	/// serialized as a JSON string literal before parsing.
	/// </summary>
	private static JsonElement ParseJsonElementFromConfigValue(string val)
	{
		try
		{
			return JsonDocument.Parse(val).RootElement.Clone();
		}
		catch (JsonException)
		{
			// The value is a plain string (not valid JSON); wrap it as a JSON string literal.
			return JsonDocument.Parse(JsonSerializer.Serialize(val)).RootElement.Clone();
		}
	}
}
